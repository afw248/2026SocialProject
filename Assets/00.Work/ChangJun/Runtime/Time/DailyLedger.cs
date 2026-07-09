using System;
using System.Collections.Generic;

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
        public int CustomersServed { get; private set; }

        public int NetProfit => Revenue - IngredientCost - PenaltyLoss - PurchaseCost;

        private readonly List<string> _lines = new();

        public IReadOnlyList<string> Lines => _lines;

        public void Reset()
        {
            Revenue = 0;
            IngredientCost = 0;
            PenaltyLoss = 0;
            PurchaseCost = 0;
            CustomersServed = 0;
            _lines.Clear();
        }

        public void AddRevenue(int amount, string label)
        {
            Revenue += amount;
            _lines.Add($"+ {label}: {amount:N0}원");
            CustomersServed++;
        }

        public void AddPenalty(int amount, string label)
        {
            PenaltyLoss += amount;
            _lines.Add($"- {label}: {amount:N0}원");
            CustomersServed++;
        }

        public void AddIngredientCost(int amount, string label)
        {
            IngredientCost += amount;
            _lines.Add($"- {label}: {amount:N0}원");
        }

        public void AddPurchase(int amount, string label)
        {
            PurchaseCost += amount;
            _lines.Add($"- {label}: {amount:N0}원");
        }
    }
}
