using System;
using UnityEngine;

namespace WarmBread
{
    public sealed class CustomerVisualRig
    {
        public Transform Root;
        public Transform Head;
        public Transform LeftArm;
        public Transform RightArm;
        public Transform LeftLeg;
        public Transform RightLeg;
        public Transform Accessory;
    }

    public static class ModelFactory
    {
        public static CustomerVisualRig BuildCustomer(Transform parent, CustomerData data)
        {
            var id = data != null ? data.Id : "customer";
            var seed = StableHash(id);
            var random = new System.Random(seed);
            var displayName = data != null ? data.DisplayName : "Покупатель";
            var coatColor = data != null ? data.Coat : new Color(.3f, .34f, .36f);
            var skinTone = Color.Lerp(
                new Color(.52f, .34f, .24f),
                new Color(.84f, .67f, .52f),
                .3f + (float)random.NextDouble() * .55f);
            var hairColor = Color.Lerp(
                new Color(.06f, .045f, .035f),
                new Color(.35f, .23f, .12f),
                (float)random.NextDouble());
            var trousersColor = Color.Lerp(new Color(.08f, .095f, .11f), new Color(.2f, .23f, .25f), (float)random.NextDouble());
            var shoeColor = new Color(.045f, .05f, .055f);
            var scarfColor = Color.HSVToRGB((float)random.NextDouble(), .45f, .58f);

            var coat = WorldArt.Material("Пальто " + id, coatColor, .22f, true);
            var coatDark = WorldArt.Material("Тени пальто " + id, Color.Lerp(coatColor, Color.black, .28f), .16f, true);
            var skin = WorldArt.Material("Кожа " + id, skinTone, .28f);
            var hair = WorldArt.Material("Волосы " + id, hairColor, .12f, true);
            var trousers = WorldArt.Material("Брюки " + id, trousersColor, .16f, true);
            var shoes = WorldArt.Material("Обувь " + id, shoeColor, .3f, true);
            var scarf = WorldArt.Material("Шарф " + id, scarfColor, .18f, true);
            var eyeWhite = WorldArt.Material("Белки глаз", new Color(.82f, .79f, .7f), .25f);
            var eyeDark = WorldArt.Material("Глаза " + id, new Color(.04f, .055f, .05f), .28f);

            var visual = new GameObject("Модель • " + displayName).transform;
            visual.SetParent(parent, false);

            var rig = new CustomerVisualRig { Root = visual };

            var torso = WorldArt.ChamferedBox(
                "Пальто",
                visual,
                new Vector3(0f, 1.08f, 0f),
                new Vector3(.52f, .76f, .32f),
                .08f,
                coat);
            torso.transform.localScale = new Vector3(1f, 1f, .95f);

            WorldArt.ChamferedBox(
                "Нижняя пола пальто",
                visual,
                new Vector3(0f, .72f, .01f),
                new Vector3(.58f, .36f, .35f),
                .07f,
                coatDark);

            WorldArt.ChamferedBox(
                "Планка пальто",
                visual,
                new Vector3(0f, 1.12f, -.168f),
                new Vector3(.035f, .56f, .012f),
                .006f,
                coatDark);

            for (var button = 0; button < 3; button++)
            {
                WorldArt.Shape(
                    PrimitiveType.Sphere,
                    "Пуговица",
                    new Vector3(0f, 1.3f - button * .18f, -.181f),
                    Vector3.one * .026f,
                    shoes,
                    visual);
            }

            WorldArt.ChamferedBox(
                "Шарф",
                visual,
                new Vector3(0f, 1.47f, -.04f),
                new Vector3(.35f, .09f, .34f),
                .035f,
                scarf);
            WorldArt.ChamferedBox(
                "Конец шарфа",
                visual,
                new Vector3(.12f, 1.18f, -.19f),
                new Vector3(.09f, .42f, .025f),
                .02f,
                scarf);

            var neck = WorldArt.Shape(
                PrimitiveType.Cylinder,
                "Шея",
                new Vector3(0f, 1.5f, 0f),
                new Vector3(.095f, .11f, .095f),
                skin,
                visual);
            neck.transform.localRotation = Quaternion.identity;

            var headRoot = new GameObject("Голова pivot").transform;
            headRoot.SetParent(visual, false);
            headRoot.localPosition = new Vector3(0f, 1.7f, 0f);
            rig.Head = headRoot;

            WorldArt.Shape(
                PrimitiveType.Sphere,
                "Голова",
                Vector3.zero,
                new Vector3(.31f, .36f, .29f),
                skin,
                headRoot);
            WorldArt.Shape(
                PrimitiveType.Sphere,
                "Нос",
                new Vector3(0f, .005f, -.158f),
                new Vector3(.045f, .055f, .065f),
                skin,
                headRoot);

            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.Shape(
                    PrimitiveType.Sphere,
                    "Ухо",
                    new Vector3(side * .165f, .005f, 0f),
                    new Vector3(.04f, .065f, .035f),
                    skin,
                    headRoot);
                WorldArt.Shape(
                    PrimitiveType.Sphere,
                    "Белок глаза",
                    new Vector3(side * .066f, .055f, -.151f),
                    new Vector3(.035f, .027f, .018f),
                    eyeWhite,
                    headRoot);
                WorldArt.Shape(
                    PrimitiveType.Sphere,
                    "Зрачок",
                    new Vector3(side * .066f, .055f, -.169f),
                    new Vector3(.014f, .014f, .008f),
                    eyeDark,
                    headRoot);
                var brow = WorldArt.ChamferedBox(
                    "Бровь",
                    headRoot,
                    new Vector3(side * .066f, .105f, -.163f),
                    new Vector3(.075f, .012f, .012f),
                    .004f,
                    hair);
                brow.transform.localRotation = Quaternion.Euler(0f, 0f, side * -4f);
            }

            WorldArt.ChamferedBox(
                "Рот",
                headRoot,
                new Vector3(0f, -.08f, -.163f),
                new Vector3(.075f, .012f, .01f),
                .005f,
                WorldArt.Material("Губы " + id, Color.Lerp(skinTone, new Color(.45f, .12f, .12f), .35f), .2f));

            var headwear = Math.Abs(seed) % 4;
            if (headwear == 0)
            {
                WorldArt.Shape(
                    PrimitiveType.Sphere,
                    "Вязаная шапка",
                    new Vector3(0f, .17f, .01f),
                    new Vector3(.34f, .19f, .31f),
                    scarf,
                    headRoot);
                WorldArt.Shape(
                    PrimitiveType.Sphere,
                    "Помпон",
                    new Vector3(0f, .35f, .02f),
                    Vector3.one * .085f,
                    scarf,
                    headRoot);
            }
            else if (headwear == 1)
            {
                WorldArt.Shape(
                    PrimitiveType.Sphere,
                    "Волосы",
                    new Vector3(0f, .145f, .02f),
                    new Vector3(.33f, .18f, .3f),
                    hair,
                    headRoot);
                for (var i = -2; i <= 2; i++)
                {
                    WorldArt.Shape(
                        PrimitiveType.Capsule,
                        "Прядь",
                        new Vector3(i * .055f, .02f + Mathf.Abs(i) * .012f, -.145f),
                        new Vector3(.04f, .11f, .035f),
                        hair,
                        headRoot);
                }
            }
            else if (headwear == 2)
            {
                WorldArt.ChamferedBox(
                    "Кепка",
                    headRoot,
                    new Vector3(0f, .185f, 0f),
                    new Vector3(.34f, .11f, .31f),
                    .045f,
                    coatDark);
                WorldArt.ChamferedBox(
                    "Козырёк",
                    headRoot,
                    new Vector3(0f, .15f, -.195f),
                    new Vector3(.24f, .035f, .14f),
                    .018f,
                    coatDark);
            }
            else
            {
                WorldArt.Shape(
                    PrimitiveType.Sphere,
                    "Волосы",
                    new Vector3(0f, .13f, .02f),
                    new Vector3(.33f, .19f, .3f),
                    hair,
                    headRoot);
            }

            rig.LeftArm = BuildArm(visual, -1, coat, skin);
            rig.RightArm = BuildArm(visual, 1, coat, skin);
            rig.LeftLeg = BuildLeg(visual, -1, trousers, shoes);
            rig.RightLeg = BuildLeg(visual, 1, trousers, shoes);

            if (Math.Abs(seed) % 3 == 0)
            {
                var bag = new GameObject("Сумка").transform;
                bag.SetParent(visual, false);
                bag.localPosition = new Vector3(.34f, .82f, .02f);
                WorldArt.ChamferedBox(
                    "Корпус сумки",
                    bag,
                    Vector3.zero,
                    new Vector3(.28f, .34f, .14f),
                    .04f,
                    WorldArt.Material("Сумка " + id, Color.Lerp(coatColor, Color.black, .46f), .3f, true));
                var strap = WorldArt.SharedMesh(
                    "model:bag-strap",
                    () => ProceduralMeshFactory.CreateTorus("Ремень сумки", .28f, .015f, 24, 6));
                WorldArt.MeshObject(
                    "Ремень",
                    strap,
                    coatDark,
                    bag,
                    new Vector3(-.16f, .28f, 0f),
                    Quaternion.Euler(90f, 0f, 20f),
                    new Vector3(.75f, 1.2f, .6f));
                rig.Accessory = bag;
            }
            else if (Math.Abs(seed) % 5 == 0)
            {
                var umbrella = new GameObject("Сложенный зонт").transform;
                umbrella.SetParent(rig.RightArm, false);
                umbrella.localPosition = new Vector3(0f, -.56f, -.03f);
                WorldArt.Shape(
                    PrimitiveType.Cylinder,
                    "Трость зонта",
                    Vector3.zero,
                    new Vector3(.018f, .36f, .018f),
                    WorldArt.MetalMaterial("Зонт металл", new Color(.22f, .24f, .24f)),
                    umbrella);
                WorldArt.Shape(
                    PrimitiveType.Cone,
                    "Ткань зонта",
                    new Vector3(0f, -.31f, 0f),
                    new Vector3(.09f, .23f, .09f),
                    scarf,
                    umbrella);
                rig.Accessory = umbrella;
            }

            if (data != null && data.Child)
            {
                visual.localScale = Vector3.one * .8f;
            }

            return rig;
        }

        private static Transform BuildArm(Transform parent, int side, Material coat, Material skin)
        {
            var pivot = new GameObject(side < 0 ? "Левая рука pivot" : "Правая рука pivot").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(side * .32f, 1.34f, 0f);

            var upper = WorldArt.ChamferedBox(
                "Рукав",
                pivot,
                new Vector3(side * .04f, -.24f, 0f),
                new Vector3(.17f, .48f, .2f),
                .06f,
                coat);
            upper.transform.localRotation = Quaternion.Euler(0f, 0f, side * -4f);
            WorldArt.Shape(
                PrimitiveType.Sphere,
                "Кисть",
                new Vector3(side * .075f, -.51f, -.015f),
                new Vector3(.13f, .16f, .12f),
                skin,
                pivot);
            return pivot;
        }

        private static Transform BuildLeg(Transform parent, int side, Material trousers, Material shoes)
        {
            var pivot = new GameObject(side < 0 ? "Левая нога pivot" : "Правая нога pivot").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(side * .145f, .7f, 0f);

            WorldArt.ChamferedBox(
                "Брюки",
                pivot,
                new Vector3(0f, -.3f, 0f),
                new Vector3(.21f, .62f, .23f),
                .055f,
                trousers);
            WorldArt.ChamferedBox(
                "Ботинок",
                pivot,
                new Vector3(0f, -.65f, -.055f),
                new Vector3(.23f, .14f, .36f),
                .05f,
                shoes);
            return pivot;
        }

        public static GameObject BuildCar(
            Transform parent,
            string name,
            Vector3 position,
            Color bodyColor,
            bool collider,
            bool moving)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            var paint = WorldArt.Material(name + " paint", bodyColor, .54f, true, 0f, .18f);
            var paintDark = WorldArt.Material(name + " dark paint", Color.Lerp(bodyColor, Color.black, .34f), .48f, true, 0f, .2f);
            var chrome = WorldArt.MetalMaterial("Авто хром", new Color(.58f, .61f, .58f), .75f);
            var rubber = WorldArt.Material("Авто резина", new Color(.025f, .03f, .03f), .18f, true);
            var glass = WorldArt.GlassMaterial("Авто стекло", new Color(.17f, .28f, .32f, .42f), .94f);
            var headlight = WorldArt.Material("Фары", new Color(1f, .84f, .48f), .7f, false, 1.2f);
            var tail = WorldArt.Material("Задние фонари", new Color(.72f, .08f, .04f), .65f, false, .75f);

            WorldArt.ChamferedBox(
                "Кузов",
                root.transform,
                new Vector3(0f, .55f, 0f),
                new Vector3(3.85f, .62f, 1.55f),
                .18f,
                paint,
                collider);
            WorldArt.ChamferedBox(
                "Капот",
                root.transform,
                new Vector3(1.35f, .88f, 0f),
                new Vector3(1.1f, .24f, 1.42f),
                .12f,
                paint);
            WorldArt.ChamferedBox(
                "Багажник",
                root.transform,
                new Vector3(-1.45f, .85f, 0f),
                new Vector3(.82f, .26f, 1.42f),
                .11f,
                paint);
            WorldArt.ChamferedBox(
                "Кабина",
                root.transform,
                new Vector3(-.12f, 1.13f, 0f),
                new Vector3(1.95f, .72f, 1.38f),
                .2f,
                paintDark);
            WorldArt.ChamferedBox(
                "Лобовое стекло",
                root.transform,
                new Vector3(.75f, 1.17f, 0f),
                new Vector3(.035f, .49f, 1.17f),
                .012f,
                glass);
            WorldArt.ChamferedBox(
                "Заднее стекло",
                root.transform,
                new Vector3(-1.0f, 1.17f, 0f),
                new Vector3(.035f, .45f, 1.15f),
                .012f,
                glass);

            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.ChamferedBox(
                    "Боковое стекло переднее",
                    root.transform,
                    new Vector3(.35f, 1.18f, side * .7f),
                    new Vector3(.72f, .46f, .025f),
                    .04f,
                    glass);
                WorldArt.ChamferedBox(
                    "Боковое стекло заднее",
                    root.transform,
                    new Vector3(-.55f, 1.18f, side * .7f),
                    new Vector3(.72f, .46f, .025f),
                    .04f,
                    glass);
                WorldArt.ChamferedBox(
                    "Дверная ручка",
                    root.transform,
                    new Vector3(.05f, .91f, side * .795f),
                    new Vector3(.18f, .035f, .025f),
                    .01f,
                    chrome);
                WorldArt.ChamferedBox(
                    "Зеркало",
                    root.transform,
                    new Vector3(.68f, 1.14f, side * .87f),
                    new Vector3(.16f, .1f, .08f),
                    .025f,
                    paintDark);

                BuildWheel(root.transform, new Vector3(1.15f, .32f, side * .79f), rubber, chrome, side);
                BuildWheel(root.transform, new Vector3(-1.18f, .32f, side * .79f), rubber, chrome, side);
            }

            WorldArt.ChamferedBox(
                "Передний бампер",
                root.transform,
                new Vector3(1.98f, .43f, 0f),
                new Vector3(.12f, .15f, 1.62f),
                .035f,
                chrome);
            WorldArt.ChamferedBox(
                "Задний бампер",
                root.transform,
                new Vector3(-1.98f, .43f, 0f),
                new Vector3(.12f, .15f, 1.62f),
                .035f,
                chrome);

            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.ChamferedBox(
                    "Фара",
                    root.transform,
                    new Vector3(1.96f, .68f, side * .47f),
                    new Vector3(.035f, .18f, .31f),
                    .025f,
                    headlight);
                WorldArt.ChamferedBox(
                    "Задний фонарь",
                    root.transform,
                    new Vector3(-1.96f, .67f, side * .5f),
                    new Vector3(.035f, .16f, .24f),
                    .022f,
                    tail);
            }

            WorldArt.ChamferedBox(
                "Номер",
                root.transform,
                new Vector3(2.045f, .43f, 0f),
                new Vector3(.018f, .13f, .42f),
                .01f,
                WorldArt.Material("Номерной знак", new Color(.78f, .77f, .66f), .42f));

            if (moving)
            {
                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
            }

            return root;
        }

        private static void BuildWheel(Transform parent, Vector3 position, Material rubber, Material chrome, int side)
        {
            var wheel = new GameObject("Колесо").transform;
            wheel.SetParent(parent, false);
            wheel.localPosition = position;
            wheel.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var tireMesh = WorldArt.SharedMesh(
                "model:car-tire",
                () => ProceduralMeshFactory.CreateTorus("Автомобильная шина", .24f, .09f, 24, 10));
            WorldArt.MeshObject("Шина", tireMesh, rubber, wheel, Vector3.zero, Quaternion.identity, Vector3.one);
            WorldArt.Shape(
                PrimitiveType.Cylinder,
                "Колпак",
                Vector3.zero,
                new Vector3(.14f, .055f, .14f),
                chrome,
                wheel);
            wheel.localScale = new Vector3(1f, side < 0 ? -1f : 1f, 1f);
        }

        public static GameObject BuildBench(Transform parent, Vector3 position, Material wood, Material metal)
        {
            var root = new GameObject("Старая скамейка");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            for (var index = 0; index < 4; index++)
            {
                WorldArt.ChamferedBox(
                    "Доска сиденья",
                    root.transform,
                    new Vector3(0f, .52f, -.24f + index * .16f),
                    new Vector3(2.15f, .075f, .13f),
                    .022f,
                    wood);
            }

            for (var index = 0; index < 3; index++)
            {
                WorldArt.ChamferedBox(
                    "Доска спинки",
                    root.transform,
                    new Vector3(0f, .78f + index * .15f, .35f),
                    new Vector3(2.15f, .12f, .075f),
                    .022f,
                    wood);
            }

            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.ChamferedBox(
                    "Ножка",
                    root.transform,
                    new Vector3(side * .78f, .27f, .05f),
                    new Vector3(.1f, .55f, .56f),
                    .025f,
                    metal,
                    true);
                var back = WorldArt.ChamferedBox(
                    "Опора спинки",
                    root.transform,
                    new Vector3(side * .78f, .77f, .34f),
                    new Vector3(.085f, .9f, .085f),
                    .02f,
                    metal);
                back.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            }

            return root;
        }

        public static GameObject BuildTree(Transform parent, Vector3 position, int variant)
        {
            var root = new GameObject("Тополь " + variant);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            var bark = WorldArt.Material("Кора тополя", new Color(.24f, .22f, .17f), .1f, true);
            var leafBase = Color.Lerp(new Color(.22f, .28f, .13f), new Color(.43f, .38f, .12f), Mathf.Repeat(variant * .13f, 1f));
            var leaves = WorldArt.Material("Листва " + variant, leafBase, .08f, true);

            WorldArt.Shape(
                PrimitiveType.Cylinder,
                "Ствол",
                new Vector3(0f, 2.6f, 0f),
                new Vector3(.24f, 2.6f, .24f),
                bark,
                root.transform,
                true);

            for (var branch = 0; branch < 5; branch++)
            {
                var angle = branch / 5f * Mathf.PI * 2f;
                var limb = WorldArt.Shape(
                    PrimitiveType.Cylinder,
                    "Ветка",
                    new Vector3(Mathf.Cos(angle) * .4f, 4.4f + branch * .22f, Mathf.Sin(angle) * .4f),
                    new Vector3(.07f, 1.25f, .07f),
                    bark,
                    root.transform);
                limb.transform.localRotation = Quaternion.Euler(25f, -angle * Mathf.Rad2Deg, 32f);
            }

            var random = new System.Random(variant * 137 + 91);
            for (var cluster = 0; cluster < 11; cluster++)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var radius = .25f + (float)random.NextDouble() * 1.25f;
                var height = 4.5f + (float)random.NextDouble() * 3.3f;
                WorldArt.Shape(
                    PrimitiveType.Sphere,
                    "Крона",
                    new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius),
                    new Vector3(1.35f, 1.85f, 1.2f) * (.75f + (float)random.NextDouble() * .45f),
                    leaves,
                    root.transform);
            }

            return root;
        }

        public static GameObject BuildStreetLamp(Transform parent, Vector3 position, Material metal, Material shade)
        {
            var root = new GameObject("Уличный фонарь");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            WorldArt.Shape(PrimitiveType.Cylinder, "Столб", new Vector3(0f, 3.2f, 0f), new Vector3(.11f, 3.2f, .11f), metal, root.transform, true);
            var arm = WorldArt.Shape(PrimitiveType.Cylinder, "Кронштейн", new Vector3(.38f, 6.17f, 0f), new Vector3(.055f, .48f, .055f), metal, root.transform);
            arm.transform.localRotation = Quaternion.Euler(0f, 0f, 62f);
            WorldArt.ChamferedBox("Плафон", root.transform, new Vector3(.72f, 6.35f, 0f), new Vector3(.62f, .18f, .46f), .065f, shade);
            WorldArt.Lamp("Свет фонаря", new Vector3(.72f, 6.15f, 0f), new Color(1f, .72f, .34f), 1.45f, 12f, root.transform);
            return root;
        }

        public static GameObject BuildCashRegister(Transform parent, Vector3 position, Material enamel, Material dark, Material glass)
        {
            var root = new GameObject("Касса Электроника");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            WorldArt.ChamferedBox("Корпус", root.transform, Vector3.zero, new Vector3(.62f, .27f, .48f), .06f, enamel, true);
            var display = WorldArt.ChamferedBox("Дисплей", root.transform, new Vector3(0f, .12f, -.247f), new Vector3(.4f, .1f, .018f), .015f, glass);
            display.transform.localRotation = Quaternion.Euler(-4f, 0f, 0f);
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    WorldArt.ChamferedBox(
                        "Клавиша",
                        root.transform,
                        new Vector3(-.18f + column * .12f, .155f, -.08f + row * .09f),
                        new Vector3(.085f, .025f, .065f),
                        .014f,
                        column == 3 ? enamel : dark);
                }
            }
            WorldArt.ChamferedBox("Денежный ящик", root.transform, new Vector3(0f, -.09f, -.252f), new Vector3(.48f, .08f, .018f), .01f, dark);
            return root;
        }

        public static GameObject BuildRadio(Transform parent, Vector3 position, Material wood, Material metal, Material dark)
        {
            var root = new GameObject("Радиоприёмник");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            WorldArt.ChamferedBox("Корпус", root.transform, Vector3.zero, new Vector3(.62f, .4f, .28f), .065f, wood, true);
            WorldArt.ChamferedBox("Шкала", root.transform, new Vector3(.1f, .09f, -.147f), new Vector3(.34f, .08f, .014f), .01f, WorldArt.GlassMaterial("Стекло радио", new Color(.52f, .46f, .24f, .5f)));
            WorldArt.ChamferedBox("Индикатор", root.transform, new Vector3(.02f, .09f, -.157f), new Vector3(.012f, .065f, .008f), .003f, WorldArt.Material("Красный индикатор", new Color(.7f, .12f, .05f), .4f, false, .8f));

            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 6; column++)
                {
                    WorldArt.Shape(
                        PrimitiveType.Cylinder,
                        "Отверстие динамика",
                        new Vector3(-.2f + column * .045f, .07f - row * .045f, -.153f),
                        new Vector3(.009f, .006f, .009f),
                        dark,
                        root.transform);
                }
            }

            WorldArt.Shape(PrimitiveType.Cylinder, "Ручка громкости", new Vector3(.23f, -.09f, -.16f), new Vector3(.045f, .025f, .045f), metal, root.transform);
            var antenna = WorldArt.Shape(PrimitiveType.Cylinder, "Антенна", new Vector3(-.2f, .58f, .02f), new Vector3(.008f, .42f, .008f), metal, root.transform);
            antenna.transform.localRotation = Quaternion.Euler(0f, 0f, -17f);
            return root;
        }

        public static GameObject BuildKettle(Transform parent, Vector3 position, Material enamel, Material metal, Material dark)
        {
            var root = new GameObject("Эмалированный чайник");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            var profile = new[]
            {
                new Vector2(.16f, 0f),
                new Vector2(.2f, .04f),
                new Vector2(.22f, .22f),
                new Vector2(.18f, .34f),
                new Vector2(.11f, .38f)
            };
            var bodyMesh = WorldArt.SharedMesh("model:kettle-body", () => ProceduralMeshFactory.CreateLathe("Корпус чайника", profile, 24));
            WorldArt.MeshObject("Корпус", bodyMesh, enamel, root.transform, Vector3.zero, Quaternion.identity, Vector3.one, true);
            WorldArt.Shape(PrimitiveType.Cylinder, "Крышка", new Vector3(0f, .4f, 0f), new Vector3(.13f, .025f, .13f), metal, root.transform);
            WorldArt.Shape(PrimitiveType.Sphere, "Ручка крышки", new Vector3(0f, .45f, 0f), Vector3.one * .045f, dark, root.transform);

            var spout = WorldArt.Shape(PrimitiveType.Cylinder, "Носик", new Vector3(.25f, .27f, 0f), new Vector3(.075f, .23f, .075f), enamel, root.transform);
            spout.transform.localRotation = Quaternion.Euler(0f, 0f, -58f);

            var handleMesh = WorldArt.SharedMesh("model:kettle-handle", () => ProceduralMeshFactory.CreateTorus("Ручка чайника", .24f, .025f, 24, 8));
            var handle = WorldArt.MeshObject("Ручка", handleMesh, dark, root.transform, new Vector3(-.1f, .25f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(1f, 1.15f, 1f));
            handle.transform.localScale = new Vector3(.85f, 1.15f, 1f);
            return root;
        }

        public static GameObject BuildPhone(Transform parent, Vector3 position, Material bodyMaterial, Material dark)
        {
            var root = new GameObject("Дисковый телефон");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            WorldArt.ChamferedBox("Основание", root.transform, Vector3.zero, new Vector3(.46f, .18f, .36f), .065f, bodyMaterial, true);
            var disk = WorldArt.SharedMesh("model:phone-dial", () => ProceduralMeshFactory.CreateTorus("Диск телефона", .095f, .025f, 20, 8));
            WorldArt.MeshObject("Наборный диск", disk, dark, root.transform, new Vector3(0f, .105f, -.08f), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
            WorldArt.Shape(PrimitiveType.Cylinder, "Центр диска", new Vector3(0f, .11f, -.08f), new Vector3(.05f, .018f, .05f), bodyMaterial, root.transform);

            var handset = new GameObject("Трубка").transform;
            handset.SetParent(root.transform, false);
            handset.localPosition = new Vector3(0f, .2f, 0f);
            WorldArt.ChamferedBox("Рукоять", handset, Vector3.zero, new Vector3(.32f, .065f, .08f), .025f, dark);
            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.Shape(PrimitiveType.Sphere, "Динамик", new Vector3(side * .18f, 0f, 0f), new Vector3(.11f, .09f, .11f), dark, handset);
            }
            return root;
        }

        public static GameObject BuildCat(Transform parent, Vector3 position)
        {
            var root = new GameObject("Кот Плюш");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            var orange = WorldArt.Material("Шерсть Плюша", new Color(.68f, .37f, .14f), .2f, true);
            var light = WorldArt.Material("Светлая шерсть Плюша", new Color(.82f, .61f, .34f), .2f, true);
            var dark = WorldArt.Material("Глаза Плюша", new Color(.05f, .06f, .045f), .3f);
            var green = WorldArt.Material("Зелёные глаза Плюша", new Color(.36f, .62f, .27f), .55f);

            WorldArt.Shape(PrimitiveType.Sphere, "Туловище", new Vector3(-.08f, .14f, 0f), new Vector3(.46f, .25f, .28f), orange, root.transform, true);
            WorldArt.Shape(PrimitiveType.Sphere, "Голова", new Vector3(.24f, .22f, -.03f), new Vector3(.23f, .21f, .22f), orange, root.transform);
            WorldArt.Shape(PrimitiveType.Sphere, "Грудка", new Vector3(.12f, .13f, -.14f), new Vector3(.18f, .17f, .08f), light, root.transform);

            for (var side = -1; side <= 1; side += 2)
            {
                var ear = WorldArt.Shape(PrimitiveType.Cone, "Ухо", new Vector3(.24f + side * .095f, .43f, -.02f), new Vector3(.085f, .13f, .075f), orange, root.transform);
                ear.transform.localRotation = Quaternion.Euler(0f, 0f, side * -12f);
                WorldArt.Shape(PrimitiveType.Sphere, "Глаз", new Vector3(.24f + side * .065f, .25f, -.19f), new Vector3(.036f, .045f, .018f), green, root.transform);
                WorldArt.Shape(PrimitiveType.Sphere, "Зрачок", new Vector3(.24f + side * .065f, .25f, -.208f), new Vector3(.012f, .034f, .007f), dark, root.transform);
                WorldArt.Line(
                    "Усы",
                    new[]
                    {
                        new Vector3(.25f + side * .045f, .16f, -.2f),
                        new Vector3(.25f + side * .27f, .13f, -.22f)
                    },
                    .006f,
                    light,
                    root.transform);
            }

            WorldArt.Shape(PrimitiveType.Sphere, "Нос", new Vector3(.24f, .175f, -.215f), Vector3.one * .028f, dark, root.transform);
            var tailMesh = WorldArt.SharedMesh("model:cat-tail", () => ProceduralMeshFactory.CreateTorus("Хвост", .24f, .045f, 24, 8));
            WorldArt.MeshObject("Хвост", tailMesh, orange, root.transform, new Vector3(-.36f, .15f, .05f), Quaternion.Euler(90f, 0f, 25f), new Vector3(1f, .85f, 1f));
            return root;
        }

        public static GameObject BuildCrate(Transform parent, Vector3 position, Vector3 size, Material wood)
        {
            var root = new GameObject("Деревянный ящик");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            WorldArt.ChamferedBox("Основа", root.transform, Vector3.zero, size, .025f, wood, true);
            var strip = WorldArt.Material("Рейки ящика", Color.Lerp(new Color(.42f, .27f, .13f), Color.black, .12f), .12f, true);
            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.ChamferedBox("Вертикальная рейка", root.transform, new Vector3(side * size.x * .38f, 0f, -size.z * .51f), new Vector3(.06f, size.y * .92f, .025f), .008f, strip);
            }
            WorldArt.ChamferedBox("Горизонтальная рейка", root.transform, new Vector3(0f, 0f, -size.z * .51f), new Vector3(size.x * .92f, .055f, .025f), .008f, strip);
            return root;
        }

        public static GameObject BuildMug(Transform parent, Vector3 position, Color color)
        {
            var root = new GameObject("Эмалированная кружка");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            var enamel = WorldArt.Material("Эмаль кружки " + color, color, .45f, true);
            var profile = new[]
            {
                new Vector2(.065f, 0f),
                new Vector2(.072f, .015f),
                new Vector2(.075f, .15f),
                new Vector2(.078f, .165f)
            };
            var mugMesh = WorldArt.SharedMesh("model:mug", () => ProceduralMeshFactory.CreateLathe("Кружка", profile, 20, true, false));
            WorldArt.MeshObject("Чашка", mugMesh, enamel, root.transform, Vector3.zero, Quaternion.identity, Vector3.one);
            var handleMesh = WorldArt.SharedMesh("model:mug-handle", () => ProceduralMeshFactory.CreateTorus("Ручка кружки", .065f, .014f, 18, 6));
            WorldArt.MeshObject("Ручка", handleMesh, enamel, root.transform, new Vector3(.075f, .085f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(.75f, 1f, 1f));
            return root;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 23;
                value = value ?? string.Empty;
                for (var i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return hash;
            }
        }
    }
}
