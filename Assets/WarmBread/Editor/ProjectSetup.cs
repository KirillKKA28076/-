using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WarmBread.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/WarmBread/Scenes/Bakery_Street.unity";
        private const string PipelinePath = "Assets/Generated/BreadURP.asset";
        private const string RendererPath = "Assets/Generated/BreadRenderer.asset";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        static ProjectSetup()
        {
            if (!Application.isBatchMode) EditorApplication.delayCall += AutoSetup;
        }

        private static void AutoSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoSetup;
                return;
            }

            if (NeedsSetup()) Prepare();
        }

        [MenuItem("Тёплый хлеб/1. Подготовить проект")]
        public static void Prepare()
        {
            EnsureFolder("Assets/Generated");
            EnsureFolder("Assets/Resources/Products");
            EnsureFolder("Assets/Resources/Customers");

            EnsureTmpResources();
            var pipeline = EnsureRenderPipeline();

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            QualitySettings.vSyncCount = 1;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.companyName = "WarmBread";
            PlayerSettings.productName = "Тёплый хлеб";

            EnsureAlwaysIncludedShaders();
            EnsureProducts();
            EnsureCustomers();
            EnsureBuildScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Тёплый хлеб: проект подготовлен. Откройте Bakery_Street и нажмите Play.");
        }

        [MenuItem("Тёплый хлеб/2. Открыть игровую сцену")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("Не найдена игровая сцена: " + ScenePath);
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        [MenuItem("Тёплый хлеб/3. Собрать Windows x64")]
        public static void BuildWindows()
        {
            Prepare();

            if (ContentValidator.ValidateProject(false) > 0)
            {
                throw new InvalidOperationException("Сборка остановлена: проверка контента нашла ошибки.");
            }

            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/WarmBread.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.StrictMode
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Сборка не завершена: " + report.summary.result);
            }
        }

        private static bool NeedsSetup()
        {
            if (!File.Exists(PipelinePath) ||
                !File.Exists(TmpSettingsPath) ||
                AssetDatabase.FindAssets("t:ProductData", new[] { "Assets/Resources/Products" }).Length == 0 ||
                AssetDatabase.FindAssets("t:CustomerData", new[] { "Assets/Resources/Customers" }).Length == 0)
            {
                return true;
            }

            return !EditorBuildSettings.scenes.Any(scene =>
                scene.enabled &&
                string.Equals(scene.path, ScenePath, StringComparison.Ordinal));
        }

        private static void EnsureTmpResources()
        {
            if (File.Exists(TmpSettingsPath)) return;

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            if (packageInfo == null)
            {
                Debug.LogError("Не удалось определить путь пакета TextMeshPro.");
                return;
            }

            var package = Path.Combine(
                packageInfo.resolvedPath,
                "Package Resources/TMP Essential Resources.unitypackage");

            if (!File.Exists(package))
            {
                Debug.LogError("Не найден TMP Essential Resources: " + package);
                return;
            }

            AssetDatabase.ImportPackage(package, false);
        }

        private static UniversalRenderPipelineAsset EnsureRenderPipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline != null) return pipeline;

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "Bread Renderer";
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.name = "Bread URP";
            pipeline.msaaSampleCount = 2;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 65f;
            pipeline.supportsHDR = true;
            pipeline.supportsCameraDepthTexture = true;
            pipeline.useSRPBatcher = true;
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
            return pipeline;
        }

        private static void EnsureAlwaysIncludedShaders()
        {
            var graphicsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")
                .FirstOrDefault();
            if (graphicsAsset == null) return;

            var settings = new SerializedObject(graphicsAsset);
            var shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null) return;

            foreach (var name in new[]
                     {
                         "Universal Render Pipeline/Lit",
                         "Universal Render Pipeline/Particles/Unlit",
                         "TextMeshPro/Distance Field",
                         "TextMeshPro/Mobile/Distance Field"
                     })
            {
                var shader = Shader.Find(name);
                if (shader == null) continue;

                var exists = false;
                for (var i = 0; i < shaders.arraySize; i++)
                {
                    if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists) continue;

                var index = shaders.arraySize;
                shaders.InsertArrayElementAtIndex(index);
                shaders.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            }

            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureProducts()
        {
            foreach (var product in ProductCatalog.CreateDefaults())
            {
                var path = "Assets/Resources/Products/" + product.Id + ".asset";
                if (AssetDatabase.LoadAssetAtPath<ProductData>(path) == null)
                {
                    AssetDatabase.CreateAsset(product, path);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(product);
                }
            }
        }

        private static void EnsureCustomers()
        {
            var people = CustomerData.Defaults();
            for (var i = 0; i < people.Length; i++)
            {
                var path = "Assets/Resources/Customers/Customer_" + i + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<CustomerData>(path);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(people[i], path);
                    continue;
                }

                existing.Configure(
                    people[i].Id,
                    people[i].DisplayName,
                    people[i].Greeting,
                    people[i].Story,
                    people[i].Coat,
                    people[i].Child,
                    people[i].LateVisitor);

                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(people[i]);
            }
        }

        private static void EnsureBuildScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var index = scenes.FindIndex(scene =>
                string.Equals(scene.path, ScenePath, StringComparison.Ordinal));

            if (index >= 0)
            {
                scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
            }
            else
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                path == "Assets" ||
                AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(parent)) return;

            parent = parent.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
