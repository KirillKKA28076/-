#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace WarmBread.Editor
{
    public static class ContentValidator
    {
        [MenuItem("WarmBread/Validate Current Scene")]
        public static void ValidateCurrentScene()
        {
            var objects = Object.FindObjectsOfType<MonoBehaviour>(true);
            var missing = 0;
            foreach (var component in objects)
            {
                if (component == null) missing++;
            }

            if (missing == 0)
                Debug.Log("[WarmBread] Validation passed: no missing MonoBehaviour references found.");
            else
                Debug.LogWarning("[WarmBread] Validation found " + missing + " missing components.");
        }
    }
}
#endif
