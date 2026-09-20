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

        private static readonly string[] RequiredScripts =
        {
            "Assets/WarmBread/Scripts/Core/CampaignRules.cs",
            "Assets/WarmBread/Scripts/Core/GameSession.cs",
            "Assets/WarmBread/Scripts/Core/SaveSystem.cs",
            "Assets/WarmBread/Scripts/World/ProceduralMeshFactory.cs",
            "Assets/WarmBread/Scripts/World/ModelFactory.cs",
            "Assets/WarmBread/Scripts/World/StockDisplay.cs",
            "Assets/WarmBread/Scripts/World/WorldBuilder.cs",
            "Assets/WarmBread/Scripts/UI/GameUI.cs"
        };

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
            ValidateCampaign(errors);
            ValidateProceduralMeshes(errors);
            ValidateCurrentScene(errors);

            foreach (var error in errors)
            {
                Debug.LogError("[WarmBread] " + error);
            }

            if (errors.Count == 0 && logSuccess)
            {
                Debug.Log(
                    "[WarmBread] Проверка пройдена: сцена, кампания, модели, каталог, " +
                    "покупатели, шрифты и ссылки в порядке.");
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

            foreach (var path in RequiredScripts)
            {
                if (AssetDatabase.LoadAssetAtPath<MonoScript>(path) == null)
                {
                    errors.Add("Не найден обязательный игровой скрипт: " + path);
                }
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

            if (SaveData.CurrentVersion < 3)
            {
                errors.Add("Формат сохранений должен поддерживать кампанию версии 3.");
            }
        }

        private static void ValidateProducts(ICollection<string> errors)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var guids = AssetDatabase.FindAssets("t:ProductData");

            if (guids.Length < 10)
            {
                errors.Add(
                    "В каталоге должно быть не меньше десяти базовых товаров. " +
                    "Запустите подготовку проекта.");
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

                if (product.Price < product.Cost)
                {
                    errors.Add("Розничная цена ниже закупочной: " + path);
                }
            }

            foreach (var required in new[]
                     {
                         "bread_white",
                         "bread_black",
                         "pirozhok_meat",
                         "pirozhok_potato",
                         "bulochka",
                         "water",
                         "lemonade",
                         "gum"
                     })
            {
                if (!ids.Contains(required)) errors.Add("Нет обязательного товара: " + required);
            }
        }

        private static void ValidateCustomers(ICollection<string> errors)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var guids = AssetDatabase.FindAssets("t:CustomerData");
            var lateVisitors = 0;
            var children = 0;

            if (guids.Length < 6)
            {
                errors.Add(
                    "Нужно не меньше шести базовых покупателей. " +
                    "Запустите подготовку проекта.");
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

                if (customer.LateVisitor) lateVisitors++;
                if (customer.Child) children++;
            }

            if (lateVisitors == 0) errors.Add("Нужен хотя бы один поздний посетитель.");
            if (children == 0) errors.Add("Нужен хотя бы один ребёнок для проверки ограничений заказов.");
        }

        private static void ValidateCampaign(ICollection<string> errors)
        {
            if (CampaignRules.CampaignDays != 7)
            {
                errors.Add("Кампания должна состоять ровно из семи дней.");
                return;
            }

            var titles = new HashSet<string>(StringComparer.Ordinal);
            for (var day = 1; day <= CampaignRules.CampaignDays; day++)
            {
                var plan = CampaignRules.Get(day);
                if (plan == null)
                {
                    errors.Add("Не найден план кампании для дня " + day + ".");
                    continue;
                }

                if (plan.Day != day) errors.Add("План кампании имеет неправильный номер дня: " + day);
                if (string.IsNullOrWhiteSpace(plan.Title) || !titles.Add(plan.Title))
                {
                    errors.Add("У дня кампании пустое или повторяющееся название: " + day);
                }
                if (string.IsNullOrWhiteSpace(plan.Description))
                {
                    errors.Add("У дня кампании нет описания: " + day);
                }
                if (plan.SalesGoal <= 0 || plan.RevenueGoal <= 0 || plan.Rent <= 0 || plan.GoalBonus <= 0)
                {
                    errors.Add("Некорректная экономика плана дня: " + day);
                }
            }

            foreach (var ending in new[] { "home", "warm-light", "hard-autumn" })
            {
                if (string.IsNullOrWhiteSpace(CampaignRules.EndingTitle(ending)) ||
                    string.IsNullOrWhiteSpace(CampaignRules.EndingText(ending)))
                {
                    errors.Add("Не заполнен финал кампании: " + ending);
                }
            }
        }

        private static void ValidateProceduralMeshes(ICollection<string> errors)
        {
            Mesh loaf = null;
            Mesh bottle = null;
            Mesh torus = null;

            try
            {
                loaf = ProceduralMeshFactory.CreateLoaf("Validation loaf", .5f, .25f, .2f);
                bottle = ProceduralMeshFactory.CreateLathe(
                    "Validation bottle",
                    new[]
                    {
                        new Vector2(.08f, 0f),
                        new Vector2(.08f, .3f),
                        new Vector2(.03f, .4f)
                    },
                    16);
                torus = ProceduralMeshFactory.CreateTorus("Validation torus", .1f, .03f, 16, 8);

                foreach (var mesh in new[] { loaf, bottle, torus })
                {
                    if (mesh == null || mesh.vertexCount < 16 || mesh.triangles.Length < 24)
                    {
                        errors.Add("Процедурный генератор создал пустую или повреждённую модель.");
                        break;
                    }

                    if (mesh.bounds.size.sqrMagnitude <= .0001f)
                    {
                        errors.Add("У процедурной модели некорректные границы.");
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                errors.Add("Ошибка проверки процедурных моделей: " + exception.Message);
            }
            finally
            {
                if (loaf != null) UnityEngine.Object.DestroyImmediate(loaf);
                if (bottle != null) UnityEngine.Object.DestroyImmediate(bottle);
                if (torus != null) UnityEngine.Object.DestroyImmediate(torus);
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
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                        transform.gameObject);
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
