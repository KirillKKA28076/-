using System;
using System.IO;
using UnityEngine;

namespace WarmBread
{
    public static class SaveSystem
    {
        public static string PathName => Path.Combine(Application.persistentDataPath, "warm-bread-v1.json");
        public static bool Exists => File.Exists(PathName);
        public static bool Write(SaveData data)
        {
            try
            {
                string path = PathName, temp = path + ".tmp";
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                return true;
            }
            catch (Exception ex) { Debug.LogWarning("Сохранение не записано: " + ex.Message); return false; }
        }
        public static SaveData Read()
        {
            foreach (string path in new[] { PathName, PathName + ".bak" })
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                    if (data == null || data.version != 1 || data.day < 1 || data.day > 100000 ||
                        data.cash < -10000000 || data.cash > 100000000 || data.stock == null ||
                        data.journal == null || data.reputation < 0 || data.reputation > 100) continue;
                    data.stock.RemoveAll(b => b == null || string.IsNullOrEmpty(b.productId) || b.quantity <= 0 || b.quantity > 10000 || float.IsNaN(b.bakedAt) || float.IsInfinity(b.bakedAt));
                    return data;
                }
                catch (Exception ex) { Debug.LogWarning("Чтение сохранения: " + ex.Message); }
            }
            return null;
        }
    }
}
