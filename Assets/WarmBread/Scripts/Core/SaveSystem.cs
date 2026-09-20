using System;
using System.IO;
using UnityEngine;

namespace WarmBread
{
    public static class SaveSystem
    {
        private const int MaxStockBatches = 5000;
        private const int MaxJournalEntries = 1000;

        public static string PathName => Path.Combine(Application.persistentDataPath, "warm-bread-v1.json");
        public static bool Exists => File.Exists(PathName) || File.Exists(PathName + ".bak");

        public static bool Write(SaveData data)
        {
            if (data == null) return false;

            try
            {
                data.version = SaveData.CurrentVersion;
                var path = PathName;
                var temp = path + ".tmp";
                var backup = path + ".bak";

                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));

                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(temp, path, backup);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        ReplaceWithFallback(temp, path, backup);
                    }
                    catch (IOException)
                    {
                        ReplaceWithFallback(temp, path, backup);
                    }
                }
                else
                {
                    File.Move(temp, path);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Сохранение не записано: " + exception.Message);
                return false;
            }
        }

        public static SaveData Read()
        {
            foreach (var path in new[] { PathName, PathName + ".bak" })
            {
                try
                {
                    if (!File.Exists(path)) continue;

                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                    if (!TryMigrateAndValidate(data)) continue;
                    return data;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Чтение сохранения: " + exception.Message);
                }
            }

            return null;
        }

        private static void ReplaceWithFallback(string temp, string path, string backup)
        {
            if (File.Exists(path)) File.Copy(path, backup, true);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }

        private static bool TryMigrateAndValidate(SaveData data)
        {
            if (data == null) return false;

            if (data.version == 1)
            {
                data.version = SaveData.CurrentVersion;
                if (data.journalIds == null) data.journalIds = new System.Collections.Generic.List<string>();
            }

            if (data.version != SaveData.CurrentVersion ||
                data.day < 1 ||
                data.day > 100000 ||
                data.cash < -10000000 ||
                data.cash > 100000000 ||
                data.reputation < 0 ||
                data.reputation > 100 ||
                data.stock == null ||
                data.journal == null)
            {
                return false;
            }

            if (data.journalIds == null)
            {
                data.journalIds = new System.Collections.Generic.List<string>();
            }

            if (data.stock.Count > MaxStockBatches ||
                data.journal.Count > MaxJournalEntries ||
                data.journalIds.Count > MaxJournalEntries)
            {
                return false;
            }

            data.stock.RemoveAll(batch =>
                batch == null ||
                string.IsNullOrWhiteSpace(batch.productId) ||
                batch.quantity <= 0 ||
                batch.quantity > 10000 ||
                float.IsNaN(batch.bakedAt) ||
                float.IsInfinity(batch.bakedAt));

            data.journal.RemoveAll(string.IsNullOrWhiteSpace);
            data.journalIds.RemoveAll(string.IsNullOrWhiteSpace);
            return true;
        }
    }
}
