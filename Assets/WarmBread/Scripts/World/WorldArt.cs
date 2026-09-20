using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace WarmBread
{
    public static class WorldArt
    {
        private static readonly Dictionary<string, Material> Materials =
            new Dictionary<string, Material>(StringComparer.Ordinal);
        private static readonly List<UnityEngine.Object> GeneratedResources =
            new List<UnityEngine.Object>();

        public static TMP_FontAsset Font { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            ReleaseGeneratedResources();
            Materials.Clear();
            Font = null;
        }

        public static void PrepareFont()
        {
            if (Font != null) return;

            var source = Resources.Load<Font>("Fonts/BreadSans");
            if (source == null)
            {
                source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (source == null)
            {
                Debug.LogError("[WarmBread] Не найден кириллический шрифт.");
                return;
            }

            Font = TMP_FontAsset.CreateFontAsset(
                source,
                42,
                5,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024,
                1024);

            if (Font == null)
            {
                Debug.LogError("[WarmBread] Не удалось создать TMP Font Asset.");
                return;
            }

            Font.name = "WarmBread Runtime Font";
            Font.hideFlags = HideFlags.DontSave;
            Font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            Font.isMultiAtlasTexturesEnabled = true;
            GeneratedResources.Add(Font);
        }

        public static Material Material(
            string name,
            Color color,
            float smooth = .15f,
            bool weathered = false,
            float glow = 0f)
        {
            var key = BuildMaterialKey(name, color, smooth, weathered, glow);
            if (Materials.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("Не найден совместимый Lit shader.");
            }

            var material = new Material(shader)
            {
                name = string.IsNullOrWhiteSpace(name) ? "WarmBread Material" : name,
                hideFlags = HideFlags.DontSave
            };

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", Mathf.Clamp01(smooth));
            }

            if (glow > 0f)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color * glow);
                }
            }

            if (weathered)
            {
                var texture = CreateWeatheredTexture(name);
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                else if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            }

            material.enableInstancing = true;
            Materials[key] = material;
            GeneratedResources.Add(material);
            return material;
        }

        public static GameObject Cube(
            string name,
            Vector3 position,
            Vector3 size,
            Material material,
            Transform parent = null,
            bool collider = true)
        {
            return Shape(PrimitiveType.Cube, name, position, size, material, parent, collider);
        }

        public static GameObject Part(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 size,
            Material material)
        {
            return Shape(PrimitiveType.Cube, name, position, size, material, parent, false);
        }

        public static GameObject Shape(
            PrimitiveType type,
            string name,
            Vector3 position,
            Vector3 size,
            Material material,
            Transform parent = null,
            bool collider = false)
        {
            var gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localScale = size;

            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;

            if (!collider)
            {
                var primitiveCollider = gameObject.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
                    primitiveCollider.enabled = false;
                    UnityEngine.Object.Destroy(primitiveCollider);
                }
            }

            return gameObject;
        }

        public static void Line(
            string name,
            Vector3[] points,
            float width,
            Material material,
            Transform parent)
        {
            if (points == null || points.Length < 2) return;

            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);

            var line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.startWidth = width;
            line.endWidth = width;
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.numCapVertices = 2;
        }

        public static TextMeshPro Label(
            string text,
            Vector3 position,
            float size,
            Color color,
            Transform parent,
            float yaw = 180f)
        {
            if (Font == null) PrepareFont();

            var gameObject = new GameObject("Надпись • " + text);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var label = gameObject.AddComponent<TextMeshPro>();
            if (Font != null) label.font = Font;
            label.text = text ?? string.Empty;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.rectTransform.sizeDelta = new Vector2(5f, 2f);
            label.enableWordWrapping = false;

            var renderer = label.GetComponent<Renderer>();
            if (renderer != null) renderer.shadowCastingMode = ShadowCastingMode.Off;
            return label;
        }

        public static Light Lamp(
            string name,
            Vector3 position,
            Color color,
            float intensity,
            float range,
            Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;

            var light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = Mathf.Max(0f, intensity);
            light.range = Mathf.Max(.1f, range);
            return light;
        }

        public static void Product(
            Transform parent,
            ProductData product,
            Vector3 position,
            float scale = 1f)
        {
            if (product == null) return;

            var root = new GameObject(product.Title).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localScale = Vector3.one * Mathf.Max(.01f, scale);

            var material = Material(product.Id, product.Tint, .22f, true);
            var pale = Material("Мука", new Color(.83f, .73f, .54f));

            if (product.Id == "water" || product.Id == "lemonade")
            {
                Shape(
                    PrimitiveType.Cylinder,
                    "Бутылка",
                    new Vector3(0f, .15f, 0f),
                    new Vector3(.13f, .15f, .13f),
                    material,
                    root);

                Shape(
                    PrimitiveType.Cylinder,
                    "Горлышко",
                    new Vector3(0f, .32f, 0f),
                    new Vector3(.05f, .045f, .05f),
                    pale,
                    root);

                Part(
                    "Этикетка",
                    root,
                    new Vector3(0f, .16f, -.068f),
                    new Vector3(.08f, .1f, .01f),
                    pale);
            }
            else if (product.Id.StartsWith("sig_", StringComparison.Ordinal) || product.Id == "gum")
            {
                Part(
                    "Упаковка",
                    root,
                    new Vector3(0f, .08f, 0f),
                    new Vector3(.1f, .16f, .06f),
                    material);
            }
            else
            {
                var dimensions = product.Id == "bread_white"
                    ? new Vector3(.4f, .14f, .18f)
                    : product.Id == "bread_black"
                        ? new Vector3(.28f, .18f, .2f)
                        : new Vector3(.21f, .12f, .16f);

                Shape(
                    PrimitiveType.Sphere,
                    "Корочка",
                    new Vector3(0f, dimensions.y * .45f, 0f),
                    dimensions,
                    material,
                    root);

                for (var i = -1; i <= 1; i++)
                {
                    var cut = Part(
                        "Надрез",
                        root,
                        new Vector3(i * .085f, dimensions.y * .84f, -.025f),
                        new Vector3(.018f, .008f, .085f),
                        pale);
                    cut.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
                }
            }
        }

        private static Texture2D CreateWeatheredTexture(string name)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGB24, true)
            {
                name = (name ?? "Material") + " Weathering",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            var pixels = new Color[64 * 64];
            var random = new System.Random(2002 + (name == null ? 0 : name.Length * 17));
            for (var y = 0; y < 64; y++)
            {
                for (var x = 0; x < 64; x++)
                {
                    var grain = .83f + (float)random.NextDouble() * .17f;
                    var stain = Mathf.PerlinNoise(x * .15f, y * .15f) * .15f;
                    pixels[y * 64 + x] = Color.white * Mathf.Clamp01(grain - stain);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            GeneratedResources.Add(texture);
            return texture;
        }

        private static string BuildMaterialKey(
            string name,
            Color color,
            float smooth,
            bool weathered,
            float glow)
        {
            var color32 = (Color32)color;
            return string.Concat(
                name ?? string.Empty,
                "|",
                color32.r,
                ",",
                color32.g,
                ",",
                color32.b,
                ",",
                color32.a,
                "|",
                Mathf.RoundToInt(smooth * 1000f),
                "|",
                weathered ? "1" : "0",
                "|",
                Mathf.RoundToInt(glow * 1000f));
        }

        private static void ReleaseGeneratedResources()
        {
            for (var i = GeneratedResources.Count - 1; i >= 0; i--)
            {
                var resource = GeneratedResources[i];
                if (resource == null) continue;

                if (Application.isPlaying) UnityEngine.Object.Destroy(resource);
                else UnityEngine.Object.DestroyImmediate(resource);
            }

            GeneratedResources.Clear();
        }
    }
}
