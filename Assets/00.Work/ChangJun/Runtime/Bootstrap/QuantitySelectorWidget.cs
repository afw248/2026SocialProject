using System;
using ChangJun.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// -10 / -1 / 입력 / +1 / +10 수량 선택 위젯. min~max로 clamp한다.
    /// </summary>
    public sealed class QuantitySelectorWidget
    {
        private readonly TMP_InputField _input;
        private readonly Action<int> _onChanged;
        private readonly int _min;
        private readonly int _max;
        private int _quantity;

        public int Quantity => _quantity;
        public int Min => _min;
        public int Max => _max;

        public QuantitySelectorWidget(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Action<int> onChanged, int initial = 0,
            int min = 0, int max = EconomyClamp.MaxOrderQuantity)
        {
            _onChanged = onChanged;
            _min = Mathf.Min(min, max);
            _max = Mathf.Max(min, max);
            _quantity = Mathf.Clamp(initial, _min, _max);

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
            _quantity = Mathf.Clamp(qty, _min, _max);
            _input.SetTextWithoutNotify(_quantity.ToString());
            if (notify) _onChanged?.Invoke(_quantity);
        }

        private void Adjust(int delta)
        {
            // int 오버플로우 방지: long으로 합산 후 clamp
            long next = (long)_quantity + delta;
            SetQuantity((int)Math.Clamp(next, _min, _max));
        }

        private void CommitInput(string text)
        {
            if (!int.TryParse(text, out int parsed))
                parsed = _min;
            SetQuantity(parsed);
        }

        private TMP_InputField CreateIntegerInput(Transform parent, Vector2 min, Vector2 max)
        {
            var fieldRt = UiFactory.CreatePanel(parent, "Input",
                min, max, Vector2.zero, Vector2.zero);
            fieldRt.gameObject.AddComponent<Image>().color = Color.white;

            var textArea = new GameObject("Text Area", typeof(RectTransform));
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
            input.characterLimit = Mathf.Max(1, _max.ToString().Length);
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
