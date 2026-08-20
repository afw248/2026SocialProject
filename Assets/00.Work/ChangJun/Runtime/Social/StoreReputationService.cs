using System;
using ChangJun.Data;
using ChangJun.Progression;
using ChangJun.Time;
using UnityEngine;

namespace ChangJun.Social
{
    /// <summary>
    /// 상생 지수 — 이해도·성공률·금기율로 계산. 정부 보조금 게이트.
    /// </summary>
    public sealed class StoreReputationService : MonoBehaviour
    {
        public static StoreReputationService Instance { get; private set; }

        private int _successCount;
        private int _totalOrders;
        private int _tabooCount;

        public event Action<float> OnReputationChanged;

        public float Reputation { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetDailyStats()
        {
            _successCount = 0;
            _totalOrders = 0;
            _tabooCount = 0;
            Recalculate();
        }

        public void RecordOrder(bool success, bool taboo)
        {
            _totalOrders++;
            if (success) _successCount++;
            if (taboo) _tabooCount++;
            Recalculate();
        }

        public int GetDailySubsidy()
        {
            if (Reputation >= 0.75f) return 200;
            if (Reputation >= 0.5f) return 100;
            if (Reputation >= 0.3f) return 50;
            return 0;
        }

        public float GetSpawnBoost() => 1f + Mathf.Clamp(Reputation - 0.3f, 0f, 0.5f);

        public void ApplyCommunityMeal(int donatedUnits)
        {
            if (donatedUnits <= 0) return;
            Reputation = Mathf.Clamp01(Reputation + donatedUnits * 0.008f);
            OnReputationChanged?.Invoke(Reputation);
        }

        public void PayDailySubsidy(DailyLedger ledger)
        {
            int subsidy = GetDailySubsidy();
            if (subsidy <= 0 || ledger == null) return;
            Economy.MoneyManager.Instance?.AddMoney(subsidy);
            ledger.AddSubsidy(subsidy, "다문화 상생 보조금");
        }

        private void Recalculate()
        {
            float avgUnderstanding = 0f;
            int count = 0;
            if (UnderstandingManager.Instance != null)
            {
                foreach (CultureGroup c in Enum.GetValues(typeof(CultureGroup)))
                {
                    if (c == CultureGroup.None) continue;
                    avgUnderstanding += UnderstandingManager.Instance.GetUnderstanding(c);
                    count++;
                }
            }
            if (count > 0) avgUnderstanding /= count;

            float successRate = _totalOrders > 0 ? _successCount / (float)_totalOrders : 0.7f;
            float tabooRate = _totalOrders > 0 ? _tabooCount / (float)_totalOrders : 0f;

            Reputation = (avgUnderstanding / 100f) * successRate * (1f - tabooRate * 0.5f);
            Reputation = Mathf.Clamp01(Reputation);
            OnReputationChanged?.Invoke(Reputation);
        }
    }
}
