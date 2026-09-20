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
        private static readonly Dictionary<string, Mesh> Meshes =
            new Dictionary<string, Mesh>(StringComparer.Ordinal);
        private static readonly List<UnityEngine.Object> GeneratedResources =
            new List<UnityEngine.Object>();

        public static TMP_FontAsset Font { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            ReleaseGeneratedResources();
            Materials.Clear();
            Meshes.Clear();
            Font = null;
        }

        public static void PrepareFont()
        {
            if (Font != null) return;

            var source = Resources.Load<Font>("Fonts/BreadSans");
            if (source == null) source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            TrackGenerated(Font);
        }

        public static Material Material(
            string name,
            Color color,
            float smooth = .15f,
            bool weathered = false,
            float glow = 0f,
            float metallic = 0f,
            bool transparent = false)
        {
            var key = BuildMaterialKey(name, color, smooth, weathered, glow, metallic, transparent);
            if (Materials.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Не найден совместимый Lit shader.");

            var material = new Material(shader)
            {
                name = string.IsNullOrWhiteSpace(name) ? "WarmBread Material" : name,
                hideFlags = HideFlags.DontSave
            };

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", Mathf.Clamp01(smooth));
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Mathf.Clamp01(metallic));

            if (glow > 0f)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * glow);
            }

            if (weathered)
            {
                var texture = CreateWeatheredTexture(name);
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            }

            if (transparent || color.a < .999f) ConfigureTransparent(material);
            material.enableInstancing = true;
            Materials[key] = material;
            TrackGenerated(material);
            return material;
        }

        public static Material GlassMaterial(string name, Color color, float smoothness = .92f)
        {
            color.a = Mathf.Clamp(color.a <= 0f ? .28f : color.a, .04f, .75f);
            return Material(name, color, smoothness, false, 0f, 0f, true);
        }

        public static Material MetalMaterial(string name, Color color, float smoothness = .55f)
        {
            return Material(name, color, smoothness, true, 0f, .72f, false);
        }

        public static Mesh SharedMesh(string key, Func<Mesh> factory)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Mesh key is required.", nameof(key));
            if (Meshes.TryGetValue(key, out var mesh) && mesh != null) return mesh;
            mesh = factory != null ? factory() : null;
            if (mesh == null) throw new InvalidOperationException("Procedural mesh factory returned null for " + key);
            mesh.hideFlags = HideFlags.DontSave;
            Meshes[key] = mesh;
            TrackGenerated(mesh);
            return mesh;
        }

        public static GameObject MeshObject(
            string name,
            Mesh mesh,
            Material material,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            bool collider = false)
        {
            var gameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = rotation;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;

            if (collider && mesh != null)
            {
                var meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }

            return gameObject;
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

        public static GameObject ChamferedBox(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 size,
            float bevel,
            Material material,
            bool collider = false)
        {
            var key = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "chamfer:{0:0.###}:{1:0.###}:{2:0.###}:{3:0.###}",
                size.x,
                size.y,
                size.z,
                bevel);
            var mesh = SharedMesh(key, () => ProceduralMeshFactory.CreateChamferedBox(name + " Mesh", size, bevel));
            return MeshObject(name, mesh, material, parent, position, Quaternion.identity, Vector3.one, collider);
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
            line.numCornerVertices = 2;
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
            light.shadows = LightShadows.None;
            return light;
        }

        public static void Product(Transform parent, ProductData product, Vector3 position, float scale = 1f)
        {
            if (product == null) return;
            var root = new GameObject(product.Title).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localScale = Vector3.one * Mathf.Max(.01f, scale);

            var crust = Material(product.Id + " crust", product.Tint, .2f, true);
            var crumb = Material("Тёплая мякоть", new Color(.88f, .75f, .5f), .12f, true);
            var ink = Material(product.Id + " print", Color.Lerp(product.Tint, Color.black, .55f), .18f);

            if (product.Id == "bread_white" || product.Id == "bread_black")
            {
                var white = product.Id == "bread_white";
                var mesh = SharedMesh(
                    white ? "product:loaf:white" : "product:loaf:black",
                    () => ProceduralMeshFactory.CreateLoaf(
                        white ? "Нарезной батон" : "Бородинский хлеб",
                        white ? .52f : .38f,
                        white ? .23f : .26f,
                        white ? .2f : .22f));
                MeshObject("Формовой хлеб", mesh, crust, root, Vector3.up * .11f, Quaternion.identity, Vector3.one);
                var cuts = white ? 4 : 2;
                for (var i = 0; i < cuts; i++)
                {
                    var x = (i - (cuts - 1) * .5f) * (white ? .11f : .12f);
                    var cut = ChamferedBox("Надрез", root, new Vector3(x, .215f, -.015f), new Vector3(.018f, .012f, .14f), .005f, crumb);
                    cut.transform.localRotation = Quaternion.Euler(0f, -22f, 0f);
                }
            }
            else if (product.Id == "pirozhok_meat" || product.Id == "pirozhok_potato")
            {
                var mesh = SharedMesh(
                    "product:pirozhok",
                    () => ProceduralMeshFactory.CreateLoaf("Пирожок", .27f, .18f, .12f, 12, 16));
                MeshObject("Печёный пирожок", mesh, crust, root, Vector3.up * .07f, Quaternion.identity, Vector3.one);
                ChamferedBox("Защип", root, new Vector3(0f, .14f, 0f), new Vector3(.18f, .018f, .025f), .008f, crumb);
            }
            else if (product.Id == "bulochka")
            {
                var torus = SharedMesh(
                    "product:bun:ring",
                    () => ProceduralMeshFactory.CreateTorus("Сдоба", .09f, .045f, 22, 10));
                MeshObject("Завиток булочки", torus, crust, root, new Vector3(0f, .09f, 0f), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
                Shape(PrimitiveType.Sphere, "Сердцевина", new Vector3(0f, .09f, 0f), new Vector3(.095f, .055f, .095f), crust, root);
                for (var i = 0; i < 8; i++)
                {
                    var angle = i / 8f * Mathf.PI * 2f;
                    Shape(PrimitiveType.Sphere, "Мак", new Vector3(Mathf.Cos(angle) * .06f, .15f, Mathf.Sin(angle) * .06f), Vector3.one * .009f, ink, root);
                }
            }
            else if (product.Id == "water" || product.Id == "lemonade")
            {
                var profile = new[]
                {
                    new Vector2(.075f, 0f),
                    new Vector2(.082f, .018f),
                    new Vector2(.082f, .24f),
                    new Vector2(.07f, .29f),
                    new Vector2(.038f, .325f),
                    new Vector2(.03f, .39f)
                };
                var bottleMesh = SharedMesh("product:bottle", () => ProceduralMeshFactory.CreateLathe("Бутылка", profile, 20));
                var liquidColor = product.Id == "water"
                    ? new Color(.47f, .72f, .82f, .34f)
                    : new Color(.8f, .55f, .12f, .58f);
                var bottleMaterial = GlassMaterial(product.Id + " bottle", liquidColor, .9f);
                MeshObject("Бутылка", bottleMesh, bottleMaterial, root, Vector3.zero, Quaternion.identity, Vector3.one);
                Shape(PrimitiveType.Cylinder, "Крышка", new Vector3(0f, .405f, 0f), new Vector3(.034f, .018f, .034f), ink, root);
                ChamferedBox("Этикетка", root, new Vector3(0f, .2f, -.084f), new Vector3(.12f, .095f, .008f), .012f, crumb);
                ChamferedBox("Полоса этикетки", root, new Vector3(0f, .2f, -.089f), new Vector3(.085f, .016f, .005f), .004f, ink);
            }
            else if (product.Id.StartsWith("sig_", StringComparison.Ordinal))
            {
                ChamferedBox("Пачка", root, new Vector3(0f, .095f, 0f), new Vector3(.105f, .19f, .064f), .012f, crust);
                ChamferedBox("Фольга", root, new Vector3(0f, .164f, -.034f), new Vector3(.086f, .036f, .008f), .004f, MetalMaterial("Фольга", new Color(.65f, .65f, .58f), .72f));
                ChamferedBox("Марка", root, new Vector3(0f, .09f, -.036f), new Vector3(.078f, .055f, .007f), .008f, crumb);
                ChamferedBox("Логотип", root, new Vector3(0f, .09f, -.041f), new Vector3(.05f, .012f, .004f), .003f, ink);
            }
            else if (product.Id == "gum")
            {
                ChamferedBox("Жвачка", root, new Vector3(0f, .04f, 0f), new Vector3(.14f, .075f, .028f), .012f, crust);
                ChamferedBox("Этикетка", root, new Vector3(0f, .04f, -.016f), new Vector3(.095f, .032f, .006f), .008f, crumb);
            }
            else
            {
                ChamferedBox("Упаковка", root, new Vector3(0f, .08f, 0f), new Vector3(.14f, .16f, .08f), .015f, crust);
            }
        }

        public static void MarkStatic(GameObject root)
        {
            if (root == null) return;
            root.isStatic = true;
            foreach (Transform child in root.transform) MarkStatic(child.gameObject);
        }

        public static void TrackGenerated(UnityEngine.Object resource)
        {
            if (resource != null && !GeneratedResources.Contains(resource)) GeneratedResources.Add(resource);
        }

        private static Texture2D CreateWeatheredTexture(string name)
        {
            const int resolution = 64;
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, true)
            {
                name = (name ?? "Material") + " Weathering",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            var pixels = new Color[resolution * resolution];
            var seed = StableHash(name ?? string.Empty);
            var random = new System.Random(seed);
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var grain = .82f + (float)random.NextDouble() * .18f;
                    var broad = Mathf.PerlinNoise((x + seed % 23) * .09f, (y + seed % 31) * .09f) * .13f;
                    var scratches = (x * 13 + y * 7 + seed) % 97 == 0 ? .18f : 0f;
                    pixels[y * resolution + x] = Color.white * Mathf.Clamp01(grain - broad - scratches);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            TrackGenerated(texture);
            return texture;
        }

        private static void ConfigureTransparent(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            if (material.HasProperty("_Cull")) material.SetInt("_Cull", (int)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static string BuildMaterialKey(
            string name,
            Color color,
            float smooth,
            bool weathered,
            float glow,
            float metallic,
            bool transparent)
        {
            var color32 = (Color32)color;
            return string.Concat(
                name ?? string.Empty,
                "|",
                color32.r, ",", color32.g, ",", color32.b, ",", color32.a,
                "|", Mathf.RoundToInt(smooth * 1000f),
                "|", weathered ? "1" : "0",
                "|", Mathf.RoundToInt(glow * 1000f),
                "|", Mathf.RoundToInt(metallic * 1000f),
                "|", transparent ? "1" : "0");
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return hash;
            }
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
