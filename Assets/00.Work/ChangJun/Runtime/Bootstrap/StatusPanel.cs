using ChangJun.Data;
using ChangJun.Inventory;
using ChangJun.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 이해도 게이지 + 재고 현황 탭 패널.
    /// </summary>
    public sealed class StatusPanel
    {
        private readonly GameObject _root;
        private readonly RectTransform _gaugeContent;
        private readonly RectTransform _stockContent;

        public StatusPanel(RectTransform parent)
        {
            _root = UiFactory.CreatePanel(parent, "StatusPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            _root.AddComponent<Image>().color = new Color(0.09f, 0.11f, 0.16f, 0.95f);

            UiFactory.CreateText(_root.transform, "Title", "매장 정보",
                new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 26);

            UiFactory.CreateText(_root.transform, "GaugeLabel", "문화별 이해도",
                new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.9f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 20,
                new Color(0.75f, 0.85f, 1f));

            _gaugeContent = UiFactory.CreatePanel(_root.transform, "Gauges",
                new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.83f),
                Vector2.zero, Vector2.zero);
            var gaugeLayout = _gaugeContent.gameObject.AddComponent<VerticalLayoutGroup>();
            gaugeLayout.spacing = 8;
            gaugeLayout.childControlHeight = true;
            gaugeLayout.childControlWidth = true;
            gaugeLayout.childForceExpandWidth = true;

            foreach (CultureGroup culture in System.Enum.GetValues(typeof(CultureGroup)))
            {
                if (culture == CultureGroup.None) continue;
                CreateGaugeRow(_gaugeContent, culture);
            }

            UiFactory.CreateText(_root.transform, "StockLabel", "재고 현황",
                new Vector2(0.04f, 0.46f), new Vector2(0.96f, 0.51f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 20,
                new Color(0.75f, 0.85f, 1f));

            var scrollRt = UiFactory.CreatePanel(_root.transform, "StockScroll",
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.45f),
                Vector2.zero, Vector2.zero);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.02f);

            _stockContent = UiFactory.CreateStretchChild(viewport, "Content");
            _stockContent.pivot = new Vector2(0.5f, 1f);
            _stockContent.anchorMin = new Vector2(0, 1);
            _stockContent.anchorMax = new Vector2(1, 1);

            var vlg = _stockContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            _stockContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _stockContent;

            UnderstandingManager.Instance.OnUnderstandingChanged += (_, _) => RefreshGauges();
            InventoryManager.Instance.OnStockChanged += RebuildStockList;

            RefreshGauges();
            RebuildStockList();
        }

        public GameObject Root => _root;

        public void RefreshGauges()
        {
            foreach (Transform child in _gaugeContent)
            {
                var cultureName = child.name.Replace("Gauge_", "");
                if (!System.Enum.TryParse(cultureName, out CultureGroup culture)) continue;

                int value = UnderstandingManager.Instance.GetUnderstanding(culture);
                var fill = child.Find("BarBg/Fill")?.GetComponent<Image>();
                var label = child.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (fill != null) fill.fillAmount = value / 100f;
                if (label != null) label.text = $"{culture}  {value}%";
            }
        }

        private void RebuildStockList()
        {
            foreach (Transform child in _stockContent)
                Object.Destroy(child.gameObject);

            foreach (var ing in InventoryManager.Instance.GetAllIngredients())
            {
                if (!UnderstandingManager.Instance.IsUnlocked(ing.code)) continue;

                int stock = InventoryManager.Instance.GetStock(ing.code);
                int warehouse = InventoryManager.Instance.GetWarehouse(ing.code);
                var row = UiFactory.CreateStretchChild(_stockContent, $"Stock_{ing.code}");
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;
                var tmp = row.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.text = $"{ing.displayName}  보유 {stock}  /  배달대기 {warehouse}";
                tmp.fontSize = 18;
                tmp.color = stock > 0 ? Color.white : new Color(1f, 0.6f, 0.6f);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                KoreanUiFont.Apply(tmp);
            }
        }

        private static void CreateGaugeRow(Transform parent, CultureGroup culture)
        {
            var row = UiFactory.CreateStretchChild(parent, $"Gauge_{culture}");
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;

            UiFactory.CreateText(row, "Label", culture.ToString(),
                new Vector2(0, 0.5f), new Vector2(0.35f, 1),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 16);

            var barBg = UiFactory.CreatePanel(row, "BarBg",
                new Vector2(0.36f, 0.15f), new Vector2(1, 0.85f),
                Vector2.zero, Vector2.zero);
            barBg.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(barBg, false);
            UiFactory.Stretch(fillGo.GetComponent<RectTransform>());
            var fill = fillGo.AddComponent<Image>();
            fill.color = new Color(0.4f, 0.75f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
        }
    }
}
