using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    public sealed class UnderstandingGaugeHud
    {
        private readonly Dictionary<CultureGroup, Image> _fills = new();
        private readonly Dictionary<CultureGroup, TextMeshProUGUI> _labels = new();
        private CultureGroup _highlight = CultureGroup.None;

        public UnderstandingGaugeHud(RectTransform parent)
        {
            var panel = UiFactory.CreatePanel(parent, "UnderstandingHud",
                new Vector2(0.24f, 0.92f), new Vector2(0.72f, 0.99f),
                Vector2.zero, Vector2.zero);

            float width = 1f / 6f;
            int index = 0;
            foreach (CultureGroup culture in System.Enum.GetValues(typeof(CultureGroup)))
            {
                if (culture == CultureGroup.None) continue;

                float xMin = index * width;
                float xMax = xMin + width - 0.01f;
                CreateGauge(panel, culture, xMin, xMax);
                index++;
            }

            UnderstandingManager.Instance.OnUnderstandingChanged += OnChanged;
        }

        public void Highlight(CultureGroup culture) => _highlight = culture;

        public void RefreshAll()
        {
            foreach (CultureGroup culture in System.Enum.GetValues(typeof(CultureGroup)))
            {
                if (culture == CultureGroup.None) continue;
                OnChanged(culture, UnderstandingManager.Instance.GetUnderstanding(culture));
            }
        }

        private void OnChanged(CultureGroup culture, int value)
        {
            if (!_fills.TryGetValue(culture, out var fill)) return;
            fill.fillAmount = value / 100f;
            if (_labels.TryGetValue(culture, out var label))
                label.text = $"{culture} {value}";
        }

        private void CreateGauge(RectTransform parent, CultureGroup culture, float xMin, float xMax)
        {
            var row = UiFactory.CreatePanel(parent, $"Gauge_{culture}",
                new Vector2(xMin, 0), new Vector2(xMax, 1),
                new Vector2(2, 0), new Vector2(-2, 0));

            _labels[culture] = UiFactory.CreateText(row, "Label", culture.ToString(),
                new Vector2(0, 0.55f), new Vector2(1, 1),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 14);

            var barBg = UiFactory.CreatePanel(row, "BarBg",
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.5f),
                Vector2.zero, Vector2.zero);
            barBg.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(barBg, false);
            UiFactory.Stretch(fillGo.GetComponent<RectTransform>());
            var fill = fillGo.AddComponent<Image>();
            fill.color = culture == _highlight
                ? new Color(0.4f, 0.85f, 1f)
                : new Color(0.35f, 0.65f, 0.9f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            _fills[culture] = fill;
        }
    }
}
