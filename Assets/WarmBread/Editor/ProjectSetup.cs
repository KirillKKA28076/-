using System;
using System.IO;
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
        static ProjectSetup()
        {
            if (!Application.isBatchMode) EditorApplication.delayCall += AutoSetup;
        }
        private static void AutoSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += AutoSetup; return; }
            if (!File.Exists(PipelinePath)) Prepare();
        }
        [MenuItem("Тёплый хлеб/1. Подготовить проект")]
        public static void Prepare()
        {
            EnsureFolder("Assets/Generated");EnsureFolder("Assets/Resources/Products");EnsureFolder("Assets/Resources/Customers");
            // TMP поставляет шейдеры и настройки в Essential Resources; импорт без диалога.
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
                var package = Path.Combine(info.resolvedPath,"Package Resources/TMP Essential Resources.unitypackage");
                AssetDatabase.ImportPackage(package,false);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer,"Assets/Generated/BreadRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.msaaSampleCount=2;pipeline.renderScale=1;pipeline.shadowDistance=65;
                pipeline.supportsHDR=true;pipeline.supportsCameraDepthTexture=true;pipeline.useSRPBatcher=true;
                AssetDatabase.CreateAsset(pipeline,PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            QualitySettings.vSyncCount=1;PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.companyName="WarmBread";PlayerSettings.productName="Тёплый хлеб";
            // Удерживаем используемые runtime-материалами шейдеры в Windows build.
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=settings.FindProperty("m_AlwaysIncludedShaders");
            foreach(string name in new[]{"Universal Render Pipeline/Lit","Universal Render Pipeline/Particles/Unlit","TextMeshPro/Distance Field","TextMeshPro/Mobile/Distance Field"})
            {
                var shader=Shader.Find(name);if(shader==null)continue;
                bool exists=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)exists=true;
                if(!exists){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            foreach(var p in ProductCatalog.CreateDefaults())
            {
                var path="Assets/Resources/Products/"+p.Id+".asset";
                if(!File.Exists(path))AssetDatabase.CreateAsset(p,path);else UnityEngine.Object.DestroyImmediate(p);
            }
            var people=CustomerData.Defaults();
            for(int i=0;i<people.Length;i++)
            {
                var path="Assets/Resources/Customers/Customer_"+i+".asset";
                if(!File.Exists(path))AssetDatabase.CreateAsset(people[i],path);else UnityEngine.Object.DestroyImmediate(people[i]);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("Тёплый хлеб: проект подготовлен. Откройте Bakery_Street и нажмите Play.");
        }
        [MenuItem("Тёплый хлеб/2. Открыть игровую сцену")]
        public static void OpenScene()
        { if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath); }
        [MenuItem("Тёплый хлеб/3. Собрать Windows x64")]
        public static void BuildWindows()
        {
            Prepare();Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Windows/WarmBread.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Сборка не завершена: "+report.summary.result);
        }
        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            var parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
