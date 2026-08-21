using System;
using ChangJun.Data;
using ChangJun.Inventory;
using ChangJun.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 정보 — 독립 풀스크린 화면. 이해도 스킬트리 + 재고 현황.
    /// </summary>
    public sealed class StatusPanel
    {
        private readonly GameObject _root;
        private readonly UnderstandingTreePanel _treePanel;
        private readonly RectTransform _stockContent;

        public event Action OnBack;

        public StatusPanel()
        {
            _root = UiFactory.CreateOverlayRoot("StatusOverlay", 60);
            _root.SetActive(false);

            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = UiTheme.Background;

            var header = UiTheme.CreateHeaderBar(_root.transform, "정보", 72f, 78f);
            UiTheme.CreateBackButton(header, () => OnBack?.Invoke());

            var body = UiTheme.CreateScreenBody(_root.transform, 72f, 20f);

            var treeHost = UiFactory.CreatePanel(body, "TreeHost",
                new Vector2(0f, 0.38f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);
            _treePanel = new UnderstandingTreePanel(treeHost);

            UiFactory.CreateText(body, "StockLabel", "재고 현황",
                new Vector2(0.02f, 0.32f), new Vector2(0.98f, 0.37f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 20, UiTheme.TextMuted);

            var scrollRt = UiTheme.CreateBorderedPanel(body, "StockScroll",
                new Vector2(0.02f, 0f), new Vector2(0.98f, 0.31f),
                Vector2.zero, Vector2.zero, UiTheme.TanRow, 3f);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            UiFactory.ConfigureScroll(scroll);

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            _stockContent = UiFactory.CreateStretchChild(viewport, "Content");
            _stockContent.pivot = new Vector2(0.5f, 1f);
            _stockContent.anchorMin = new Vector2(0, 1);
            _stockContent.anchorMax = new Vector2(1, 1);

            var vlg = _stockContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            _stockContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _stockContent;

            UnderstandingManager.Instance.OnUnderstandingChanged += (_, _) => RefreshTree();
            InventoryManager.Instance.OnStockChanged += RebuildStockList;

            RefreshTree();
            RebuildStockList();
        }

        public void Show() => _root.SetActive(true);
        public void Hide() => _root.SetActive(false);

        public void RefreshGauges() => RefreshTree();

        public void RefreshTree() => _treePanel?.RefreshAll();

        private void RebuildStockList()
        {
            foreach (Transform child in _stockContent)
                UnityEngine.Object.Destroy(child.gameObject);

            if (InventoryManager.Instance == null) return;

            foreach (var ing in InventoryManager.Instance.GetAllIngredients())
            {
                if (ing == null) continue;
                if (UnderstandingManager.Instance == null) continue;
                if (!UnderstandingManager.Instance.IsUnlocked(ing.code)) continue;

                int stock = InventoryManager.Instance.GetStock(ing.code);
                int warehouse = InventoryManager.Instance.GetWarehouse(ing.code);

                var row = UiFactory.CreateStretchChild(_stockContent, $"Stock_{ing.code}");
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
                row.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.5f);

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(row, false);
                UiFactory.Stretch(labelGo.GetComponent<RectTransform>());
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                string tags = ing.isFairTrade ? " · 공정무역" : ing.isLocalSourced ? " · 로컬" : "";
                tmp.text = $"  {ing.displayName}{tags}    보유 {stock}  ·  배달대기 {warehouse}";
                tmp.fontSize = 18;
                tmp.color = stock > 0 ? UiTheme.TextDark : UiTheme.Danger;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;
                KoreanUiFont.Apply(tmp);
            }
        }
    }
}
