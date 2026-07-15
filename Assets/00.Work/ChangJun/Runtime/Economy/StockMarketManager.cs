using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.News;
using ChangJun.Time;
using UnityEngine;

namespace ChangJun.Economy
{
    /// <summary>
    /// 문화권 테마 주식 시세·보유·거래. 뉴스 이벤트와 연동한다.
    /// </summary>
    public sealed class StockMarketManager : MonoBehaviour
    {
        public static StockMarketManager Instance { get; private set; }

        private readonly Dictionary<string, StockTickerSO> _tickers = new();
        private readonly Dictionary<string, int> _prices = new();
        private readonly Dictionary<string, int> _previousClose = new();
        private readonly Dictionary<string, int> _holdings = new();

        private List<StockTickerSO> _tickerList = new();

        public event Action OnMarketUpdated;

        public IReadOnlyList<StockTickerSO> Tickers => _tickerList;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTickers();
            InitializePrices();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RollDailyMarket(NewsSO news)
        {
            foreach (var ticker in _tickerList)
            {
                if (ticker == null) continue;

                string code = ticker.code;
                int prev = _prices.TryGetValue(code, out var p) ? p : ticker.basePrice;
                _previousClose[code] = prev;

                float drift = UnityEngine.Random.Range(-ticker.volatility, ticker.volatility);
                float newsImpact = GetNewsImpact(ticker, news);
                int next = Mathf.Max(100, Mathf.RoundToInt(prev * (1f + drift + newsImpact)));
                _prices[code] = next;
            }

            OnMarketUpdated?.Invoke();
        }

        public int GetPrice(string code) =>
            _prices.TryGetValue(code, out var p) ? p : 0;

        public int GetPreviousClose(string code) =>
            _previousClose.TryGetValue(code, out var p) ? p : GetPrice(code);

        public float GetChangePercent(string code)
        {
            int prev = GetPreviousClose(code);
            int now = GetPrice(code);
            if (prev <= 0) return 0f;
            return (now - prev) / (float)prev * 100f;
        }

        public int GetHolding(string code) =>
            _holdings.TryGetValue(code, out var qty) ? qty : 0;

        public int GetPortfolioValue()
        {
            int total = 0;
            foreach (var kv in _holdings)
                total += kv.Value * GetPrice(kv.Key);
            return total;
        }

        public bool TryBuy(string code, int quantity = 1)
        {
            if (quantity <= 0 || !_tickers.ContainsKey(code)) return false;

            int cost = GetPrice(code) * quantity;
            if (MoneyManager.Instance.Money < cost) return false;

            MoneyManager.Instance.SpendMoney(cost);
            _holdings[code] = GetHolding(code) + quantity;
            DayLoopController.Instance?.Ledger.AddStockPurchase(cost, $"{_tickers[code].displayName} x{quantity}");
            OnMarketUpdated?.Invoke();
            return true;
        }

        public bool TrySell(string code, int quantity = 1)
        {
            if (quantity <= 0 || GetHolding(code) < quantity) return false;

            int revenue = GetPrice(code) * quantity;
            _holdings[code] = GetHolding(code) - quantity;
            if (_holdings[code] <= 0)
                _holdings.Remove(code);

            MoneyManager.Instance.AddMoney(revenue);
            DayLoopController.Instance?.Ledger.AddStockSale(revenue, $"{_tickers[code].displayName} x{quantity}");
            OnMarketUpdated?.Invoke();
            return true;
        }

        private void LoadTickers()
        {
            _tickerList = new List<StockTickerSO>(Resources.LoadAll<StockTickerSO>("Craft/Stocks"));
            _tickerList.Sort((a, b) => string.CompareOrdinal(a.code, b.code));
            _tickers.Clear();
            foreach (var t in _tickerList)
            {
                if (t != null && !string.IsNullOrEmpty(t.code))
                    _tickers[t.code] = t;
            }
        }

        private void InitializePrices()
        {
            _prices.Clear();
            _previousClose.Clear();
            foreach (var ticker in _tickerList)
            {
                if (ticker == null) continue;
                _prices[ticker.code] = ticker.basePrice;
                _previousClose[ticker.code] = ticker.basePrice;
            }
        }

        private static float GetNewsImpact(StockTickerSO ticker, NewsSO news)
        {
            if (news == null || ticker == null) return 0f;

            bool direct = !string.IsNullOrEmpty(news.primaryStockCode)
                && news.primaryStockCode == ticker.code;
            bool cultureMatch = ticker.cultureGroup == news.cultureGroup;

            if (!direct && !cultureMatch) return 0f;

            return news.sentiment switch
            {
                NewsSentiment.Positive => (news.priceMultiplier - 1f) * 0.6f,
                NewsSentiment.Negative => (news.priceMultiplier - 1f) * 0.5f,
                NewsSentiment.Discrimination => -0.04f,
                _ => 0f,
            };
        }
    }
}
