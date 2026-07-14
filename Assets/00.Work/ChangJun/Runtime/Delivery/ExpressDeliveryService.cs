using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Inventory;
using ChangJun.Time;
using UnityEngine;

namespace ChangJun.Delivery
{
    public sealed class ExpressDeliveryOrder
    {
        public string IngredientCode { get; }
        public int Quantity { get; }
        public ExpressDeliveryTier Tier { get; }
        public int ArrivalMinutes { get; }
        public int PaidCost { get; }

        public ExpressDeliveryOrder(string ingredientCode, int quantity, ExpressDeliveryTier tier,
            int arrivalMinutes, int paidCost)
        {
            IngredientCode = ingredientCode;
            Quantity = quantity;
            Tier = tier;
            ArrivalMinutes = arrivalMinutes;
            PaidCost = paidCost;
        }
    }

    /// <summary>
    /// 영업 중 즉시 주문 배달 — 한집(30분·2배) / 알뜰(60분·정가).
    /// </summary>
    public sealed class ExpressDeliveryService : MonoBehaviour
    {
        public static ExpressDeliveryService Instance { get; private set; }

        private readonly List<ExpressDeliveryOrder> _pending = new();
        private int _lastCheckedMinute = -1;

        public IReadOnlyList<ExpressDeliveryOrder> Pending => _pending;

        public event Action OnPendingChanged;
        public event Action<ExpressDeliveryOrder> OnOrderArrived;

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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void HookDayLoop(DayLoopController dayLoop)
        {
            if (dayLoop == null) return;
            dayLoop.OnTimeChanged -= HandleTimeChanged;
            dayLoop.OnTimeChanged += HandleTimeChanged;
        }

        public void UnhookDayLoop(DayLoopController dayLoop)
        {
            if (dayLoop == null) return;
            dayLoop.OnTimeChanged -= HandleTimeChanged;
        }

        public void ResetForNewDay()
        {
            _pending.Clear();
            _lastCheckedMinute = -1;
            OnPendingChanged?.Invoke();
        }

        public void BeginBusinessTracking()
        {
            _lastCheckedMinute = DayLoopController.Instance.CurrentMinutes;
        }

        public bool TryPlaceOrder(ExpressDeliveryTier tier, Dictionary<string, int> cart)
        {
            if (DayLoopController.Instance.Phase != DayPhase.Open)
                return false;

            if (cart == null || cart.Count == 0)
                return false;

            var config = DayLoopController.Instance.Config;
            float priceMult = tier == ExpressDeliveryTier.Hanjip
                ? config.expressDeliveryPriceMultiplier
                : 1f;
            int deliveryMinutes = tier == ExpressDeliveryTier.Hanjip
                ? config.expressDeliveryMinutes
                : config.economyDeliveryMinutes;

            int totalCost = 0;
            foreach (var pair in cart)
            {
                var ing = InventoryManager.Instance.GetIngredient(pair.Key);
                if (ing == null || pair.Value <= 0) continue;
                totalCost += Mathf.RoundToInt(ing.purchasePrice * priceMult) * pair.Value;
            }

            if (totalCost <= 0)
                return false;

            if (totalCost > MoneyManager.Instance.Money)
                return false;

            int arrival = DayLoopController.Instance.CurrentMinutes + deliveryMinutes;
            string tierLabel = tier == ExpressDeliveryTier.Hanjip ? "한집배달" : "알뜰배달";

            foreach (var pair in cart)
            {
                if (pair.Value <= 0) continue;
                var ing = InventoryManager.Instance.GetIngredient(pair.Key);
                if (ing == null) continue;

                int lineCost = Mathf.RoundToInt(ing.purchasePrice * priceMult) * pair.Value;
                _pending.Add(new ExpressDeliveryOrder(
                    pair.Key,
                    pair.Value,
                    tier,
                    arrival,
                    lineCost));

                DayLoopController.Instance.Ledger.AddPurchase(
                    lineCost,
                    $"{tierLabel} {ing.displayName} x{pair.Value}");
            }

            MoneyManager.Instance.SpendMoney(totalCost);
            OnPendingChanged?.Invoke();
            ProcessMinutesUpTo(DayLoopController.Instance.CurrentMinutes);
            return true;
        }

        public static string FormatArrival(int totalMinutes)
        {
            int hour = totalMinutes / 60;
            int minute = totalMinutes % 60;
            int displayHour = hour % 12;
            if (displayHour == 0) displayHour = 12;
            string ampm = hour < 12 ? "AM" : "PM";
            return $"{displayHour}:{minute:00} {ampm}";
        }

        private void HandleTimeChanged(int hour, int minute)
        {
            int now = hour * 60 + minute;
            ProcessMinutesUpTo(now);
        }

        private void ProcessMinutesUpTo(int now)
        {
            if (_lastCheckedMinute < 0)
                _lastCheckedMinute = now;

            for (int t = _lastCheckedMinute + 1; t <= now; t++)
                DeliverAt(t);

            _lastCheckedMinute = now;
        }

        private void DeliverAt(int minute)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var order = _pending[i];
                if (order.ArrivalMinutes > minute) continue;

                InventoryManager.Instance.AddStock(order.IngredientCode, order.Quantity);
                _pending.RemoveAt(i);
                OnOrderArrived?.Invoke(order);
                OnPendingChanged?.Invoke();
            }
        }
    }
}
