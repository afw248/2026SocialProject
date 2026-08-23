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
    /// <summary>
    /// 하루의 시작 — Day Loop UI 목업 스타일 전면 화면.
    /// 상단 3단계 표시(아침 준비 · 영업 · 마감) + 큰 아이콘 + 오늘의 배달 브리핑.
    /// </summary>
    public sealed class MorningDeliveryOverlay
    {
        private static readonly Color NightBg = new Color32(0x3A, 0x24, 0x15, 0xFF);
        private static readonly Color CreamText = new Color32(0xF2, 0xE4, 0xC8, 0xFF);
        private static readonly Color DimStep = new Color32(0x8A, 0x62, 0x38, 0xFF);

        private readonly GameObject _root;
        private readonly TextMeshProUGUI _dayText;
        private readonly TextMeshProUGUI _bodyText;
        private readonly RectTransform _bodyContent;
        private readonly ScrollRect _scroll;

        public event Action OnReceived;

        public MorningDeliveryOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("MorningDelivery", 80);
            _root.SetActive(false);

            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = NightBg;

            CreatePhaseSteps(_root.transform);

            var centerRt = UiFactory.CreatePanel(_root.transform, "Center",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-360f, -300f), new Vector2(360f, 260f));

            AnalogClockWidget.Create(centerRt, "Icon",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-90f, -220f), new Vector2(90f, -40f),
                8, 0, true);

            UiFactory.CreateText(centerRt, "Title", "아침 준비",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -300f), new Vector2(0f, -260f),
                TextAlignmentOptions.Center, 34, Color.white);

            _dayText = UiFactory.CreateText(centerRt, "Day", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -336f), new Vector2(0f, -304f),
                TextAlignmentOptions.Center, 16, CreamText);

            // ── 오늘의 배달 브리핑 (테두리 박스, 스크롤) ──
            var infoBoxOuter = UiFactory.CreatePanel(centerRt, "InfoBox",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -560f), new Vector2(0f, -356f));
            infoBoxOuter.gameObject.AddComponent<Image>().color = DimStep;
            var infoBox = UiFactory.CreatePanel(infoBoxOuter, "InfoBox_Fill",
                Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            infoBox.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);

            var scrollRt = UiFactory.CreateStretchChild(infoBox, "Scroll");
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
            _bodyContent.offsetMin = new Vector2(20f, 0f);
            _bodyContent.offsetMax = new Vector2(-20f, 0f);

            var fitter = _bodyContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _bodyText = _bodyContent.gameObject.AddComponent<TextMeshProUGUI>();
            _bodyText.fontSize = 15;
            _bodyText.color = CreamText;
            _bodyText.alignment = TextAlignmentOptions.Top;
            _bodyText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            _bodyText.verticalAlignment = VerticalAlignmentOptions.Top;
            _bodyText.textWrappingMode = TextWrappingModes.Normal;
            _bodyText.overflowMode = TextOverflowModes.Overflow;
            _bodyText.lineSpacing = 4f;
            _bodyText.margin = new Vector4(0f, 12f, 0f, 12f);
            _bodyText.raycastTarget = false;
            KoreanUiFont.Apply(_bodyText);

            _scroll.viewport = viewport;
            _scroll.content = _bodyContent;

            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(_root.transform, "StartBtn",
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-140f, 56f), new Vector2(140f, 132f)),
                "준비 시작하기", UiTheme.Gold, Receive, 18, UiTheme.TextDark);
        }

        private static void CreatePhaseSteps(Transform parent)
        {
            var row = UiFactory.CreatePanel(parent, "PhaseSteps",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-360f, -104f), new Vector2(360f, -40f));
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateStep(row, "아침 준비", 8, 0, true);
            CreateStep(row, "영업", 10, 0, false);
            CreateStep(row, "마감", 21, 0, false);
        }

        private static void CreateStep(Transform parent, string label, int hour, int minute, bool active)
        {
            var wrap = new GameObject($"Step_{label}", typeof(RectTransform));
            wrap.transform.SetParent(parent, false);

            AnalogClockWidget.Create(wrap.transform, "Clock",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-28f, -56f), new Vector2(28f, 0f),
                hour, minute, active);

            UiFactory.CreateText(wrap.transform, "Label", label,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-90f, -88f), new Vector2(90f, -60f),
                TextAlignmentOptions.Center, 14, active ? Color.white : CreamText);
        }

        public void Show()
        {
            DeliveryManager.Instance.RollMorningEvent();
            _dayText.text = DayLoopController.Instance.FormatDayClock();
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
