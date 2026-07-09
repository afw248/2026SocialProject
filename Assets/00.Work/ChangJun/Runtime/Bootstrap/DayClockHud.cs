using ChangJun.Time;
using TMPro;
using UnityEngine;

namespace ChangJun.Bootstrap
{
    public sealed class DayClockHud
    {
        private readonly TextMeshProUGUI _dayText;
        private readonly TextMeshProUGUI _clockText;

        public DayClockHud(RectTransform parent)
        {
            var panel = UiFactory.CreatePanel(parent, "DayClock",
                new Vector2(0.02f, 0.92f), new Vector2(0.22f, 0.99f),
                Vector2.zero, Vector2.zero);

            _dayText = UiFactory.CreateText(panel, "Day", "1일째",
                new Vector2(0, 0.5f), new Vector2(1, 1),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 22);

            _clockText = UiFactory.CreateText(panel, "Clock", "10:00 AM",
                new Vector2(0, 0), new Vector2(1, 0.5f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 26,
                new Color(1f, 0.95f, 0.7f));

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
