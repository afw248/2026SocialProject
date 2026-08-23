using System;
using ChangJun.Data;
using UnityEngine;

namespace ChangJun.Time
{
    /// <summary>
    /// 하루 루프(아침→영업→정산→구매)와 시계를 관리한다.
    /// 영업 중에는 배경으로 시간이 흐르고, 손님 처리 시 추가 시간이 더해진다.
    /// </summary>
    public sealed class DayLoopController : MonoBehaviour
    {
        public static DayLoopController Instance { get; private set; }

        private DayConfigSO _config;
        private int _day = 1;
        private int _hour;
        private int _minute;
        private float _minuteAccumulator;
        private DayPhase _phase;

        public int Day => _day;
        public int Hour => _hour;
        public int Minute => _minute;
        public int CurrentMinutes => _hour * 60 + _minute;
        public DayPhase Phase => _phase;
        public DayConfigSO Config => _config;
        public DailyLedger Ledger { get; } = new();

        public event Action<DayPhase> OnPhaseChanged;
        public event Action<int, int> OnTimeChanged;
        public event Action<int> OnDayChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _config = Resources.Load<DayConfigSO>("Craft/DayConfig");
            if (_config == null)
            {
                Debug.LogWarning("[DayLoop] DayConfig 없음 — 기본값 사용");
                _config = ScriptableObject.CreateInstance<DayConfigSO>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_phase != DayPhase.Open) return;

            _minuteAccumulator += UnityEngine.Time.deltaTime * _config.passiveMinutesPerRealSecond;

            while (_minuteAccumulator >= 1f)
            {
                _minuteAccumulator -= 1f;
                AddMinutes(1);
                if (_hour >= _config.closeHour)
                {
                    CloseShop();
                    return;
                }
            }
        }

        public void StartNewGame()
        {
            _day = 1;
            Ledger.Reset();
            EnterMorning();
            OnDayChanged?.Invoke(_day);
        }

        public void EnterMorning()
        {
            _phase = DayPhase.Morning;
            _minuteAccumulator = 0f;
            SetTime(_config.morningHour, 0);
            OnPhaseChanged?.Invoke(_phase);
        }

        public void BeginBusinessDay()
        {
            _phase = DayPhase.Open;
            _minuteAccumulator = 0f;
            SetTime(_config.openHour, 0);
            OnPhaseChanged?.Invoke(_phase);
        }

        /// <summary>손님 1명 처리 완료 시 — 배경 흐름에 더해 서비스 보너스 분을 즉시 추가한다.</summary>
        public void AdvanceAfterCustomer()
        {
            if (_phase != DayPhase.Open) return;

            int min = Mathf.Min(_config.minutesPerCustomerMin, _config.minutesPerCustomerMax);
            int max = Mathf.Max(_config.minutesPerCustomerMin, _config.minutesPerCustomerMax);
            if (min <= 0 && max <= 0)
                min = max = _config.minutesPerCustomer;

            int bonus = UnityEngine.Random.Range(min, max + 1);
            AddMinutes(bonus);

            if (_hour >= _config.closeHour)
                CloseShop();
        }

        public void CloseShop()
        {
            if (_phase == DayPhase.Closed || _phase == DayPhase.Settlement || _phase == DayPhase.Shopping)
                return;

            _phase = DayPhase.Closed;
            _minuteAccumulator = 0f;
            SetTime(_config.closeHour, 0);
            OnPhaseChanged?.Invoke(_phase);

            _phase = DayPhase.Settlement;
            OnPhaseChanged?.Invoke(_phase);
        }

        public void EnterShopping()
        {
            _phase = DayPhase.Shopping;
            _minuteAccumulator = 0f;
            OnPhaseChanged?.Invoke(_phase);
        }

        public void AdvanceToNextDay()
        {
            _day++;
            Ledger.Reset();
            OnDayChanged?.Invoke(_day);
            EnterMorning();
        }

        public string FormatClock()
        {
            int displayHour = _hour % 12;
            if (displayHour == 0) displayHour = 12;
            string ampm = _hour < 12 ? "AM" : "PM";
            return $"{ampm} {displayHour}:{_minute:00}";
        }

        public string FormatDayClock() => $"{_day}일차  {FormatClock()}";

        private void SetTime(int hour, int minute)
        {
            _hour = hour;
            _minute = minute;
            OnTimeChanged?.Invoke(_hour, _minute);
        }

        private void AddMinutes(int minutes)
        {
            if (minutes <= 0) return;

            _minute += minutes;
            while (_minute >= 60)
            {
                _minute -= 60;
                _hour++;
            }
            OnTimeChanged?.Invoke(_hour, _minute);
        }
    }
}
