using System;
using UnityEngine;

namespace ChangJun.Economy
{
    /// <summary>
    /// 주문·재고·자산 수량/금액의 상한을 한곳에서 관리하고,
    /// 곱셈·덧셈 오버플로우를 포화 연산으로 막는다.
    /// </summary>
    public static class EconomyClamp
    {
        /// <summary>재료 상점·영업 중 배달 주문 1품목당 최대 수량</summary>
        public const int MaxOrderQuantity = 99;

        /// <summary>주식 매수/매도 1회 최대 수량</summary>
        public const int MaxStockTradeQuantity = 999;

        /// <summary>재료 재고·창고 보유 상한</summary>
        public const int MaxInventoryQuantity = 9999;

        /// <summary>주식 보유 상한(종목당)</summary>
        public const int MaxHoldingQuantity = 99999;

        /// <summary>현금 상한</summary>
        public const int MaxMoney = 999_999_999;

        public static int ClampOrderQuantity(int qty) =>
            Mathf.Clamp(qty, 0, MaxOrderQuantity);

        public static int ClampStockTradeQuantity(int qty) =>
            Mathf.Clamp(qty, 1, MaxStockTradeQuantity);

        public static int ClampInventoryQuantity(int qty) =>
            Mathf.Clamp(qty, 0, MaxInventoryQuantity);

        public static int ClampHoldingQuantity(int qty) =>
            Mathf.Clamp(qty, 0, MaxHoldingQuantity);

        public static int ClampMoney(int value) =>
            Mathf.Clamp(value, 0, MaxMoney);

        public static int SafeAdd(int a, int b)
        {
            long sum = (long)a + b;
            return (int)Math.Clamp(sum, 0L, MaxMoney);
        }

        public static int SafeAddInventory(int a, int b)
        {
            long sum = (long)a + b;
            return (int)Math.Clamp(sum, 0L, MaxInventoryQuantity);
        }

        public static int SafeAddHolding(int a, int b)
        {
            long sum = (long)a + b;
            return (int)Math.Clamp(sum, 0L, MaxHoldingQuantity);
        }

        public static int SafeMultiply(int unitPrice, int quantity)
        {
            if (unitPrice <= 0 || quantity <= 0) return 0;
            long product = (long)unitPrice * quantity;
            return (int)Math.Clamp(product, 0L, MaxMoney);
        }

        public static int SafeLineCost(int unitPrice, int quantity, float multiplier = 1f)
        {
            if (unitPrice <= 0 || quantity <= 0) return 0;
            double raw = (double)unitPrice * quantity * Math.Max(0.0, multiplier);
            if (double.IsNaN(raw) || double.IsInfinity(raw))
                return MaxMoney;
            return (int)Math.Clamp(Math.Round(raw), 0.0, MaxMoney);
        }
    }
}
