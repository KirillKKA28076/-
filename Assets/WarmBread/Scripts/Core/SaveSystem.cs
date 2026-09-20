using System;
using System.IO;
using UnityEngine;

namespace WarmBread
{
    public static class SaveSystem
    {
        private const int MaxStockBatches = 5000;
        private const int MaxJournalEntries = 1000;
        private const int MaxCampaignCounter = 1000000;

        // Имя оставлено прежним, чтобы сохранения ранних версий автоматически мигрировали.
        public static string PathName => Path.Combine(Application.persistentDataPath, "warm-bread-v1.json");
        public static bool Exists => File.Exists(PathName) || File.Exists(PathName + ".bak");

        public static bool Write(SaveData data)
        {
            if (data == null) return false;

            try
            {
                data.version = SaveData.CurrentVersion;
                SanitizeForWrite(data);

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
                TryDeleteTemporary();
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

                    var text = File.ReadAllText(path);
                    if (string.IsNullOrWhiteSpace(text) || text.Length > 8 * 1024 * 1024) continue;

                    var data = JsonUtility.FromJson<SaveData>(text);
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

        public static bool DeleteAll()
        {
            try
            {
                foreach (var path in new[] { PathName, PathName + ".bak", PathName + ".tmp" })
                {
                    if (File.Exists(path)) File.Delete(path);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Сохранение не удалено: " + exception.Message);
                return false;
            }
        }

        private static void ReplaceWithFallback(string temp, string path, string backup)
        {
            if (File.Exists(path)) File.Copy(path, backup, true);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }

        private static bool TryMigrateAndValidate(SaveData data)
        {
            if (data == null || data.version < 1 || data.version > SaveData.CurrentVersion) return false;

            if (data.version <= 1 && data.journalIds == null)
            {
                data.journalIds = new System.Collections.Generic.List<string>();
            }

            if (data.version <= 2)
            {
                data.totalSales = Mathf.Max(0, data.totalSales);
                data.totalRevenue = Mathf.Max(0, data.totalRevenue);
                data.goalsCompleted = Mathf.Max(0, data.goalsCompleted);
                data.campaignCompleted = false;
                data.endingId = string.Empty;
            }

            data.version = SaveData.CurrentVersion;
            if (data.stock == null || data.journal == null) return false;
            if (data.journalIds == null) data.journalIds = new System.Collections.Generic.List<string>();
            if (data.endingId == null) data.endingId = string.Empty;

            if (data.day < 1 || data.day > 100000 ||
                data.cash < -10000000 || data.cash > 100000000 ||
                data.reputation < 0 || data.reputation > 100 ||
                data.totalSales < 0 || data.totalSales > MaxCampaignCounter ||
                data.totalRevenue < 0 || data.totalRevenue > 100000000 ||
                data.goalsCompleted < 0 || data.goalsCompleted > MaxCampaignCounter ||
                data.endingId.Length > 64 ||
                data.stock.Count > MaxStockBatches ||
                data.journal.Count > MaxJournalEntries ||
                data.journalIds.Count > MaxJournalEntries)
            {
                return false;
            }

            SanitizeCollections(data);
            return true;
        }

        private static void SanitizeForWrite(SaveData data)
        {
            if (data.stock == null) data.stock = new System.Collections.Generic.List<StockBatch>();
            if (data.journal == null) data.journal = new System.Collections.Generic.List<string>();
            if (data.journalIds == null) data.journalIds = new System.Collections.Generic.List<string>();
            if (data.endingId == null) data.endingId = string.Empty;
            SanitizeCollections(data);
        }

        private static void SanitizeCollections(SaveData data)
        {
            data.stock.RemoveAll(batch =>
                batch == null ||
                string.IsNullOrWhiteSpace(batch.productId) ||
                batch.quantity <= 0 ||
                batch.quantity > 10000 ||
                float.IsNaN(batch.bakedAt) ||
                float.IsInfinity(batch.bakedAt));

            data.journal.RemoveAll(string.IsNullOrWhiteSpace);
            data.journalIds.RemoveAll(string.IsNullOrWhiteSpace);

            if (data.stock.Count > MaxStockBatches)
            {
                data.stock.RemoveRange(MaxStockBatches, data.stock.Count - MaxStockBatches);
            }

            if (data.journal.Count > MaxJournalEntries)
            {
                data.journal.RemoveRange(MaxJournalEntries, data.journal.Count - MaxJournalEntries);
            }

            if (data.journalIds.Count > MaxJournalEntries)
            {
                data.journalIds.RemoveRange(MaxJournalEntries, data.journalIds.Count - MaxJournalEntries);
            }
        }

        private static void TryDeleteTemporary()
        {
            try
            {
                var temp = PathName + ".tmp";
                if (File.Exists(temp)) File.Delete(temp);
            }
            catch
            {
                // Вторичная очистка не должна скрывать исходную ошибку сохранения.
            }
        }
    }
}
