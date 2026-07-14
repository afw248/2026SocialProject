using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 탭 콘텐츠 영역용 메모장 — 항목 편집·삭제 지원.
    /// </summary>
    public sealed class MemoPadPanel
    {
        private const string PrefKey = "CupRice_MemoItems";

        private readonly GameObject _root;
        private readonly RectTransform _listContent;
        private readonly List<MemoItem> _items = new();

        public GameObject Root => _root;

        public MemoPadPanel(RectTransform parent)
        {
            _root = UiFactory.CreatePanel(parent, "MemoPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            _root.AddComponent<Image>().color = new Color(0.98f, 0.98f, 0.95f, 0.98f);

            UiFactory.CreateText(_root.transform, "Title", "MEMO",
                new Vector2(0.05f, 0.92f), new Vector2(0.65f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 26,
                new Color(0.1f, 0.1f, 0.1f));

            var addRt = UiFactory.CreatePanel(_root.transform, "AddBtn",
                new Vector2(0.68f, 0.93f), new Vector2(0.95f, 0.99f),
                Vector2.zero, Vector2.zero);
            var addBtn = addRt.gameObject.AddComponent<Button>();
            addBtn.targetGraphic = addRt.gameObject.AddComponent<Image>();
            addBtn.targetGraphic.color = new Color(0.75f, 0.85f, 0.75f);
            addBtn.onClick.AddListener(() => AddItem("새 항목", focus: true));
            UiFactory.CreateText(addRt, "T", "+ 추가", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, 20,
                new Color(0.1f, 0.2f, 0.1f));

            var scrollRt = UiFactory.CreatePanel(_root.transform, "Scroll",
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.9f),
                Vector2.zero, Vector2.zero);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.02f);

            _listContent = UiFactory.CreateStretchChild(viewport, "Content");
            _listContent.pivot = new Vector2(0.5f, 1f);
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);

            var vlg = _listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            _listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _listContent;

            Load();
            if (_items.Count == 0)
                AddItem("배달 체크하기", focus: false);
        }

        private void AddItem(string text, bool focus)
        {
            var item = new MemoItem(_listContent, text, Save, RemoveItem);
            _items.Add(item);
            Save();
            if (focus) item.FocusAndSelectAll();
        }

        private void RemoveItem(MemoItem item)
        {
            _items.Remove(item);
            item.Destroy();
            Save();
        }

        private void Save()
        {
            var data = new MemoSaveData { items = new List<MemoSaveItem>() };
            foreach (var item in _items)
                data.items.Add(new MemoSaveItem { text = item.Text, done = item.IsDone });
            PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(PrefKey)) return;
            var data = JsonUtility.FromJson<MemoSaveData>(PlayerPrefs.GetString(PrefKey));
            if (data?.items == null) return;
            foreach (var item in data.items)
                AddItemLoaded(item.text, item.done);
        }

        private void AddItemLoaded(string text, bool done)
        {
            var item = new MemoItem(_listContent, text, Save, RemoveItem);
            item.SetDone(done);
            _items.Add(item);
        }

        [Serializable]
        private class MemoSaveData
        {
            public List<MemoSaveItem> items;
        }

        [Serializable]
        private class MemoSaveItem
        {
            public string text;
            public bool done;
        }

        private sealed class MemoItem
        {
            private readonly GameObject _row;
            private readonly Toggle _toggle;
            private readonly TMP_InputField _input;
            private readonly Action<MemoItem> _onRemove;

            public string Text => _input.text;
            public bool IsDone => _toggle.isOn;

            public MemoItem(RectTransform parent, string text, Action onChanged,
                Action<MemoItem> onRemove)
            {
                _onRemove = onRemove;
                _row = UiFactory.CreateStretchChild(parent, "MemoRow").gameObject;
                _row.AddComponent<LayoutElement>().preferredHeight = 48;

                var hlg = _row.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 6;
                hlg.padding = new RectOffset(2, 2, 2, 2);
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;

                var toggleGo = new GameObject("Toggle", typeof(RectTransform));
                toggleGo.transform.SetParent(_row.transform, false);
                toggleGo.AddComponent<LayoutElement>().preferredWidth = 32;
                _toggle = toggleGo.AddComponent<Toggle>();
                var toggleBg = toggleGo.AddComponent<Image>();
                toggleBg.color = Color.white;
                _toggle.targetGraphic = toggleBg;
                var check = new GameObject("Check", typeof(RectTransform));
                check.transform.SetParent(toggleGo.transform, false);
                UiFactory.Stretch(check.GetComponent<RectTransform>());
                var checkImg = check.AddComponent<Image>();
                checkImg.color = new Color(0.2f, 0.6f, 0.3f);
                _toggle.graphic = checkImg;
                _toggle.onValueChanged.AddListener(_ => onChanged());

                _input = CreateInputField(_row.transform, text, onChanged);

                var delGo = new GameObject("Delete", typeof(RectTransform));
                delGo.transform.SetParent(_row.transform, false);
                delGo.AddComponent<LayoutElement>().preferredWidth = 36;
                var delImg = delGo.AddComponent<Image>();
                delImg.color = new Color(0.85f, 0.35f, 0.35f);
                var delBtn = delGo.AddComponent<Button>();
                delBtn.targetGraphic = delImg;
                delBtn.onClick.AddListener(() => _onRemove(this));
                var delLabelGo = new GameObject("Label", typeof(RectTransform));
                delLabelGo.transform.SetParent(delGo.transform, false);
                UiFactory.Stretch(delLabelGo.GetComponent<RectTransform>());
                var delLabel = delLabelGo.AddComponent<TextMeshProUGUI>();
                delLabel.text = "×";
                delLabel.fontSize = 24;
                delLabel.color = Color.white;
                delLabel.alignment = TextAlignmentOptions.Center;
                delLabel.raycastTarget = false;
                KoreanUiFont.Apply(delLabel);
            }

            public void SetDone(bool done) => _toggle.isOn = done;

            public void FocusAndSelectAll()
            {
                _input.ActivateInputField();
                _input.MoveTextEnd(false);
            }

            public void Destroy() => UnityEngine.Object.Destroy(_row);

            private static TMP_InputField CreateInputField(Transform parent, string text,
                Action onChanged)
            {
                var root = new GameObject("InputField", typeof(RectTransform));
                root.transform.SetParent(parent, false);
                root.AddComponent<LayoutElement>().flexibleWidth = 1f;

                var bg = root.AddComponent<Image>();
                bg.color = new Color(1f, 1f, 1f, 0.85f);

                var input = root.AddComponent<TMP_InputField>();

                var textArea = new GameObject("TextArea", typeof(RectTransform));
                textArea.transform.SetParent(root.transform, false);
                var textAreaRt = textArea.GetComponent<RectTransform>();
                UiFactory.Stretch(textAreaRt);
                textAreaRt.offsetMin = new Vector2(8, 4);
                textAreaRt.offsetMax = new Vector2(-8, -4);

                var textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(textArea.transform, false);
                UiFactory.Stretch(textGo.GetComponent<RectTransform>());
                var textComp = textGo.AddComponent<TextMeshProUGUI>();
                textComp.fontSize = 18;
                textComp.color = Color.black;
                textComp.alignment = TextAlignmentOptions.MidlineLeft;
                KoreanUiFont.Apply(textComp);

                var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
                placeholderGo.transform.SetParent(textArea.transform, false);
                UiFactory.Stretch(placeholderGo.GetComponent<RectTransform>());
                var placeholder = placeholderGo.AddComponent<TextMeshProUGUI>();
                placeholder.text = "내용 입력…";
                placeholder.fontSize = 18;
                placeholder.fontStyle = FontStyles.Italic;
                placeholder.color = new Color(0.4f, 0.4f, 0.4f);
                KoreanUiFont.Apply(placeholder);

                input.textViewport = textAreaRt;
                input.textComponent = textComp;
                input.placeholder = placeholder;
                input.lineType = TMP_InputField.LineType.SingleLine;
                input.text = text;
                input.onValueChanged.AddListener(_ => onChanged());
                input.onEndEdit.AddListener(_ => onChanged());

                return input;
            }
        }
    }
}
