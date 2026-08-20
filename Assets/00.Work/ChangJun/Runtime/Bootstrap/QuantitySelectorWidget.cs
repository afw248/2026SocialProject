using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// -10 / -1 / 입력 / +1 / +10 수량 선택 위젯.
    /// </summary>
    public sealed class QuantitySelectorWidget
    {
        private readonly TMP_InputField _input;
        private readonly Action<int> _onChanged;
        private int _quantity;

        public int Quantity => _quantity;

        public QuantitySelectorWidget(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Action<int> onChanged, int initial = 0)
        {
            _onChanged = onChanged;
            _quantity = Mathf.Max(0, initial);

            var root = UiFactory.CreatePanel(parent, "QtySelector",
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);

            CreateStepButton(root, "Minus10", new Vector2(0f, 0f), new Vector2(0.18f, 1f),
                "-10", new Color(0.82f, 0.82f, 0.88f), () => Adjust(-10));
            CreateStepButton(root, "Minus1", new Vector2(0.19f, 0f), new Vector2(0.36f, 1f),
                "-1", new Color(0.85f, 0.85f, 0.9f), () => Adjust(-1));

            _input = CreateIntegerInput(root, new Vector2(0.37f, 0f), new Vector2(0.63f, 1f));

            CreateStepButton(root, "Plus1", new Vector2(0.64f, 0f), new Vector2(0.82f, 1f),
                "+1", new Color(0.85f, 0.92f, 0.85f), () => Adjust(1));
            CreateStepButton(root, "Plus10", new Vector2(0.83f, 0f), new Vector2(1f, 1f),
                "+10", new Color(0.78f, 0.9f, 0.78f), () => Adjust(10));

            SetQuantity(_quantity, notify: false);
        }

        public void SetQuantity(int qty, bool notify = true)
        {
            _quantity = Mathf.Max(0, qty);
            _input.SetTextWithoutNotify(_quantity.ToString());
            if (notify) _onChanged?.Invoke(_quantity);
        }

        private void Adjust(int delta) => SetQuantity(_quantity + delta);

        private void CommitInput(string text)
        {
            if (!int.TryParse(text, out int parsed) || parsed < 0)
                parsed = 0;
            SetQuantity(parsed);
        }

        private TMP_InputField CreateIntegerInput(Transform parent, Vector2 min, Vector2 max)
        {
            var fieldRt = UiFactory.CreatePanel(parent, "Input",
                min, max, Vector2.zero, Vector2.zero);
            fieldRt.gameObject.AddComponent<Image>().color = Color.white;

            var textArea = new GameObject("TextArea", typeof(RectTransform));
            textArea.transform.SetParent(fieldRt, false);
            UiFactory.Stretch(textArea.GetComponent<RectTransform>());

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(textArea.transform, false);
            UiFactory.Stretch(textGo.GetComponent<RectTransform>());
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 20;
            text.color = new Color(0.1f, 0.25f, 0.5f);
            text.alignment = TextAlignmentOptions.Center;
            KoreanUiFont.Apply(text);

            var input = fieldRt.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = textArea.GetComponent<RectTransform>();
            input.textComponent = text;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onEndEdit.AddListener(CommitInput);
            return input;
        }

        private static void CreateStepButton(Transform parent, string name,
            Vector2 min, Vector2 max, string label, Color color, Action onClick)
        {
            var rt = UiFactory.CreatePanel(parent, name, min, max, Vector2.zero, Vector2.zero);
            var btn = rt.gameObject.AddComponent<Button>();
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            UiFactory.CreateText(rt, "T", label,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 16, new Color(0.12f, 0.14f, 0.2f));
        }
    }
}
