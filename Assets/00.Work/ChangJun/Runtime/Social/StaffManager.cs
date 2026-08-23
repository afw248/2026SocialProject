using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Time;
using UnityEngine;

namespace ChangJun.Social
{
    /// <summary>
    /// 문화권별 직원 고용. 같이 일하는 공존 — 해당 문화 이해도 보너스와 금기 힌트.
    /// </summary>
    public sealed class StaffManager : MonoBehaviour
    {
        public const int MaxHired = 2;

        public static StaffManager Instance { get; private set; }

        private readonly List<StaffSO> _catalog = new();
        private readonly HashSet<string> _hired = new();

        public IReadOnlyList<StaffSO> Catalog => _catalog;

        public event Action OnRosterChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _catalog.AddRange(Resources.LoadAll<StaffSO>("Craft/Staff"));
            if (_catalog.Count == 0)
                SeedFallbackCatalog();
        }

        public bool IsHired(StaffSO staff) =>
            staff != null && _hired.Contains(staff.staffId);

        public int HiredCount => _hired.Count;

        public bool TryHire(StaffSO staff)
        {
            if (staff == null || _hired.Contains(staff.staffId)) return false;
            if (_hired.Count >= MaxHired) return false;
            if (MoneyManager.Instance == null || MoneyManager.Instance.Money < staff.hireCost)
                return false;

            MoneyManager.Instance.SpendMoney(staff.hireCost);
            _hired.Add(staff.staffId);
            OnRosterChanged?.Invoke();
            return true;
        }

        public int ScaleUnderstandingGain(CultureGroup culture, int baseGain)
        {
            if (baseGain <= 0) return baseGain;
            float bonus = 0f;
            foreach (var staff in _catalog)
            {
                if (staff == null || !_hired.Contains(staff.staffId)) continue;
                if (staff.cultureGroup == culture)
                    bonus = Mathf.Max(bonus, staff.understandingBonus);
            }
            return Mathf.Max(baseGain, Mathf.RoundToInt(baseGain * (1f + bonus)));
        }

        public string GetTabooHint(CraftCustomerSO customer)
        {
            if (customer == null || customer.diet == Diet.None) return null;
            foreach (var staff in _catalog)
            {
                if (staff == null || !_hired.Contains(staff.staffId)) continue;
                if (staff.cultureGroup != customer.cultureGroup) continue;
                if (!string.IsNullOrWhiteSpace(staff.tabooHint))
                    return $"{staff.displayName}: {staff.tabooHint}";
            }
            return null;
        }

        public void PayDailyWages(DailyLedger ledger)
        {
            int total = 0;
            foreach (var staff in _catalog)
            {
                if (staff == null || !_hired.Contains(staff.staffId)) continue;
                total += staff.dailyWage;
            }
            if (total <= 0 || ledger == null) return;
            MoneyManager.Instance?.SpendMoney(total);
            ledger.AddPurchase(total, "직원 급여");
        }

        private void SeedFallbackCatalog()
        {
            _catalog.Add(Make("amin", "아민", CultureGroup.Muslim, 200, 90, 0.25f,
                "돼지고기·알코올은 빼세요. 칼과 도마도 구분하는 게 좋아요.",
                "할랄 주방을 아는 조리원."));
            _catalog.Add(Make("anand", "아난드", CultureGroup.Hindu, 200, 90, 0.25f,
                "소고기는 안 됩니다. 채식 커리면 안심하세요.",
                "남아시아 식문화를 아는 동료."));
            _catalog.Add(Make("linh", "린", CultureGroup.SEAsian, 180, 80, 0.2f,
                "해산물 알레르기와 가격에 민감한 손님이 많아요.",
                "동남아 이주 노동자 커뮤니티와 가깝다."));
            _catalog.Add(Make("michelle", "미셸", CultureGroup.AfricanAmerican, 200, 90, 0.2f,
                "소울푸드는 정성이에요. 대충 내면 바로 티 납니다.",
                "동네 상생 캠페인에서 만났다."));
        }

        private static StaffSO Make(string id, string name, CultureGroup culture,
            int hire, int wage, float bonus, string hint, string bio)
        {
            var so = ScriptableObject.CreateInstance<StaffSO>();
            so.staffId = id;
            so.displayName = name;
            so.cultureGroup = culture;
            so.hireCost = hire;
            so.dailyWage = wage;
            so.understandingBonus = bonus;
            so.tabooHint = hint;
            so.bio = bio;
            return so;
        }
    }
}
