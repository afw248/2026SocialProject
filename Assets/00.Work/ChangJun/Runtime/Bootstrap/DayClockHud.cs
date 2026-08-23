using ChangJun.Economy;
using ChangJun.Time;
using TMPro;

namespace ChangJun.Bootstrap
{
    public sealed class DayClockHud
    {
        private readonly TextMeshProUGUI _clockText;
        private readonly TextMeshProUGUI _moneyText;

        public DayClockHud(UiTheme.HeaderMeta meta)
        {
            _clockText = meta.Clock;
            _moneyText = meta.Money;

            if (DayLoopController.Instance != null)
            {
                DayLoopController.Instance.OnTimeChanged += (_, _) => Refresh();
                DayLoopController.Instance.OnDayChanged += _ => Refresh();
            }
            if (MoneyManager.Instance != null)
                MoneyManager.Instance.OnMoneyChanged += _ => Refresh();
            Refresh();
        }

        private void Refresh() => UiTheme.RefreshHeaderMeta(new UiTheme.HeaderMeta
        {
            Clock = _clockText,
            Money = _moneyText
        });
    }
}
