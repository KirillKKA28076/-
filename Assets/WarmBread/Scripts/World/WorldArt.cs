using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace WarmBread
{
    public static class WorldArt
    {
        private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static TMP_FontAsset Font { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { materials.Clear(); Font = null; }
        public static void PrepareFont()
        {
            var source = Resources.Load<Font>("Fonts/BreadSans");
            if (source == null) source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Font = TMP_FontAsset.CreateFontAsset(source, 42, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            Font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            Font.isMultiAtlasTexturesEnabled = true;
        }
        public static Material Material(string name, Color color, float smooth = .15f, bool weathered = false, float glow = 0)
        {
            if (materials.TryGetValue(name, out var old)) return old;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name = name;
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", smooth);
            if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * glow); }
            if (weathered)
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGB24, true);
                var pixels = new Color[4096]; var random = new System.Random(2002);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float grain = .83f + (float)random.NextDouble() * .17f;
                    float stain = Mathf.PerlinNoise(x * .15f, y * .15f) * .15f;
                    pixels[y * 64 + x] = Color.white * (grain - stain);
                }
                texture.SetPixels(pixels); texture.Apply(); texture.wrapMode = TextureWrapMode.Repeat;
                m.SetTexture("_BaseMap", texture);
            }
            m.enableInstancing = true; materials[name] = m; return m;
        }
        public static GameObject Cube(string name, Vector3 pos, Vector3 size, Material material, Transform parent = null, bool collider = true)
        { return Shape(PrimitiveType.Cube, name, pos, size, material, parent, collider); }
        public static GameObject Part(string name, Transform parent, Vector3 pos, Vector3 size, Material material)
        { return Shape(PrimitiveType.Cube, name, pos, size, material, parent, false); }
        public static GameObject Shape(PrimitiveType type, string name, Vector3 pos, Vector3 size, Material material, Transform parent = null, bool collider = false)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = size;
            var renderer = g.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            if (!collider) { var c = g.GetComponent<Collider>(); c.enabled = false; Object.Destroy(c); }
            return g;
        }
        public static void Line(string name, Vector3[] points, float width, Material mat, Transform parent)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            var line = g.AddComponent<LineRenderer>(); line.positionCount = points.Length; line.SetPositions(points);
            line.startWidth = line.endWidth = width; line.sharedMaterial = mat; line.useWorldSpace = false; line.numCapVertices = 2;
        }
        public static TextMeshPro Label(string text, Vector3 position, float size, Color color, Transform parent, float yaw = 180)
        {
            var g = new GameObject("Надпись • " + text); g.transform.SetParent(parent, false);
            g.transform.localPosition = position; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var t = g.AddComponent<TextMeshPro>(); t.font = Font; t.text = text; t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center; t.color = color; t.rectTransform.sizeDelta = new Vector2(5, 2);
            t.enableWordWrapping = false; t.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return t;
        }
        public static Light Lamp(string name, Vector3 position, Color color, float intensity, float range, Transform parent)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = position;
            var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range;
            return l;
        }
        public static void Product(Transform parent, ProductData product, Vector3 position, float scale = 1)
        {
            var root = new GameObject(product.Title).transform; root.SetParent(parent, false); root.localPosition = position; root.localScale = Vector3.one * scale;
            var m = Material(product.Id, product.Tint, .22f, true);
            var pale = Material("Мука", new Color(.83f, .73f, .54f));
            if (product.Id == "water" || product.Id == "lemonade")
            {
                Shape(PrimitiveType.Cylinder, "Бутылка", new Vector3(0, .15f, 0), new Vector3(.13f, .15f, .13f), m, root);
                Shape(PrimitiveType.Cylinder, "Горлышко", new Vector3(0, .32f, 0), new Vector3(.05f, .045f, .05f), pale, root);
                Part("Этикетка", root, new Vector3(0, .16f, -.068f), new Vector3(.08f, .1f, .01f), pale);
            }
            else if (product.Id.StartsWith("sig_") || product.Id == "gum")
                Part("Упаковка", root, new Vector3(0, .08f, 0), new Vector3(.1f, .16f, .06f), m);
            else
            {
                Vector3 dims = product.Id == "bread_white" ? new Vector3(.4f,.14f,.18f) : product.Id == "bread_black" ? new Vector3(.28f,.18f,.2f) : new Vector3(.21f,.12f,.16f);
                Shape(PrimitiveType.Sphere, "Корочка", new Vector3(0, dims.y * .45f, 0), dims, m, root);
                for (int i = -1; i <= 1; i++)
                { var cut = Part("Надрез", root, new Vector3(i * .085f, dims.y * .84f, -.025f), new Vector3(.018f,.008f,.085f), pale); cut.transform.localRotation = Quaternion.Euler(0, -25, 0); }
            }
        }
    }
}
