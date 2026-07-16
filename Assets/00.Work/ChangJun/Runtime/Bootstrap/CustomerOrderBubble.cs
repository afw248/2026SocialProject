using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Inventory;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 좌측 고정 주문 도크 — 영수증 스타일.
    /// </summary>
    public sealed class CustomerOrderBubble
    {
        private readonly GameObject _root;
        private readonly CanvasGroup _group;
        private readonly TextMeshProUGUI _headerText;
        private readonly TextMeshProUGUI _nameText;
        private readonly TextMeshProUGUI _orderText;
        private readonly RectTransform _iconRow;
        private static int _orderCounter;

        public event Action OnAccepted;

        public CustomerOrderBubble(RectTransform dock)
        {
            IngredientVisualCatalog.EnsureLoaded();

            _root = new GameObject("OrderDock", typeof(RectTransform));
            _root.transform.SetParent(dock, false);
            UiFactory.Stretch(_root.GetComponent<RectTransform>());

            _group = _root.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            var panel = ReceiptUiHelper.CreatePaperPanel(_root.transform, "Panel",
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));

            _headerText = UiFactory.CreateText(panel, "Header", "주문번호 #1",
                new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.96f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 15, ReceiptUiHelper.MutedInk);

            _nameText = UiFactory.CreateText(panel, "Name", "",
                new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.86f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 20, ReceiptUiHelper.InkColor);
            _nameText.fontStyle = FontStyles.Bold;

            ReceiptUiHelper.CreateDashedRule(panel,
                new Vector2(0.06f, 0.7f), new Vector2(0.94f, 0.73f));

            _orderText = UiFactory.CreateText(panel, "Order", "",
                new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.69f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 17, ReceiptUiHelper.InkColor);
            _orderText.textWrappingMode = TextWrappingModes.Normal;

            _iconRow = UiFactory.CreatePanel(panel, "IconRow",
                new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.5f),
                Vector2.zero, Vector2.zero);
            var hlg = _iconRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            ReceiptUiHelper.CreatePaperButton(panel, "오케이",
                new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.18f),
                Accept, ReceiptUiHelper.AccentBrown);

            _root.SetActive(false);
        }

        public void Show(CraftCustomerSO customer)
        {
            if (customer == null) return;

            _orderCounter++;
            _headerText.text = $"주문번호 #{_orderCounter}  ·  {DayLoopController.Instance.Day}일차";

            string dietLabel = customer.diet == Diet.None ? "" : $" · {customer.diet}";
            _nameText.text = customer.customerName + dietLabel;
            _orderText.text = customer.orderLine;

            RebuildIcons(customer);

            _root.SetActive(true);
            _group.alpha = 1f;
        }

        public void HideImmediate()
        {
            _group.alpha = 0f;
            _root.SetActive(false);
        }

        private void RebuildIcons(CraftCustomerSO customer)
        {
            foreach (Transform child in _iconRow)
                UnityEngine.Object.Destroy(child.gameObject);

            var menu = customer.requiredMenu;
            if (menu?.ingredientCodes == null) return;

            UiFactory.CreateText(_iconRow, "Eq", "=",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 22, ReceiptUiHelper.InkColor);

            for (int i = 0; i < menu.ingredientCodes.Length; i++)
            {
                if (i > 0)
                {
                    UiFactory.CreateText(_iconRow, $"Plus{i}", "+",
                        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                        TextAlignmentOptions.Center, 20, ReceiptUiHelper.MutedInk);
                }

                string code = menu.ingredientCodes[i];
                string label = GetIngredientLabel(code);
                CreateIconChip(_iconRow, code, label);
            }
        }

        private static string GetIngredientLabel(string code)
        {
            foreach (var ing in InventoryManager.Instance.GetAllIngredients())
            {
                if (ing != null && ing.code == code)
                    return ing.displayName;
            }
            return code;
        }

        private static void CreateIconChip(Transform parent, string code, string label)
        {
            var chip = new GameObject($"Icon_{code}", typeof(RectTransform));
            chip.transform.SetParent(parent, false);
            var le = chip.AddComponent<LayoutElement>();
            le.preferredWidth = 52f;
            le.preferredHeight = 52f;

            var img = chip.AddComponent<Image>();
            var sprite = IngredientVisualCatalog.GetButtonIcon(code);
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = sprite != null ? Color.white : new Color(0.3f, 0.35f, 0.45f);

            var trigger = chip.AddComponent<IngredientHoverTrigger>();
            trigger.Setup(label);
        }

        private void Accept()
        {
            _group.alpha = 0f;
            _root.SetActive(false);
            OnAccepted?.Invoke();
        }
    }
}
