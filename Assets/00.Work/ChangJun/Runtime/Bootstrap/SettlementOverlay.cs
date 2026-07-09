using System;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    public sealed class SettlementOverlay
    {
        private readonly GameObject _root;
        private readonly TextMeshProUGUI _bodyText;

        public event Action OnDismissed;

        public SettlementOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("SettlementOverlay", 90);
            _root.SetActive(false);

            var dim = UiFactory.CreateStretchChild(_root.transform, "Dim");
            dim.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.75f);

            var panel = UiFactory.CreatePanel(_root.transform, "Panel",
                new Vector2(0.25f, 0.15f), new Vector2(0.75f, 0.85f),
                Vector2.zero, Vector2.zero);
            panel.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.96f, 0.9f);

            UiFactory.CreateText(panel, "Title", "오늘의 정산",
                new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 32,
                new Color(0.2f, 0.15f, 0.1f));

            var scrollRt = UiFactory.CreatePanel(panel, "Scroll",
                new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.86f),
                Vector2.zero, Vector2.zero);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.02f);

            var content = UiFactory.CreateStretchChild(viewport, "Content");
            content.pivot = new Vector2(0.5f, 1f);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.offsetMin = new Vector2(0, 0);
            content.offsetMax = new Vector2(0, 0);

            _bodyText = content.gameObject.AddComponent<TextMeshProUGUI>();
            _bodyText.fontSize = 22;
            _bodyText.color = new Color(0.15f, 0.12f, 0.1f);
            _bodyText.alignment = TextAlignmentOptions.TopLeft;
            _bodyText.enableWordWrapping = true;
            KoreanUiFont.Apply(_bodyText);

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            var btnRt = UiFactory.CreatePanel(panel, "NextBtn",
                new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.14f),
                Vector2.zero, Vector2.zero);
            var btn = btnRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnRt.gameObject.AddComponent<Image>();
            btn.targetGraphic.color = new Color(0.2f, 0.45f, 0.25f);
            btn.onClick.AddListener(Dismiss);

            UiFactory.CreateText(btnRt, "Label", "다음",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 26);
        }

        public void Show()
        {
            var ledger = DayLoopController.Instance.Ledger;
            var sb = new System.Text.StringBuilder();
            foreach (var line in ledger.Lines)
                sb.AppendLine(line);
            sb.AppendLine();
            sb.AppendLine($"총 매출: {ledger.Revenue:N0}원");
            sb.AppendLine($"재료 비용: {ledger.IngredientCost:N0}원");
            sb.AppendLine($"패널티: {ledger.PenaltyLoss:N0}원");
            sb.AppendLine($"구매 비용: {ledger.PurchaseCost:N0}원");
            sb.AppendLine($"손님 수: {ledger.CustomersServed}명");
            sb.AppendLine();
            sb.AppendLine($"<b>순이익: {ledger.NetProfit:N0}원</b>");

            _bodyText.text = sb.ToString();
            contentResize();
            _root.SetActive(true);
        }

        private void contentResize()
        {
            var content = _bodyText.rectTransform;
            content.sizeDelta = new Vector2(0, _bodyText.preferredHeight + 24);
        }

        public void Hide() => _root.SetActive(false);

        private void Dismiss()
        {
            Hide();
            OnDismissed?.Invoke();
        }
    }
}
