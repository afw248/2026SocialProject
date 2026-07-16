using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Inventory;
using ChangJun.Progression;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    public sealed class IngredientShopOverlay
    {
        private const int PurchasePackSize = 10;

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
        private readonly Dictionary<string, int> _cart = new();
        private readonly Dictionary<string, TextMeshProUGUI> _cardQtyLabels = new();
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
            var scrollRt = UiFactory.CreatePanel(panel, "Scroll",
                new Vector2(0.03f, 0.14f), new Vector2(0.58f, 0.9f),
                Vector2.zero, Vector2.zero);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.02f);

            _gridContent = UiFactory.CreateStretchChild(viewport, "Content");
            _gridContent.pivot = new Vector2(0.5f, 1f);
            _gridContent.anchorMin = new Vector2(0, 1);
            _gridContent.anchorMax = new Vector2(1, 1);

            var grid = _gridContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(260, 130);
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
            RebuildGrid();
            RefreshCart();
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private void RebuildGrid()
        {
            _cardQtyLabels.Clear();
            foreach (Transform child in _gridContent)
                UnityEngine.Object.Destroy(child.gameObject);

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
            le.preferredHeight = 130;
            le.preferredWidth = 260;
            card.gameObject.AddComponent<Image>().color = Color.white;

            int stock = InventoryManager.Instance.GetStock(ing.code);
            int warehouse = InventoryManager.Instance.GetWarehouse(ing.code);

            UiFactory.CreateText(card, "Name", $"{ing.displayName}\n{ing.purchasePrice:N0}원",
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.95f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 17,
                new Color(0.1f, 0.12f, 0.2f));

            UiFactory.CreateText(card, "Stock",
                $"보유 {stock}  ·  배달대기 {warehouse}",
                new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.52f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14,
                new Color(0.35f, 0.4f, 0.5f));

            var qtyText = UiFactory.CreateText(card, "Qty", "0",
                new Vector2(0.3f, 0.08f), new Vector2(0.5f, 0.34f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 22,
                new Color(0.1f, 0.25f, 0.5f));
            _cardQtyLabels[ing.code] = qtyText;

            var minusRt = UiFactory.CreatePanel(card, "Minus",
                new Vector2(0.05f, 0.08f), new Vector2(0.28f, 0.34f),
                Vector2.zero, Vector2.zero);
            var minusBtn = minusRt.gameObject.AddComponent<Button>();
            minusBtn.targetGraphic = minusRt.gameObject.AddComponent<Image>();
            minusBtn.targetGraphic.color = new Color(0.85f, 0.85f, 0.9f);
            string code = ing.code;
            minusBtn.onClick.AddListener(() => AdjustCart(code, -PurchasePackSize));
            UiFactory.CreateText(minusRt, "T", $"-{PurchasePackSize}", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, 18);

            var plusRt = UiFactory.CreatePanel(card, "Plus",
                new Vector2(0.52f, 0.08f), new Vector2(0.75f, 0.34f),
                Vector2.zero, Vector2.zero);
            var plusBtn = plusRt.gameObject.AddComponent<Button>();
            plusBtn.targetGraphic = plusRt.gameObject.AddComponent<Image>();
            plusBtn.targetGraphic.color = new Color(0.85f, 0.9f, 0.85f);
            plusBtn.onClick.AddListener(() => AdjustCart(code, PurchasePackSize));
            UiFactory.CreateText(plusRt, "T", $"+{PurchasePackSize}", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, 18);
        }

        private void AdjustCart(string code, int delta)
        {
            if (_showingReceipt) return;

            int next = (_cart.TryGetValue(code, out var q) ? q : 0) + delta;
            if (next < 0) next = 0;
            if (next == 0) _cart.Remove(code);
            else _cart[code] = next;

            if (_cardQtyLabels.TryGetValue(code, out var label))
                label.text = next.ToString();

            RefreshCart();
        }

        private void RefreshCart()
        {
            foreach (Transform child in _cartListContent)
                UnityEngine.Object.Destroy(child.gameObject);

            int total = 0;
            bool hasItems = _cart.Count > 0;
            _cartEmptyText.gameObject.SetActive(!hasItems && !_showingReceipt);

            var sorted = new List<KeyValuePair<string, int>>(_cart);
            sorted.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

            foreach (var pair in sorted)
            {
                var ing = ResolveIngredient(pair.Key);
                if (ing == null) continue;

                int lineTotal = ing.purchasePrice * pair.Value;
                total += lineTotal;
                CreateReceiptLine(_cartListContent, ing.displayName, pair.Value, lineTotal, false);
            }

            UpdateTotals(total);
        }

        private void UpdateTotals(int total)
        {
            string totalLine = _showingReceipt
                ? "구매한 재료는 아침에 도착합니다"
                : $"합계  {total:N0}원";

            _cartTotalText.text = totalLine;
            _totalText.text = $"합계: {total:N0}원";
            _balanceText.text = $"잔액: {MoneyManager.Instance.Money:N0}원";
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

        private static void CreateReceiptLine(Transform parent, string name, int qty, int lineTotal,
            bool isPurchaseResult)
        {
            var row = new GameObject($"Line_{name}", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = isPurchaseResult ? 50f : 34f;
            le.minHeight = le.preferredHeight;

            var bg = row.AddComponent<Image>();
            bg.color = isPurchaseResult
                ? new Color(0.85f, 0.95f, 0.88f, 0.95f)
                : new Color(1f, 1f, 1f, 0.92f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 4;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var nameCell = CreateLineCell(row.transform, 1f, 80f);
            var nameTmp = nameCell.AddComponent<TextMeshProUGUI>();
            nameTmp.text = name;
            nameTmp.fontSize = isPurchaseResult ? 15 : 17;
            nameTmp.color = new Color(0.1f, 0.12f, 0.2f);
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.raycastTarget = false;
            KoreanUiFont.Apply(nameTmp);

            var qtyCell = CreateLineCell(row.transform, 56f, 56f);
            var qtyTmp = qtyCell.AddComponent<TextMeshProUGUI>();
            qtyTmp.text = isPurchaseResult ? $"+{qty}개" : $"×{qty}";
            qtyTmp.fontSize = isPurchaseResult ? 16 : 17;
            qtyTmp.color = new Color(0.15f, 0.25f, 0.45f);
            qtyTmp.alignment = TextAlignmentOptions.Center;
            qtyTmp.raycastTarget = false;
            KoreanUiFont.Apply(qtyTmp);

            var priceCell = CreateLineCell(row.transform, 72f, 72f);
            var priceTmp = priceCell.AddComponent<TextMeshProUGUI>();
            priceTmp.text = isPurchaseResult ? $"{lineTotal:N0}원" : $"{lineTotal:N0}원";
            priceTmp.fontSize = 17;
            priceTmp.color = new Color(0.1f, 0.15f, 0.25f);
            priceTmp.alignment = TextAlignmentOptions.MidlineRight;
            priceTmp.raycastTarget = false;
            KoreanUiFont.Apply(priceTmp);
        }

        private static GameObject CreateLineCell(Transform parent, float flexOrWidth, float minWidth)
        {
            var cell = new GameObject("Cell", typeof(RectTransform));
            cell.transform.SetParent(parent, false);
            var le = cell.AddComponent<LayoutElement>();
            if (flexOrWidth <= 1f)
            {
                le.flexibleWidth = 1f;
                le.minWidth = minWidth;
            }
            else
            {
                le.preferredWidth = flexOrWidth;
                le.minWidth = minWidth;
            }
            return cell;
        }

        private void OnActionButton()
        {
            if (_showingReceipt)
            {
                Hide();
                OnShoppingComplete?.Invoke();
                return;
            }

            ConfirmPurchase();
        }

        private void ConfirmPurchase()
        {
            if (_cart.Count == 0) return;

            int total = 0;
            foreach (var pair in _cart)
            {
                var ing = ResolveIngredient(pair.Key);
                if (ing != null) total += ing.purchasePrice * pair.Value;
            }

            if (total > MoneyManager.Instance.Money)
            {
                Debug.Log("[Shop] 잔액 부족");
                return;
            }

            var purchased = new Dictionary<string, int>(_cart);

            foreach (var pair in purchased)
            {
                InventoryManager.Instance.PurchaseToWarehouse(pair.Key, pair.Value);
                var ing = ResolveIngredient(pair.Key);
                if (ing != null)
                    DayLoopController.Instance.Ledger.AddPurchase(
                        ing.purchasePrice * pair.Value,
                        $"{ing.displayName} x{pair.Value}");
            }

            if (total > 0)
                MoneyManager.Instance.SpendMoney(total);

            ShowPurchaseReceipt(purchased);
        }

        private void ShowPurchaseReceipt(Dictionary<string, int> purchased)
        {
            _showingReceipt = true;
            _cart.Clear();
            _actionButtonLabel.text = "확인";
            _cartHeader.SetActive(false);
            _receiptBanner.gameObject.SetActive(true);
            _receiptBanner.text = "구매 완료! 내일 아침 배달됩니다.";

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

                int lineTotal = ing.purchasePrice * pair.Value;
                total += lineTotal;

                int warehouse = InventoryManager.Instance.GetWarehouse(ing.code);
                int stock = InventoryManager.Instance.GetStock(ing.code);
                CreateReceiptLine(
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

