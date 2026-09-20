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
        private RectTransform canvas;
        private RectTransform hud;
        private RectTransform overlay;
        private TextMeshProUGUI clock;
        private TextMeshProUGUI prompt;
        private TextMeshProUGUI message;
        private TextMeshProUGUI stats;
        private TextMeshProUGUI goal;
        private TextMeshProUGUI orderText;
        private TextMeshProUGUI bagText;
        private TextMeshProUGUI patience;
        private bool home = true;
        private bool notebook;
        private bool newGameConfirm;
        private int tab;
        private float messageTime;
        private float nextHudUpdate;
        private string lastMessage = string.Empty;

        private readonly Color ink = new Color(.91f, .86f, .74f);
        private readonly Color muted = new Color(.59f, .65f, .6f);
        private readonly Color amber = new Color(.84f, .6f, .32f);
        private readonly Color success = new Color(.46f, .72f, .48f);
        private readonly Color danger = new Color(.76f, .34f, .28f);
        private readonly Color panel = new Color(.07f, .105f, .1f, .97f);

        public void Initialize(GameSession session, PlayerController controller, Atmosphere atmosphere)
        {
            game = session;
            player = controller;
            mood = atmosphere;

            var root = new GameObject(
                "Интерфейс",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<RectTransform>();
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = .5f;

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            BuildHud();
            EventBus.Changed += OnGameChanged;
            EventBus.Message += Notify;
            RefreshView();
        }

        private void BuildHud()
        {
            hud = Full("HUD", canvas);
            stats = Text(hud, string.Empty, 32f, 24f, 950f, 35f, 19f, ink);
            clock = Text(hud, string.Empty, 1080f, 24f, 488f, 70f, 18f, ink);
            clock.alignment = TextAlignmentOptions.TopRight;
            goal = Text(hud, string.Empty, 32f, 64f, 1000f, 34f, 16f, amber);

            var card = Panel(hud, 22f, 112f, 365f, 242f, new Color(.07f, .105f, .1f, .88f));
            Text(card, "У ОКНА", 20f, 16f, 300f, 25f, 13f, amber);
            orderText = Text(card, string.Empty, 20f, 47f, 325f, 150f, 18f, ink);
            patience = Text(card, string.Empty, 20f, 205f, 320f, 25f, 13f, muted);

            bagText = Text(hud, string.Empty, 30f, 648f, 540f, 145f, 17f, ink);
            prompt = Text(hud, string.Empty, 430f, 630f, 740f, 44f, 20f, ink);
            prompt.alignment = TextAlignmentOptions.Center;
            message = Text(hud, string.Empty, 350f, 716f, 900f, 95f, 20f, ink);
            message.alignment = TextAlignmentOptions.Center;

            Text(
                hud,
                "WASD  ходить     E  взять / действие     F  выдать     X  очистить пакет     TAB  блокнот     R  радио     ESC  пауза",
                30f,
                853f,
                1540f,
                28f,
                14f,
                muted);

            var reticle = Text(hud, "·", 790f, 438f, 20f, 20f, 24f, ink);
            reticle.alignment = TextAlignmentOptions.Center;
        }

        private void OnDestroy()
        {
            EventBus.Changed -= OnGameChanged;
            EventBus.Message -= Notify;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void Notify(string value)
        {
            lastMessage = value ?? string.Empty;
            messageTime = Time.unscaledTime + 9f;
            if (message != null) message.text = lastMessage;
        }

        private void Update()
        {
            if (game == null) return;

            HandleInput();
            if (Time.unscaledTime >= nextHudUpdate)
            {
                nextHudUpdate = Time.unscaledTime + .15f;
                RefreshHud();
            }

            if (message != null && messageTime < Time.unscaledTime) message.text = string.Empty;
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || home) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (notebook)
                {
                    notebook = false;
                    game.Modal = game.Paying;
                }
                else if (!game.Report && !game.Paying)
                {
                    game.Paused = !game.Paused;
                }

                Time.timeScale = game.Paused ? 0f : 1f;
                AudioListener.pause = game.Paused;
                RefreshView();
            }

            if (keyboard.tabKey.wasPressedThisFrame &&
                game.Running &&
                !game.Paused &&
                !game.Paying)
            {
                notebook = !notebook;
                game.Modal = notebook;
                RefreshView();
            }

            if (keyboard.rKey.wasPressedThisFrame && !game.Paused && mood != null)
            {
                mood.NextStation();
            }
        }

        public void OpenNotebook(int page = 0)
        {
            if (game == null || !game.Running || game.Paying) return;
            notebook = true;
            tab = Mathf.Clamp(page, 0, 3);
            game.Modal = true;
            RefreshView();
        }

        private void OnGameChanged()
        {
            RefreshHud();
            if (NeedsOverlay()) RebuildOverlay();
        }

        private void RefreshView()
        {
            RefreshHud();
            RebuildOverlay();
        }

        private void RefreshHud()
        {
            if (game == null || hud == null) return;
            hud.gameObject.SetActive(!home);

            var plan = game.CurrentPlan;
            var campaignDay = Mathf.Min(game.Day, CampaignRules.CampaignDays);
            var dayLabel = game.Day <= CampaignRules.CampaignDays
                ? "ДЕНЬ " + campaignDay + "/" + CampaignRules.CampaignDays
                : "СВОБОДНЫЙ РЕЖИМ • ДЕНЬ " + game.Day;

            stats.text =
                "ТЁПЛЫЙ ХЛЕБ     /     " + Money.Format(game.Cash) +
                "     /     ДОВЕРИЕ  " + game.Reputation + "%" +
                "     /     ПРОДАНО  " + game.TotalSales;

            var hour = Mathf.Clamp((int)game.Hour, 0, 23);
            var minute = Mathf.Clamp((int)((game.Hour % 1f) * 60f), 0, 59);
            clock.text =
                "СЕНТЯБРЬ 2002  /  СМЕНА " + game.Day + "\n" +
                hour.ToString("00") + ":" + minute.ToString("00") +
                "  •  " + (mood != null ? mood.WeatherLabel : "погода");

            goal.text = dayLabel +
                        (plan != null ? "  •  " + plan.Title.ToUpperInvariant() + "  •  " : "  •  ") +
                        game.GoalProgressText();
            goal.color = game.IsDailyGoalReached() ? success : amber;

            prompt.text = game.Modal || game.Paused || player == null ? string.Empty : player.Prompt;

            var queueCount = game.Queue != null ? game.Queue.Count : 0;
            var customer = game.Customer;
            patience.text = customer != null
                ? "Терпение  " + Mathf.CeilToInt(customer.Patience * 100f) + "%     ·     В очереди: " + queueCount
                : "В очереди: " + queueCount;

            orderText.text = customer == null
                ? "Покупатели скоро подойдут.\n\nА пока — послушайте двор и проверьте запас."
                : (customer.Data != null ? customer.Data.DisplayName : "Покупатель") +
                  "\n\n" + customer.Order.Describe(game.Products) +
                  "\nИтого: " + Money.Format(customer.Order.Total);

            var groupedBag = game.Bag
                .Where(batch => batch != null)
                .GroupBy(batch => batch.productId)
                .Select(group =>
                {
                    var product = game.FindProduct(group.Key);
                    var title = product != null ? product.Title : group.Key;
                    return title + "  × " + group.Sum(batch => batch.quantity);
                });

            var bagCount = game.Bag.Where(batch => batch != null).Sum(batch => batch.quantity);
            bagText.text =
                "БУМАЖНЫЙ ПАКЕТ  /  " + bagCount + "\n" +
                (bagCount == 0
                    ? "Пока пуст. Товары на полках за спиной."
                    : string.Join("\n", groupedBag));
        }

        private bool NeedsOverlay()
        {
            return home || game.Report || game.Paused || game.Paying || notebook;
        }

        private void RebuildOverlay()
        {
            if (canvas == null) return;

            if (overlay != null)
            {
                overlay.gameObject.SetActive(false);
                Destroy(overlay.gameObject);
                overlay = null;
            }

            if (!NeedsOverlay()) return;
            overlay = Full("Окно интерфейса", canvas);

            if (home) Home();
            else if (game.Report) Report();
            else if (game.Paused) Pause();
            else if (game.Paying) CashDesk();
            else if (notebook) Notebook();
        }

        private void Home()
        {
            Panel(overlay, 0f, 0f, 670f, 900f, new Color(.045f, .075f, .07f, .97f));
            Text(
                overlay,
                "СЕМИДНЕВНАЯ ИСТОРИЯ БОЛЬШОГО ДВОРА",
                64f,
                62f,
                540f,
                35f,
                13f,
                amber);
            Text(overlay, "Тёплый\nхлеб", 56f, 132f, 570f, 235f, 88f, ink);
            Text(
                overlay,
                "У каждого дома есть свой запах.",
                64f,
                394f,
                540f,
                45f,
                22f,
                ink);
            Text(
                overlay,
                "Сентябрь, 2002. За стеклом моросит дождь.\n" +
                "За семь смен маленький ларёк должен стать частью двора.\n" +
                "Считайте сдачу, берегите свежий хлеб и запоминайте людей.",
                64f,
                450f,
                535f,
                125f,
                18f,
                muted);

            Button(
                overlay,
                newGameConfirm ? "Подтвердить новую историю" : "Начать семидневную историю",
                64f,
                604f,
                490f,
                58f,
                () =>
                {
                    if (SaveSystem.Exists && !newGameConfirm)
                    {
                        newGameConfirm = true;
                        RebuildOverlay();
                        return;
                    }

                    SaveSystem.DeleteAll();
                    newGameConfirm = false;
                    home = false;
                    game.Begin(false);
                    RefreshView();
                });

            if (SaveSystem.Exists)
            {
                Button(
                    overlay,
                    "Продолжить сохранённую историю",
                    64f,
                    680f,
                    490f,
                    48f,
                    () =>
                    {
                        newGameConfirm = false;
                        home = false;
                        game.Begin(true);
                        RefreshView();
                    },
                    true);
            }

            Text(
                overlay,
                "Версия 0.3 • процедурные 3D-модели • три финала\n" +
                "Автосохранение выполняется в начале каждой смены.",
                64f,
                798f,
                540f,
                65f,
                14f,
                muted);
        }

        private RectTransform Sheet(string title, string eyebrow, float height = 730f)
        {
            Panel(overlay, 0f, 0f, 1600f, 900f, new Color(0f, 0f, 0f, .58f));
            var sheet = Panel(overlay, 230f, 85f, 1140f, height, panel);
            Text(sheet, eyebrow, 38f, 24f, 1030f, 25f, 13f, amber);
            Text(sheet, title, 36f, 61f, 1050f, 66f, 39f, ink);
            return sheet;
        }

        private void CashDesk()
        {
            var customer = game.Customer;
            if (customer == null) return;

            var customerName = customer.Data != null ? customer.Data.DisplayName : "Покупатель";
            var sheet = Sheet("Сдача — дело точное", "КАССА / " + customerName);
            Text(sheet, "К ОПЛАТЕ\n" + Money.Format(customer.Order.Total), 40f, 152f, 290f, 95f, 25f, ink);
            Text(sheet, "ПОКУПАТЕЛЬ ДАЛ\n" + Money.Format(customer.Order.Paid), 395f, 152f, 320f, 95f, 25f, ink);
            Text(sheet, "В ВАШЕЙ РУКЕ\n" + Money.Format(game.ChangeInHand), 775f, 152f, 325f, 95f, 25f, amber);
            Text(
                sheet,
                "Наберите сдачу монетами и купюрами. Готового ответа нет — район запоминает точность.",
                40f,
                269f,
                1050f,
                40f,
                18f,
                muted);

            for (var index = 0; index < Money.Denominations.Length; index++)
            {
                var value = Money.Denominations[index];
                Button(
                    sheet,
                    Money.Format(value),
                    40f + index % 4 * 264f,
                    330f + index / 4 * 80f,
                    248f,
                    62f,
                    () => game.AddChange(value),
                    true);
            }

            Text(sheet, lastMessage, 40f, 515f, 1060f, 70f, 18f, amber);
            Button(sheet, "Пересчитать", 40f, 628f, 310f, 55f, game.ClearChange, true);
            Button(sheet, "Передать сдачу и пакет", 540f, 628f, 550f, 55f, game.Checkout);
        }

        private void Notebook()
        {
            var sheet = Sheet("Блокнот продавца", "ПРИЛАВОК / ПОСТАВКИ / ЛЮДИ / НЕДЕЛЯ");
            Button(sheet, "Прилавок", 40f, 137f, 205f, 43f, () => SwitchTab(0), tab != 0);
            Button(sheet, "Поставщик", 255f, 137f, 205f, 43f, () => SwitchTab(1), tab != 1);
            Button(sheet, "Истории", 470f, 137f, 205f, 43f, () => SwitchTab(2), tab != 2);
            Button(sheet, "Неделя", 685f, 137f, 175f, 43f, () => SwitchTab(3), tab != 3);
            Button(
                sheet,
                "Закрыть · TAB",
                875f,
                137f,
                218f,
                43f,
                () =>
                {
                    notebook = false;
                    game.Modal = false;
                    RefreshView();
                },
                true);

            if (tab == 2)
            {
                Text(
                    sheet,
                    game.Journal.Count == 0
                        ? "Пока чистые страницы. Истории появятся после первых покупателей."
                        : string.Join("\n\n", game.Journal),
                    40f,
                    208f,
                    1040f,
                    450f,
                    20f,
                    ink);
                return;
            }

            if (tab == 3)
            {
                WeeklyPage(sheet);
                return;
            }

            var visibleProducts = game.Products.Where(product => product != null).Take(10).ToArray();
            for (var index = 0; index < visibleProducts.Length; index++)
            {
                var product = visibleProducts[index];
                var y = 205f + index * 42f;
                Text(sheet, product.Title, 40f, y, 405f, 35f, 17f, ink);
                Text(sheet, Money.Format(product.Price), 438f, y, 132f, 35f, 17f, amber);
                Text(sheet, "На полке: " + game.Stock.Count(product.Id), 575f, y, 195f, 35f, 16f, muted);

                if (tab == 1)
                {
                    Button(
                        sheet,
                        "Ящик ×10 · " + Money.Format(product.Cost * 10),
                        790f,
                        y - 3f,
                        303f,
                        34f,
                        () => game.BuyStock(product),
                        true);
                }
                else
                {
                    Button(
                        sheet,
                        "В пакет",
                        790f,
                        y - 3f,
                        303f,
                        34f,
                        () => game.AddToBag(product),
                        true);
                }
            }

            var deliverySeconds = game.CurrentPlan != null
                ? Mathf.RoundToInt(game.CurrentPlan.DeliverySeconds)
                : 25;
            Text(
                sheet,
                tab == 1
                    ? "Доставка: около " + deliverySeconds + " сек. В пути: " + game.PendingDeliveries + ". " + lastMessage
                    : "Соберите точный заказ. Лишнее возвращается клавишей X. Время идёт, пока открыт блокнот.",
                40f,
                646f,
                1040f,
                60f,
                16f,
                muted);
        }

        private void WeeklyPage(RectTransform sheet)
        {
            var plan = game.CurrentPlan;
            var campaignDay = Mathf.Min(game.Day, CampaignRules.CampaignDays);
            Text(
                sheet,
                game.Day <= CampaignRules.CampaignDays
                    ? "ДЕНЬ " + campaignDay + " ИЗ " + CampaignRules.CampaignDays
                    : "СВОБОДНЫЙ РЕЖИМ",
                40f,
                210f,
                450f,
                35f,
                18f,
                amber);
            Text(
                sheet,
                plan != null ? plan.Title : "Обычный день",
                40f,
                255f,
                600f,
                48f,
                30f,
                ink);
            Text(
                sheet,
                plan != null ? plan.Description : string.Empty,
                40f,
                318f,
                620f,
                100f,
                19f,
                muted);

            Text(
                sheet,
                "ЦЕЛЬ СМЕНЫ\n" + game.GoalProgressText() +
                "\n\nАренда: " + Money.Format(plan != null ? plan.Rent : 5000) +
                "\nПремия: " + Money.Format(plan != null ? plan.GoalBonus : 0) +
                "\nПогода: " + (plan != null ? plan.WeatherLabel : "переменчивая"),
                40f,
                445f,
                600f,
                180f,
                19f,
                ink);

            Text(
                sheet,
                "ИТОГИ ИСТОРИИ\n\n" +
                "Продаж всего: " + game.TotalSales +
                "\nВыручка всего: " + Money.Format(game.TotalRevenue) +
                "\nВыполнено целей: " + game.GoalsCompleted + "/" + CampaignRules.CampaignDays +
                "\nОткрыто историй: " + game.Journal.Count +
                "\nДоверие района: " + game.Reputation + "%",
                705f,
                220f,
                370f,
                300f,
                21f,
                ink);

            Text(
                sheet,
                game.CampaignCompleted
                    ? "История недели завершена. Полученный финал:\n" + CampaignRules.EndingTitle(game.EndingId)
                    : "Финал зависит от доверия, кассы и числа обслуженных людей.",
                705f,
                535f,
                370f,
                110f,
                18f,
                game.CampaignCompleted ? success : muted);
        }

        private void SwitchTab(int value)
        {
            tab = value;
            RebuildOverlay();
        }

        private void Pause()
        {
            var sheet = Sheet("Тихая пауза", "МОЖНО ПЕРЕВЕСТИ ДУХ");
            Text(
                sheet,
                "Игра, время и очередь остановлены.\nПри выходе вы вернётесь к началу сохранённой смены.",
                40f,
                151f,
                1040f,
                75f,
                22f,
                muted);
            Button(
                sheet,
                "Вернуться за прилавок",
                40f,
                275f,
                500f,
                55f,
                () =>
                {
                    game.Paused = false;
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    RefreshView();
                });
            Button(
                sheet,
                "Покачивание камеры: " + (player != null && player.HeadBob ? "да" : "нет"),
                40f,
                351f,
                500f,
                50f,
                () =>
                {
                    if (player != null) player.HeadBob = !player.HeadBob;
                    RebuildOverlay();
                },
                true);
            Button(
                sheet,
                "Мышь: " + (player != null ? player.Sensitivity.ToString("0.000") : "—") + " · изменить",
                40f,
                421f,
                500f,
                50f,
                () =>
                {
                    if (player != null)
                    {
                        player.Sensitivity = player.Sensitivity > .12f
                            ? .055f
                            : player.Sensitivity + .025f;
                    }
                    RebuildOverlay();
                },
                true);
            Button(
                sheet,
                "Звук: " + Mathf.RoundToInt((mood != null ? mood.Volume : 0f) * 100f) + "% · изменить",
                40f,
                491f,
                500f,
                50f,
                () =>
                {
                    if (mood != null) mood.SetVolume(mood.Volume >= .8f ? 0f : mood.Volume + .2f);
                    RebuildOverlay();
                },
                true);
            Button(
                sheet,
                "Закрыть смену досрочно",
                590f,
                275f,
                500f,
                55f,
                () =>
                {
                    game.Paused = false;
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    notebook = false;
                    game.CloseDay();
                });
            Text(
                sheet,
                "Досрочное закрытие завершает текущую смену.\n" +
                "Аренда будет списана, а невыполненная цель не даст премию.\n" +
                "Начало следующего дня сохранится автоматически.",
                590f,
                363f,
                500f,
                160f,
                20f,
                muted);
        }

        private void Report()
        {
            notebook = false;
            if (game.IsCampaignFinale)
            {
                FinaleReport();
                return;
            }

            var sheet = Sheet("День пахнет хлебом", "СМЕНА " + game.Day + " / ИТОГИ");
            Text(
                sheet,
                "Покупателей обслужено\nВыручка\nРасходы, включая аренду\nСписано несвежих товаров\nЖалобы\nДоверие района\nВ кассе",
                40f,
                165f,
                650f,
                350f,
                26f,
                muted);
            Text(
                sheet,
                game.Sales + "\n" +
                Money.Format(game.Revenue) + "\n" +
                Money.Format(game.Expenses) + "\n" +
                game.Waste + "\n" +
                game.Complaints + "\n" +
                game.Reputation + "%\n" +
                Money.Format(game.Cash),
                800f,
                165f,
                290f,
                350f,
                26f,
                ink);

            var resultColor = game.GoalAchievedToday ? success : amber;
            var result = game.GoalAchievedToday
                ? "Цель выполнена. Премия: " + Money.Format(game.GoalBonusAwarded) + ". Доверие выросло."
                : "Цель не выполнена полностью. Завтра двор даст ещё один шанс.";
            Text(sheet, result, 40f, 535f, 1050f, 65f, 21f, resultColor);
            Text(
                sheet,
                game.Cash < 0
                    ? "Касса в минусе. Остаток товара поможет вернуть долг завтра."
                    : "Кто-то унёс домой ещё немного тепла. И это тоже результат.",
                40f,
                590f,
                1050f,
                45f,
                18f,
                game.Cash < 0 ? danger : muted);
            Button(
                sheet,
                "Завтра в шесть · сохранить и продолжить",
                40f,
                650f,
                1050f,
                55f,
                game.NextDay);
        }

        private void FinaleReport()
        {
            var title = CampaignRules.EndingTitle(game.EndingId);
            var text = CampaignRules.EndingText(game.EndingId);
            var sheet = Sheet(title, "ФИНАЛ СЕМИДНЕВНОЙ ИСТОРИИ");

            Text(sheet, text, 40f, 150f, 1050f, 195f, 22f, ink);
            Text(
                sheet,
                "ЗА НЕДЕЛЮ\n\n" +
                "Продаж: " + game.TotalSales +
                "\nОбщая выручка: " + Money.Format(game.TotalRevenue) +
                "\nВыполнено целей: " + game.GoalsCompleted + "/" + CampaignRules.CampaignDays +
                "\nИсторий в блокноте: " + game.Journal.Count +
                "\nДоверие района: " + game.Reputation + "%" +
                "\nКасса: " + Money.Format(game.Cash),
                40f,
                380f,
                470f,
                240f,
                22f,
                muted);
            Text(
                sheet,
                game.GoalAchievedToday
                    ? "Последний план выполнен. Премия: " + Money.Format(game.GoalBonusAwarded) + "."
                    : "Последний день был непростым, но история всё равно получила свой финал.",
                600f,
                405f,
                485f,
                115f,
                20f,
                game.GoalAchievedToday ? success : amber);
            Text(
                sheet,
                "После финала можно продолжить играть без ограничения по дням: цели, погода и поток покупателей будут меняться дальше.",
                600f,
                530f,
                485f,
                90f,
                18f,
                muted);
            Button(
                sheet,
                "Продолжить в свободном режиме",
                40f,
                650f,
                1050f,
                55f,
                game.NextDay);
        }

        private static RectTransform Full(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform Rect(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static RectTransform Panel(
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            Color color)
        {
            var rect = Rect("Панель", parent, x, y, width, height);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        private static TextMeshProUGUI Text(
            Transform parent,
            string value,
            float x,
            float y,
            float width,
            float height,
            float size,
            Color color)
        {
            var rect = Rect("Текст", parent, x, y, width, height);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (WorldArt.Font != null) text.font = WorldArt.Font;
            text.text = value ?? string.Empty;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.TopLeft;
            return text;
        }

        private void Button(
            Transform parent,
            string label,
            float x,
            float y,
            float width,
            float height,
            Action callback,
            bool quiet = false)
        {
            var rect = Panel(
                parent,
                x,
                y,
                width,
                height,
                quiet ? new Color(.15f, .22f, .2f) : amber);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(.95f, .95f, .82f);
            colors.pressedColor = new Color(.68f, .68f, .6f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() => callback?.Invoke());

            var text = Text(rect, label, 12f, 0f, width - 24f, height, 18f, quiet ? ink : panel);
            text.alignment = TextAlignmentOptions.Center;
        }
    }
}
