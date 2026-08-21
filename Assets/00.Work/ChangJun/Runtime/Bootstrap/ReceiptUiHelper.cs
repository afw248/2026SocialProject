using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 영수증/종이 티켓 스타일 UI 공통 헬퍼.
    /// </summary>
    public static class ReceiptUiHelper
    {
        public static readonly Color PaperColor = UiTheme.Background;
        public static readonly Color PaperDark = UiTheme.TanRow;
        public static readonly Color InkColor = UiTheme.TextDark;
        public static readonly Color MutedInk = UiTheme.TextMuted;
        public static readonly Color AccentBrown = UiTheme.Accent;

        public static RectTransform CreateDim(Transform parent, float alpha = 0.72f)
        {
            var dim = UiFactory.CreateStretchChild(parent, "Dim");
            dim.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, alpha);
            return dim;
        }

        public static RectTransform CreatePaperPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            return UiTheme.CreateShadowCard(parent, name, anchorMin, anchorMax,
                Vector2.zero, Vector2.zero, PaperColor);
        }

        public static TextMeshProUGUI CreateReceiptHeader(Transform parent, string title,
            string subtitle, Vector2 anchorMin, Vector2 anchorMax)
        {
            var titleText = UiFactory.CreateText(parent, "Title", title,
                anchorMin, anchorMax,
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 28, InkColor);
            titleText.fontStyle = FontStyles.Bold;

            if (!string.IsNullOrEmpty(subtitle))
            {
                UiFactory.CreateText(parent, "Subtitle", subtitle,
                    new Vector2(anchorMin.x, anchorMin.y - 0.08f),
                    new Vector2(anchorMax.x, anchorMin.y),
                    Vector2.zero, Vector2.zero,
                    TextAlignmentOptions.Center, 16, MutedInk);
            }

            return titleText;
        }

        public static TextMeshProUGUI CreateDashedRule(Transform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            return UiFactory.CreateText(parent, "Rule", "- - - - - - - - - - - - - - - - - - - -",
                anchorMin, anchorMax,
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 14, MutedInk);
        }

        public static TextMeshProUGUI CreateReceiptRow(Transform parent, string label, string value,
            int fontSize = 18, bool bold = false)
        {
            var row = UiFactory.CreatePanel(parent, "Row",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = fontSize + 12;
            le.minHeight = le.preferredHeight;

            var labelText = UiFactory.CreateText(row, "Label", label,
                new Vector2(0f, 0f), new Vector2(0.62f, 1f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, fontSize, InkColor);
            if (bold) labelText.fontStyle = FontStyles.Bold;

            var valueText = UiFactory.CreateText(row, "Value", value,
                new Vector2(0.62f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, fontSize, InkColor);
            if (bold) valueText.fontStyle = FontStyles.Bold;

            return valueText;
        }

        public static void CreateReceiptLine(Transform parent, string name, int qty, int lineTotal,
            bool isPurchaseResult = false)
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
            nameTmp.color = InkColor;
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
            priceTmp.text = $"{lineTotal:N0}원";
            priceTmp.fontSize = 17;
            priceTmp.color = new Color(0.1f, 0.15f, 0.25f);
            priceTmp.alignment = TextAlignmentOptions.MidlineRight;
            priceTmp.raycastTarget = false;
            KoreanUiFont.Apply(priceTmp);
        }

        public static Button CreatePaperButton(Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax, Action onClick,
            Color? bgColor = null)
        {
            var slot = UiFactory.CreatePanel(parent, $"Slot_{label}",
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);

            var fill = bgColor ?? AccentBrown;
            bool isLight = fill == UiTheme.CardWhite || fill == PaperColor || fill == PaperDark;
            return UiTheme.CreateFlatButton(slot, label, fill,
                () => onClick?.Invoke(), 22, isLight ? UiTheme.TextDark : UiTheme.CardWhite);
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
    }
}
