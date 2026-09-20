using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WarmBread
{
    public sealed class WorldBuilder : MonoBehaviour
    {
        public Light Sun { get; private set; }

        private Transform street;
        private Transform interior;
        private Transform dynamicRoot;
        private Material concrete;
        private Material teal;
        private Material wood;
        private Material iron;
        private Material cream;
        private Material glass;
        private Material dark;
        private Material paper;
        private GameSession session;
        private GameUI ui;
        private Atmosphere atmosphere;

        public void Build(GameSession game, GameUI interfaceUI, Atmosphere mood)
        {
            session = game;
            ui = interfaceUI;
            atmosphere = mood;

            concrete = WorldArt.Material(
                "Шероховатый бетон",
                new Color(.46f, .47f, .43f),
                .08f,
                true);
            teal = WorldArt.Material(
                "Выцветшая бирюза",
                new Color(.22f, .37f, .35f),
                .2f,
                true);
            wood = WorldArt.Material(
                "Старая фанера",
                new Color(.38f, .25f, .15f),
                .2f,
                true);
            iron = WorldArt.MetalMaterial(
                "Тёмный металл",
                new Color(.12f, .16f, .16f),
                .42f);
            cream = WorldArt.Material(
                "Тёплая эмаль",
                new Color(.73f, .69f, .54f),
                .25f,
                true);
            glass = WorldArt.GlassMaterial(
                "Окна • сумерки",
                new Color(.18f, .27f, .3f, .42f),
                .9f);
            dark = WorldArt.Material(
                "Резина и бакелит",
                new Color(.055f, .065f, .063f),
                .25f,
                true);
            paper = WorldArt.Material(
                "Старая бумага",
                new Color(.79f, .73f, .57f),
                .18f,
                true);

            street = new GameObject("РАЙОН • сентябрь 2002").transform;
            street.SetParent(transform, false);
            interior = new GameObject("ЛАРЁК • Тёплый хлеб").transform;
            interior.SetParent(transform, false);
            dynamicRoot = new GameObject("ДИНАМИКА • транспорт и эффекты").transform;
            dynamicRoot.SetParent(transform, false);

            Environment();
            Kiosk();
            Lighting();

            WorldArt.MarkStatic(street.gameObject);
            Physics.SyncTransforms();

            var nav = street.gameObject.AddComponent<NavMeshSurface>();
            nav.collectObjects = CollectObjects.Children;
            nav.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            nav.layerMask = ~0;
            nav.BuildNavMesh();
        }

        private void Environment()
        {
            var asphalt = WorldArt.Material(
                "Мокрый асфальт",
                new Color(.19f, .22f, .215f),
                .72f,
                true);
            var puddle = WorldArt.GlassMaterial(
                "Лужи",
                new Color(.3f, .42f, .45f, .32f),
                .98f);
            var roadPaint = WorldArt.Material(
                "Старая дорожная краска",
                new Color(.69f, .66f, .5f),
                .2f,
                true);

            WorldArt.Cube(
                "Асфальт",
                new Vector3(0f, -.2f, 10f),
                new Vector3(80f, .3f, 65f),
                asphalt,
                street,
                true);
            WorldArt.Cube(
                "Тротуар",
                new Vector3(0f, 0f, 4.3f),
                new Vector3(44f, .2f, 6.8f),
                concrete,
                street,
                true);
            WorldArt.ChamferedBox(
                "Бордюр",
                street,
                new Vector3(0f, .08f, 7.65f),
                new Vector3(44f, .26f, .22f),
                .035f,
                cream,
                true);

            for (var x = -20; x <= 20; x += 4)
            {
                WorldArt.ChamferedBox(
                    "Стёртая разметка",
                    street,
                    new Vector3(x, -.035f, 12f),
                    new Vector3(2.15f, .014f, .11f),
                    .015f,
                    roadPaint);
            }

            for (var drain = -2; drain <= 2; drain++)
            {
                var x = drain * 8.2f;
                WorldArt.ChamferedBox(
                    "Ливневая решётка",
                    street,
                    new Vector3(x, .108f, 7.38f),
                    new Vector3(.65f, .025f, .34f),
                    .03f,
                    iron);
                for (var slot = -2; slot <= 2; slot++)
                {
                    WorldArt.ChamferedBox(
                        "Щель решётки",
                        street,
                        new Vector3(x + slot * .105f, .125f, 7.38f),
                        new Vector3(.025f, .012f, .26f),
                        .005f,
                        dark);
                }
            }

            var random = new System.Random(91);
            for (var index = 0; index < 34; index++)
            {
                var pool = WorldArt.Shape(
                    PrimitiveType.Cylinder,
                    "Лужа",
                    new Vector3(
                        (float)random.NextDouble() * 42f - 21f,
                        .107f,
                        2.7f + (float)random.NextDouble() * 5f),
                    new Vector3(
                        .35f + (float)random.NextDouble() * 1.4f,
                        .002f,
                        .2f + (float)random.NextDouble() * .8f),
                    puddle,
                    street);
                pool.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 180f, 0f);
            }

            Apartment(new Vector3(-14f, 0f, 24f), 17f, 5, 0);
            Apartment(new Vector3(9f, 0f, 25f), 20f, 5, 1);
            Apartment(new Vector3(-27f, 0f, 4f), 10f, 9, 2);
            Apartment(new Vector3(29f, 0f, 9f), 13f, 7, 3);
            Apartment(new Vector3(0f, 0f, 44f), 23f, 9, 4);

            for (var index = 0; index < 10; index++)
            {
                ModelFactory.BuildTree(street, new Vector3(-24f + index * 5.4f, 0f, 18f), index);
            }

            for (var index = -1; index <= 1; index++)
            {
                var x = index * 15f;
                ModelFactory.BuildStreetLamp(street, new Vector3(x, 0f, 7.5f), iron, cream);
                if (index < 1)
                {
                    WorldArt.Line(
                        "Провода",
                        new[]
                        {
                            new Vector3(x + .72f, 6.4f, 7.5f),
                            new Vector3(x + 7.5f, 5.65f, 7.5f),
                            new Vector3(x + 15f + .72f, 6.4f, 7.5f)
                        },
                        .024f,
                        iron,
                        street);
                }
            }

            ModelFactory.BuildBench(street, new Vector3(6f, 0f, 5.8f), wood, iron);
            ModelFactory.BuildBench(street, new Vector3(-8f, 0f, 5.8f), wood, iron);
            StreetBin(new Vector3(7.65f, 0f, 5.65f));
            StreetBin(new Vector3(-6.35f, 0f, 5.65f));

            ModelFactory.BuildCar(
                street,
                "ВАЗ • бордовый",
                new Vector3(-8f, 0f, 9f),
                new Color(.43f, .15f, .11f),
                true,
                false);
            var paleCar = ModelFactory.BuildCar(
                street,
                "Москвич • светлый",
                new Vector3(7f, 0f, 15f),
                new Color(.51f, .55f, .48f),
                true,
                false);
            paleCar.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var moving = ModelFactory.BuildCar(
                dynamicRoot,
                "Проезжающий седан",
                new Vector3(-25f, 0f, 11f),
                new Color(.31f, .42f, .43f),
                true,
                true);
            moving.AddComponent<PassingCar>();

            BusStop();
            NoticeBoard();
            Bicycle(new Vector3(12.3f, .1f, 5.1f));
            StreetDetails();
        }

        private void Apartment(Vector3 center, float width, int floors, int variant)
        {
            var root = new GameObject("Панельный дом " + (variant + 1)).transform;
            root.SetParent(street, false);
            root.localPosition = center;

            var height = floors * 2.75f;
            var wall = WorldArt.Material(
                "Бетон дома " + variant,
                Color.Lerp(new Color(.43f, .44f, .41f), new Color(.52f, .49f, .43f), variant * .11f),
                .08f,
                true);
            var trim = variant % 2 == 0 ? teal : cream;
            var warmWindow = WorldArt.Material(
                "Свет в окне",
                new Color(.86f, .61f, .29f),
                .2f,
                false,
                .8f);
            var curtain = WorldArt.Material(
                "Шторы",
                new Color(.48f, .22f, .15f),
                .15f,
                true);

            WorldArt.ChamferedBox(
                "Панельный блок",
                root,
                new Vector3(0f, height * .5f, 0f),
                new Vector3(width, height, 7f),
                .08f,
                wall,
                true);
            WorldArt.Cube(
                "Цоколь",
                new Vector3(0f, .7f, -3.55f),
                new Vector3(width, 1.4f, .13f),
                trim,
                root,
                false);

            for (var floor = 0; floor < floors; floor++)
            {
                WorldArt.Cube(
                    "Шов панелей",
                    new Vector3(0f, floor * 2.75f, -3.51f),
                    new Vector3(width, .025f, .03f),
                    iron,
                    root,
                    false);

                var columns = Mathf.Max(2, (int)(width / 2.4f));
                for (var column = 0; column < columns; column++)
                {
                    var x = -width * .5f + 1.5f + column * 2.4f;
                    var y = 2f + floor * 2.75f;
                    var lit = (column + floor * 3 + variant) % 7 == 0;

                    WorldArt.ChamferedBox(
                        "Оконный проём",
                        root,
                        new Vector3(x, y, -3.56f),
                        new Vector3(1.38f, 1.62f, .12f),
                        .035f,
                        cream);
                    WorldArt.ChamferedBox(
                        "Стекло",
                        root,
                        new Vector3(x, y, -3.63f),
                        new Vector3(1.17f, 1.4f, .024f),
                        .015f,
                        lit ? warmWindow : glass);
                    WorldArt.ChamferedBox(
                        "Вертикальная рама",
                        root,
                        new Vector3(x, y, -3.655f),
                        new Vector3(.045f, 1.4f, .04f),
                        .008f,
                        cream);
                    WorldArt.ChamferedBox(
                        "Горизонтальная рама",
                        root,
                        new Vector3(x, y, -3.655f),
                        new Vector3(1.17f, .045f, .04f),
                        .008f,
                        cream);
                    WorldArt.ChamferedBox(
                        "Подоконник",
                        root,
                        new Vector3(x, y - .76f, -3.7f),
                        new Vector3(1.35f, .065f, .18f),
                        .018f,
                        concrete);

                    if ((column + floor) % 4 == 0)
                    {
                        WorldArt.ChamferedBox(
                            "Штора",
                            root,
                            new Vector3(x - .36f, y, -3.67f),
                            new Vector3(.35f, 1.22f, .012f),
                            .012f,
                            curtain);
                    }

                    if (column % 3 == 0 && floor > 0)
                    {
                        Balcony(root, x, y, floor % 2 == 0 ? teal : cream);
                    }
                }
            }

            Entrance(root, height, variant);
            WorldArt.Shape(
                PrimitiveType.Cylinder,
                "Водосточная труба",
                new Vector3(width * .42f, height * .5f, -3.65f),
                new Vector3(.07f, height * .5f, .07f),
                iron,
                root);

            var antenna = WorldArt.Shape(
                PrimitiveType.Cylinder,
                "Антенна",
                new Vector3(-width * .28f, height + 1.5f, 0f),
                new Vector3(.045f, 1.5f, .045f),
                iron,
                root);
            antenna.transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            WorldArt.Line(
                "Антенна крестовина",
                new[]
                {
                    new Vector3(-width * .28f - .8f, height + 2.3f, 0f),
                    new Vector3(-width * .28f + .8f, height + 2.3f, 0f)
                },
                .035f,
                iron,
                root);
        }

        private void Balcony(Transform root, float x, float y, Material material)
        {
            WorldArt.ChamferedBox(
                "Балконная плита",
                root,
                new Vector3(x, y - .72f, -4.02f),
                new Vector3(1.85f, .12f, .9f),
                .025f,
                concrete);
            WorldArt.ChamferedBox(
                "Ограждение балкона",
                root,
                new Vector3(x, y - .35f, -4.43f),
                new Vector3(1.72f, .72f, .08f),
                .025f,
                material);
            for (var bar = -3; bar <= 3; bar++)
            {
                WorldArt.ChamferedBox(
                    "Балконная стойка",
                    root,
                    new Vector3(x + bar * .22f, y - .35f, -4.48f),
                    new Vector3(.035f, .67f, .035f),
                    .008f,
                    iron);
            }
        }

        private void Entrance(Transform root, float height, int variant)
        {
            WorldArt.ChamferedBox(
                "Портал подъезда",
                root,
                new Vector3(0f, 1.25f, -3.63f),
                new Vector3(1.8f, 2.5f, .18f),
                .04f,
                variant % 2 == 0 ? teal : cream);
            WorldArt.ChamferedBox(
                "Дверь подъезда",
                root,
                new Vector3(0f, 1.1f, -3.74f),
                new Vector3(1.25f, 2.2f, .08f),
                .035f,
                dark);
            WorldArt.ChamferedBox(
                "Стекло двери",
                root,
                new Vector3(0f, 1.55f, -3.79f),
                new Vector3(.8f, .82f, .025f),
                .02f,
                glass);
            WorldArt.ChamferedBox(
                "Козырёк подъезда",
                root,
                new Vector3(0f, 2.65f, -4.15f),
                new Vector3(2.45f, .16f, 1.65f),
                .045f,
                iron);
            WorldArt.ChamferedBox(
                "Табличка подъезда",
                root,
                new Vector3(.7f, 2.15f, -3.82f),
                new Vector3(.32f, .22f, .018f),
                .015f,
                paper);
            WorldArt.Label(
                (variant + 1).ToString(),
                new Vector3(.7f, 2.16f, -3.84f),
                1.2f,
                new Color(.16f, .2f, .18f),
                root,
                0f);
        }

        private void StreetBin(Vector3 position)
        {
            var root = new GameObject("Урна").transform;
            root.SetParent(street, false);
            root.localPosition = position;
            WorldArt.Shape(
                PrimitiveType.Cylinder,
                "Корпус урны",
                new Vector3(0f, .38f, 0f),
                new Vector3(.32f, .38f, .32f),
                iron,
                root,
                true);
            WorldArt.Shape(
                PrimitiveType.Cylinder,
                "Обод урны",
                new Vector3(0f, .77f, 0f),
                new Vector3(.36f, .045f, .36f),
                dark,
                root);
        }

        private void BusStop()
        {
            var root = new GameObject("Остановка • Садовая").transform;
            root.SetParent(street, false);
            root.localPosition = new Vector3(14f, 0f, 5.4f);

            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.ChamferedBox(
                    "Стойка остановки",
                    root,
                    new Vector3(side * 1.75f, 1.45f, 0f),
                    new Vector3(.11f, 2.9f, .11f),
                    .025f,
                    iron,
                    true);
            }

            WorldArt.ChamferedBox(
                "Заднее стекло остановки",
                root,
                new Vector3(0f, 1.45f, .08f),
                new Vector3(3.5f, 2.75f, .045f),
                .025f,
                glass);
            WorldArt.ChamferedBox(
                "Крыша остановки",
                root,
                new Vector3(0f, 2.9f, -.7f),
                new Vector3(4f, .14f, 1.7f),
                .055f,
                iron);
            WorldArt.ChamferedBox(
                "Лавка остановки",
                root,
                new Vector3(0f, .5f, -.2f),
                new Vector3(2.7f, .12f, .55f),
                .035f,
                wood,
                true);
            WorldArt.ChamferedBox(
                "Название остановки",
                root,
                new Vector3(0f, 2.5f, -.01f),
                new Vector3(2.6f, .42f, .055f),
                .035f,
                teal);
            WorldArt.Label(
                "УЛ. САДОВАЯ",
                new Vector3(0f, 2.5f, -.05f),
                2.5f,
                new Color(.85f, .82f, .67f),
                root,
                0f);

            for (var poster = 0; poster < 2; poster++)
            {
                WorldArt.ChamferedBox(
                    "Объявление",
                    root,
                    new Vector3(-.85f + poster * 1.7f, 1.55f, -.04f),
                    new Vector3(1.1f, 1.35f, .018f),
                    .012f,
                    poster == 0 ? paper : cream);
            }
        }

        private void NoticeBoard()
        {
            var root = new GameObject("Доска объявлений").transform;
            root.SetParent(street, false);
            root.localPosition = new Vector3(-12f, 0f, 5.5f);

            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.ChamferedBox(
                    "Опора доски",
                    root,
                    new Vector3(side * .85f, 1f, 0f),
                    new Vector3(.11f, 2f, .11f),
                    .02f,
                    iron,
                    true);
            }
            WorldArt.ChamferedBox(
                "Щит",
                root,
                new Vector3(0f, 1.65f, 0f),
                new Vector3(2.2f, 1.25f, .12f),
                .035f,
                wood);

            var colors = new[]
            {
                paper,
                cream,
                WorldArt.Material("Розовая бумага", new Color(.67f, .42f, .4f), .15f, true),
                WorldArt.Material("Синяя бумага", new Color(.34f, .46f, .52f), .15f, true)
            };
            for (var index = 0; index < 8; index++)
            {
                var x = -.72f + index % 4 * .48f;
                var y = 1.35f + index / 4 * .55f;
                var note = WorldArt.ChamferedBox(
                    "Объявление",
                    root,
                    new Vector3(x, y, -.075f),
                    new Vector3(.39f, .43f, .012f),
                    .012f,
                    colors[index % colors.Length]);
                note.transform.localRotation = Quaternion.Euler(0f, 0f, (index - 3) * 1.7f);
            }
        }

        private void Bicycle(Vector3 position)
        {
            var root = new GameObject("Старый велосипед").transform;
            root.SetParent(street, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, -16f, 0f);

            var wheelMesh = WorldArt.SharedMesh(
                "model:bicycle-wheel",
                () => ProceduralMeshFactory.CreateTorus("Велосипедное колесо", .34f, .022f, 28, 6));
            for (var side = -1; side <= 1; side += 2)
            {
                var wheel = WorldArt.MeshObject(
                    "Колесо",
                    wheelMesh,
                    dark,
                    root,
                    new Vector3(side * .55f, .36f, 0f),
                    Quaternion.Euler(90f, 0f, 0f),
                    Vector3.one);
                WorldArt.Shape(
                    PrimitiveType.Cylinder,
                    "Втулка",
                    new Vector3(side * .55f, .36f, 0f),
                    new Vector3(.035f, .045f, .035f),
                    iron,
                    root);
            }

            FrameTube(root, new Vector3(-.28f, .48f, 0f), new Vector3(.48f, .84f, 0f));
            FrameTube(root, new Vector3(.48f, .84f, 0f), new Vector3(.27f, .48f, 0f));
            FrameTube(root, new Vector3(.27f, .48f, 0f), new Vector3(-.28f, .48f, 0f));
            FrameTube(root, new Vector3(-.28f, .48f, 0f), new Vector3(-.55f, .36f, 0f));
            FrameTube(root, new Vector3(.27f, .48f, 0f), new Vector3(.55f, .36f, 0f));
            WorldArt.ChamferedBox(
                "Седло",
                root,
                new Vector3(-.25f, .92f, 0f),
                new Vector3(.28f, .07f, .16f),
                .025f,
                dark);
            WorldArt.Line(
                "Руль",
                new[]
                {
                    new Vector3(.47f, .85f, 0f),
                    new Vector3(.55f, 1.08f, 0f),
                    new Vector3(.72f, 1.08f, 0f)
                },
                .025f,
                iron,
                root);
        }

        private void FrameTube(Transform root, Vector3 start, Vector3 end)
        {
            var midpoint = (start + end) * .5f;
            var length = Vector3.Distance(start, end);
            var tube = WorldArt.Shape(
                PrimitiveType.Cylinder,
                "Труба рамы",
                midpoint,
                new Vector3(.025f, length * .5f, .025f),
                teal,
                root);
            tube.transform.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
        }

        private void StreetDetails()
        {
            for (var index = 0; index < 7; index++)
            {
                var leaf = WorldArt.Shape(
                    PrimitiveType.Sphere,
                    "Опавшие листья",
                    new Vector3(-13f + index * 4.1f, .115f, 6.1f + (index % 2) * .65f),
                    new Vector3(.34f, .015f, .18f),
                    WorldArt.Material(
                        "Осенние листья " + index,
                        Color.Lerp(new Color(.45f, .31f, .08f), new Color(.35f, .15f, .04f), index / 7f),
                        .08f,
                        true),
                    street);
                leaf.transform.localRotation = Quaternion.Euler(0f, index * 21f, 0f);
            }

            WorldArt.Shape(
                PrimitiveType.Cylinder,
                "Люк",
                new Vector3(-3.5f, -.035f, 12.8f),
                new Vector3(.55f, .035f, .55f),
                iron,
                street);
        }

        private void Kiosk()
        {
            WorldArt.ChamferedBox(
                "Пол",
                interior,
                new Vector3(0f, .06f, -.25f),
                new Vector3(4.3f, .12f, 3.7f),
                .035f,
                wood,
                true);
            WorldArt.ChamferedBox(
                "Задняя стена",
                interior,
                new Vector3(0f, 1.4f, -2.1f),
                new Vector3(4.4f, 2.8f, .14f),
                .045f,
                cream,
                true);

            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.ChamferedBox(
                    "Боковая стена",
                    interior,
                    new Vector3(side * 2.15f, 1.4f, -.25f),
                    new Vector3(.14f, 2.8f, 3.7f),
                    .045f,
                    teal,
                    true);
            }

            WorldArt.ChamferedBox(
                "Крыша",
                interior,
                new Vector3(0f, 2.86f, -.25f),
                new Vector3(4.75f, .2f, 4.15f),
                .06f,
                iron,
                true);
            WorldArt.ChamferedBox(
                "Нижний фасад",
                interior,
                new Vector3(0f, .5f, 1.65f),
                new Vector3(4.3f, 1f, .17f),
                .045f,
                teal,
                true);
            WorldArt.ChamferedBox(
                "Верхний фасад",
                interior,
                new Vector3(0f, 2.46f, 1.65f),
                new Vector3(4.3f, .77f, .17f),
                .045f,
                teal,
                true);

            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.ChamferedBox(
                    "Стойка окна",
                    interior,
                    new Vector3(side * 1.85f, 1.6f, 1.65f),
                    new Vector3(.6f, 1.2f, .17f),
                    .045f,
                    teal,
                    true);
                WorldArt.ChamferedBox(
                    "Металлический уголок",
                    interior,
                    new Vector3(side * 2.1f, 1.45f, 1.76f),
                    new Vector3(.07f, 2.7f, .07f),
                    .018f,
                    iron);
            }

            WorldArt.ChamferedBox(
                "Вывеска",
                interior,
                new Vector3(0f, 2.5f, 1.78f),
                new Vector3(3.6f, .59f, .09f),
                .045f,
                cream);
            WorldArt.Label(
                "Т Ё П Л Ы Й  Х Л Е Б",
                new Vector3(0f, 2.53f, 1.84f),
                4.1f,
                new Color(.28f, .2f, .12f),
                interior);
            WorldArt.Label(
                "С 6 УТРА  •  ВСЕГДА РЯДОМ",
                new Vector3(0f, 2.32f, 1.84f),
                1.05f,
                new Color(.38f, .32f, .21f),
                interior);

            var awning = WorldArt.ChamferedBox(
                "Полосатый козырёк",
                interior,
                new Vector3(0f, 2.25f, 2.12f),
                new Vector3(4.2f, .1f, .95f),
                .035f,
                cream);
            awning.transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            for (var index = 0; index < 10; index++)
            {
                var stripe = WorldArt.ChamferedBox(
                    "Полоса козырька",
                    interior,
                    new Vector3(-1.9f + index * .42f, 2.27f, 2.13f),
                    new Vector3(.2f, .11f, .93f),
                    .025f,
                    teal);
                stripe.transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            }

            var counter = WorldArt.ChamferedBox(
                "Окно выдачи",
                interior,
                new Vector3(0f, .98f, 1.28f),
                new Vector3(3.4f, .12f, .83f),
                .035f,
                wood,
                true);
            Target(counter, "Выдать заказ / успокоить очередь", session.Serve);

            var window = new GameObject("Взаимодействие с окном");
            window.transform.SetParent(interior, false);
            window.transform.localPosition = new Vector3(0f, 1.62f, 1.68f);
            var box = window.AddComponent<BoxCollider>();
            box.size = new Vector3(1.6f, 1.03f, .05f);
            box.isTrigger = true;
            Target(window, "Поговорить с покупателем · выдать пакет", session.Serve);

            BuildShelves();
            BuildCounterProps();
            BuildBackRoomDetails();

            WorldArt.Lamp(
                "Лампа под потолком",
                new Vector3(0f, 2.53f, -.45f),
                new Color(1f, .68f, .32f),
                2.9f,
                6f,
                interior);
            WorldArt.Shape(
                PrimitiveType.Sphere,
                "Лампочка",
                new Vector3(0f, 2.53f, -.45f),
                Vector3.one * .12f,
                WorldArt.Material(
                    "Нить лампы",
                    new Color(1f, .7f, .4f),
                    .1f,
                    false,
                    4f),
                interior);

            var reflection = new GameObject("Reflection Probe • витрина").AddComponent<ReflectionProbe>();
            reflection.transform.SetParent(interior, false);
            reflection.transform.localPosition = new Vector3(0f, 1.4f, -.2f);
            reflection.size = new Vector3(5f, 3f, 4.5f);
            reflection.mode = ReflectionProbeMode.Realtime;
            reflection.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            reflection.intensity = .55f;
            reflection.RenderProbe();
        }

        private void BuildShelves()
        {
            var products = session.Products ?? new ProductData[0];
            const int columns = 5;
            var rows = Mathf.Max(1, Mathf.CeilToInt(products.Length / (float)columns));

            for (var row = 0; row < rows; row++)
            {
                var y = .78f + row * .68f;
                WorldArt.ChamferedBox(
                    "Полка",
                    interior,
                    new Vector3(0f, y, -1.72f),
                    new Vector3(3.9f, .075f, .6f),
                    .022f,
                    wood,
                    true);

                for (var column = 0; column < columns; column++)
                {
                    var index = row * columns + column;
                    if (index >= products.Length) continue;

                    var product = products[index];
                    if (product == null) continue;
                    var x = -1.52f + column * .76f;
                    var tray = WorldArt.ChamferedBox(
                        "Лоток • " + product.Title,
                        interior,
                        new Vector3(x, y + .09f, -1.67f),
                        new Vector3(.66f, .1f, .46f),
                        .035f,
                        cream,
                        true);
                    Target(
                        tray,
                        "В пакет: " + product.Title + " • " + Money.Format(product.Price),
                        () => session.AddToBag(product));

                    var display = new GameObject("Витрина • " + product.Title);
                    display.transform.SetParent(interior, false);
                    display.transform.localPosition = new Vector3(x, y + .14f, -1.68f);
                    display.AddComponent<StockDisplay>().Configure(session, product, 4);

                    WorldArt.Label(
                        (index + 1) + "  " + product.Title + "\n" + Money.Format(product.Price),
                        new Vector3(x, y - .16f, -1.37f),
                        .72f,
                        new Color(.85f, .78f, .6f),
                        interior);
                }
            }
        }

        private void BuildCounterProps()
        {
            var register = ModelFactory.BuildCashRegister(
                interior,
                new Vector3(1.25f, 1.17f, 1.22f),
                cream,
                dark,
                glass);
            Target(register, "Касса • посчитать сдачу", session.Serve);
            WorldArt.Label(
                "ЭЛЕКТРОНИКА",
                new Vector3(1.25f, 1.24f, 1.005f),
                .48f,
                new Color(.23f, .29f, .2f),
                interior,
                0f);

            var radio = ModelFactory.BuildRadio(
                interior,
                new Vector3(-1.48f, 1.2f, 1.22f),
                wood,
                iron,
                dark);
            Target(radio, "Переключить радио", atmosphere.NextStation);

            var cat = ModelFactory.BuildCat(interior, new Vector3(-.92f, 1.08f, 1.22f));
            Target(cat, "Погладить Плюша", () =>
            {
                EventBus.Say("Плюш тихо мурчит. Можно никуда не торопиться.");
                EventBus.Sound(.5f);
            });

            WorldArt.ChamferedBox(
                "Тарелка для мелочи",
                interior,
                new Vector3(.45f, 1.12f, 1.25f),
                new Vector3(.38f, .025f, .28f),
                .08f,
                iron);
            for (var coin = 0; coin < 5; coin++)
            {
                var piece = WorldArt.Shape(
                    PrimitiveType.Cylinder,
                    "Монета",
                    new Vector3(.34f + coin * .05f, 1.145f, 1.23f + (coin % 2) * .04f),
                    new Vector3(.025f, .004f, .025f),
                    WorldArt.MetalMaterial("Монеты", new Color(.63f, .52f, .23f), .6f),
                    interior);
                piece.transform.localRotation = Quaternion.Euler(90f, coin * 17f, 0f);
            }
        }

        private void BuildBackRoomDetails()
        {
            WorldArt.ChamferedBox(
                "Столик",
                interior,
                new Vector3(1.62f, .82f, -.2f),
                new Vector3(.72f, .09f, .9f),
                .035f,
                wood,
                true);
            var kettle = ModelFactory.BuildKettle(
                interior,
                new Vector3(1.55f, .91f, -.18f),
                cream,
                iron,
                dark);
            Target(kettle, "Налить чай • короткая передышка", atmosphere.Tea);
            ModelFactory.BuildMug(interior, new Vector3(1.87f, .9f, -.18f), new Color(.27f, .46f, .43f));

            WorldArt.ChamferedBox(
                "Тумба",
                interior,
                new Vector3(-1.7f, .6f, -.35f),
                new Vector3(.62f, 1f, .72f),
                .045f,
                wood,
                true);
            var phone = ModelFactory.BuildPhone(
                interior,
                new Vector3(-1.7f, 1.16f, -.35f),
                dark,
                iron);
            Target(phone, "Позвонить поставщику", () => ui.OpenNotebook(1));

            ModelFactory.BuildCrate(
                interior,
                new Vector3(-1.55f, .28f, -1.35f),
                new Vector3(.75f, .52f, .72f),
                wood);
            ModelFactory.BuildCrate(
                interior,
                new Vector3(1.55f, .28f, -1.35f),
                new Vector3(.72f, .52f, .68f),
                wood);

            WorldArt.ChamferedBox(
                "Блокнот",
                interior,
                new Vector3(-.35f, 1.04f, 1.24f),
                new Vector3(.34f, .025f, .25f),
                .025f,
                paper);
            var pencil = WorldArt.Shape(
                PrimitiveType.Cylinder,
                "Карандаш",
                new Vector3(-.25f, 1.075f, 1.18f),
                new Vector3(.012f, .17f, .012f),
                WorldArt.Material("Карандаш", new Color(.64f, .42f, .12f), .18f),
                interior);
            pencil.transform.localRotation = Quaternion.Euler(0f, 0f, 78f);

            WorldArt.Label(
                "СЕНТЯБРЬ 2002\nПН ВТ СР ЧТ ПТ СБ ВС\n 2   3   4   5   6   7   8",
                new Vector3(1.4f, 2.19f, -2.01f),
                1.15f,
                new Color(.22f, .3f, .28f),
                interior);
            WorldArt.Label(
                "Не торопись.\nХлеб любит тишину.",
                new Vector3(-.6f, 2.26f, -2.01f),
                1.15f,
                new Color(.29f, .28f, .2f),
                interior);
        }

        private void Target(GameObject gameObject, string label, Action action)
        {
            if (gameObject == null) return;
            gameObject.AddComponent<InteractionTarget>().Configure(label, action);
        }

        private void Lighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .012f;
            RenderSettings.fogColor = new Color(.43f, .49f, .5f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.43f, .51f, .58f);
            RenderSettings.ambientEquatorColor = new Color(.29f, .34f, .33f);
            RenderSettings.ambientGroundColor = new Color(.12f, .13f, .12f);

            var sunObject = new GameObject("Солнце сквозь облака");
            sunObject.transform.SetParent(transform, false);
            Sun = sunObject.AddComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.color = new Color(1f, .79f, .53f);
            Sun.intensity = .7f;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = .72f;
            sunObject.transform.rotation = Quaternion.Euler(23f, -35f, 0f);

            var volumeObject = new GameObject("Цвет • сентябрь");
            volumeObject.transform.SetParent(transform, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "WarmBread Runtime Volume";
            profile.hideFlags = HideFlags.DontSave;
            WorldArt.TrackGenerated(profile);
            volume.profile = profile;

            profile.Add<Bloom>(true).intensity.Override(.27f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.22f);
            vignette.smoothness.Override(.75f);
            var grade = profile.Add<ColorAdjustments>(true);
            grade.saturation.Override(-13f);
            grade.contrast.Override(8f);
            grade.postExposure.Override(.12f);
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(.12f);
            grain.response.Override(.65f);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        }
    }
}
