using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WarmBread
{
    public sealed class WorldBuilder : MonoBehaviour
    {
        public Light Sun { get; private set; }
        private Transform street, interior;
        private Material concrete, teal, wood, iron, cream, glass, dark;
        private GameSession session;
        private GameUI ui;
        private Atmosphere atmosphere;
        public void Build(GameSession game, GameUI interfaceUI, Atmosphere mood)
        {
            session = game; ui = interfaceUI; atmosphere = mood;
            concrete = WorldArt.Material("Шероховатый бетон", new Color(.46f,.47f,.43f), .08f, true);
            teal = WorldArt.Material("Выцветшая бирюза", new Color(.22f,.37f,.35f), .2f, true);
            wood = WorldArt.Material("Старая фанера", new Color(.38f,.25f,.15f), .2f, true);
            iron = WorldArt.Material("Тёмный металл", new Color(.12f,.16f,.16f), .45f);
            cream = WorldArt.Material("Тёплая эмаль", new Color(.73f,.69f,.54f), .25f, true);
            glass = WorldArt.Material("Окна • сумерки", new Color(.18f,.27f,.3f), .85f);
            dark = WorldArt.Material("Резина", new Color(.065f,.075f,.073f));
            street = new GameObject("РАЙОН • сентябрь 2002").transform; street.SetParent(transform);
            interior = new GameObject("ЛАРЁК • Тёплый хлеб").transform; interior.SetParent(transform);
            Environment(); Kiosk(); Lighting();
            // В NavMesh входят только статические уличные коллайдеры.
            Physics.SyncTransforms();
            var nav = street.gameObject.AddComponent<NavMeshSurface>(); nav.collectObjects = CollectObjects.Children;
            nav.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders; nav.BuildNavMesh();
        }
        private void Environment()
        {
            var asphalt = WorldArt.Material("Мокрый асфальт", new Color(.22f,.25f,.24f), .7f, true);
            WorldArt.Cube("Асфальт", new Vector3(0,-.2f,10), new Vector3(80,.3f,65), asphalt, street);
            WorldArt.Cube("Тротуар", new Vector3(0,0,4.3f), new Vector3(44,.2f,6.8f), concrete, street);
            WorldArt.Cube("Бордюр", new Vector3(0,.08f,7.65f), new Vector3(44,.26f,.2f), cream, street);
            for (int x = -20; x <= 20; x += 4)
                WorldArt.Cube("Разметка", new Vector3(x,-.04f,12), new Vector3(2.1f,.014f,.1f), cream, street, false);
            var puddle = WorldArt.Material("Лужи", new Color(.35f,.43f,.43f), .95f);
            var random = new System.Random(91);
            for (int i = 0; i < 26; i++)
                WorldArt.Shape(PrimitiveType.Cylinder, "Лужа", new Vector3((float)random.NextDouble()*42-21,.105f,3+(float)random.NextDouble()*4), new Vector3(.4f+(float)random.NextDouble()*1.2f,.002f,.25f+(float)random.NextDouble()*.7f), puddle, street);
            Apartment(new Vector3(-14,0,24), 17, 5); Apartment(new Vector3(9,0,25), 20, 5);
            Apartment(new Vector3(-27,0,4), 10, 9); Apartment(new Vector3(29,0,9), 13, 7);
            Apartment(new Vector3(0,0,44), 23, 9);
            for (int i = 0; i < 9; i++) Tree(new Vector3(-22+i*5.7f,0,18), i);
            for (int i = -1; i <= 1; i++)
            {
                float x = i*15;
                WorldArt.Shape(PrimitiveType.Cylinder,"Фонарный столб",new Vector3(x,3.3f,7.5f),new Vector3(.15f,3.3f,.15f),iron,street);
                WorldArt.Cube("Плафон",new Vector3(x,6.5f,7.3f),new Vector3(.45f,.16f,.65f),cream,street,false);
                WorldArt.Lamp("Уличный фонарь",new Vector3(x,6.25f,7.3f),new Color(1,.74f,.38f),1.4f,12,street);
                if (i < 1)
                    WorldArt.Line("Провода",new[]{new Vector3(x,6.2f,7.5f),new Vector3(x+7.5f,5.5f,7.5f),new Vector3(x+15,6.2f,7.5f)},.024f,iron,street);
            }
            Bench(new Vector3(6,0,5.8f)); Bench(new Vector3(-8,0,5.8f));
            Car(new Vector3(-8,0,9), new Color(.46f,.18f,.13f)); Car(new Vector3(7,0,15), new Color(.56f,.59f,.51f));
            var moving = Car(new Vector3(-25,0,11),new Color(.36f,.46f,.46f)); moving.AddComponent<PassingCar>();
            WorldArt.Cube("Остановка",new Vector3(14,1.4f,5.7f),new Vector3(3.6f,2.8f,.12f),teal,street);
            WorldArt.Cube("Крыша остановки",new Vector3(14,2.85f,4.9f),new Vector3(4,.13f,2),iron,street);
            WorldArt.Label("УЛ. САДОВАЯ",new Vector3(14,2.5f,5.61f),2.5f,new Color(.85f,.82f,.67f),street,0);
        }
        private void Apartment(Vector3 center, float width, int floors)
        {
            var root = new GameObject("Панельный дом").transform; root.SetParent(street); root.localPosition = center;
            float height = floors * 2.75f;
            WorldArt.Cube("Бетонный блок",new Vector3(0,height/2,0),new Vector3(width,height,7),concrete,root);
            WorldArt.Cube("Цоколь",new Vector3(0,.7f,-3.55f),new Vector3(width,1.4f,.13f),teal,root,false);
            var warmWindow = WorldArt.Material("Свет в окне",new Color(.86f,.61f,.29f),.2f,false,.8f);
            for (int floor=0;floor<floors;floor++)
            {
                WorldArt.Cube("Шов панелей",new Vector3(0,floor*2.75f,-3.51f),new Vector3(width,.025f,.03f),iron,root,false);
                for(int col=0;col<(int)(width/2.4f);col++)
                {
                    float x=-width/2+1.5f+col*2.4f,y=2+floor*2.75f;
                    WorldArt.Cube("Оконный проём",new Vector3(x,y,-3.56f),new Vector3(1.35f,1.58f,.11f),cream,root,false);
                    WorldArt.Cube("Стекло",new Vector3(x,y,-3.63f),new Vector3(1.15f,1.37f,.02f),(col+floor*3)%7==0?warmWindow:glass,root,false);
                    WorldArt.Cube("Рама",new Vector3(x,y,-3.65f),new Vector3(.05f,1.37f,.04f),cream,root,false);
                    if(col%3==0 && floor>0)
                        WorldArt.Cube("Балкон",new Vector3(x,y-.65f,-4),new Vector3(1.7f,.8f,.65f),floor%2==0?teal:cream,root,false);
                }
            }
            WorldArt.Cube("Козырёк подъезда",new Vector3(0,2,-4.1f),new Vector3(2.4f,.16f,1.6f),iron,root,false);
        }
        private void Tree(Vector3 position,int variant)
        {
            var bark=WorldArt.Material("Кора",new Color(.25f,.24f,.19f),.1f,true);
            var leaves=WorldArt.Material("Листва "+variant,new Color(.29f+variant*.008f,.34f,.19f),.08f,true);
            WorldArt.Shape(PrimitiveType.Cylinder,"Ствол тополя",position+Vector3.up*2.5f,new Vector3(.23f,2.5f,.23f),bark,street);
            for(int j=0;j<3;j++) WorldArt.Shape(PrimitiveType.Sphere,"Крона",position+Vector3.up*(4+j*1.5f),new Vector3(2.2f-j*.35f,3,2),leaves,street);
        }
        private void Bench(Vector3 p)
        {
            for(int z=0;z<3;z++) WorldArt.Cube("Доска скамейки",p+new Vector3(0,.5f,z*.14f),new Vector3(2,.08f,.11f),wood,street,false);
            for(int x=-1;x<=1;x+=2) WorldArt.Cube("Ножка скамейки",p+new Vector3(x*.8f,.27f,.12f),new Vector3(.1f,.54f,.5f),iron,street);
            WorldArt.Cube("Спинка",p+new Vector3(0,.91f,.36f),new Vector3(2,.42f,.07f),teal,street,false);
            WorldArt.Shape(PrimitiveType.Cylinder,"Урна",p+new Vector3(1.6f,.38f,0),new Vector3(.42f,.38f,.42f),iron,street);
        }
        private GameObject Car(Vector3 p,Color color)
        {
            var root=new GameObject("Седан • 1987"); root.transform.SetParent(street);root.transform.localPosition=p;
            var paint=WorldArt.Material("Автомобиль "+color,color,.45f,true);
            WorldArt.Cube("Кузов",new Vector3(0,.57f,0),new Vector3(3.8f,.57f,1.55f),paint,root.transform);
            WorldArt.Cube("Кабина",new Vector3(-.15f,1.05f,0),new Vector3(1.8f,.65f,1.38f),glass,root.transform,false);
            WorldArt.Cube("Крыша",new Vector3(-.15f,1.4f,0),new Vector3(1.95f,.09f,1.47f),paint,root.transform,false);
            for(int side=-1;side<=1;side+=2)
            {
                WorldArt.Cube("Бампер",new Vector3(side*1.95f,.39f,0),new Vector3(.12f,.14f,1.63f),cream,root.transform,false);
                for(int axle=-1;axle<=1;axle+=2)
                {
                    var wheel=WorldArt.Shape(PrimitiveType.Cylinder,"Колесо",new Vector3(axle*1.15f,.28f,side*.77f),new Vector3(.56f,.12f,.56f),dark,root.transform);
                    wheel.transform.localRotation=Quaternion.Euler(90,0,0);
                }
            }
            return root;
        }
        private void Target(GameObject go,string label,System.Action action)
        { go.AddComponent<InteractionTarget>().Configure(label,action); }
        private void Kiosk()
        {
            WorldArt.Cube("Пол",new Vector3(0,.06f,-.25f),new Vector3(4.3f,.12f,3.7f),wood,interior);
            WorldArt.Cube("Задняя стена",new Vector3(0,1.4f,-2.1f),new Vector3(4.4f,2.8f,.14f),cream,interior);
            for(int x=-1;x<=1;x+=2) WorldArt.Cube("Боковая стена",new Vector3(x*2.15f,1.4f,-.25f),new Vector3(.14f,2.8f,3.7f),teal,interior);
            WorldArt.Cube("Крыша",new Vector3(0,2.86f,-.25f),new Vector3(4.75f,.2f,4.15f),iron,interior);
            WorldArt.Cube("Фасад",new Vector3(0,.5f,1.65f),new Vector3(4.3f,1,.17f),teal,interior);
            WorldArt.Cube("Фасад сверху",new Vector3(0,2.46f,1.65f),new Vector3(4.3f,.77f,.17f),teal,interior);
            WorldArt.Cube("Стойка слева",new Vector3(-1.85f,1.6f,1.65f),new Vector3(.6f,1.2f,.17f),teal,interior);
            WorldArt.Cube("Стойка справа",new Vector3(1.85f,1.6f,1.65f),new Vector3(.6f,1.2f,.17f),teal,interior);
            WorldArt.Cube("Вывеска",new Vector3(0,2.5f,1.78f),new Vector3(3.6f,.59f,.09f),cream,interior,false);
            WorldArt.Label("Т Ё П Л Ы Й  Х Л Е Б",new Vector3(0,2.53f,1.84f),4.1f,new Color(.28f,.2f,.12f),interior);
            WorldArt.Label("С 6 УТРА  •  ВСЕГДА РЯДОМ",new Vector3(0,2.32f,1.84f),1.05f,new Color(.38f,.32f,.21f),interior);
            var awning=WorldArt.Cube("Полосатый козырёк",new Vector3(0,2.25f,2.12f),new Vector3(4.2f,.1f,.95f),cream,interior,false);
            awning.transform.localRotation=Quaternion.Euler(-9,0,0);
            for(int i=0;i<10;i++) WorldArt.Cube("Полоса козырька",new Vector3(-1.9f+i*.42f,2.27f,2.13f),new Vector3(.2f,.11f,.93f),teal,interior,false).transform.localRotation=Quaternion.Euler(-9,0,0);
            var counter=WorldArt.Cube("Окно выдачи",new Vector3(0,.98f,1.28f),new Vector3(3.4f,.12f,.83f),wood,interior);
            Target(counter,"Выдать заказ / успокоить очередь",session.Serve);
            // Триггер в проёме доступен лучу, но не закрывает вид и не блокирует движение.
            var window=new GameObject("Взаимодействие с окном");window.transform.SetParent(interior);window.transform.localPosition=new Vector3(0,1.62f,1.68f);
            var box=window.AddComponent<BoxCollider>();box.size=new Vector3(1.6f,1.03f,.05f);box.isTrigger=true;Target(window,"Поговорить с покупателем · выдать пакет",session.Serve);
            for(int row=0;row<2;row++)
            {
                float y=.89f+row*.62f;
                WorldArt.Cube("Полка",new Vector3(0,y,-1.7f),new Vector3(3.8f,.07f,.54f),wood,interior);
                for(int col=0;col<5;col++)
                {
                    var product=session.Products[row*5+col];float x=-1.52f+col*.76f;
                    var tray=WorldArt.Cube("Лоток • "+product.Title,new Vector3(x,y+.08f,-1.68f),new Vector3(.66f,.09f,.43f),cream,interior);
                    Target(tray,"В пакет: "+product.Title+" • "+Money.Format(product.Price),()=>session.AddToBag(product));
                    for(int k=0;k<2;k++) WorldArt.Product(interior,product,new Vector3(x+(k-.5f)*.26f,y+.12f,-1.68f),.8f);
                    WorldArt.Label((col+row*5+1)+"  "+product.Title+"\n"+Money.Format(product.Price),new Vector3(x,y-.14f,-1.38f),.75f,new Color(.85f,.78f,.6f),interior);
                }
            }
            var register=WorldArt.Cube("Касса",new Vector3(1.25f,1.14f,1.23f),new Vector3(.52f,.21f,.36f),cream,interior);
            Target(register,"Касса • посчитать сдачу",session.Serve);
            WorldArt.Part("Дисплей",interior,new Vector3(1.25f,1.24f,1.05f),new Vector3(.37f,.09f,.015f),glass);
            WorldArt.Label("ЭЛЕКТРОНИКА",new Vector3(1.25f,1.16f,1.035f),.5f,new Color(.23f,.29f,.2f),interior,0);
            var radio=WorldArt.Cube("Радиоприёмник",new Vector3(-1.53f,1.21f,1.23f),new Vector3(.54f,.34f,.24f),wood,interior);
            Target(radio,"Переключить радио",()=>atmosphere.NextStation());
            for(int i=0;i<7;i++) WorldArt.Part("Решётка динамика",interior,new Vector3(-1.7f+i*.045f,1.22f,1.1f),new Vector3(.013f,.2f,.018f),iron);
            WorldArt.Line("Антенна",new[]{new Vector3(-1.7f,1.38f,1.25f),new Vector3(-1.83f,1.95f,1.25f)},.012f,iron,interior);
            var table=WorldArt.Cube("Столик",new Vector3(1.69f,.83f,-.2f),new Vector3(.54f,.09f,.8f),wood,interior);
            var kettle=WorldArt.Shape(PrimitiveType.Cylinder,"Чайник",new Vector3(1.7f,1.07f,-.18f),new Vector3(.23f,.2f,.23f),cream,interior,true);
            Target(kettle,"Налить чай • короткая передышка",()=>{atmosphere.Tea();});
            WorldArt.Shape(PrimitiveType.Sphere,"Крышка",new Vector3(1.7f,1.28f,-.18f),new Vector3(.25f,.05f,.25f),iron,interior);
            var phone=WorldArt.Cube("Телефон поставщика",new Vector3(-1.75f,1.13f,-.35f),new Vector3(.38f,.18f,.3f),iron,interior);
            Target(phone,"Позвонить поставщику",()=>ui.OpenNotebook(1));
            WorldArt.Cube("Тумба",new Vector3(-1.75f,.6f,-.35f),new Vector3(.54f,1,.65f),wood,interior);
            var cat=WorldArt.Shape(PrimitiveType.Sphere,"Кот Плюш",new Vector3(-1.12f,1.14f,1.25f),new Vector3(.4f,.2f,.26f),WorldArt.Material("Рыжий кот",new Color(.64f,.36f,.15f)),interior,true);
            Target(cat,"Погладить Плюша",()=>{EventBus.Say("Плюш тихо мурчит. Можно никуда не торопиться.");EventBus.Sound(.5f);});
            WorldArt.Shape(PrimitiveType.Sphere,"Голова кота",new Vector3(-.93f,1.21f,1.22f),new Vector3(.19f,.18f,.18f),WorldArt.Material("Рыжий кот",Color.white),interior);
            WorldArt.Label("СЕНТЯБРЬ 2002\nПН ВТ СР ЧТ ПТ СБ ВС\n 2   3   4   5   6   7   8",new Vector3(1.4f,2.19f,-2.01f),1.15f,new Color(.22f,.3f,.28f),interior);
            WorldArt.Label("Не торопись.\nХлеб любит тишину.",new Vector3(-.6f,2.26f,-2.01f),1.15f,new Color(.29f,.28f,.2f),interior);
            WorldArt.Lamp("Лампа под потолком",new Vector3(0,2.53f,-.45f),new Color(1,.68f,.32f),2.9f,6,interior);
            WorldArt.Shape(PrimitiveType.Sphere,"Лампочка",new Vector3(0,2.53f,-.45f),Vector3.one*.12f,WorldArt.Material("Нить лампы",new Color(1,.7f,.4f),.1f,false,4),interior);
        }
        private void Lighting()
        {
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.012f;
            RenderSettings.fogColor=new Color(.43f,.49f,.5f);RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.43f,.51f,.58f);RenderSettings.ambientEquatorColor=new Color(.29f,.34f,.33f);RenderSettings.ambientGroundColor=new Color(.12f,.13f,.12f);
            var go=new GameObject("Солнце сквозь облака");go.transform.SetParent(transform);Sun=go.AddComponent<Light>();Sun.type=LightType.Directional;
            Sun.color=new Color(1,.79f,.53f);Sun.intensity=.7f;Sun.shadows=LightShadows.Soft;go.transform.rotation=Quaternion.Euler(23,-35,0);
            var volume=new GameObject("Цвет • сентябрь").AddComponent<Volume>();volume.transform.SetParent(transform);volume.isGlobal=true;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.profile=profile;
            profile.Add<Bloom>(true).intensity.Override(.32f);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.27f);vignette.smoothness.Override(.75f);
            var grade=profile.Add<ColorAdjustments>(true);grade.saturation.Override(-17);grade.contrast.Override(9);grade.postExposure.Override(.15f);
            var grain=profile.Add<FilmGrain>(true);grain.type.Override(FilmGrainLookup.Thin1);grain.intensity.Override(.16f);grain.response.Override(.65f);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        }
    }
}
