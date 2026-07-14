using System;
using System.Collections.Generic;
using ChangJun.Data;
using UnityEngine;

namespace ChangJun.News
{
    /// <summary>
    /// 뉴스 발행 및 문화별 가격 보정. SO 기반 확장.
    /// </summary>
    public sealed class NewsManager : MonoBehaviour
    {
        public static NewsManager Instance { get; private set; }

        private readonly Dictionary<CultureGroup, float> _multipliers = new();
        private List<NewsSO> _newsPool = new();
        private NewsSO _todayNews;

        public NewsSO TodayNews => _todayNews;

        public event Action<NewsSO> OnNewsPublished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _newsPool = new List<NewsSO>(Resources.LoadAll<NewsSO>("Craft/News"));
            ResetMultipliers();
        }

        public void ResetMultipliers()
        {
            _multipliers.Clear();
            foreach (CultureGroup culture in Enum.GetValues(typeof(CultureGroup)))
                _multipliers[culture] = 1f;
        }

        public void RollDailyNews()
        {
            _todayNews = PickWeightedNews();
            ResetMultipliers();

            if (_todayNews == null) return;

            _multipliers[_todayNews.cultureGroup] = _todayNews.priceMultiplier;
            OnNewsPublished?.Invoke(_todayNews);
        }

        public int ApplyPriceMultiplier(MenuRecipeSO menu, int basePrice)
        {
            if (menu == null) return basePrice;
            float mult = _multipliers.TryGetValue(menu.cultureGroup, out var m) ? m : 1f;
            return Mathf.RoundToInt(basePrice * mult);
        }

        public float GetMultiplier(CultureGroup culture) =>
            _multipliers.TryGetValue(culture, out var m) ? m : 1f;

        private NewsSO PickWeightedNews()
        {
            if (_newsPool.Count == 0) return null;

            float total = 0f;
            foreach (var n in _newsPool)
                if (n != null) total += n.spawnWeight;

            if (total <= 0f) return _newsPool[0];

            float roll = UnityEngine.Random.Range(0f, total);
            float acc = 0f;
            foreach (var n in _newsPool)
            {
                if (n == null) continue;
                acc += n.spawnWeight;
                if (roll <= acc) return n;
            }

            return _newsPool[0];
        }
    }
}
