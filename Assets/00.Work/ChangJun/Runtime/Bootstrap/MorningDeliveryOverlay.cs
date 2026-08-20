using System;
using System.Text;
using ChangJun.Data;
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
        private readonly RectTransform _bodyContent;
        private readonly ScrollRect _scroll;

        public event Action OnReceived;

        public MorningDeliveryOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("MorningDelivery", 80);
            _root.SetActive(false);

            ReceiptUiHelper.CreateDim(_root.transform, 0.68f);

            var panel = ReceiptUiHelper.CreatePaperPanel(_root.transform, "Panel",
                new Vector2(0.28f, 0.16f), new Vector2(0.72f, 0.84f));

            ReceiptUiHelper.CreateReceiptHeader(panel, "아침",
                "Cup Rice · Morning Brief",
                new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.97f));

            ReceiptUiHelper.CreateDashedRule(panel,
                new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.87f));

            var scrollRt = UiFactory.CreatePanel(panel, "BodyScroll",
                new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.82f),
                Vector2.zero, Vector2.zero);
            _scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            UiFactory.ConfigureScroll(_scroll);

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.001f);
            vpImg.raycastTarget = true;

            _bodyContent = UiFactory.CreateStretchChild(viewport, "Content");
            _bodyContent.pivot = new Vector2(0.5f, 1f);
            _bodyContent.anchorMin = new Vector2(0f, 1f);
            _bodyContent.anchorMax = new Vector2(1f, 1f);
            _bodyContent.offsetMin = Vector2.zero;
            _bodyContent.offsetMax = Vector2.zero;

            var fitter = _bodyContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _bodyText = _bodyContent.gameObject.AddComponent<TextMeshProUGUI>();
            _bodyText.fontSize = 20;
            _bodyText.color = ReceiptUiHelper.InkColor;
            _bodyText.alignment = TextAlignmentOptions.Top;
            _bodyText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            _bodyText.verticalAlignment = VerticalAlignmentOptions.Top;
            _bodyText.textWrappingMode = TextWrappingModes.Normal;
            _bodyText.overflowMode = TextOverflowModes.Overflow;
            _bodyText.lineSpacing = 6f;
            _bodyText.raycastTarget = false;
            KoreanUiFont.Apply(_bodyText);

            _scroll.viewport = viewport;
            _scroll.content = _bodyContent;

            ReceiptUiHelper.CreateDashedRule(panel,
                new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.19f));

            ReceiptUiHelper.CreatePaperButton(panel, "하루 시작",
                new Vector2(0.2f, 0.04f), new Vector2(0.8f, 0.14f),
                Receive, new Color(0.55f, 0.42f, 0.28f));
        }

        public void Show()
        {
            DeliveryManager.Instance.RollMorningEvent();
            _bodyText.text = BuildMorningMessage();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bodyContent);
            _scroll.verticalNormalizedPosition = 1f;
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private static string BuildMorningMessage()
        {
            int day = DayLoopController.Instance.Day;
            bool hasDelivery = HasPendingDelivery();
            var evt = DeliveryManager.Instance.LastEvent;
            var freshness = DeliveryManager.Instance.Freshness;

            var sb = new StringBuilder();
            sb.AppendLine($"『 {day}일차 아침 』");
            sb.AppendLine();
            sb.AppendLine("창밖의 공기가 아직 차갑습니다.");
            sb.AppendLine("컵밥 가게의 문을 열고,");
            sb.AppendLine("오늘의 손님을 맞을 준비를 합니다.");
            sb.AppendLine();
            sb.AppendLine("재료를 확인하고, 레시피를 떠올리며");
            sb.AppendLine("차분히 하루를 시작해 보세요.");

            if (!hasDelivery)
            {
                sb.AppendLine();
                sb.AppendLine("- - - - - - - - -");
                sb.AppendLine("오늘은 새벽에 도착한 배달이 없습니다.");
                sb.AppendLine("창고 재고로 영업을 이어가 주세요.");
                return sb.ToString().TrimEnd();
            }

            sb.AppendLine();
            sb.AppendLine("- - - - - - - - -");
            sb.AppendLine("【 새벽 배달 도착 】");
            sb.AppendLine($"신선도  {freshness}%");
            sb.AppendLine();

            foreach (var ing in InventoryManager.Instance.GetAllIngredients())
            {
                int warehouse = InventoryManager.Instance.GetWarehouse(ing.code);
                if (warehouse <= 0) continue;
                sb.AppendLine($"{ing.displayName}  ×{warehouse}");
            }

            if (evt != null && evt.eventType != ChangJun.Data.DeliveryEventType.None)
            {
                sb.AppendLine();
                sb.AppendLine("- - - - - - - - -");
                sb.AppendLine($"※ {evt.headline}");
                if (!string.IsNullOrWhiteSpace(evt.body))
                    sb.AppendLine(evt.body);
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("배달은 무사히 도착했습니다.");
                sb.AppendLine("창고에 넣어 두고 영업을 시작하세요.");
            }

            return sb.ToString().TrimEnd();
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
