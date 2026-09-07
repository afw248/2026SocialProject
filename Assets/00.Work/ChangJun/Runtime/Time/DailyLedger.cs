using System;
using System.Collections.Generic;
using ChangJun.Economy;

namespace ChangJun.Time
{
    /// <summary>
    /// 당일 매출·비용·손실을 집계한다.
    /// </summary>
    public sealed class DailyLedger
    {
        public int Revenue { get; private set; }
        public int IngredientCost { get; private set; }
        public int PenaltyLoss { get; private set; }
        public int PurchaseCost { get; private set; }
        public int StockPurchaseCost { get; private set; }
        public int StockSaleRevenue { get; private set; }
        public int CustomersServed { get; private set; }

        public int NetProfit => Revenue - IngredientCost - PenaltyLoss - PurchaseCost
            - StockPurchaseCost + StockSaleRevenue + SubsidyIncome + DividendIncome;

        public int SubsidyIncome { get; private set; }
        public int DividendIncome { get; private set; }

        private readonly List<string> _lines = new();
        private readonly List<string> _missedOrders = new();
        private readonly Dictionary<string, (int count, int total)> _menuSales = new();

        public IReadOnlyList<string> Lines => _lines;

        /// <summary>메뉴별 판매 수·합계 — 정산 화면 판매 내역 표에 쓰인다.</summary>
        public IEnumerable<(string name, int count, int total)> MenuSales
        {
            get
            {
                foreach (var kv in _menuSales)
                    yield return (kv.Key, kv.Value.count, kv.Value.total);
            }
        }

        /// <summary>금기위반·오주문으로 놓친 주문 사유 목록.</summary>
        public IReadOnlyList<string> MissedOrders => _missedOrders;

        public void Reset()
        {
            Revenue = 0;
            IngredientCost = 0;
            PenaltyLoss = 0;
            PurchaseCost = 0;
            StockPurchaseCost = 0;
            StockSaleRevenue = 0;
            SubsidyIncome = 0;
            DividendIncome = 0;
            CustomersServed = 0;
            _lines.Clear();
            _missedOrders.Clear();
            _menuSales.Clear();
        }

        public void AddRevenue(int amount, string label)
        {
            amount = EconomyClamp.ClampMoney(amount);
            Revenue = EconomyClamp.SafeAdd(Revenue, amount);
            _lines.Add($"+ {label}: {amount:N0}원");
            CustomersServed++;

            var prev = _menuSales.TryGetValue(label, out var v) ? v : (0, 0);
            _menuSales[label] = (
                EconomyClamp.SafeAdd(prev.Item1, 1),
                EconomyClamp.SafeAdd(prev.Item2, amount));
        }

        public void AddWalkout(string label)
        {
            _missedOrders.Add(label);
            _lines.Add($"· {label}");
        }

        public void AddPenalty(int amount, string label)
        {
            amount = EconomyClamp.ClampMoney(amount);
            PenaltyLoss = EconomyClamp.SafeAdd(PenaltyLoss, amount);
            _lines.Add($"- {label}: {amount:N0}원");
            CustomersServed++;
            _missedOrders.Add(label);
        }

        public void AddIngredientCost(int amount, string label)
        {
            amount = EconomyClamp.ClampMoney(amount);
            IngredientCost = EconomyClamp.SafeAdd(IngredientCost, amount);
            _lines.Add($"- {label}: {amount:N0}원");
        }

        public void AddPurchase(int amount, string label)
        {
            amount = EconomyClamp.ClampMoney(amount);
            PurchaseCost = EconomyClamp.SafeAdd(PurchaseCost, amount);
            _lines.Add($"- {label}: {amount:N0}원");
        }

        public void AddStockPurchase(int amount, string label)
        {
            amount = EconomyClamp.ClampMoney(amount);
            StockPurchaseCost = EconomyClamp.SafeAdd(StockPurchaseCost, amount);
            _lines.Add($"- 주식 매수 {label}: {amount:N0}원");
        }

        public void AddStockSale(int amount, string label)
        {
            amount = EconomyClamp.ClampMoney(amount);
            StockSaleRevenue = EconomyClamp.SafeAdd(StockSaleRevenue, amount);
            _lines.Add($"+ 주식 매도 {label}: {amount:N0}원");
        }

        public void AddSubsidy(int amount, string label)
        {
            amount = EconomyClamp.ClampMoney(amount);
            SubsidyIncome = EconomyClamp.SafeAdd(SubsidyIncome, amount);
            _lines.Add($"+ {label}: {amount:N0}원");
        }

        public void AddDividend(int amount, string label)
        {
            amount = EconomyClamp.ClampMoney(amount);
            DividendIncome = EconomyClamp.SafeAdd(DividendIncome, amount);
            _lines.Add($"+ {label}: {amount:N0}원");
        }
    }
}
