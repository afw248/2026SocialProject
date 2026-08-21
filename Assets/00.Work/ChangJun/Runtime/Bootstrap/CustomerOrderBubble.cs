using System;
using ChangJun.Data;
using ChangJun.Inventory;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 주문받기 화면 — Cupbap Order UI 목업의 "주문받기" 상태 전체 화면.
    /// 손님 슬롯 자리 + 주문서 티켓 + 접수 버튼으로 구성된다.
    /// </summary>
    public sealed class CustomerOrderBubble
    {
        private static readonly Color NightBg = new Color32(0x6B, 0x42, 0x29, 0xFF);

        private readonly GameObject _root;
        private readonly TextMeshProUGUI _headerText;
        private readonly TextMeshProUGUI _nameText;
        private readonly TextMeshProUGUI _orderText;
        private readonly RectTransform _iconRow;
        private static int _orderCounter;

        public event Action OnAccepted;

        public CustomerOrderBubble(RectTransform dock)
        {
            IngredientVisualCatalog.EnsureLoaded();

            _root = new GameObject("OrderScreen", typeof(RectTransform));
            _root.transform.SetParent(dock, false);
            UiFactory.Stretch(_root.GetComponent<RectTransform>());

            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = NightBg;

            // ── 인내심 표시(장식용) ──
            var patienceZone = UiFactory.CreatePanel(_root.transform, "Patience",
                new Vector2(0.06f, 0.85f), new Vector2(0.32f, 0.94f), Vector2.zero, Vector2.zero);
            UiFactory.CreateText(patienceZone, "Label", "인내심",
                new Vector2(0f, 0.6f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 13, Color.white);
            var patienceBar = UiTheme.CreateBorderedPanel(patienceZone, "Bar",
                new Vector2(0f, 0f), new Vector2(1f, 0.55f), Vector2.zero, Vector2.zero, UiTheme.CardWhite, 2f);
            var pHlg = patienceBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            pHlg.childControlWidth = true;
            pHlg.childControlHeight = true;
            pHlg.childForceExpandWidth = true;
            CreatePatienceSegment(patienceBar, UiTheme.Danger);
            CreatePatienceSegment(patienceBar, UiTheme.Gold);
            CreatePatienceSegment(patienceBar, UiTheme.Success);

            // ── 손님 슬롯 (장식용 플레이스홀더) ──
            var slot = UiTheme.CreateBorderedPanel(_root.transform, "CustomerSlot",
                new Vector2(0.06f, 0.14f), new Vector2(0.42f, 0.80f), Vector2.zero, Vector2.zero,
                new Color32(0xFF, 0xD9, 0xA0, 0xFF), 3f);
            UiFactory.CreateText(slot, "SlotTitle", "CUSTOMER SLOT",
                new Vector2(0f, 0.9f), new Vector2(1f, 0.98f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 16, UiTheme.TextDark);
            UiFactory.CreateText(slot, "SlotSub", "손님 캐릭터 자리 (placeholder)",
                new Vector2(0f, 0.84f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 12, new Color(0.35f, 0.25f, 0.16f));
            var head = UiFactory.CreatePanel(slot, "Head",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70f, 40f), new Vector2(70f, 180f));
            var headBorder = head.gameObject.AddComponent<Image>();
            headBorder.color = UiTheme.Border;
            var headFill = UiFactory.CreatePanel(head, "Fill", Vector2.zero, Vector2.one,
                new Vector2(3f, 3f), new Vector2(-3f, -3f));
            headFill.gameObject.AddComponent<Image>().color = UiTheme.Accent;
            var body = UiFactory.CreatePanel(slot, "Body",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-130f, -140f), new Vector2(130f, 46f));
            var bodyBorder = body.gameObject.AddComponent<Image>();
            bodyBorder.color = UiTheme.Border;
            var bodyFill = UiFactory.CreatePanel(body, "Fill", Vector2.zero, Vector2.one,
                new Vector2(3f, 3f), new Vector2(-3f, -3f));
            bodyFill.gameObject.AddComponent<Image>().color = UiTheme.Gold;

            // ── 주문서 티켓 ──
            UiFactory.CreateText(_root.transform, "TicketLabel", "주문서 · ORDER TICKET",
                new Vector2(0.46f, 0.82f), new Vector2(0.78f, 0.88f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, UiTheme.CardWhite);

            var panel = UiTheme.CreateShadowCard(_root.transform, "Panel",
                new Vector2(0.46f, 0.30f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero,
                UiTheme.Background, 4f, 6f);

            _headerText = UiFactory.CreateText(panel, "Header", "주문번호 #1",
                new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.96f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 15, UiTheme.TextMuted);

            _nameText = UiFactory.CreateText(panel, "Name", "",
                new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.86f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 20, UiTheme.TextDark);
            _nameText.fontStyle = FontStyles.Bold;

            ReceiptUiHelper.CreateDashedRule(panel,
                new Vector2(0.06f, 0.7f), new Vector2(0.94f, 0.73f));

            _orderText = UiFactory.CreateText(panel, "Order", "",
                new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.69f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, 17, UiTheme.TextDark);
            _orderText.textWrappingMode = TextWrappingModes.Normal;

            _iconRow = UiFactory.CreatePanel(panel, "IconRow",
                new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.5f),
                Vector2.zero, Vector2.zero);
            var hlg = _iconRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            // ── 접수 버튼 ──
            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(_root.transform, "AcceptBtn",
                    new Vector2(0.46f, 0.16f), new Vector2(0.94f, 0.27f), Vector2.zero, Vector2.zero),
                "바로 주문 받기", UiTheme.Accent, Accept, 18);

            _root.SetActive(false);
        }

        private static void CreatePatienceSegment(Transform parent, Color color)
        {
            var seg = new GameObject("Seg", typeof(RectTransform));
            seg.transform.SetParent(parent, false);
            seg.AddComponent<Image>().color = color;
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
        }

        public void HideImmediate() => _root.SetActive(false);

        private void RebuildIcons(CraftCustomerSO customer)
        {
            foreach (Transform child in _iconRow)
                UnityEngine.Object.Destroy(child.gameObject);

            var menu = customer.requiredMenu;
            if (menu?.ingredientCodes == null) return;

            UiFactory.CreateText(_iconRow, "Eq", "=",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 22, UiTheme.TextDark);

            for (int i = 0; i < menu.ingredientCodes.Length; i++)
            {
                if (i > 0)
                {
                    UiFactory.CreateText(_iconRow, $"Plus{i}", "+",
                        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                        TextAlignmentOptions.Center, 20, UiTheme.TextMuted);
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
            _root.SetActive(false);
            OnAccepted?.Invoke();
        }
    }
}
