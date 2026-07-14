using System;
using ChangJun.Delivery;
using ChangJun.Inventory;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    public sealed class MorningDeliveryOverlay
    {
        private readonly GameObject _root;
        private readonly TextMeshProUGUI _bodyText;

        public event Action OnReceived;

        public MorningDeliveryOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("MorningDelivery", 80);
            _root.SetActive(false);

            var panel = UiFactory.CreatePanel(_root.transform, "Panel",
                new Vector2(0.32f, 0.32f), new Vector2(0.68f, 0.68f),
                Vector2.zero, Vector2.zero);
            panel.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.97f, 1f);

            UiFactory.CreateText(panel, "Title", "아침",
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 30,
                new Color(0.1f, 0.2f, 0.35f));

            _bodyText = UiFactory.CreateText(panel, "Body", "",
                new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.8f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 22,
                new Color(0.15f, 0.15f, 0.2f));
            _bodyText.enableWordWrapping = true;

            var btnRt = UiFactory.CreatePanel(panel, "Btn",
                new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.2f),
                Vector2.zero, Vector2.zero);
            var btn = btnRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnRt.gameObject.AddComponent<Image>();
            btn.targetGraphic.color = new Color(0.2f, 0.5f, 0.3f);
            btn.onClick.AddListener(Receive);
            UiFactory.CreateText(btnRt, "Label", "하루 시작",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 24);
        }

        public void Show()
        {
            DeliveryManager.Instance.RollMorningEvent();
            _bodyText.text = BuildMorningMessage();
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private static string BuildMorningMessage()
        {
            int day = DayLoopController.Instance.Day;
            bool hasDelivery = HasPendingDelivery();

            if (!hasDelivery)
                return $"{day}일차 아침입니다.\n\n가게 문을 열 준비를 합니다.";

            var freshness = DeliveryManager.Instance.Freshness;
            var evt = DeliveryManager.Instance.LastEvent;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("어제 주문한 재료가 도착했습니다.");
            sb.AppendLine($"신선도 {freshness}%");

            if (evt != null)
            {
                sb.AppendLine();
                sb.AppendLine(evt.headline);
            }

            return sb.ToString();
        }

        private static bool HasPendingDelivery()
        {
            foreach (var ing in InventoryManager.Instance.GetAllIngredients())
            {
                if (InventoryManager.Instance.GetWarehouse(ing.code) > 0)
                    return true;
            }
            return false;
        }

        private void Receive()
        {
            DeliveryManager.Instance.CompleteDelivery();
            Hide();
            OnReceived?.Invoke();
        }
    }
}
