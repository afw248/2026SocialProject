using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Progression;
using UnityEngine;

namespace ChangJun.Social
{
    public enum ActiveCulturalEvent
    {
        None,
        CultureFestival,
        FusionWorkshop,
    }

    /// <summary>
    /// 이해도 임계치 기반 문화 교류 이벤트.
    /// </summary>
    public sealed class CulturalEventManager : MonoBehaviour
    {
        public static CulturalEventManager Instance { get; private set; }

        private ActiveCulturalEvent _todayEvent = ActiveCulturalEvent.None;
        private CultureGroup _festivalCulture = CultureGroup.None;
        private readonly HashSet<string> _unlockedFusionMenuCodes = new();
        private readonly HashSet<CultureGroup> _milestone100 = new();

        public ActiveCulturalEvent TodayEvent => _todayEvent;
        public CultureGroup FestivalCulture => _festivalCulture;
        public float FestivalSpawnMultiplier => _todayEvent == ActiveCulturalEvent.CultureFestival ? 2f : 1f;
        public float FestivalPriceMultiplier => _todayEvent == ActiveCulturalEvent.CultureFestival ? 1.1f : 1f;

        public event Action<ActiveCulturalEvent, CultureGroup> OnEventStarted;

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

        public void RollDailyEvents()
        {
            _todayEvent = ActiveCulturalEvent.None;
            _festivalCulture = CultureGroup.None;

            if (UnderstandingManager.Instance == null) return;

            foreach (CultureGroup c in Enum.GetValues(typeof(CultureGroup)))
            {
                if (c == CultureGroup.None) continue;
                if (UnderstandingManager.Instance.GetUnderstanding(c) >= 80)
                {
                    _todayEvent = ActiveCulturalEvent.CultureFestival;
                    _festivalCulture = c;
                    OnEventStarted?.Invoke(_todayEvent, c);
                    return;
                }
            }

            int culturesAt50 = 0;
            foreach (CultureGroup c in Enum.GetValues(typeof(CultureGroup)))
            {
                if (c == CultureGroup.None) continue;
                if (UnderstandingManager.Instance.GetUnderstanding(c) >= 50)
                    culturesAt50++;
            }

            if (culturesAt50 >= 2)
            {
                _todayEvent = ActiveCulturalEvent.FusionWorkshop;
                UnlockDefaultFusionMenus();
                OnEventStarted?.Invoke(_todayEvent, CultureGroup.None);
            }
        }

        public bool IsFusionMenuUnlocked(string menuCode) =>
            _unlockedFusionMenuCodes.Contains(menuCode);

        public bool HasMilestone100(CultureGroup culture) => _milestone100.Contains(culture);

        public float GetCulturePriceBuff(CultureGroup culture)
        {
            if (!_milestone100.Contains(culture)) return 1f;
            return 1.05f;
        }

        public void CheckMilestones(CultureGroup culture, int value)
        {
            if (value >= 100 && _milestone100.Add(culture))
                Debug.Log($"[CulturalEvent] {culture} 이해도 100% 달성 — 해당 문화 메뉴 +5%");
        }

        private void UnlockDefaultFusionMenus()
        {
            foreach (var code in new[] { "M20", "M21", "M22", "M23" })
                _unlockedFusionMenuCodes.Add(code);
        }

        public void ForceUnlockFusion(string menuCode) => _unlockedFusionMenuCodes.Add(menuCode);
    }
}
