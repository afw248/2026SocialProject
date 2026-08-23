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
        private readonly UiTheme.HeaderMeta _headerMeta;
        private readonly Dictionary<string, int> _cart = new();
        private readonly Dictionary<string, QuantitySelectorWidget> _qtySelectors = new();
        private IReadOnlyList<IngredientSO> _ingredients;
        private bool _showingReceipt;

        public event Action OnShoppingComplete;

        public IngredientShopOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("ShopOverlay", 85);
            _root.SetActive(false);

            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = UiTheme.Background;

            var header = UiTheme.CreateHeaderBar(_root.transform, "재료 상점");
            _headerMeta = UiTheme.CreateHeaderMeta(header);

            var panel = UiTheme.CreateScreenBody(_root.transform, 72f, 24f);

            // ── 왼쪽: 재료 목록 ──
            var upgradeScroll = UiTheme.CreateBorderedPanel(panel, "UpgradeScroll",
                new Vector2(0.03f, 0.80f), new Vector2(0.74f, 0.92f),
                Vector2.zero, Vector2.zero, UiTheme.TanRow, 2f);
            var upgradeHlg = upgradeScroll.gameObject.AddComponent<HorizontalLayoutGroup>();
            upgradeHlg.spacing = 6;
            upgradeHlg.padding = new RectOffset(6, 6, 4, 4);
            upgradeHlg.childControlWidth = true;
            upgradeHlg.childControlHeight = true;
            upgradeHlg.childForceExpandWidth = true;
            _upgradeContent = upgradeScroll;

            var scrollRt = UiFactory.CreatePanel(panel, "Scroll",
                new Vector2(0.03f, 0.14f), new Vector2(0.74f, 0.79f),
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
            grid.cellSize = new Vector2(268, 196);
            grid.spacing = new Vector2(16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.UpperCenter;
            _gridContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _gridContent;

            // ── 오른쪽: 장바구니 ──
            var cartPanel = UiTheme.CreateBorderedPanel(panel, "CartPanel",
                new Vector2(0.76f, 0.14f), new Vector2(0.97f, 0.9f),
                Vector2.zero, Vector2.zero, UiTheme.CardWhite, 3f);

            UiFactory.CreateText(cartPanel, "CartTitle", "장바구니",
                new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 24, UiTheme.TextDark);

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
                TextAlignmentOptions.Center, 20, UiTheme.TextFaint);

            _cartTotalText = UiFactory.CreateText(cartPanel, "CartTotal", "합계  0원",
                new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.16f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 22, UiTheme.TextDark);
            _cartTotalText.fontStyle = FontStyles.Bold;

            _receiptBanner = UiFactory.CreateText(cartPanel, "ReceiptBanner", "",
                new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.91f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 18, UiTheme.Success);
            _receiptBanner.fontStyle = FontStyles.Bold;
            _receiptBanner.gameObject.SetActive(false);

            // ── 하단 바 ──
            _totalText = UiFactory.CreateText(panel, "Total", "합계: 0원",
                new Vector2(0.03f, 0.05f), new Vector2(0.4f, 0.12f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 22, UiTheme.TextDark);

            _balanceText = UiFactory.CreateText(panel, "Balance", "",
                new Vector2(0.4f, 0.05f), new Vector2(0.62f, 0.12f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 20, UiTheme.TextMuted);

            _actionButton = UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(panel, "BuyBtn",
                    new Vector2(0.64f, 0.04f), new Vector2(0.97f, 0.12f),
                    Vector2.zero, Vector2.zero),
                "구매 완료", UiTheme.Accent, OnActionButton, 24);
            _actionButtonLabel = _actionButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Show(IReadOnlyList<IngredientSO> ingredients)
        {
            _ingredients = ingredients;
            _cart.Clear();
            _showingReceipt = false;
            RefreshHeader();
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

        private void RefreshHeader() => UiTheme.RefreshHeaderMeta(_headerMeta);

        private void RebuildUpgrades()
        {
            foreach (Transform child in _upgradeContent)
                UnityEngine.Object.Destroy(child.gameObject);

            if (ShopUpgradeManager.Instance == null) return;

            foreach (var upgrade in ShopUpgradeManager.Instance.Catalog)
            {
                if (upgrade == null) continue;
                bool owned = ShopUpgradeManager.Instance.Owns(upgrade.upgradeType);
                bool equipped = ShopUpgradeManager.Instance.IsEquipped(upgrade.upgradeType);
                var cap = upgrade;
                var btnGo = new GameObject($"Up_{upgrade.upgradeType}", typeof(RectTransform));
                btnGo.transform.SetParent(_upgradeContent, false);
                btnGo.AddComponent<LayoutElement>().preferredHeight = 48;
                var img = btnGo.AddComponent<Image>();
                img.color = !owned ? UiTheme.Gold
                    : equipped ? UiTheme.Success
                    : UiTheme.TextFaint;
                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() =>
                {
                    if (!ShopUpgradeManager.Instance.Owns(cap.upgradeType))
                    {
                        if (ShopUpgradeManager.Instance.TryPurchase(cap))
                        {
                            RefreshHeader();
                            RebuildUpgrades();
                        }
                        return;
                    }
                    ShopUpgradeManager.Instance.ToggleEquipped(cap.upgradeType);
                    RebuildUpgrades();
                });

                var label = new GameObject("L", typeof(RectTransform));
                label.transform.SetParent(btnGo.transform, false);
                UiFactory.Stretch(label.GetComponent<RectTransform>());
                var tmp = label.AddComponent<TextMeshProUGUI>();
                tmp.text = !owned
                    ? $"{upgrade.displayName}\n{upgrade.purchaseCost:N0}원 · {upgrade.description}"
                    : equipped
                        ? $"{upgrade.displayName}\n사용중 · 눌러서 해제"
                        : $"{upgrade.displayName}\n꺼짐 · 눌러서 사용";
                tmp.fontSize = 11;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = owned && equipped ? UiTheme.CardWhite : UiTheme.TextDark;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
                tmp.raycastTarget = false;
                KoreanUiFont.Apply(tmp);
            }

            if (StaffManager.Instance == null) return;
            foreach (var staff in StaffManager.Instance.Catalog)
            {
                if (staff == null) continue;
                var cap = staff;
                bool hired = StaffManager.Instance.IsHired(staff);
                var btnGo = new GameObject($"Staff_{staff.staffId}", typeof(RectTransform));
                btnGo.transform.SetParent(_upgradeContent, false);
                btnGo.AddComponent<LayoutElement>().preferredHeight = 36;
                var img = btnGo.AddComponent<Image>();
                img.color = hired ? UiTheme.Info : new Color32(0xD9, 0x8C, 0xB0, 0xFF);
                if (!hired)
                {
                    var btn = btnGo.AddComponent<Button>();
                    btn.targetGraphic = img;
                    btn.onClick.AddListener(() =>
                    {
                        if (StaffManager.Instance.TryHire(cap))
                        {
                            RefreshHeader();
                            RebuildUpgrades();
                        }
                    });
                }

                var label = new GameObject("L", typeof(RectTransform));
                label.transform.SetParent(btnGo.transform, false);
                UiFactory.Stretch(label.GetComponent<RectTransform>());
                var tmpStaff = label.AddComponent<TextMeshProUGUI>();
                tmpStaff.text = hired
                    ? $"✓ 직원 {staff.displayName}"
                    : $"고용 {staff.displayName} ({staff.hireCost:N0}원)";
                tmpStaff.fontSize = 13;
                tmpStaff.alignment = TextAlignmentOptions.Center;
                tmpStaff.color = hired ? UiTheme.CardWhite : UiTheme.TextDark;
                tmpStaff.raycastTarget = false;
                KoreanUiFont.Apply(tmpStaff);
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
            var cardWrap = UiFactory.CreateStretchChild(_gridContent, $"Card_{ing.code}");
            var card = UiTheme.CreateBorderedPanel(cardWrap, "Fill",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UiTheme.CardWhite, 3f);

            var iconRt = UiFactory.CreatePanel(card, "Icon",
                new Vector2(0f, 0.52f), new Vector2(0f, 0.52f),
                new Vector2(14f, -36f), new Vector2(86f, 36f));
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = IngredientVisualCatalog.GetButtonIcon(ing.code);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = iconImg.sprite != null ? Color.white : new Color(0.35f, 0.38f, 0.45f);

            int stock = InventoryManager.Instance.GetStock(ing.code);
            int warehouse = InventoryManager.Instance.GetWarehouse(ing.code);

            int unitPrice = InventoryManager.Instance.GetEffectivePurchasePrice(ing);

            UiFactory.CreateText(card, "Name", $"{ing.displayName}\n{unitPrice:N0}원",
                new Vector2(0f, 0.52f), new Vector2(1f, 0.94f),
                new Vector2(98f, 0f), new Vector2(-10f, 0f),
                TextAlignmentOptions.MidlineLeft, 16, UiTheme.TextDark);

            UiFactory.CreateText(card, "Stock",
                $"보유 {stock}  ·  배달대기 {warehouse}",
                new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.52f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 13, UiTheme.TextMuted);

            string code = ing.code;
            int initial = _cart.TryGetValue(code, out var q) ? q : 0;
            _qtySelectors[code] = new QuantitySelectorWidget(
                card, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.38f),
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
            RefreshHeader();
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
            header.gameObject.AddComponent<Image>().color = UiTheme.TanRow;

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
            tmp.color = UiTheme.TextDark;
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
            RefreshHeader();
        }
    }
}

