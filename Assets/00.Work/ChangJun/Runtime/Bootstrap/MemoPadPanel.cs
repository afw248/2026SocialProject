using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 메모 탭 — 손님 주문 기록만 보여준다.
    /// </summary>
    public sealed class MemoPadPanel
    {
        private const int MaxOrderRows = 40;

        private readonly GameObject _root;
        private readonly RectTransform _orderHistoryContent;
        private readonly List<GameObject> _orderRows = new();
        private GameObject _placeholderRow;

        public GameObject Root => _root;

        public MemoPadPanel(RectTransform parent)
        {
            _root = UiFactory.CreatePanel(parent, "MemoPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            _root.AddComponent<Image>().color = new Color(0.98f, 0.98f, 0.95f, 0.98f);

            UiFactory.CreateText(_root.transform, "Title", "MEMO · 손님 주문",
                new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 26,
                new Color(0.1f, 0.1f, 0.1f));

            UiFactory.CreateText(_root.transform, "OrderTitle", "오늘 손님들이 시킨 주문",
                new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.91f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 18,
                new Color(0.35f, 0.28f, 0.2f));

            var orderScrollRt = UiFactory.CreatePanel(_root.transform, "OrderScroll",
                new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.85f),
                Vector2.zero, Vector2.zero);
            orderScrollRt.gameObject.AddComponent<Image>().color = new Color(0.93f, 0.9f, 0.82f, 0.95f);

            var orderScroll = orderScrollRt.gameObject.AddComponent<ScrollRect>();
            orderScroll.horizontal = false;
            orderScroll.vertical = true;
            UiFactory.ConfigureScroll(orderScroll);

            var orderViewport = UiFactory.CreateStretchChild(orderScrollRt, "Viewport");
            orderViewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            orderViewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            _orderHistoryContent = UiFactory.CreateStretchChild(orderViewport, "Content");
            _orderHistoryContent.pivot = new Vector2(0.5f, 1f);
            _orderHistoryContent.anchorMin = new Vector2(0, 1);
            _orderHistoryContent.anchorMax = new Vector2(1, 1);

            var orderVlg = _orderHistoryContent.gameObject.AddComponent<VerticalLayoutGroup>();
            orderVlg.spacing = 8;
            orderVlg.padding = new RectOffset(8, 8, 8, 8);
            orderVlg.childControlWidth = true;
            orderVlg.childControlHeight = true;
            orderVlg.childForceExpandWidth = true;
            orderVlg.childForceExpandHeight = false;
            _orderHistoryContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            orderScroll.viewport = orderViewport;
            orderScroll.content = _orderHistoryContent;

            AddOrderHistoryPlaceholder();
        }

        public void RecordCustomerOrder(CraftCustomerSO customer)
        {
            if (customer == null) return;

            string menuName = customer.requiredMenu != null
                ? customer.requiredMenu.displayName
                : "메뉴 미정";
            string orderLine = string.IsNullOrWhiteSpace(customer.orderLine)
                ? "(주문 대사 없음)"
                : customer.orderLine.Trim();

            RemoveOrderHistoryPlaceholder();

            string clock = DayLoopController.Instance != null
                ? DayLoopController.Instance.FormatClock()
                : "--:--";

            var row = new GameObject("OrderRow", typeof(RectTransform));
            row.transform.SetParent(_orderHistoryContent, false);
            row.transform.SetAsFirstSibling();
            row.AddComponent<LayoutElement>().preferredHeight = 88;
            row.AddComponent<Image>().color = new Color(1f, 0.98f, 0.92f, 0.98f);

            var timeGo = new GameObject("Time", typeof(RectTransform));
            timeGo.transform.SetParent(row.transform, false);
            var timeRt = timeGo.GetComponent<RectTransform>();
            timeRt.anchorMin = new Vector2(0.02f, 0.68f);
            timeRt.anchorMax = new Vector2(0.98f, 0.96f);
            timeRt.offsetMin = Vector2.zero;
            timeRt.offsetMax = Vector2.zero;
            var timeTmp = timeGo.AddComponent<TextMeshProUGUI>();
            timeTmp.text = $"주문 시각  {clock}";
            timeTmp.fontSize = 14;
            timeTmp.color = new Color(0.45f, 0.38f, 0.28f);
            timeTmp.alignment = TextAlignmentOptions.MidlineLeft;
            timeTmp.raycastTarget = false;
            KoreanUiFont.Apply(timeTmp);

            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(row.transform, false);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.02f, 0.38f);
            nameRt.anchorMax = new Vector2(0.98f, 0.70f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = $"{customer.customerName}  ·  {menuName}";
            nameTmp.fontSize = 18;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = new Color(0.2f, 0.14f, 0.08f);
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.raycastTarget = false;
            KoreanUiFont.Apply(nameTmp);

            var orderGo = new GameObject("Order", typeof(RectTransform));
            orderGo.transform.SetParent(row.transform, false);
            var orderRt = orderGo.GetComponent<RectTransform>();
            orderRt.anchorMin = new Vector2(0.02f, 0.04f);
            orderRt.anchorMax = new Vector2(0.98f, 0.38f);
            orderRt.offsetMin = Vector2.zero;
            orderRt.offsetMax = Vector2.zero;
            var orderTmp = orderGo.AddComponent<TextMeshProUGUI>();
            orderTmp.text = $"\"{orderLine}\"";
            orderTmp.fontSize = 16;
            orderTmp.color = new Color(0.28f, 0.22f, 0.16f);
            orderTmp.alignment = TextAlignmentOptions.TopLeft;
            orderTmp.textWrappingMode = TextWrappingModes.Normal;
            orderTmp.overflowMode = TextOverflowModes.Ellipsis;
            orderTmp.raycastTarget = false;
            KoreanUiFont.Apply(orderTmp);

            _orderRows.Insert(0, row);
            while (_orderRows.Count > MaxOrderRows)
            {
                int last = _orderRows.Count - 1;
                var old = _orderRows[last];
                _orderRows.RemoveAt(last);
                if (old != null) Object.Destroy(old);
            }
        }

        public void ClearOrderHistory()
        {
            foreach (var row in _orderRows)
            {
                if (row != null) Object.Destroy(row);
            }
            _orderRows.Clear();

            if (_placeholderRow != null)
                Object.Destroy(_placeholderRow);
            _placeholderRow = null;

            foreach (Transform child in _orderHistoryContent)
                Object.Destroy(child.gameObject);

            AddOrderHistoryPlaceholder();
        }

        private void AddOrderHistoryPlaceholder()
        {
            var row = new GameObject("OrderRow_Placeholder", typeof(RectTransform));
            row.transform.SetParent(_orderHistoryContent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 48;
            _placeholderRow = row;

            var labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(row.transform, false);
            UiFactory.Stretch(labelGo.GetComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "아직 손님 주문이 없습니다.\n손님이 오케이를 누르면 여기에 기록이 쌓입니다.";
            tmp.fontSize = 16;
            tmp.fontStyle = FontStyles.Italic;
            tmp.color = new Color(0.45f, 0.42f, 0.38f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
        }

        private void RemoveOrderHistoryPlaceholder()
        {
            if (_placeholderRow != null)
            {
                Object.Destroy(_placeholderRow);
                _placeholderRow = null;
            }

            foreach (Transform child in _orderHistoryContent)
            {
                if (child.name == "OrderRow_Placeholder")
                    Object.Destroy(child.gameObject);
            }
        }
    }
}
