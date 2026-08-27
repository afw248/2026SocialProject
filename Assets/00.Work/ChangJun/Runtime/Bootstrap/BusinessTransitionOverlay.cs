using System;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 영업·마감 단계 전환. 준비/영업/마감은 시계 아이콘으로 표시한다.
    /// </summary>
    public sealed class BusinessTransitionOverlay
    {
        private static readonly Color NightBg = new Color32(0x3A, 0x24, 0x15, 0xFF);
        private static readonly Color CreamText = new Color32(0xF2, 0xE4, 0xC8, 0xFF);

        private readonly GameObject _root;
        private readonly TextMeshProUGUI _titleText;
        private readonly TextMeshProUGUI _dayText;
        private readonly Image _buttonFill;
        private readonly TextMeshProUGUI _buttonText;
        private readonly RectTransform _centerClock;
        private readonly RectTransform _prepClock;
        private readonly RectTransform _openClock;
        private readonly RectTransform _closeClock;
        private readonly TextMeshProUGUI _prepLabel;
        private readonly TextMeshProUGUI _openLabel;
        private readonly TextMeshProUGUI _closeLabel;
        private readonly UiTheme.HeaderMeta _headerMeta;
        private Action _onAction;

        public BusinessTransitionOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("BusinessTransition", 78);
            _root.SetActive(false);

            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = NightBg;

            var hudBar = UiFactory.CreatePanel(_root.transform, "HudBar",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -56f), Vector2.zero);
            _headerMeta = UiTheme.CreateHeaderMeta(hudBar);

            var row = UiFactory.CreatePanel(_root.transform, "PhaseSteps",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-360f, -168f), new Vector2(360f, -104f));
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            _prepClock = CreateStep(row, "아침 준비", 8, 0, false, out _prepLabel);
            _openClock = CreateStep(row, "영업", 10, 0, false, out _openLabel);
            _closeClock = CreateStep(row, "마감", 21, 0, false, out _closeLabel);

            var centerRt = UiFactory.CreatePanel(_root.transform, "Center",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-140f, -80f), new Vector2(140f, 200f));

            _centerClock = AnalogClockWidget.Create(centerRt, "Icon",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-90f, -180f), new Vector2(90f, 0f),
                10, 0, true);

            _titleText = UiFactory.CreateText(centerRt, "Title", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -230f), new Vector2(0f, -190f),
                TextAlignmentOptions.Center, 34, Color.white);

            _dayText = UiFactory.CreateText(centerRt, "Day", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -266f), new Vector2(0f, -234f),
                TextAlignmentOptions.Center, 16, CreamText);

            var btnHolder = UiFactory.CreatePanel(_root.transform, "ActionBtn",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-140f, 56f), new Vector2(140f, 132f));
            var actionBtn = UiTheme.CreateFlatButton(btnHolder, "", UiTheme.Gold, () => _onAction?.Invoke(), 18, UiTheme.TextDark);
            _buttonFill = (Image)actionBtn.targetGraphic;
            _buttonText = _buttonFill.GetComponentInChildren<TextMeshProUGUI>();
        }

        private static RectTransform CreateStep(Transform parent, string label, int hour, int minute,
            bool active, out TextMeshProUGUI labelText)
        {
            var wrap = new GameObject($"Step_{label}", typeof(RectTransform));
            wrap.transform.SetParent(parent, false);

            var clock = AnalogClockWidget.Create(wrap.transform, "Clock",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-28f, -56f), new Vector2(28f, 0f),
                hour, minute, active);

            labelText = UiFactory.CreateText(wrap.transform, "Label", label,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-90f, -88f), new Vector2(90f, -60f),
                TextAlignmentOptions.Center, 14, CreamText);

            return clock;
        }

        public void ShowOpen(Action onStart)
        {
            _onAction = onStart;
            AnalogClockWidget.SetHands(_prepClock, 8, 0, false);
            AnalogClockWidget.SetHands(_openClock, 10, 0, true);
            AnalogClockWidget.SetHands(_closeClock, 21, 0, false);
            AnalogClockWidget.SetHands(_centerClock, 10, 0, true);
            _prepLabel.color = CreamText;
            _openLabel.color = Color.white;
            _closeLabel.color = CreamText;

            _titleText.text = "영업 시작";
            _dayText.text = DayLoopController.Instance.FormatDayClock();
            UiTheme.RefreshHeaderMeta(_headerMeta);
            _buttonFill.color = UiTheme.Accent;
            _buttonText.text = "영업 시작하기";
            _buttonText.color = UiTheme.CardWhite;

            _root.SetActive(true);
        }

        public void ShowClosing(Action onProceed)
        {
            _onAction = onProceed;
            AnalogClockWidget.SetHands(_prepClock, 8, 0, false);
            AnalogClockWidget.SetHands(_openClock, 10, 0, false);
            AnalogClockWidget.SetHands(_closeClock, 21, 0, true);
            AnalogClockWidget.SetHands(_centerClock, 21, 0, true);
            _prepLabel.color = CreamText;
            _openLabel.color = CreamText;
            _closeLabel.color = Color.white;

            _titleText.text = "영업 종료";
            _dayText.text = DayLoopController.Instance.FormatDayClock();
            UiTheme.RefreshHeaderMeta(_headerMeta);
            _buttonFill.color = new Color32(0xD9, 0x8C, 0xB0, 0xFF);
            _buttonText.text = "정산하러 가기";
            _buttonText.color = UiTheme.CardWhite;

            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);
    }
}
