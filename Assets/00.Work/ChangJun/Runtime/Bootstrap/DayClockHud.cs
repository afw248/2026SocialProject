using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    public sealed class DayClockHud
    {
        private readonly TextMeshProUGUI _dayText;
        private readonly TextMeshProUGUI _clockText;

        public DayClockHud(RectTransform parent)
        {
            var panel = UiTheme.CreateBorderedPanel(parent, "DayClock",
                new Vector2(0f, 0.18f), new Vector2(0f, 0.82f),
                new Vector2(20f, 0f), new Vector2(230f, 0f), UiTheme.CardWhite, 2f);

            var dot = UiFactory.CreatePanel(panel, "Dot",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -6f), new Vector2(26f, 6f));
            dot.gameObject.AddComponent<Image>().color = UiTheme.Gold;

            _dayText = UiFactory.CreateText(panel, "Day", "1일째",
                new Vector2(0f, 0.5f), new Vector2(1f, 1f),
                new Vector2(34f, 0f), Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 20, UiTheme.TextDark);

            _clockText = UiFactory.CreateText(panel, "Clock", "10:00 AM",
                new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                new Vector2(34f, 0f), Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 22, UiTheme.TextMuted);

            DayLoopController.Instance.OnTimeChanged += (_, _) => Refresh();
            DayLoopController.Instance.OnDayChanged += _ => Refresh();
            Refresh();
        }

        private void Refresh()
        {
            var day = DayLoopController.Instance;
            _dayText.text = $"{day.Day}일째";
            _clockText.text = day.FormatClock();
        }
    }
}
