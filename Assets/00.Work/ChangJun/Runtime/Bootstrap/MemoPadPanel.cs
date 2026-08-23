using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 메모장 — 독립 풀스크린 화면. 손님 주문 기록을 보여준다.
    /// </summary>
    public sealed class MemoPadPanel
    {
        private const int MaxOrderRows = 40;

        private readonly GameObject _root;
        private readonly RectTransform _orderHistoryContent;
        private readonly List<GameObject> _orderRows = new();
        private GameObject _placeholderRow;
        private readonly UiTheme.HeaderMeta _headerMeta;

        public event Action OnBack;

        public MemoPadPanel()
        {
            _root = UiFactory.CreateOverlayRoot("MemoOverlay", 60);
            _root.SetActive(false);
            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = UiTheme.Background;

            var header = UiTheme.CreateHeaderBar(_root.transform, "메모장", 72f, 78f);
            UiTheme.CreateBackButton(header, () => OnBack?.Invoke());
            _headerMeta = UiTheme.CreateHeaderMeta(header);

            var body = UiTheme.CreateScreenBody(_root.transform, 72f, 24f);

            UiFactory.CreateText(body, "OrderTitle", "오늘 손님들이 시킨 주문",
                new Vector2(0.02f, 0.94f), new Vector2(0.98f, 1f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 18, UiTheme.TextMuted);

            var orderScrollRt = UiTheme.CreateBorderedPanel(body, "OrderScroll",
                new Vector2(0f, 0f), new Vector2(1f, 0.92f),
                Vector2.zero, Vector2.zero, UiTheme.TanRow, 3f);

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

        public void Show()
        {
            UiTheme.RefreshHeaderMeta(_headerMeta);
            _root.SetActive(true);
        }
        public void Hide() => _root.SetActive(false);

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

            var rowWrap = new GameObject("OrderRow", typeof(RectTransform));
            rowWrap.transform.SetParent(_orderHistoryContent, false);
            rowWrap.transform.SetAsFirstSibling();
            rowWrap.AddComponent<LayoutElement>().preferredHeight = 92;

            var row = UiTheme.CreateShadowCard(rowWrap.transform, "Card",
                Vector2.zero, Vector2.one, new Vector2(0f, 4f), new Vector2(-4f, 0f),
                UiTheme.CardWhite, 3f, 4f).gameObject;

            var timeGo = new GameObject("Time", typeof(RectTransform));
            timeGo.transform.SetParent(row.transform, false);
            var timeRt = timeGo.GetComponent<RectTransform>();
            timeRt.anchorMin = new Vector2(0.04f, 0.66f);
            timeRt.anchorMax = new Vector2(0.96f, 0.94f);
            timeRt.offsetMin = Vector2.zero;
            timeRt.offsetMax = Vector2.zero;
            var timeTmp = timeGo.AddComponent<TextMeshProUGUI>();
            timeTmp.text = $"주문 시각  {clock}";
            timeTmp.fontSize = 13;
            timeTmp.color = UiTheme.TextMuted;
            timeTmp.alignment = TextAlignmentOptions.MidlineLeft;
            timeTmp.raycastTarget = false;
            KoreanUiFont.Apply(timeTmp);

            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(row.transform, false);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.04f, 0.36f);
            nameRt.anchorMax = new Vector2(0.96f, 0.68f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = $"{customer.customerName}  ·  {menuName}";
            nameTmp.fontSize = 18;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = UiTheme.TextDark;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.raycastTarget = false;
            KoreanUiFont.Apply(nameTmp);

            var orderGo = new GameObject("Order", typeof(RectTransform));
            orderGo.transform.SetParent(row.transform, false);
            var orderRt = orderGo.GetComponent<RectTransform>();
            orderRt.anchorMin = new Vector2(0.04f, 0.04f);
            orderRt.anchorMax = new Vector2(0.96f, 0.36f);
            orderRt.offsetMin = Vector2.zero;
            orderRt.offsetMax = Vector2.zero;
            var orderTmp = orderGo.AddComponent<TextMeshProUGUI>();
            orderTmp.text = $"\"{orderLine}\"";
            orderTmp.fontSize = 16;
            orderTmp.color = UiTheme.TextFaint;
            orderTmp.alignment = TextAlignmentOptions.TopLeft;
            orderTmp.textWrappingMode = TextWrappingModes.Normal;
            orderTmp.overflowMode = TextOverflowModes.Ellipsis;
            orderTmp.raycastTarget = false;
            KoreanUiFont.Apply(orderTmp);

            _orderRows.Insert(0, rowWrap);
            while (_orderRows.Count > MaxOrderRows)
            {
                int last = _orderRows.Count - 1;
                var old = _orderRows[last];
                _orderRows.RemoveAt(last);
                if (old != null) UnityEngine.Object.Destroy(old);
            }
        }

        public void ClearOrderHistory()
        {
            foreach (var row in _orderRows)
            {
                if (row != null) UnityEngine.Object.Destroy(row);
            }
            _orderRows.Clear();

            if (_placeholderRow != null)
                UnityEngine.Object.Destroy(_placeholderRow);
            _placeholderRow = null;

            foreach (Transform child in _orderHistoryContent)
                UnityEngine.Object.Destroy(child.gameObject);

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
            tmp.color = UiTheme.TextFaint;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
        }

        private void RemoveOrderHistoryPlaceholder()
        {
            if (_placeholderRow != null)
            {
                UnityEngine.Object.Destroy(_placeholderRow);
                _placeholderRow = null;
            }

            foreach (Transform child in _orderHistoryContent)
            {
                if (child.name == "OrderRow_Placeholder")
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}
