using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Inventory;
using ChangJun.Progression;
using ChangJun.Social;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    public sealed class IngredientShopOverlay
    {
        private readonly GameObject _root;
        private readonly RectTransform _gridContent;
        private readonly RectTransform _cartListContent;
        private readonly GameObject _cartHeader;
        private readonly TextMeshProUGUI _receiptBanner;
        private readonly TextMeshProUGUI _cartEmptyText;
        private readonly TextMeshProUGUI _cartTotalText;
        private readonly TextMeshProUGUI _totalText;
        private readonly TextMeshProUGUI _balanceText;
        private readonly Button _actionButton;
        private readonly TextMeshProUGUI _actionButtonLabel;
        private readonly RectTransform _upgradeContent;
        private readonly Dictionary<string, int> _cart = new();
        private readonly Dictionary<string, QuantitySelectorWidget> _qtySelectors = new();
        private IReadOnlyList<IngredientSO> _ingredients;
        private bool _showingReceipt;

        public event Action OnShoppingComplete;

        public IngredientShopOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("ShopOverlay", 85);
            _root.SetActive(false);

            var panel = UiFactory.CreatePanel(_root.transform, "Panel",
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f),
                Vector2.zero, Vector2.zero);
            panel.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.96f, 0.98f);

            UiFactory.CreateText(panel, "Title", "온라인 재료 마켓",
                new Vector2(0.03f, 0.92f), new Vector2(0.55f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 30,
                new Color(0.1f, 0.2f, 0.4f));

            // ── 왼쪽: 재료 목록 ──
            var upgradeScroll = UiFactory.CreatePanel(panel, "UpgradeScroll",
                new Vector2(0.03f, 0.82f), new Vector2(0.58f, 0.9f),
                Vector2.zero, Vector2.zero);
            upgradeScroll.gameObject.AddComponent<Image>().color = new Color(0.9f, 0.93f, 0.98f);
            var upgradeHlg = upgradeScroll.gameObject.AddComponent<HorizontalLayoutGroup>();
            upgradeHlg.spacing = 6;
            upgradeHlg.padding = new RectOffset(6, 6, 4, 4);
            upgradeHlg.childControlWidth = true;
            upgradeHlg.childControlHeight = true;
            upgradeHlg.childForceExpandWidth = true;
            _upgradeContent = upgradeScroll;

            var scrollRt = UiFactory.CreatePanel(panel, "Scroll",
                new Vector2(0.03f, 0.14f), new Vector2(0.58f, 0.81f),
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
            grid.cellSize = new Vector2(260, 160);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.padding = new RectOffset(6, 6, 6, 6);
            _gridContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _gridContent;

            // ── 오른쪽: 장바구니 ──
            var cartPanel = UiFactory.CreatePanel(panel, "CartPanel",
                new Vector2(0.6f, 0.14f), new Vector2(0.97f, 0.9f),
                Vector2.zero, Vector2.zero);
            cartPanel.gameObject.AddComponent<Image>().color = new Color(0.88f, 0.91f, 0.96f);

            UiFactory.CreateText(cartPanel, "CartTitle", "장바구니",
                new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 24,
                new Color(0.1f, 0.2f, 0.35f));

            CreateCartHeader(cartPanel, out _cartHeader);

            var cartScrollRt = UiFactory.CreatePanel(cartPanel, "CartScroll",
                new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.82f),
                Vector2.zero, Vector2.zero);
            var cartScroll = cartScrollRt.gameObject.AddComponent<ScrollRect>();
            cartScroll.horizontal = false;
            UiFactory.ConfigureScroll(cartScroll);

            var cartViewport = UiFactory.CreateStretchChild(cartScrollRt, "Viewport");
            cartViewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            cartViewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.02f);

            _cartListContent = UiFactory.CreateStretchChild(cartViewport, "Content");
            _cartListContent.pivot = new Vector2(0.5f, 1f);
            _cartListContent.anchorMin = new Vector2(0, 1);
            _cartListContent.anchorMax = new Vector2(1, 1);

            var cartVlg = _cartListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            cartVlg.spacing = 6;
            cartVlg.padding = new RectOffset(4, 4, 4, 4);
            cartVlg.childControlWidth = true;
            cartVlg.childControlHeight = true;
            cartVlg.childForceExpandWidth = true;
            cartVlg.childForceExpandHeight = false;
            _cartListContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            cartScroll.viewport = cartViewport;
            cartScroll.content = _cartListContent;

            _cartEmptyText = UiFactory.CreateText(cartPanel, "Empty", "담은 재료가 없습니다",
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.6f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 20,
                new Color(0.45f, 0.48f, 0.55f));

            _cartTotalText = UiFactory.CreateText(cartPanel, "CartTotal", "합계  0원",
                new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.16f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 22,
                new Color(0.1f, 0.15f, 0.25f));
            _cartTotalText.fontStyle = FontStyles.Bold;

            _receiptBanner = UiFactory.CreateText(cartPanel, "ReceiptBanner", "",
                new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.91f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 18,
                new Color(0.15f, 0.45f, 0.25f));
            _receiptBanner.fontStyle = FontStyles.Bold;
            _receiptBanner.gameObject.SetActive(false);

            // ── 하단 바 ──
            _totalText = UiFactory.CreateText(panel, "Total", "합계: 0원",
                new Vector2(0.03f, 0.05f), new Vector2(0.4f, 0.12f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 22,
                new Color(0.1f, 0.15f, 0.25f));

            _balanceText = UiFactory.CreateText(panel, "Balance", "",
                new Vector2(0.4f, 0.05f), new Vector2(0.62f, 0.12f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 20,
                new Color(0.2f, 0.35f, 0.2f));

            var buyRt = UiFactory.CreatePanel(panel, "BuyBtn",
                new Vector2(0.64f, 0.04f), new Vector2(0.97f, 0.12f),
                Vector2.zero, Vector2.zero);
            _actionButton = buyRt.gameObject.AddComponent<Button>();
            _actionButton.targetGraphic = buyRt.gameObject.AddComponent<Image>();
            _actionButton.targetGraphic.color = new Color(0.15f, 0.4f, 0.7f);
            _actionButton.onClick.AddListener(OnActionButton);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(buyRt, false);
            UiFactory.Stretch(labelGo.GetComponent<RectTransform>());
            _actionButtonLabel = labelGo.AddComponent<TextMeshProUGUI>();
            _actionButtonLabel.text = "구매 완료";
            _actionButtonLabel.fontSize = 24;
            _actionButtonLabel.color = Color.white;
            _actionButtonLabel.alignment = TextAlignmentOptions.Center;
            _actionButtonLabel.raycastTarget = false;
            KoreanUiFont.Apply(_actionButtonLabel);
        }

        public void Show(IReadOnlyList<IngredientSO> ingredients)
        {
            _ingredients = ingredients;
            _cart.Clear();
            _showingReceipt = false;
            _cartHeader.SetActive(true);
            _receiptBanner.gameObject.SetActive(false);
            _actionButtonLabel.text = "구매 완료";
            _actionButton.interactable = true;
            RebuildUpgrades();
            RebuildGrid();
            RefreshCart();
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private void RebuildUpgrades()
        {
            foreach (Transform child in _upgradeContent)
                UnityEngine.Object.Destroy(child.gameObject);

            if (ShopUpgradeManager.Instance == null) return;

            foreach (var upgrade in ShopUpgradeManager.Instance.Catalog)
            {
                if (upgrade == null) continue;
                bool owned = ShopUpgradeManager.Instance.Owns(upgrade.upgradeType);
                var cap = upgrade;
                var btnGo = new GameObject($"Up_{upgrade.upgradeType}", typeof(RectTransform));
                btnGo.transform.SetParent(_upgradeContent, false);
                btnGo.AddComponent<LayoutElement>().preferredHeight = 36;
                var img = btnGo.AddComponent<Image>();
                img.color = owned ? new Color(0.55f, 0.75f, 0.55f) : new Color(0.55f, 0.65f, 0.85f);
                if (!owned)
                {
                    var btn = btnGo.AddComponent<Button>();
                    btn.targetGraphic = img;
                    btn.onClick.AddListener(() =>
                    {
                        if (ShopUpgradeManager.Instance.TryPurchase(cap))
                            RebuildUpgrades();
                    });
                }

                var label = new GameObject("L", typeof(RectTransform));
                label.transform.SetParent(btnGo.transform, false);
                UiFactory.Stretch(label.GetComponent<RectTransform>());
                var tmp = label.AddComponent<TextMeshProUGUI>();
                tmp.text = owned
                    ? $"✓ {upgrade.displayName}"
                    : $"{upgrade.displayName} ({upgrade.purchaseCost:N0}원)";
                tmp.fontSize = 13;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                KoreanUiFont.Apply(tmp);
            }
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
                CreateShopCard(ing);
            }
        }

        private void CreateShopCard(IngredientSO ing)
        {
            var card = UiFactory.CreateStretchChild(_gridContent, $"Card_{ing.code}");
            var le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 160;
            le.preferredWidth = 260;
            card.gameObject.AddComponent<Image>().color = Color.white;

            var iconRt = UiFactory.CreatePanel(card, "Icon",
                new Vector2(0.04f, 0.58f), new Vector2(0.22f, 0.92f),
                Vector2.zero, Vector2.zero);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = IngredientVisualCatalog.GetButtonIcon(ing.code);
            iconImg.preserveAspect = true;
            iconImg.color = iconImg.sprite != null ? Color.white : new Color(0.35f, 0.38f, 0.45f);

            int stock = InventoryManager.Instance.GetStock(ing.code);
            int warehouse = InventoryManager.Instance.GetWarehouse(ing.code);

            int unitPrice = InventoryManager.Instance.GetEffectivePurchasePrice(ing);

            UiFactory.CreateText(card, "Name", $"{ing.displayName}\n{unitPrice:N0}원",
                new Vector2(0.24f, 0.58f), new Vector2(0.96f, 0.92f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 17,
                new Color(0.1f, 0.12f, 0.2f));

            UiFactory.CreateText(card, "Stock",
                $"보유 {stock}  ·  배달대기 {warehouse}",
                new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.58f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14,
                new Color(0.35f, 0.4f, 0.5f));

            string code = ing.code;
            int initial = _cart.TryGetValue(code, out var q) ? q : 0;
            _qtySelectors[code] = new QuantitySelectorWidget(
                card, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.42f),
                qty => SetCartQuantity(code, qty), initial);
        }

        private void SetCartQuantity(string code, int qty)
        {
            if (_showingReceipt) return;

            if (qty <= 0) _cart.Remove(code);
            else _cart[code] = qty;

            RefreshCart();
        }

        private void AdjustCart(string code, int delta)
        {
            if (_showingReceipt) return;

            int next = (_cart.TryGetValue(code, out var q) ? q : 0) + delta;
            SetCartQuantity(code, next);

            if (_qtySelectors.TryGetValue(code, out var selector))
                selector.SetQuantity(next, notify: false);
        }

        private void RefreshCart()
        {
            foreach (Transform child in _cartListContent)
                UnityEngine.Object.Destroy(child.gameObject);

            int total = 0;
            int unitCount = 0;
            float bulk = GetBulkDiscountMultiplier(_cart, out unitCount);
            bool hasItems = _cart.Count > 0;
            _cartEmptyText.gameObject.SetActive(!hasItems && !_showingReceipt);

            var sorted = new List<KeyValuePair<string, int>>(_cart);
            sorted.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

            foreach (var pair in sorted)
            {
                var ing = ResolveIngredient(pair.Key);
                if (ing == null) continue;

                int unitPrice = InventoryManager.Instance.GetEffectivePurchasePrice(ing);
                int lineTotal = Mathf.RoundToInt(unitPrice * pair.Value * bulk);
                total += lineTotal;
                ReceiptUiHelper.CreateReceiptLine(_cartListContent, ing.displayName, pair.Value, lineTotal, false);
            }

            UpdateTotals(total, bulk, unitCount);
            RefreshActionButtonLabel();
        }

        private void RefreshActionButtonLabel()
        {
            if (_showingReceipt)
            {
                _actionButtonLabel.text = "계속 구매";
                return;
            }

            // 장바구니가 비어 있어도 하루를 넘길 수 있음
            _actionButtonLabel.text = _cart.Count > 0 ? "구매 완료" : "구매 없이 넘어가기";
        }

        private void UpdateTotals(int total, float bulk = 1f, int unitCount = 0)
        {
            string bulkNote = unitCount >= 20 ? " (대량 −10%)" : unitCount >= 10 ? " (대량 −5%)" : "";
            string totalLine = _showingReceipt
                ? "구매한 재료는 아침에 도착합니다"
                : $"합계  {total:N0}원{bulkNote}";

            _cartTotalText.text = totalLine;
            _totalText.text = $"합계: {total:N0}원{bulkNote}";
            _balanceText.text = $"잔액: {MoneyManager.Instance.Money:N0}원";
        }

        private static float GetBulkDiscountMultiplier(Dictionary<string, int> cart, out int unitCount)
        {
            unitCount = 0;
            foreach (var pair in cart)
                unitCount += pair.Value;
            if (unitCount >= 20) return 0.9f;
            if (unitCount >= 10) return 0.95f;
            return 1f;
        }

        private int CalculateCartTotal()
        {
            float bulk = GetBulkDiscountMultiplier(_cart, out _);
            int total = 0;
            foreach (var pair in _cart)
            {
                var ing = ResolveIngredient(pair.Key);
                if (ing == null) continue;
                int unitPrice = InventoryManager.Instance.GetEffectivePurchasePrice(ing);
                total += Mathf.RoundToInt(unitPrice * pair.Value * bulk);
            }
            return total;
        }

        private IngredientSO ResolveIngredient(string code)
        {
            var ing = InventoryManager.Instance.GetIngredient(code);
            if (ing != null) return ing;

            if (_ingredients == null) return null;
            foreach (var candidate in _ingredients)
            {
                if (candidate != null && candidate.code == code)
                    return candidate;
            }
            return null;
        }

        private static void CreateCartHeader(Transform cartPanel, out GameObject headerRoot)
        {
            var header = UiFactory.CreatePanel(cartPanel, "CartHeader",
                new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.91f),
                Vector2.zero, Vector2.zero);
            headerRoot = header.gameObject;
            header.gameObject.AddComponent<Image>().color = new Color(0.75f, 0.8f, 0.88f);

            var hlg = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 4;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            CreateHeaderCell(header, "품목", 1f, TextAlignmentOptions.MidlineLeft);
            CreateHeaderCell(header, "수량", 56f, TextAlignmentOptions.Center);
            CreateHeaderCell(header, "금액", 72f, TextAlignmentOptions.MidlineRight);
        }

        private static void CreateHeaderCell(Transform parent, string label, float width,
            TextAlignmentOptions align)
        {
            var cell = new GameObject(label, typeof(RectTransform));
            cell.transform.SetParent(parent, false);
            var le = cell.AddComponent<LayoutElement>();
            if (width <= 1f)
            {
                le.flexibleWidth = 1f;
                le.minWidth = 80f;
            }
            else
            {
                le.preferredWidth = width;
                le.minWidth = width;
            }

            var tmp = cell.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 15;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.15f, 0.2f, 0.3f);
            tmp.alignment = align;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
        }

        private void OnActionButton()
        {
            if (_showingReceipt)
            {
                ResumeShoppingAfterReceipt();
                return;
            }

            // 아무것도 안 사도 다음날로 진행 가능
            if (_cart.Count == 0)
            {
                Hide();
                OnShoppingComplete?.Invoke();
                return;
            }

            ConfirmPurchase();
        }

        private void ResumeShoppingAfterReceipt()
        {
            _showingReceipt = false;
            _cart.Clear();
            _cartHeader.SetActive(true);
            _receiptBanner.gameObject.SetActive(false);

            foreach (Transform child in _cartListContent)
                UnityEngine.Object.Destroy(child.gameObject);

            foreach (var selector in _qtySelectors.Values)
                selector.SetQuantity(0, notify: false);

            RebuildGrid();
            RefreshCart();
        }

        private void ConfirmPurchase()
        {
            if (_cart.Count == 0)
            {
                Hide();
                OnShoppingComplete?.Invoke();
                return;
            }

            int total = CalculateCartTotal();

            if (total > MoneyManager.Instance.Money)
            {
                Debug.Log("[Shop] 잔액 부족");
                return;
            }

            var purchased = new Dictionary<string, int>(_cart);

            float bulk = GetBulkDiscountMultiplier(purchased, out _);

            foreach (var pair in purchased)
            {
                InventoryManager.Instance.PurchaseToWarehouse(pair.Key, pair.Value);
                var ing = ResolveIngredient(pair.Key);
                if (ing != null)
                {
                    int unitPrice = InventoryManager.Instance.GetEffectivePurchasePrice(ing);
                    int lineCost = Mathf.RoundToInt(unitPrice * pair.Value * bulk);
                    DayLoopController.Instance.Ledger.AddPurchase(
                        lineCost,
                        $"{ing.displayName} x{pair.Value}");
                }
            }

            if (total > 0)
                MoneyManager.Instance.SpendMoney(total);

            ShowPurchaseReceipt(purchased, bulk);
        }

        private void ShowPurchaseReceipt(Dictionary<string, int> purchased, float bulk)
        {
            _showingReceipt = true;
            _cart.Clear();
            _actionButtonLabel.text = "계속 구매";
            _cartHeader.SetActive(false);
            _receiptBanner.gameObject.SetActive(true);
            _receiptBanner.text = "구매 완료! 내일 아침 배달됩니다. 더 살 수도 있어요.";

            foreach (Transform child in _cartListContent)
                UnityEngine.Object.Destroy(child.gameObject);

            _cartEmptyText.gameObject.SetActive(false);

            int total = 0;
            var sorted = new List<KeyValuePair<string, int>>(purchased);
            sorted.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

            foreach (var pair in sorted)
            {
                var ing = ResolveIngredient(pair.Key);
                if (ing == null) continue;

                int unitPrice = InventoryManager.Instance.GetEffectivePurchasePrice(ing);
                int lineTotal = Mathf.RoundToInt(unitPrice * pair.Value * bulk);
                total += lineTotal;

                int warehouse = InventoryManager.Instance.GetWarehouse(ing.code);
                int stock = InventoryManager.Instance.GetStock(ing.code);
                ReceiptUiHelper.CreateReceiptLine(
                    _cartListContent,
                    $"{ing.displayName}\n배달대기 {warehouse} · 보유 {stock}",
                    pair.Value,
                    lineTotal,
                    true);
            }

            RebuildGrid();
            _cartTotalText.text = $"결제  {total:N0}원";
            _totalText.text = $"합계: {total:N0}원";
            _balanceText.text = $"잔액: {MoneyManager.Instance.Money:N0}원";
        }
    }
}

