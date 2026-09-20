using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace WarmBread
{
    public sealed class GameUI : MonoBehaviour
    {
        private GameSession game;
        private PlayerController player;
        private Atmosphere mood;
        private RectTransform canvas, hud, overlay;
        private TextMeshProUGUI clock, prompt, message, stats, orderText, bagText, patience;
        private bool home=true, notebook, newGameConfirm;
        private int tab;
        private float messageTime;
        private string lastMessage="";
        private readonly Color ink=new Color(.91f,.86f,.74f);
        private readonly Color muted=new Color(.59f,.65f,.6f);
        private readonly Color amber=new Color(.84f,.6f,.32f);
        private readonly Color panel=new Color(.07f,.105f,.1f,.97f);
        public void Initialize(GameSession session,PlayerController controller,Atmosphere atmosphere)
        {
            game=session;player=controller;mood=atmosphere;
            var root=new GameObject("Интерфейс",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(transform);canvas=root.GetComponent<RectTransform>();root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            if(FindObjectOfType<EventSystem>()==null)
            {var es=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            hud=Full("HUD",canvas);
            stats=Text(hud,"",32,25,900,35,19,ink);
            clock=Text(hud,"",1120,25,448,60,19,ink);clock.alignment=TextAlignmentOptions.TopRight;
            var card=Panel(hud,22,102,350,230,new Color(.07f,.105f,.1f,.88f));
            Text(card,"У ОКНА",20,16,290,25,13,amber);
            orderText=Text(card,"",20,47,310,135,19,ink);
            patience=Text(card,"",20,195,300,25,13,muted);
            bagText=Text(hud,"",30,655,520,128,17,ink);
            prompt=Text(hud,"",430,630,740,44,20,ink);prompt.alignment=TextAlignmentOptions.Center;
            message=Text(hud,"",365,720,870,90,20,ink);message.alignment=TextAlignmentOptions.Center;
            Text(hud,"WASD  ходить     E  взять / действие     F  выдать     X  очистить пакет     TAB  блокнот     R  радио     ESC  пауза",30,853,1540,28,14,muted);
            var reticle=Text(hud,"·",790,438,20,20,24,ink);reticle.alignment=TextAlignmentOptions.Center;
            EventBus.Changed+=Refresh;EventBus.Message+=Notify;Refresh();
        }
        private void OnDestroy(){EventBus.Changed-=Refresh;EventBus.Message-=Notify;Time.timeScale=1;}
        private void Notify(string value){lastMessage=value;messageTime=Time.unscaledTime+9;if(message!=null)message.text=value;}
        private void Update()
        {
            if(game==null)return;
            var k=Keyboard.current;
            if(k!=null && !home)
            {
                if(k.escapeKey.wasPressedThisFrame)
                {
                    if(notebook){notebook=false;game.Modal=game.Paying;}
                    else if(!game.Report)game.Paused=!game.Paused;
                    Time.timeScale=game.Paused?0:1;AudioListener.pause=game.Paused;Refresh();
                }
                if(k.tabKey.wasPressedThisFrame && game.Running && !game.Paused && !game.Paying)
                {notebook=!notebook;game.Modal=notebook;Refresh();}
                if(k.rKey.wasPressedThisFrame && !game.Paused)mood.NextStation();
            }
            clock.text="СЕНТЯБРЬ 2002  /  СМЕНА "+game.Day+"\n"+((int)game.Hour).ToString("00")+":"+((int)((game.Hour%1)*60)).ToString("00")+"  •  мелкий дождь";
            stats.text="ТЁПЛЫЙ ХЛЕБ     /     "+Money.Format(game.Cash)+"     /     ДОВЕРИЕ  "+game.Reputation+"%";
            prompt.text=game.Modal||game.Paused?"":player.Prompt;
            if(messageTime<Time.unscaledTime)message.text="";
            var customer=game.Customer;
            patience.text=customer!=null?"Терпение  "+Mathf.CeilToInt(customer.Patience*100)+"%     ·     В очереди: "+game.Queue.Count:"В очереди: "+game.Queue.Count;
        }
        public void OpenNotebook(int page=0){if(!game.Running||game.Paying)return;notebook=true;tab=page;game.Modal=true;Refresh();}
        private void Refresh()
        {
            if(canvas==null)return;
            if(overlay!=null){overlay.gameObject.SetActive(false);Destroy(overlay.gameObject);}
            overlay=Full("Окно интерфейса",canvas);
            hud.gameObject.SetActive(!home);
            var customer=game.Customer;
            orderText.text=customer==null?"Покупатели скоро подойдут.\n\nА пока — послушайте дождь.":customer.Data.DisplayName+"\n\n"+customer.Order.Describe(game.Products)+"\nИтого: "+Money.Format(customer.Order.Total);
            bagText.text="БУМАЖНЫЙ ПАКЕТ  /  "+game.Bag.Count+"\n"+(game.Bag.Count==0?"Пока пуст. Товары на полках за спиной.":string.Join("\n",game.Bag.GroupBy(b=>b.productId).Select(g=>game.Products.First(p=>p.Id==g.Key).Title+"  × "+g.Count())));
            if(home)Home();
            else if(game.Report)Report();
            else if(game.Paused)Pause();
            else if(game.Paying)CashDesk();
            else if(notebook)Notebook();
        }
        private void Home()
        {
            Panel(overlay,0,0,650,900,new Color(.055f,.085f,.08f,.95f));
            Text(overlay,"МАЛЕНЬКИЕ ИСТОРИИ БОЛЬШОГО ДВОРА",64,69,520,35,13,amber);
            Text(overlay,"Тёплый\nхлеб",56,147,550,230,88,ink);
            Text(overlay,"У каждого дома есть свой запах.",64,403,530,45,22,ink);
            Text(overlay,"Сентябрь, 2002. За стеклом моросит дождь.\nВы открываете маленький ларёк у остановки.\nЧай, радио и люди, которых ждёшь завтра.",64,457,510,120,19,muted);
            Button(overlay,newGameConfirm?"Подтвердить новую историю":"Открыть ларёк",64,608,480,58,()=>{
                if(SaveSystem.Exists&&!newGameConfirm){newGameConfirm=true;Refresh();return;}
                home=false;game.Begin(false);Refresh();});
            if(SaveSystem.Exists)Button(overlay,"Продолжить сохранённую смену",64,680,480,48,()=>{home=false;game.Begin(true);Refresh();},true);
            Text(overlay,"Сохраняется начало каждой смены.\nПервая версия • оригинальная процедурная графика",64,795,520,65,14,muted);
        }
        private RectTransform Sheet(string title,string eyebrow)
        {
            Panel(overlay,0,0,1600,900,new Color(0,0,0,.55f));
            var sheet=Panel(overlay,230,85,1140,730,panel);
            Text(sheet,eyebrow,38,24,1000,25,13,amber);Text(sheet,title,36,61,1040,66,39,ink);
            return sheet;
        }
        private void CashDesk()
        {
            var customer=game.Customer;if(customer==null)return;
            var sheet=Sheet("Сдача — дело точное","КАССА / "+customer.Data.DisplayName);
            Text(sheet,"К ОПЛАТЕ\n"+Money.Format(customer.Order.Total),40,152,290,95,25,ink);
            Text(sheet,"ПОКУПАТЕЛЬ ДАЛ\n"+Money.Format(customer.Order.Paid),395,152,320,95,25,ink);
            Text(sheet,"В ВАШЕЙ РУКЕ\n"+Money.Format(game.ChangeInHand),775,152,325,95,25,amber);
            Text(sheet,"Наберите сдачу монетами и купюрами. Подсказки с готовым ответом нет.",40,269,1050,40,18,muted);
            for(int i=0;i<Money.Denominations.Length;i++)
            {int value=Money.Denominations[i];Button(sheet,Money.Format(value),40+(i%4)*264,330+(i/4)*80,248,62,()=>game.AddChange(value),true);}
            Text(sheet,lastMessage,40,515,1060,70,18,amber);
            Button(sheet,"Пересчитать",40,628,310,55,game.ClearChange,true);
            Button(sheet,"Передать сдачу и пакет",540,628,550,55,game.Checkout);
        }
        private void Notebook()
        {
            var sheet=Sheet("Блокнот продавца","ЦЕНЫ / ПОСТАВКИ / ЛЮДИ");
            Button(sheet,"Прилавок",40,137,240,43,()=>{tab=0;Refresh();},tab!=0);
            Button(sheet,"Поставщик",292,137,240,43,()=>{tab=1;Refresh();},tab!=1);
            Button(sheet,"Истории",544,137,240,43,()=>{tab=2;Refresh();},tab!=2);
            Button(sheet,"Закрыть · TAB",818,137,275,43,()=>{notebook=false;game.Modal=false;Refresh();},true);
            if(tab==2)
            {Text(sheet,game.Journal.Count==0?"Пока чистые страницы. Истории появятся после первых покупателей.":string.Join("\n\n",game.Journal),40,208,1040,450,21,ink);return;}
            for(int i=0;i<game.Products.Length;i++)
            {
                var p=game.Products[i];float y=209+i*41;
                Text(sheet,p.Title,40,y,450,35,18,ink);Text(sheet,Money.Format(p.Price),470,y,125,35,18,amber);
                Text(sheet,"На полке: "+game.Stock.Count(p.Id),610,y,185,35,17,muted);
                if(tab==1)Button(sheet,"Ящик ×10 · "+Money.Format(p.Cost*10),815,y-3,278,34,()=>game.BuyStock(p),true);
                else Button(sheet,"В пакет",815,y-3,278,34,()=>game.AddToBag(p),true);
            }
            Text(sheet,tab==1?"Доставка: 25 секунд. В пути: "+game.PendingDeliveries+". "+lastMessage:"Соберите точный заказ. Лишнее можно вернуть клавишей X. Время идёт, пока открыт блокнот.",40,644,1040, sixty(),16,muted);
        }
        private static float sixty()=>60;
        private void Pause()
        {
            var sheet=Sheet("Тихая пауза","МОЖНО ПЕРЕВЕСТИ ДУХ");
            Text(sheet,"Игра и очередь остановлены.\nПри выходе вы вернётесь к началу сохранённой смены.",40,151,1040,75,22,muted);
            Button(sheet,"Вернуться за прилавок",40,275,500,55,()=>{game.Paused=false;Time.timeScale=1;AudioListener.pause=false;Refresh();});
            Button(sheet,"Покачивание камеры: "+(player.HeadBob?"да":"нет"),40,351,500,50,()=>{player.HeadBob=!player.HeadBob;Refresh();},true);
            Button(sheet,"Мышь: "+player.Sensitivity.ToString("0.000")+" · изменить",40,421,500,50,()=>{player.Sensitivity=player.Sensitivity>.12f?.055f:player.Sensitivity+.025f;Refresh();},true);
            Button(sheet,"Звук: "+Mathf.RoundToInt(mood.Volume*100)+"% · изменить",40,491,500,50,()=>{mood.SetVolume(mood.Volume>=.8f?0:mood.Volume+.2f);Refresh();},true);
            Button(sheet,"Закрыть смену досрочно",590,275,500,55,()=>{game.Paused=false;Time.timeScale=1;AudioListener.pause=false;notebook=false;game.CloseDay();});
            Text(sheet,"Закрытие смены спишет аренду 50 ₽.\nНа следующем экране можно начать новый день.\nЕго начало будет сохранено автоматически.",590,363,500,150,20,muted);
        }
        private void Report()
        {
            notebook=false;
            var sheet=Sheet("День пахнет хлебом","СМЕНА "+game.Day+" / ИТОГИ");
            Text(sheet,"Покупателей обслужено\nВыручка\nРасходы, включая аренду\nСписано несвежих товаров\nЖалобы\nДоверие района\nВ кассе",40,165,650,370,27,muted);
            Text(sheet,game.Sales+"\n"+Money.Format(game.Revenue)+"\n"+Money.Format(game.Expenses)+"\n"+game.Waste+"\n"+game.Complaints+"\n"+game.Reputation+"%\n"+Money.Format(game.Cash),800,165,290,370,27,ink);
            Text(sheet,game.Cash<0?"Касса в минусе. Остаток товара поможет вернуть долг завтра.":"Кто-то унёс домой ещё немного тепла. И это тоже результат.",40,551,1050,60,21,amber);
            Button(sheet,"Завтра в шесть · сохранить и продолжить",40,641,1050,55,game.NextDay);
        }
        private static RectTransform Full(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        private static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        private static RectTransform Panel(Transform parent,float x,float y,float w,float h,Color color)
        {var r=Rect("Панель",parent,x,y,w,h);r.gameObject.AddComponent<Image>().color=color;return r;}
        private static TextMeshProUGUI Text(Transform parent,string value,float x,float y,float w,float h,float size,Color color)
        {
            var r=Rect("Текст",parent,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=WorldArt.Font;t.text=value;t.fontSize=size;t.color=color;
            t.raycastTarget=false;t.enableWordWrapping=true;t.overflowMode=TextOverflowModes.Ellipsis;return t;
        }
        private void Button(Transform parent,string label,float x,float y,float w,float h,Action callback,bool quiet=false)
        {
            var rect=Panel(parent,x,y,w,h,quiet?new Color(.15f,.22f,.2f):amber);
            var button=rect.gameObject.AddComponent<Button>();var colors=button.colors;colors.highlightedColor=new Color(1.14f,1.14f,1.14f);colors.pressedColor=new Color(.8f,.8f,.8f);button.colors=colors;
            button.targetGraphic=rect.GetComponent<Image>();button.onClick.AddListener(()=>callback());
            var text=Text(rect,label,12,0,w-24,h,18,quiet?ink:panel);text.alignment=TextAlignmentOptions.Midline;
        }
    }
}
