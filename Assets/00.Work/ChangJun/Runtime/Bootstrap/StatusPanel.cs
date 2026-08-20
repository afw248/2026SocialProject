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
            _root.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.97f);

            UiFactory.CreateText(_root.transform, "Title", "매장 정보",
                new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 28);

            UiFactory.CreateText(_root.transform, "GaugeLabel", "문화별 이해도",
                new Vector2(0.04f, 0.85f), new Vector2(0.96f, 0.91f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 20,
                new Color(0.85f, 0.9f, 1f));

            _gaugeContent = UiFactory.CreatePanel(_root.transform, "Gauges",
                new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.84f),
                Vector2.zero, Vector2.zero);
            var gaugeLayout = _gaugeContent.gameObject.AddComponent<VerticalLayoutGroup>();
            gaugeLayout.spacing = 10;
            gaugeLayout.padding = new RectOffset(4, 4, 4, 4);
            gaugeLayout.childControlHeight = true;
            gaugeLayout.childControlWidth = true;
            gaugeLayout.childForceExpandWidth = true;
            gaugeLayout.childForceExpandHeight = false;

            foreach (CultureGroup culture in System.Enum.GetValues(typeof(CultureGroup)))
            {
                if (culture == CultureGroup.None) continue;
                CreateGaugeRow(_gaugeContent, culture);
            }

            UiFactory.CreateText(_root.transform, "StockLabel", "재고 현황",
                new Vector2(0.04f, 0.41f), new Vector2(0.96f, 0.47f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 20,
                new Color(0.85f, 0.9f, 1f));

            var scrollRt = UiFactory.CreatePanel(_root.transform, "StockScroll",
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.40f),
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

                int value = Mathf.Clamp(UnderstandingManager.Instance.GetUnderstanding(culture), 0, 100);
                var fillRt = child.Find("Track/Fill")?.GetComponent<RectTransform>();
                var valueLabel = child.Find("Value")?.GetComponent<TextMeshProUGUI>();
                var nameLabel = child.Find("Name")?.GetComponent<TextMeshProUGUI>();

                if (fillRt != null)
                    fillRt.anchorMax = new Vector2(value / 100f, 1f);

                if (valueLabel != null)
                    valueLabel.text = $"{value}%";

                if (nameLabel != null)
                    nameLabel.text = CultureDisplayName(culture);
            }
        }

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

                // Image와 TMP는 같은 GO에 둘 수 없음 — 자식에 텍스트
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(row, false);
                UiFactory.Stretch(labelGo.GetComponent<RectTransform>());
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text = $"  {ing.displayName}    보유 {stock}  ·  배달대기 {warehouse}";
                tmp.fontSize = 18;
                tmp.color = stock > 0 ? new Color(0.92f, 0.94f, 0.98f) : new Color(1f, 0.55f, 0.55f);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;
                KoreanUiFont.Apply(tmp);
            }
        }

        private static void CreateGaugeRow(Transform parent, CultureGroup culture)
        {
            var row = UiFactory.CreateStretchChild(parent, $"Gauge_{culture}");
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;
            row.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.035f);

            UiFactory.CreateText(row, "Name", CultureDisplayName(culture),
                new Vector2(0.02f, 0f), new Vector2(0.28f, 1f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 17,
                new Color(0.92f, 0.94f, 0.98f));

            var track = UiFactory.CreatePanel(row, "Track",
                new Vector2(0.30f, 0.22f), new Vector2(0.82f, 0.78f),
                Vector2.zero, Vector2.zero);
            var trackImg = track.gameObject.AddComponent<Image>();
            trackImg.color = new Color(0.12f, 0.14f, 0.2f, 1f);

            var outline = track.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.12f);
            outline.effectDistance = new Vector2(1f, -1f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(track, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);
            var fill = fillGo.AddComponent<Image>();
            fill.color = CultureBarColor(culture);

            UiFactory.CreateText(row, "Value", "0%",
                new Vector2(0.84f, 0f), new Vector2(0.98f, 1f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 18,
                new Color(0.95f, 0.97f, 1f)).fontStyle = FontStyles.Bold;
        }

        private static string CultureDisplayName(CultureGroup culture) => culture switch
        {
            CultureGroup.Korean => "한식",
            CultureGroup.Muslim => "무슬림",
            CultureGroup.Hindu => "힌두",
            CultureGroup.Vegan => "비건",
            CultureGroup.SEAsian => "동남아",
            CultureGroup.AfricanAmerican => "아프리칸",
            _ => culture.ToString(),
        };

        private static Color CultureBarColor(CultureGroup culture) => culture switch
        {
            CultureGroup.Korean => new Color(0.95f, 0.45f, 0.35f),
            CultureGroup.Muslim => new Color(0.25f, 0.72f, 0.55f),
            CultureGroup.Hindu => new Color(0.95f, 0.7f, 0.25f),
            CultureGroup.Vegan => new Color(0.45f, 0.82f, 0.4f),
            CultureGroup.SEAsian => new Color(0.4f, 0.65f, 0.95f),
            CultureGroup.AfricanAmerican => new Color(0.75f, 0.5f, 0.95f),
            _ => new Color(0.5f, 0.75f, 1f),
        };
    }
}


