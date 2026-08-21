using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Delivery;
using ChangJun.Economy;
using ChangJun.Inventory;
using ChangJun.Progression;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 영업 중 긴급 배달 주문 UI — 한집 / 알뜰 선택.
    /// </summary>
    public sealed class ExpressDeliveryOverlay
    {
        private readonly GameObject _root;
        private readonly RectTransform _gridContent;
        private readonly RectTransform _pendingContent;
        private readonly TextMeshProUGUI _tierInfoText;
        private readonly TextMeshProUGUI _totalText;
        private readonly TextMeshProUGUI _balanceText;
        private readonly Button _hanjipBtn;
        private readonly Button _economyBtn;
        private readonly Image _hanjipBtnImg;
        private readonly Image _economyBtnImg;
        private readonly Dictionary<string, int> _cart = new();
        private readonly Dictionary<string, QuantitySelectorWidget> _qtySelectors = new();
        private IReadOnlyList<IngredientSO> _ingredients;
        private ExpressDeliveryTier _tier = ExpressDeliveryTier.Economy;
        private bool _visible;
        private bool _bound;

        public ExpressDeliveryOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("ExpressDelivery", 72);
            _root.SetActive(false);

            var panel = UiTheme.CreateBorderedPanel(_root.transform, "Panel",
                new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f),
                Vector2.zero, Vector2.zero, UiTheme.Background);

            UiFactory.CreateText(panel, "Title", "영업 중 배달",
                new Vector2(0.03f, 0.92f), new Vector2(0.5f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 28, UiTheme.TextDark);

            var tierBar = UiFactory.CreatePanel(panel, "TierBar",
                new Vector2(0.52f, 0.92f), new Vector2(0.97f, 0.99f),
                Vector2.zero, Vector2.zero);

            _economyBtn = CreateTierButton(tierBar, "알뜰 60분", new Vector2(0f, 0f),
                new Vector2(0.48f, 1f), UiTheme.Success, SelectEconomy);
            _hanjipBtn = CreateTierButton(tierBar, "한집 30분", new Vector2(0.52f, 0f),
                new Vector2(1f, 1f), UiTheme.Accent, SelectHanjip);
            _economyBtnImg = _economyBtn.targetGraphic as Image;
            _hanjipBtnImg = _hanjipBtn.targetGraphic as Image;

            _tierInfoText = UiFactory.CreateText(panel, "TierInfo", "",
                new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.91f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 18, UiTheme.TextMuted);

            var scrollRt = UiFactory.CreatePanel(panel, "Scroll",
                new Vector2(0.03f, 0.28f), new Vector2(0.7f, 0.85f),
                Vector2.zero, Vector2.zero);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            UiFactory.ConfigureScroll(scroll);

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.02f);

            _gridContent = UiFactory.CreateStretchChild(viewport, "Content");
            _gridContent.pivot = new Vector2(0.5f, 1f);
            _gridContent.anchorMin = new Vector2(0, 1);
            _gridContent.anchorMax = new Vector2(1, 1);

            var grid = _gridContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(240, 150);
            grid.spacing = new Vector2(8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.padding = new RectOffset(4, 4, 4, 4);
            _gridContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = _gridContent;

            var pendingPanel = UiTheme.CreateBorderedPanel(panel, "Pending",
                new Vector2(0.6f, 0.28f), new Vector2(0.97f, 0.85f),
                Vector2.zero, Vector2.zero, UiTheme.CardWhite, 3f);

            UiFactory.CreateText(pendingPanel, "PendingTitle", "배달 예정",
                new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 20, UiTheme.TextDark);

            var pendingScrollRt = UiFactory.CreatePanel(pendingPanel, "PendingScroll",
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.88f),
                Vector2.zero, Vector2.zero);
            var pendingScroll = pendingScrollRt.gameObject.AddComponent<ScrollRect>();
            pendingScroll.horizontal = false;
            UiFactory.ConfigureScroll(pendingScroll);

            var pendingViewport = UiFactory.CreateStretchChild(pendingScrollRt, "Viewport");
            pendingViewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            pendingViewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.02f);

            _pendingContent = UiFactory.CreateStretchChild(pendingViewport, "Content");
            _pendingContent.pivot = new Vector2(0.5f, 1f);
            _pendingContent.anchorMin = new Vector2(0, 1);
            _pendingContent.anchorMax = new Vector2(1, 1);

            var pendingVlg = _pendingContent.gameObject.AddComponent<VerticalLayoutGroup>();
            pendingVlg.spacing = 4;
            pendingVlg.padding = new RectOffset(4, 4, 4, 4);
            pendingVlg.childControlWidth = true;
            pendingVlg.childControlHeight = true;
            pendingVlg.childForceExpandWidth = true;
            pendingVlg.childForceExpandHeight = false;
            _pendingContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            pendingScroll.viewport = pendingViewport;
            pendingScroll.content = _pendingContent;

            _totalText = UiFactory.CreateText(panel, "Total", "합계: 0원",
                new Vector2(0.03f, 0.18f), new Vector2(0.4f, 0.25f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 22, UiTheme.TextDark);

            _balanceText = UiFactory.CreateText(panel, "Balance", "",
                new Vector2(0.4f, 0.18f), new Vector2(0.62f, 0.25f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 20, UiTheme.TextMuted);

            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(panel, "Close",
                    new Vector2(0.64f, 0.16f), new Vector2(0.8f, 0.26f),
                    Vector2.zero, Vector2.zero),
                "닫기", UiTheme.CardWhite, Hide, 22, UiTheme.TextDark);

            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(panel, "Order",
                    new Vector2(0.82f, 0.16f), new Vector2(0.97f, 0.26f),
                    Vector2.zero, Vector2.zero),
                "주문", UiTheme.Accent, PlaceOrder, 22);
        }

        public void Bind(ExpressDeliveryService service)
        {
            if (service == null || _bound) return;
            _bound = true;
            service.OnPendingChanged += RefreshPending;
            service.OnOrderArrived += _ => RefreshPending();
        }

        public bool IsVisible => _root.activeSelf;

        public void Show(IReadOnlyList<IngredientSO> ingredients)
        {
            if (DayLoopController.Instance.Phase != DayPhase.Open)
                return;

            _ingredients = ingredients;
            _cart.Clear();
            _tier = ExpressDeliveryTier.Economy;
            _visible = true;
            DayLoopController.Instance.OnTimeChanged += HandleTimeChanged;
            RefreshTierVisuals();
            RebuildGrid();
            RefreshCartTotal();
            RefreshPending();
            _root.SetActive(true);
        }

        public void Toggle(IReadOnlyList<IngredientSO> ingredients)
        {
            if (IsVisible) Hide();
            else Show(ingredients);
        }

        public void Hide()
        {
            _visible = false;
            if (DayLoopController.Instance != null)
                DayLoopController.Instance.OnTimeChanged -= HandleTimeChanged;
            _root.SetActive(false);
        }

        private void HandleTimeChanged(int hour, int minute)
        {
            if (!_visible) return;
            RefreshTierVisuals();
            RefreshPending();
        }

        private void SelectEconomy()
        {
            _tier = ExpressDeliveryTier.Economy;
            RefreshTierVisuals();
            RebuildGrid();
            RefreshCartTotal();
        }

        private void SelectHanjip()
        {
            _tier = ExpressDeliveryTier.Hanjip;
            RefreshTierVisuals();
            RebuildGrid();
            RefreshCartTotal();
        }

        private void RefreshTierVisuals()
        {
            if (DayLoopController.Instance == null) return;

            var config = DayLoopController.Instance.Config;
            bool hanjip = _tier == ExpressDeliveryTier.Hanjip;

            if (_economyBtnImg != null)
            {
                var c = UiTheme.Success;
                _economyBtnImg.color = hanjip ? new Color(c.r, c.g, c.b, 0.45f) : c;
            }

            if (_hanjipBtnImg != null)
            {
                var c = UiTheme.Accent;
                _hanjipBtnImg.color = hanjip ? c : new Color(c.r, c.g, c.b, 0.45f);
            }

            if (_tierInfoText == null) return;

            int minutes = hanjip ? config.expressDeliveryMinutes : config.economyDeliveryMinutes;
            float mult = hanjip ? config.expressDeliveryPriceMultiplier : 1f;
            int now = DayLoopController.Instance.CurrentMinutes;
            int eta = now + minutes;
            int remain = minutes;
            _tierInfoText.text =
                $"{(hanjip ? "한집" : "알뜰")} 배달 · {remain}분 후 도착 " +
                $"({ExpressDeliveryService.FormatArrival(eta)}) · 가격 x{mult:0.#}";
        }

        private static string FormatRemaining(int arrivalMinutes)
        {
            if (DayLoopController.Instance == null) return "";
            int remain = arrivalMinutes - DayLoopController.Instance.CurrentMinutes;
            if (remain <= 0) return "곧 도착";
            return $"{remain}분 후 ({ExpressDeliveryService.FormatArrival(arrivalMinutes)})";
        }

        private void RebuildGrid()
        {
            _qtySelectors.Clear();
            foreach (Transform child in _gridContent)
                UnityEngine.Object.Destroy(child.gameObject);

            IngredientVisualCatalog.EnsureLoaded();
            foreach (var ing in _ingredients)
            {
                if (ing == null) continue;
                if (!UnderstandingManager.Instance.IsUnlocked(ing.code)) continue;
                CreateCard(ing);
            }
        }

        private void CreateCard(IngredientSO ing)
        {
            var cardWrap = UiFactory.CreateStretchChild(_gridContent, $"Card_{ing.code}");
            cardWrap.gameObject.AddComponent<LayoutElement>().preferredHeight = 150;
            var card = UiTheme.CreateBorderedPanel(cardWrap, "Fill",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UiTheme.CardWhite, 3f);

            var config = DayLoopController.Instance.Config;
            float mult = _tier == ExpressDeliveryTier.Hanjip
                ? config.expressDeliveryPriceMultiplier
                : 1f;
            int unitPrice = Mathf.RoundToInt(ing.purchasePrice * mult);

            var iconRt = UiFactory.CreatePanel(card, "Icon",
                new Vector2(0.04f, 0.58f), new Vector2(0.2f, 0.92f),
                Vector2.zero, Vector2.zero);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = IngredientVisualCatalog.GetButtonIcon(ing.code);
            iconImg.preserveAspect = true;
            iconImg.color = iconImg.sprite != null ? Color.white : new Color(0.35f, 0.38f, 0.45f);

            UiFactory.CreateText(card, "Name", $"{ing.displayName}\n{unitPrice:N0}원/개",
                new Vector2(0.22f, 0.58f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 16, UiTheme.TextDark);

            UiFactory.CreateText(card, "Stock", $"보유 {InventoryManager.Instance.GetStock(ing.code)}",
                new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.58f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, UiTheme.TextMuted);

            string code = ing.code;
            int initial = _cart.TryGetValue(code, out var q) ? q : 0;
            _qtySelectors[code] = new QuantitySelectorWidget(
                card, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.42f),
                qty => SetCartQuantity(code, qty), initial);
        }

        private void SetCartQuantity(string code, int qty)
        {
            if (qty <= 0) _cart.Remove(code);
            else _cart[code] = qty;
            RefreshCartTotal();
        }

        private void AdjustCart(string code, int delta)
        {
            int next = (_cart.TryGetValue(code, out var q) ? q : 0) + delta;
            SetCartQuantity(code, next);
            if (_qtySelectors.TryGetValue(code, out var selector))
                selector.SetQuantity(next, notify: false);
        }

        private void RefreshCartTotal()
        {
            var config = DayLoopController.Instance.Config;
            float mult = _tier == ExpressDeliveryTier.Hanjip
                ? config.expressDeliveryPriceMultiplier
                : 1f;

            int total = 0;
            foreach (var pair in _cart)
            {
                var ing = InventoryManager.Instance.GetIngredient(pair.Key);
                if (ing == null) continue;
                total += Mathf.RoundToInt(ing.purchasePrice * mult) * pair.Value;
            }

            _totalText.text = $"합계: {total:N0}원";
            _balanceText.text = $"잔액: {MoneyManager.Instance.Money:N0}원";
        }

        private void RefreshPending()
        {
            if (!_visible || _pendingContent == null) return;
            if (ExpressDeliveryService.Instance == null) return;

            foreach (Transform child in _pendingContent)
                UnityEngine.Object.Destroy(child.gameObject);

            var pending = ExpressDeliveryService.Instance.Pending;
            if (pending.Count == 0)
            {
                var empty = new GameObject("Empty", typeof(RectTransform));
                empty.transform.SetParent(_pendingContent, false);
                empty.AddComponent<LayoutElement>().preferredHeight = 36;
                var tmp = empty.AddComponent<TextMeshProUGUI>();
                tmp.text = "예정된 배달 없음";
                tmp.fontSize = 16;
                tmp.color = UiTheme.TextFaint;
                tmp.alignment = TextAlignmentOptions.Center;
                KoreanUiFont.Apply(tmp);
                return;
            }

            foreach (var order in pending)
            {
                var ing = InventoryManager.Instance.GetIngredient(order.IngredientCode);
                if (ing == null) continue;

                var row = new GameObject("PendingRow", typeof(RectTransform));
                row.transform.SetParent(_pendingContent, false);
                row.AddComponent<LayoutElement>().preferredHeight = 40;
                row.AddComponent<Image>().color = UiTheme.TanRow;

                var textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(row.transform, false);
                UiFactory.Stretch(textGo.GetComponent<RectTransform>());
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                string tier = order.Tier == ExpressDeliveryTier.Hanjip ? "한집" : "알뜰";
                tmp.text =
                    $"{ing.displayName} x{order.Quantity}  [{tier}]\n" +
                    FormatRemaining(order.ArrivalMinutes);
                tmp.fontSize = 15;
                tmp.color = UiTheme.TextDark;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.margin = new Vector4(8f, 4f, 4f, 4f);
                KoreanUiFont.Apply(tmp);
            }
        }

        private void PlaceOrder()
        {
            if (_cart.Count == 0) return;

            if (!ExpressDeliveryService.Instance.TryPlaceOrder(_tier, _cart))
            {
                Debug.Log("[ExpressDelivery] 주문 실패 — 잔액 부족 또는 영업 시간 아님");
                return;
            }

            _cart.Clear();
            foreach (var selector in _qtySelectors.Values)
                selector.SetQuantity(0, notify: false);

            RefreshCartTotal();
            RefreshPending();
            RefreshTierVisuals();
        }

        private static Button CreateTierButton(Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color color, Action onClick)
        {
            var rt = UiFactory.CreatePanel(parent, label, anchorMin, anchorMax,
                Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());
            UiFactory.CreateText(rt, "T", label,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 17, Color.white);
            return btn;
        }
    }
}
