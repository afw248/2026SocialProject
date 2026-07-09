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

        private CustomerOrderBubble _orderBubble;

        private StatusPanel _statusPanel;

        private SideTabBar _tabBar;

        private SettlementOverlay _settlement;

        private IngredientShopOverlay _shop;

        private MorningDeliveryOverlay _morningDelivery;

        private NewsOverlay _newsOverlay;

        private ExpressDeliveryOverlay _expressDelivery;

        private ScreenFadeController _fade;



        private RectTransform _contentArea;

        private GameObject _orderDockRoot;

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

            if (Delivery.DeliveryManager.Instance == null)

                new GameObject("Delivery").AddComponent<Delivery.DeliveryManager>();

            if (ExpressDeliveryService.Instance == null)

                new GameObject("ExpressDelivery").AddComponent<ExpressDeliveryService>();



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

            var canvasGo = new GameObject("UI_Canvas");

            var canvas = canvasGo.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;



            var scaler = canvasGo.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution = new Vector2(1920, 1080);

            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();



            var root = canvasGo.transform as RectTransform;



            _fade = new ScreenFadeController(this);

            _ = new DayClockHud(root);



            _contentArea = UiFactory.CreatePanel(root, "TabContent",

                new Vector2(0.26f, 0.12f), new Vector2(0.88f, 0.64f),

                Vector2.zero, Vector2.zero);



            _hud = new CraftHudPresenter(root, _contentArea, _controller, _ingredients);

            _hud.BindMoney(MoneyManager.Instance);



            var memoPanel = new MemoPadPanel(_contentArea);

            var recipePanel = new RecipeBookPanel(_contentArea, _menus, _ingredients);

            _statusPanel = new StatusPanel(_contentArea);



            _tabBar = new SideTabBar(root, HandleTabSelected);

            _tabBar.RegisterPanel(MainTab.Craft, _hud.CraftRoot.gameObject);

            _tabBar.RegisterPanel(MainTab.Memo, memoPanel.Root);

            _tabBar.RegisterPanel(MainTab.Recipe, recipePanel.Root);

            _tabBar.RegisterPanel(MainTab.Status, _statusPanel.Root);



            _orderDockRoot = UiFactory.CreatePanel(root, "OrderDock",

                new Vector2(0.02f, 0.38f), new Vector2(0.24f, 0.64f),

                Vector2.zero, Vector2.zero).gameObject;

            _orderBubble = new CustomerOrderBubble(_orderDockRoot.transform as RectTransform);



            _settlement = new SettlementOverlay();

            _shop = new IngredientShopOverlay();

            _morningDelivery = new MorningDeliveryOverlay();

            _newsOverlay = new NewsOverlay();

            _expressDelivery = new ExpressDeliveryOverlay();

        }



        private void HandleTabSelected(MainTab tab)

        {

            if (tab == MainTab.Status)

                _statusPanel.RefreshGauges();

        }



        private void WireEvents()

        {

            _controller.OnCraftJudged += HandleCraftJudged;

            _controller.OnSelectionChanged += list => _hud.UpdateSlot(list);

            _orderBubble.OnAccepted += OnOrderAccepted;

            _settlement.OnDismissed += OnSettlementDismissed;

            _shop.OnShoppingComplete += OnShoppingComplete;

            _morningDelivery.OnReceived += OnMorningReceived;

            _hud.OnDeliveryRequested += () => _expressDelivery.Show(_ingredients);

            var express = ExpressDeliveryService.Instance;

            if (express != null)

            {

                express.HookDayLoop(DayLoopController.Instance);

                _expressDelivery.Bind(express);

                express.OnOrderArrived += HandleExpressArrived;

            }

            DayLoopController.Instance.OnDayChanged += HandleDayChanged;

            DayLoopController.Instance.OnPhaseChanged += HandlePhaseChanged;



            HandlePhaseChanged(DayLoopController.Instance.Phase);

        }



        private void SetGameplayHudVisible(bool visible)

        {

            _contentArea.gameObject.SetActive(visible);

            _tabBar.SetVisible(visible);

            _hud.HeaderRoot.SetActive(visible);

            _orderDockRoot.SetActive(visible);

            if (!visible)

                _expressDelivery.Hide();

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

                    SetGameplayHudVisible(true);

                    _hud.SetInteractable(_orderAccepted);

                    _controller.SetCraftEnabled(_orderAccepted);

                    SpawnNextCustomer();

                    break;

                case DayPhase.Settlement:

                    _orderAccepted = false;

                    _orderBubble.HideImmediate();

                    SetGameplayHudVisible(false);

                    _settlement.Show();

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

        }



        private void OnMorningReceived()

        {

            NewsManager.Instance.RollDailyNews();

            if (!_newsOverlay.TryShow(BeginBusinessAfterMorning))

                BeginBusinessAfterMorning();

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

            _controller.Initialize(new RecipeBook(_menus), next, _ingredients);

            _orderBubble.Show(next);

        }



        private void OnOrderAccepted()

        {

            _orderAccepted = true;

            if (DayLoopController.Instance.Phase == DayPhase.Open)

            {

                _controller.SetCraftEnabled(true);

                _hud.SetInteractable(true);

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

        }

    }

}


