using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Inventory;
using UnityEngine;

namespace ChangJun.Delivery
{
    /// <summary>
    /// 아침 배달 신선도·이벤트 처리.
    /// </summary>
    public sealed class DeliveryManager : MonoBehaviour
    {
        public static DeliveryManager Instance { get; private set; }

        private List<DeliveryEventSO> _events = new();
        private int _freshness = 100;
        private DeliveryEventSO _lastEvent;

        public int Freshness => _freshness;
        public DeliveryEventSO LastEvent => _lastEvent;

        public event System.Action<int, DeliveryEventSO> OnDeliveryProcessed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _events = new List<DeliveryEventSO>(Resources.LoadAll<DeliveryEventSO>("Craft/Delivery"));
        }

        public void RollMorningEvent()
        {
            _freshness = 100;
            _lastEvent = PickEvent();

            if (_lastEvent != null)
                _freshness = Mathf.Clamp(_freshness - _lastEvent.freshnessPenalty, 0, 100);
        }

        public void CompleteDelivery()
        {
            if (_lastEvent != null)
                ApplyStockLoss(_lastEvent.stockLossRatio);

            InventoryManager.Instance.ReceiveDeliveries(out _);

            if (_lastEvent != null && _lastEvent.bonusWarehouseUnitsPerIngredient > 0)
                ApplyLocalBonus(_lastEvent.bonusWarehouseUnitsPerIngredient);

            OnDeliveryProcessed?.Invoke(_freshness, _lastEvent);
        }

        private static void ApplyLocalBonus(int units)
        {
            foreach (var ing in InventoryManager.Instance.GetAllIngredients())
            {
                if (ing == null || !ing.isLocalSourced) continue;
                InventoryManager.Instance.AddStock(ing.code, units);
            }
        }

        [System.Obsolete("Use RollMorningEvent + CompleteDelivery")]
        public void ProcessMorningDelivery()
        {
            RollMorningEvent();
            CompleteDelivery();
        }

        public int ApplyFreshnessMultiplier(int basePrice)
        {
            float mult = Mathf.Lerp(0.7f, 1f, _freshness / 100f);
            return Mathf.RoundToInt(basePrice * mult);
        }

        private DeliveryEventSO PickEvent()
        {
            if (_events.Count == 0) return null;

            float total = 0f;
            foreach (var e in _events)
                if (e != null) total += e.spawnWeight;

            if (total <= 0f) return null;

            float roll = Random.Range(0f, total);
            float acc = 0f;
            foreach (var e in _events)
            {
                if (e == null) continue;
                acc += e.spawnWeight;
                if (roll <= acc) return e;
            }

            return null;
        }

        private void ApplyStockLoss(float ratio)
        {
            if (ratio <= 0f) return;

            foreach (var ing in InventoryManager.Instance.GetAllIngredients())
            {
                int total = InventoryManager.Instance.GetStock(ing.code)
                            + InventoryManager.Instance.GetWarehouse(ing.code);
                int loss = Mathf.CeilToInt(total * ratio);
                InventoryManager.Instance.ReduceStock(ing.code, loss);
            }
        }
    }
}
