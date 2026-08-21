using System;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// Day Loop UI 목업의 "영업"·"마감" 단계 전환 화면.
    /// 아침(MorningDeliveryOverlay)과 같은 톤 — 상단 3단계 표시 + 큰 아이콘 + CTA 버튼.
    /// </summary>
    public sealed class BusinessTransitionOverlay
    {
        private static readonly Color NightBg = new Color32(0x3A, 0x24, 0x15, 0xFF);
        private static readonly Color CreamText = new Color32(0xF2, 0xE4, 0xC8, 0xFF);
        private static readonly Color DimStep = new Color32(0x8A, 0x62, 0x38, 0xFF);

        private readonly GameObject _root;
        private readonly TextMeshProUGUI _titleText;
        private readonly TextMeshProUGUI _dayText;
        private readonly Image _iconFill;
        private readonly Image _buttonFill;
        private readonly TextMeshProUGUI _buttonText;
        private readonly Image _stepOpenDot;
        private readonly Image _stepCloseDot;
        private readonly TextMeshProUGUI _openLabelText;
        private readonly TextMeshProUGUI _closeLabelText;
        private Action _onAction;

        public BusinessTransitionOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("BusinessTransition", 78);
            _root.SetActive(false);

            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = NightBg;

            var row = UiFactory.CreatePanel(_root.transform, "PhaseSteps",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-360f, -104f), new Vector2(360f, -40f));
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateStep(row, "아침 준비", out _);
            _stepOpenDot = CreateStep(row, "영업", out var openLabel);
            _stepCloseDot = CreateStep(row, "마감", out var closeLabel);
            _openLabelText = openLabel;
            _closeLabelText = closeLabel;

            var centerRt = UiFactory.CreatePanel(_root.transform, "Center",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-140f, -80f), new Vector2(140f, 200f));

            var icon = UiFactory.CreatePanel(centerRt, "Icon",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-110f, -220f), new Vector2(110f, 0f));
            icon.gameObject.AddComponent<Image>().color = UiTheme.Border;
            var iconFillRt = UiFactory.CreatePanel(icon, "Fill",
                Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            _iconFill = iconFillRt.gameObject.AddComponent<Image>();

            _titleText = UiFactory.CreateText(centerRt, "Title", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -260f), new Vector2(0f, -220f),
                TextAlignmentOptions.Center, 34, Color.white);

            _dayText = UiFactory.CreateText(centerRt, "Day", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -296f), new Vector2(0f, -264f),
                TextAlignmentOptions.Center, 16, CreamText);

            var btnHolder = UiFactory.CreatePanel(_root.transform, "ActionBtn",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-140f, 56f), new Vector2(140f, 132f));
            var actionBtn = UiTheme.CreateFlatButton(btnHolder, "", UiTheme.Gold, () => _onAction?.Invoke(), 18, UiTheme.TextDark);
            _buttonFill = (Image)actionBtn.targetGraphic;
            _buttonText = _buttonFill.GetComponentInChildren<TextMeshProUGUI>();
        }

        private static Image CreateStep(Transform parent, string label, out TextMeshProUGUI labelText)
        {
            var wrap = new GameObject($"Step_{label}", typeof(RectTransform));
            wrap.transform.SetParent(parent, false);

            var dot = UiFactory.CreatePanel(wrap.transform, "Dot",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-32f, -64f), new Vector2(32f, 0f));
            dot.gameObject.AddComponent<Image>().color = UiTheme.Border;
            var dotFillRt = UiFactory.CreatePanel(dot, "Fill", Vector2.zero, Vector2.one,
                new Vector2(4f, 4f), new Vector2(-4f, -4f));
            var dotFill = dotFillRt.gameObject.AddComponent<Image>();
            dotFill.color = DimStep;

            labelText = UiFactory.CreateText(wrap.transform, "Label", label,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-90f, -88f), new Vector2(90f, -68f),
                TextAlignmentOptions.Center, 14, CreamText);

            return dotFill;
        }

        /// <summary>영업 시작 전환 — 재료 상점/주식 시장 이후, 첫 손님이 뜨기 전에 보여준다.</summary>
        public void ShowOpen(Action onStart)
        {
            _onAction = onStart;
            _stepOpenDot.color = UiTheme.Gold;
            _stepCloseDot.color = DimStep;
            _openLabelText.color = Color.white;
            _closeLabelText.color = CreamText;

            _iconFill.color = UiTheme.Gold;
            _titleText.text = "영업 시작";
            _dayText.text = $"{DayLoopController.Instance.Day}일차 · {DayLoopController.Instance.FormatClock()}";
            _buttonFill.color = UiTheme.Accent;
            _buttonText.text = "영업 시작하기";
            _buttonText.color = UiTheme.CardWhite;

            _root.SetActive(true);
        }

        /// <summary>영업 종료 전환 — 마지막 손님 이후, 정산 화면으로 넘어가기 전에 보여준다.</summary>
        public void ShowClosing(Action onProceed)
        {
            _onAction = onProceed;
            _stepOpenDot.color = DimStep;
            _stepCloseDot.color = UiTheme.Gold;
            _openLabelText.color = CreamText;
            _closeLabelText.color = Color.white;

            _iconFill.color = new Color32(0xD9, 0xC7, 0xF2, 0xFF);
            _titleText.text = "영업 종료";
            _dayText.text = $"{DayLoopController.Instance.Day}일차 · {DayLoopController.Instance.FormatClock()}";
            _buttonFill.color = new Color32(0xD9, 0x8C, 0xB0, 0xFF);
            _buttonText.text = "정산하러 가기";
            _buttonText.color = UiTheme.CardWhite;

            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);
    }
}
