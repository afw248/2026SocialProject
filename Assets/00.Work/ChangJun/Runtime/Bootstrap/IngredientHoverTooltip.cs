using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 재료 버튼 호버 시 음식 이름을 표시하는 작은 툴팁.
    /// </summary>
    public sealed class IngredientHoverTooltip : MonoBehaviour
    {
        private static IngredientHoverTooltip _instance;
        private RectTransform _panel;
        private TextMeshProUGUI _label;
        private Canvas _canvas;
        private RectTransform _canvasRt;
        private bool _visible;

        public static void Ensure(Transform canvasRoot)
        {
            if (_instance != null) return;

            var go = new GameObject("IngredientHoverTooltip", typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            _instance = go.AddComponent<IngredientHoverTooltip>();
            _instance.Build();
        }

        public static void Show(string text, Vector2 screenPos)
        {
            if (_instance == null) return;
            _instance.ShowInternal(text, screenPos);
        }

        public static void Hide()
        {
            if (_instance == null) return;
            _instance.HideInternal();
        }

        private void Build()
        {
            _canvas = GetComponentInParent<Canvas>();
            _canvasRt = _canvas != null ? _canvas.transform as RectTransform : transform as RectTransform;

            _panel = gameObject.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0f);
            _panel.sizeDelta = new Vector2(160, 40);

            var img = gameObject.AddComponent<Image>();
            img.color = new Color(0.08f, 0.09f, 0.12f, 0.94f);
            img.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(transform, false);
            UiFactory.Stretch(labelGo.GetComponent<RectTransform>());
            _label = labelGo.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 18;
            _label.color = Color.white;
            _label.alignment = TextAlignmentOptions.Center;
            _label.raycastTarget = false;
            KoreanUiFont.Apply(_label);

            gameObject.SetActive(false);
        }

        private void ShowInternal(string text, Vector2 screenPos)
        {
            _label.text = text;
            _visible = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Follow(screenPos);
        }

        private void HideInternal()
        {
            _visible = false;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_visible) return;
            var mouse = Mouse.current;
            if (mouse == null) return;
            Follow(mouse.position.ReadValue());
        }

        private void Follow(Vector2 screenPos)
        {
            if (_canvasRt == null) return;

            Camera cam = null;
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = _canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRt, screenPos, cam, out var local);
            _panel.anchoredPosition = local + new Vector2(0f, 28f);
        }
    }

    /// <summary>
    /// 재료 버튼에 붙여 호버 툴팁을 띄운다.
    /// </summary>
    public sealed class IngredientHoverTrigger : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        private string _label;

        public void Setup(string label) => _label = label;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_label)) return;
            IngredientHoverTooltip.Show(_label, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IngredientHoverTooltip.Hide();
        }

        private void OnDisable() => IngredientHoverTooltip.Hide();
    }
}
