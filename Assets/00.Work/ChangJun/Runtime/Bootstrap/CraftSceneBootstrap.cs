using System.Collections;

using System.Collections.Generic;

using ChangJun.Craft;

using ChangJun.Customer;

using ChangJun.Data;

using ChangJun.Delivery;

using ChangJun.Economy;

using ChangJun.Inventory;

using ChangJun.Judge;

using ChangJun.News;

using ChangJun.Progression;

using ChangJun.Social;

using ChangJun.Time;

using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.InputSystem.UI;

using UnityEngine.UI;



namespace ChangJun.Bootstrap

{

    /// <summary>

    /// Craft 씬 조립 루트 — 매니저·Presenter를 연결하고 DayPhase에 따라 UI를 제어한다.

    /// </summary>

    public sealed class CraftSceneBootstrap : MonoBehaviour

    {

        private CraftController _controller;

        private CraftHudPresenter _hud;

        private MemoPadPanel _memoPanel;

        private CustomerOrderBubble _orderBubble;

        private StatusPanel _statusPanel;

        private RecipeBookPanel _recipePanel;

        private SideTabBar _tabBar;

        private SettlementOverlay _settlement;

        private IngredientShopOverlay _shop;

        private MorningDeliveryOverlay _morningDelivery;

        private NewsOverlay _newsOverlay;

        private StockMarketOverlay _stockMarket;

        private BusinessTransitionOverlay _businessTransition;

        private ExpressDeliveryOverlay _expressDelivery;

        private ScreenFadeController _fade;



        private RectTransform _contentArea;

        private GameObject _topBar;

        private GameObject _orderDockRoot;

        private bool _phaseHudVisible;

        private bool _subScreenActive;

        private List<IngredientSO> _ingredients;

        private List<MenuRecipeSO> _menus;

        private List<CraftCustomerSO> _customers;

        private CraftCustomerSO _lastCustomer;

        private Coroutine _nextCustomerRoutine;

        private Coroutine _dayTransitionRoutine;

        private bool _orderAccepted;



        private void Start()

        {

            _ingredients = new List<IngredientSO>(Resources.LoadAll<IngredientSO>("Craft/Ingredients"));

            _menus = new List<MenuRecipeSO>(Resources.LoadAll<MenuRecipeSO>("Craft/Menus"));

            _customers = new List<CraftCustomerSO>(Resources.LoadAll<CraftCustomerSO>("Craft/Customers"));

            _menus.Sort((a, b) => string.CompareOrdinal(a.code, b.code));



            if (_ingredients.Count == 0 || _menus.Count == 0 || _customers.Count == 0)

            {

                Debug.LogError("[Bootstrap] SO 에셋 없음. Tools > CupRice > Build Craft Prototype 실행.");

                return;

            }



            EnsureEventSystem();

            BuildManagers();

            BuildUI();

            WireEvents();

            DayLoopController.Instance.StartNewGame();

        }



        private static void EnsureEventSystem()

        {

            if (FindFirstObjectByType<EventSystem>() != null) return;

            var esGo = new GameObject("EventSystem");

            esGo.AddComponent<EventSystem>();

            esGo.AddComponent<InputSystemUIInputModule>();

        }



        private void BuildManagers()

        {

            if (MoneyManager.Instance == null)

                new GameObject("MoneyManager").AddComponent<MoneyManager>();

            if (DayLoopController.Instance == null)

                new GameObject("DayLoop").AddComponent<DayLoopController>();

            if (InventoryManager.Instance == null)

                new GameObject("Inventory").AddComponent<InventoryManager>();

            if (UnderstandingManager.Instance == null)

                new GameObject("Understanding").AddComponent<UnderstandingManager>();

            if (NewsManager.Instance == null)

                new GameObject("News").AddComponent<NewsManager>();

            if (StockMarketManager.Instance == null)

                new GameObject("StockMarket").AddComponent<StockMarketManager>();

            if (Delivery.DeliveryManager.Instance == null)

                new GameObject("Delivery").AddComponent<Delivery.DeliveryManager>();

            if (ExpressDeliveryService.Instance == null)

                new GameObject("ExpressDelivery").AddComponent<ExpressDeliveryService>();

            if (StoreReputationService.Instance == null)

                new GameObject("StoreReputation").AddComponent<StoreReputationService>();

            if (CulturalEventManager.Instance == null)

                new GameObject("CulturalEvents").AddComponent<CulturalEventManager>();

            if (SchoolLunchContractService.Instance == null)

                new GameObject("SchoolLunch").AddComponent<SchoolLunchContractService>();

            if (ShopUpgradeManager.Instance == null)

                new GameObject("ShopUpgrades").AddComponent<ShopUpgradeManager>();



            var config = DayLoopController.Instance.Config;

            InventoryManager.Instance.Initialize(_ingredients, config.starterStockPerIngredient);



            var thresholds = new List<UnderstandingThresholdSO>(

                Resources.LoadAll<UnderstandingThresholdSO>("Craft/Thresholds"));

            UnderstandingManager.Instance.Initialize(_ingredients, thresholds, config);



            _controller = gameObject.AddComponent<CraftController>();

            _controller.Initialize(new RecipeBook(_menus), null, _ingredients);

            _controller.SetCraftEnabled(false);

        }



        private void BuildUI()

        {

            var existingCanvas = GameObject.Find("UI_Canvas");

            if (existingCanvas != null)

                Destroy(existingCanvas);

            var canvasGo = new GameObject("UI_Canvas");

            var canvas = canvasGo.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder = 0;

            canvas.overrideSorting = true;



            var scaler = canvasGo.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution = new Vector2(1920, 1080);

            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();



            var root = canvasGo.transform as RectTransform;



            CreateBackground();

            // 예전 배경 스프라이트가 주문받기/조리 UI 여백 사이로 비쳐 보이지 않도록,
            // UI_Canvas 전체를 덮는 단일 크림색 배경을 가장 먼저 깐다.
            var rootBg = UiFactory.CreateStretchChild(root, "RootBg");
            var rootBgImg = rootBg.gameObject.AddComponent<Image>();
            rootBgImg.color = UiTheme.Background;
            rootBgImg.raycastTarget = false;



            _fade = new ScreenFadeController(this);

            var topBar = UiTheme.CreateHeaderBar(root, string.Empty, 72f);
            _topBar = topBar.gameObject;
            _ = new DayClockHud(topBar);



            _contentArea = UiFactory.CreatePanel(root, "TabContent",

                new Vector2(0.02f, 0.03f), new Vector2(0.9f, 0.9f),

                Vector2.zero, Vector2.zero);



            _hud = new CraftHudPresenter(root, _contentArea, _controller, _ingredients, topBar);

            _hud.BindMoney(MoneyManager.Instance);



            // 메모/도감/정보는 탭박스 안에서 스와핑되는 게 아니라, 목업처럼 각자 헤더·뒤로가기를
            // 가진 독립 풀스크린 화면으로 뜬다.
            _memoPanel = new MemoPadPanel();
            _recipePanel = new RecipeBookPanel(_menus, _ingredients);
            _statusPanel = new StatusPanel();

            _memoPanel.OnBack += ReturnToCraftHome;
            _recipePanel.OnBack += ReturnToCraftHome;
            _statusPanel.OnBack += ReturnToCraftHome;

            _tabBar = new SideTabBar(root);
            _tabBar.OnTabSelected += HandleTabSelected;



            // 주문받기 화면 — 조리 콘텐츠와 같은 자리를 차지하는 전면 화면 (토글로 전환)
            _orderDockRoot = UiFactory.CreatePanel(root, "OrderDock",

                new Vector2(0.02f, 0.03f), new Vector2(0.9f, 0.9f),

                Vector2.zero, Vector2.zero).gameObject;

            _orderBubble = new CustomerOrderBubble(_orderDockRoot.transform as RectTransform);



            _settlement = new SettlementOverlay();

            _shop = new IngredientShopOverlay();

            _morningDelivery = new MorningDeliveryOverlay();

            _newsOverlay = new NewsOverlay();

            _stockMarket = new StockMarketOverlay();
            _stockMarket.OnBack += () =>
            {
                _stockMarket.Hide();
                _newsOverlay.TryShow(ShowStockMarketFromNews);
            };

            _expressDelivery = new ExpressDeliveryOverlay();

            _businessTransition = new BusinessTransitionOverlay();

        }



        private static void CreateBackground()
        {
            var canvasGo = new GameObject("BackgroundCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;
            canvas.overrideSorting = true;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.GetComponent<RectTransform>();
            UiFactory.Stretch(root);

            var bg = UiFactory.CreateStretchChild(root, "Background");
            var img = bg.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = false;
            img.color = Color.white;

            var sprite = Resources.Load<Sprite>("Craft/Sprites/CraftBackground");
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
            }
            else
            {
                img.color = new Color(0.08f, 0.08f, 0.12f, 1f);
                Debug.LogWarning("[Bootstrap] CraftBackground 스프라이트를 찾지 못했습니다.");
            }
        }

        private void HandleTabSelected(MainTab tab)
        {
            _subScreenActive = true;
            RefreshHomeVisibility();

            switch (tab)
            {
                case MainTab.Memo:
                    _memoPanel.Show();
                    break;
                case MainTab.Recipe:
                    _recipePanel.Show();
                    break;
                case MainTab.Status:
                    _statusPanel.RefreshTree();
                    _statusPanel.Show();
                    break;
            }
        }

        private void ReturnToCraftHome()
        {
            _memoPanel.Hide();
            _recipePanel.Hide();
            _statusPanel.Hide();
            _subScreenActive = false;
            RefreshHomeVisibility();
        }



        private void WireEvents()

        {

            _controller.OnCraftJudged += HandleCraftJudged;

            _controller.OnSelectionChanged += list => _hud.UpdateSlot(list);

            _orderBubble.OnAccepted += OnOrderAccepted;

            _settlement.OnDismissed += OnSettlementDismissed;

            _shop.OnShoppingComplete += OnShoppingComplete;

            _morningDelivery.OnReceived += OnMorningReceived;

            _tabBar.OnDeliveryRequested += () => _expressDelivery.Toggle(_ingredients);

            var express = ExpressDeliveryService.Instance;

            if (express != null)

            {

                express.HookDayLoop(DayLoopController.Instance);

                _expressDelivery.Bind(express);

                express.OnOrderArrived += HandleExpressArrived;

            }

            DayLoopController.Instance.OnDayChanged += HandleDayChanged;

            DayLoopController.Instance.OnPhaseChanged += HandlePhaseChanged;

            if (UnderstandingManager.Instance != null)

                UnderstandingManager.Instance.OnNodeUnlocked += HandleNodeUnlocked;



            HandlePhaseChanged(DayLoopController.Instance.Phase);

        }



        /// <summary>제작 홈 화면(탭콘텐츠·상단바·사이드내비·주문 도크)의 실제 표시 여부를 갱신한다.
        /// 영업(Open) 페이즈이면서 동시에 메모/도감/정보 같은 서브 화면이 떠있지 않을 때만 보인다.</summary>
        private void RefreshHomeVisibility()
        {
            bool showHome = _phaseHudVisible && !_subScreenActive;
            _contentArea.gameObject.SetActive(showHome);
            _tabBar.SetVisible(showHome);
            _topBar.SetActive(showHome);
            _orderDockRoot.SetActive(showHome);
        }

        private void SetGameplayHudVisible(bool visible)
        {
            _phaseHudVisible = visible;
            _subScreenActive = false;
            _memoPanel.Hide();
            _recipePanel.Hide();
            _statusPanel.Hide();

            if (!visible)
                _expressDelivery.Hide();

            RefreshHomeVisibility();
        }



        private void HandlePhaseChanged(DayPhase phase)

        {

            switch (phase)

            {

                case DayPhase.Morning:

                    _orderAccepted = false;

                    _orderBubble.HideImmediate();

                    SetGameplayHudVisible(false);

                    _morningDelivery.Show();

                    break;

                case DayPhase.Open:

                    ExpressDeliveryService.Instance?.BeginBusinessTracking();

                    _businessTransition.ShowOpen(() =>
                    {
                        _businessTransition.Hide();
                        SetGameplayHudVisible(true);
                        _hud.SetInteractable(_orderAccepted);
                        _controller.SetCraftEnabled(_orderAccepted);
                        SpawnNextCustomer();
                    });

                    break;

                case DayPhase.Settlement:

                    _orderAccepted = false;

                    _orderBubble.HideImmediate();

                    SetGameplayHudVisible(false);

                    _businessTransition.ShowClosing(() =>
                    {
                        _businessTransition.Hide();
                        _settlement.Show();
                    });

                    break;

                case DayPhase.Shopping:

                    _orderBubble.HideImmediate();

                    SetGameplayHudVisible(false);

                    _shop.Show(_ingredients);

                    break;

                case DayPhase.Closed:

                    _orderAccepted = false;

                    _orderBubble.HideImmediate();

                    break;

            }

        }



        private void HandleDayChanged(int day)

        {

            ExpressDeliveryService.Instance?.ResetForNewDay();

            StoreReputationService.Instance?.ResetDailyStats();

            SchoolLunchContractService.Instance?.AdvanceDay();

            SchoolLunchContractService.Instance?.TryStartContract(day);

            var config = DayLoopController.Instance.Config;

            if (config.inflationIntervalDays > 0 && day > 1 && day % config.inflationIntervalDays == 0)

                InventoryManager.Instance?.ApplyInflation(config.inflationRatePerTick);

            if (config.dividendIntervalDays > 0 && day > 1 && day % config.dividendIntervalDays == 0)

                StockMarketManager.Instance?.PayDividends(config.dividendRate);

            _memoPanel?.ClearOrderHistory();

        }



        private void OnMorningReceived()

        {

            NewsManager.Instance.RollDailyNews();

            CulturalEventManager.Instance?.RollDailyEvents();

            StockMarketManager.Instance?.RollDailyMarket(NewsManager.Instance.TodayNews);

            if (!_newsOverlay.TryShow(ShowStockMarketFromNews))

                BeginBusinessAfterMorning();

        }



        private void ShowStockMarketFromNews()
        {
            _newsOverlay.Hide();
            _stockMarket.Show(BeginBusinessAfterMorning);
        }



        private void BeginBusinessAfterMorning()

        {

            DayLoopController.Instance.BeginBusinessDay();

        }



        private void SpawnNextCustomer()

        {

            if (DayLoopController.Instance.Phase != DayPhase.Open) return;



            if (!CustomerSpawnService.TryPick(_customers, _lastCustomer, out var next))

            {

                Debug.LogWarning("[Bootstrap] 스폰 가능한 손님 없음 — 영업 종료");

                DayLoopController.Instance.CloseShop();

                return;

            }



            _lastCustomer = next;

            _orderAccepted = false;

            _controller.SetCraftEnabled(false);

            _hud.SetInteractable(false);

            _hud.SetCookViewVisible(false);

            _controller.Initialize(new RecipeBook(_menus), next, _ingredients);

            _orderBubble.Show(next);

        }



        private void OnOrderAccepted()

        {

            _orderAccepted = true;

            _memoPanel?.RecordCustomerOrder(_controller.CurrentCustomer);

            if (DayLoopController.Instance.Phase == DayPhase.Open)

            {

                _controller.SetCraftEnabled(true);

                _hud.SetInteractable(true);

                _hud.SetCookViewVisible(true);

            }

        }



        private void HandleCraftJudged(CraftResult result, MenuRecipeSO matched)

        {

            _hud.ShowResult(result, matched);



            if (DayLoopController.Instance.Phase != DayPhase.Open)

                return;



            if (_nextCustomerRoutine != null)

                StopCoroutine(_nextCustomerRoutine);

            _nextCustomerRoutine = StartCoroutine(NextCustomerAfterDelay(1.2f));

        }



        private IEnumerator NextCustomerAfterDelay(float delay)

        {

            yield return new WaitForSeconds(delay);



            if (DayLoopController.Instance.Phase == DayPhase.Open)

                SpawnNextCustomer();



            _nextCustomerRoutine = null;

        }



        private void OnSettlementDismissed()

        {

            DayLoopController.Instance.EnterShopping();

        }



        private void HandleExpressArrived(ExpressDeliveryOrder order)

        {

            var ing = InventoryManager.Instance.GetIngredient(order.IngredientCode);

            if (ing != null)

                _hud.ShowDeliveryArrived(ing.displayName, order.Quantity);

        }



        private void OnShoppingComplete()

        {

            if (_dayTransitionRoutine != null)

                StopCoroutine(_dayTransitionRoutine);

            _dayTransitionRoutine = StartCoroutine(AdvanceDayWithFade());

        }



        private IEnumerator AdvanceDayWithFade()

        {

            yield return _fade.Transition(0.4f, 0.6f, 0.8f, () =>

            {

                DayLoopController.Instance.AdvanceToNextDay();

            });

            _dayTransitionRoutine = null;

        }



        private void HandleNodeUnlocked(UnderstandingNodeSO node)

        {

            if (node == null) return;

            _hud?.ShowBanner($"[해금] {node.displayName}");

        }



        private void OnDestroy()

        {

            var express = ExpressDeliveryService.Instance;

            if (express != null)

            {

                express.OnOrderArrived -= HandleExpressArrived;

                express.UnhookDayLoop(DayLoopController.Instance);

            }

            if (DayLoopController.Instance != null)

            {

                DayLoopController.Instance.OnDayChanged -= HandleDayChanged;

                DayLoopController.Instance.OnPhaseChanged -= HandlePhaseChanged;

            }

            if (UnderstandingManager.Instance != null)

                UnderstandingManager.Instance.OnNodeUnlocked -= HandleNodeUnlocked;

        }

    }

}


