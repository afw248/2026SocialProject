using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 개입/방관·흥정 등 두 갈래 선택. 기존 화면 위에 얇게 얹는다.
    /// </summary>
    public sealed class SocialChoiceOverlay
    {
        private readonly GameObject _root;
        private readonly TextMeshProUGUI _titleText;
        private readonly TextMeshProUGUI _bodyText;
        private readonly TextMeshProUGUI _leftLabel;
        private readonly TextMeshProUGUI _rightLabel;
        private Action _onLeft;
        private Action _onRight;

        public SocialChoiceOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("SocialChoice", 220);
            _root.SetActive(false);

            var dim = UiFactory.CreateStretchChild(_root.transform, "Dim");
            dim.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.03f, 0.55f);

            var card = UiTheme.CreateShadowCard(_root.transform, "Card",
                new Vector2(0.22f, 0.28f), new Vector2(0.78f, 0.74f),
                Vector2.zero, Vector2.zero, UiTheme.CardWhite, 4f, 8f);

            _titleText = UiFactory.CreateText(card, "Title", "",
                new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.94f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 22, UiTheme.TextDark);
            _titleText.fontStyle = FontStyles.Bold;

            _bodyText = UiFactory.CreateText(card, "Body", "",
                new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.76f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 17, UiTheme.TextMuted);
            _bodyText.textWrappingMode = TextWrappingModes.Normal;

            var left = UiFactory.CreatePanel(card, "Left",
                new Vector2(0.06f, 0.08f), new Vector2(0.48f, 0.26f),
                Vector2.zero, Vector2.zero);
            var leftBtn = UiTheme.CreateFlatButton(left, "A", UiTheme.Accent, () => Complete(true), 16);
            _leftLabel = leftBtn.GetComponentInChildren<TextMeshProUGUI>();

            var right = UiFactory.CreatePanel(card, "Right",
                new Vector2(0.52f, 0.08f), new Vector2(0.94f, 0.26f),
                Vector2.zero, Vector2.zero);
            var rightBtn = UiTheme.CreateFlatButton(right, "B", UiTheme.TanRow, () => Complete(false), 16, UiTheme.TextDark);
            _rightLabel = rightBtn.GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Show(string title, string body, string leftLabel, string rightLabel,
            Action onLeft, Action onRight)
        {
            _titleText.text = title ?? "";
            _bodyText.text = body ?? "";
            _leftLabel.text = leftLabel ?? "확인";
            _rightLabel.text = rightLabel ?? "취소";
            _onLeft = onLeft;
            _onRight = onRight;
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private void Complete(bool left)
        {
            Hide();
            var cb = left ? _onLeft : _onRight;
            _onLeft = null;
            _onRight = null;
            cb?.Invoke();
        }
    }
}
