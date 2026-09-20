#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WarmBread.Editor
{
    public static class ContentValidator
    {
        private const string ScenePath = "Assets/WarmBread/Scenes/Bakery_Street.unity";
        private const string FontPath = "Assets/Resources/Fonts/BreadSans.ttf";

        [MenuItem("Тёплый хлеб/4. Проверить проект")]
        public static void ValidateFromMenu()
        {
            ValidateProject(true);
        }

        public static int ValidateProject(bool logSuccess)
        {
            var errors = new List<string>();
            ValidateRequiredAssets(errors);
            ValidateProducts(errors);
            ValidateCustomers(errors);
            ValidateCurrentScene(errors);

            foreach (var error in errors)
            {
                Debug.LogError("[WarmBread] " + error);
            }

            if (errors.Count == 0 && logSuccess)
            {
                Debug.Log("[WarmBread] Проверка пройдена: сцена, каталог, покупатели, шрифты и ссылки в порядке.");
            }

            return errors.Count;
        }

        private static void ValidateRequiredAssets(ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                errors.Add("Не найдена игровая сцена: " + ScenePath);
            }

            if (AssetDatabase.LoadAssetAtPath<Font>(FontPath) == null)
            {
                errors.Add("Не найден кириллический шрифт: " + FontPath);
            }

            if (!EditorBuildSettings.scenes.Any(scene =>
                    scene.enabled &&
                    string.Equals(scene.path, ScenePath, StringComparison.Ordinal)))
            {
                errors.Add("Игровая сцена не включена в Build Settings.");
            }

            if (Shader.Find("Universal Render Pipeline/Lit") == null)
            {
                errors.Add("Не найден shader Universal Render Pipeline/Lit.");
            }

            if (Shader.Find("Universal Render Pipeline/Particles/Unlit") == null)
            {
                errors.Add("Не найден shader Universal Render Pipeline/Particles/Unlit.");
            }
        }

        private static void ValidateProducts(ICollection<string> errors)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var guids = AssetDatabase.FindAssets("t:ProductData");

            if (guids.Length == 0)
            {
                errors.Add("Не найдено ни одного ProductData. Запустите подготовку проекта.");
                return;
            }

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var product = AssetDatabase.LoadAssetAtPath<ProductData>(path);
                if (product == null)
                {
                    errors.Add("Не удалось загрузить ProductData: " + path);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(product.Id))
                {
                    errors.Add("У товара пустой ID: " + path);
                }
                else if (!ids.Add(product.Id))
                {
                    errors.Add("Повторяющийся ID товара '" + product.Id + "': " + path);
                }

                if (string.IsNullOrWhiteSpace(product.Title))
                {
                    errors.Add("У товара нет названия: " + path);
                }

                if (product.Price <= 0)
                {
                    errors.Add("Цена товара должна быть больше нуля: " + path);
                }

                if (product.Cost < 0 || product.ShelfLifeHours < 0f)
                {
                    errors.Add("Некорректная закупочная цена или срок годности: " + path);
                }
            }
        }

        private static void ValidateCustomers(ICollection<string> errors)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var guids = AssetDatabase.FindAssets("t:CustomerData");

            if (guids.Length == 0)
            {
                errors.Add("Не найдено ни одного CustomerData. Запустите подготовку проекта.");
                return;
            }

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var customer = AssetDatabase.LoadAssetAtPath<CustomerData>(path);
                if (customer == null)
                {
                    errors.Add("Не удалось загрузить CustomerData: " + path);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(customer.Id))
                {
                    errors.Add("У покупателя пустой ID: " + path);
                }
                else if (!ids.Add(customer.Id))
                {
                    errors.Add("Повторяющийся ID покупателя '" + customer.Id + "': " + path);
                }

                if (string.IsNullOrWhiteSpace(customer.DisplayName) ||
                    string.IsNullOrWhiteSpace(customer.Greeting) ||
                    string.IsNullOrWhiteSpace(customer.Story))
                {
                    errors.Add("У покупателя не заполнены имя, приветствие или история: " + path);
                }
            }
        }

        private static void ValidateCurrentScene(ICollection<string> errors)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;

            var missingScripts = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                }
            }

            if (missingScripts > 0)
            {
                errors.Add("В открытой сцене найдено отсутствующих MonoBehaviour: " + missingScripts);
            }
        }
    }
}
#endif
