using System;
using ChangJun.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 좌측 고정 주문 도크 — 말풍선 대신 정돈된 패널로 표시한다.
    /// </summary>
    public sealed class CustomerOrderBubble
    {
        private readonly GameObject _root;
        private readonly CanvasGroup _group;
        private readonly TextMeshProUGUI _nameText;
        private readonly TextMeshProUGUI _orderText;

        public event Action OnAccepted;

        public CustomerOrderBubble(RectTransform dock)
        {
            _root = new GameObject("OrderDock", typeof(RectTransform));
            _root.transform.SetParent(dock, false);
            UiFactory.Stretch(_root.GetComponent<RectTransform>());

            _group = _root.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            var panel = UiFactory.CreateStretchChild(_root.transform, "Panel");
            var bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.97f, 0.95f, 0.9f);
            bg.raycastTarget = true;

            var border = panel.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.55f, 0.45f, 0.3f, 0.8f);
            border.effectDistance = new Vector2(1.5f, -1.5f);

            UiFactory.CreateText(panel, "Title", "주문",
                new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.98f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 16,
                new Color(0.45f, 0.38f, 0.28f));

            _nameText = UiFactory.CreateText(panel, "Name", "",
                new Vector2(0.06f, 0.7f), new Vector2(0.94f, 0.86f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 19,
                new Color(0.25f, 0.18f, 0.1f));
            _nameText.fontStyle = FontStyles.Bold;

            _orderText = UiFactory.CreateText(panel, "Order", "",
                new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.7f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 18,
                new Color(0.15f, 0.12f, 0.08f));
            _orderText.enableWordWrapping = true;
            _orderText.lineSpacing = 2f;

            var okBtnRt = UiFactory.CreatePanel(panel, "OkBtn",
                new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.2f),
                Vector2.zero, Vector2.zero);
            var okImg = okBtnRt.gameObject.AddComponent<Image>();
            okImg.color = new Color(0.82f, 0.72f, 0.45f);
            var okBtn = okBtnRt.gameObject.AddComponent<Button>();
            okBtn.targetGraphic = okImg;
            okBtn.onClick.AddListener(Accept);

            UiFactory.CreateText(okBtnRt, "Label", "오케이",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 22,
                new Color(0.2f, 0.15f, 0.08f));

            _root.SetActive(false);
        }

        public void Show(CraftCustomerSO customer)
        {
            if (customer == null) return;

            string dietLabel = customer.diet == Diet.None ? "" : $" · {customer.diet}";
            _nameText.text = customer.customerName + dietLabel;
            _orderText.text = customer.orderLine;

            _root.SetActive(true);
            _group.alpha = 1f;
        }

        public void HideImmediate()
        {
            _group.alpha = 0f;
            _root.SetActive(false);
        }

        private void Accept()
        {
            _group.alpha = 0f;
            _root.SetActive(false);
            OnAccepted?.Invoke();
        }
    }
}
