using ChangJun.Data;
using ChangJun.Inventory;
using ChangJun.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 이해도 스킬트리 + 재고 현황 탭 패널.
    /// </summary>
    public sealed class StatusPanel
    {
        private readonly GameObject _root;
        private readonly UnderstandingTreePanel _treePanel;
        private readonly RectTransform _stockContent;

        public StatusPanel(RectTransform parent)
        {
            _root = UiFactory.CreatePanel(parent, "StatusPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            _root.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.97f);

            var treeHost = UiFactory.CreatePanel(_root.transform, "TreeHost",
                new Vector2(0.02f, 0.38f), new Vector2(0.98f, 0.98f),
                Vector2.zero, Vector2.zero);
            _treePanel = new UnderstandingTreePanel(treeHost);

            UiFactory.CreateText(_root.transform, "StockLabel", "재고 현황",
                new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.37f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 20,
                new Color(0.85f, 0.9f, 1f));

            var scrollRt = UiFactory.CreatePanel(_root.transform, "StockScroll",
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.31f),
                Vector2.zero, Vector2.zero);
            scrollRt.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.1f, 0.65f);
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

        public GameObject Root => _root;

        public void RefreshGauges() => RefreshTree();

        public void RefreshTree() => _treePanel?.RefreshAll();

        private void RebuildStockList()
        {
            foreach (Transform child in _stockContent)
                Object.Destroy(child.gameObject);

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
                row.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(row, false);
                UiFactory.Stretch(labelGo.GetComponent<RectTransform>());
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                string tags = ing.isFairTrade ? " · 공정무역" : ing.isLocalSourced ? " · 로컬" : "";
                tmp.text = $"  {ing.displayName}{tags}    보유 {stock}  ·  배달대기 {warehouse}";
                tmp.fontSize = 18;
                tmp.color = stock > 0 ? new Color(0.92f, 0.94f, 0.98f) : new Color(1f, 0.55f, 0.55f);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;
                KoreanUiFont.Apply(tmp);
            }
        }
    }
}
