using System;
using System.Collections.Generic;
using ChangJun.Craft;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Inventory;
using ChangJun.Judge;
using ChangJun.Progression;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 재료 그리드·액션 바·결과 텍스트 HUD.
    /// </summary>
    public sealed class CraftHudPresenter
    {
        private const int GridColumns = 4;

        private readonly CraftController _controller;
        private readonly RectTransform _craftRoot;
        private readonly TextMeshProUGUI _moneyText;
        private readonly TextMeshProUGUI _slotText;
        private readonly TextMeshProUGUI _resultText;
        private readonly Dictionary<string, Button> _ingredientButtons = new();
        private readonly Dictionary<string, Image> _ingredientImages = new();
        private Button _craftButton;
        private Button _clearButton;
        private bool _craftEnabled = true;

        public RectTransform CraftRoot => _craftRoot;
        public GameObject HeaderRoot { get; }

        public event Action OnDeliveryRequested;

        public CraftHudPresenter(RectTransform root, RectTransform craftContent,
            CraftController controller, IReadOnlyList<IngredientSO> ingredients)
        {
            _controller = controller;

            var header = UiFactory.CreatePanel(root, "Header",
                new Vector2(0.26f, 0.66f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);
            HeaderRoot = header.gameObject;
            header.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.92f);

            var toolbar = UiFactory.CreateStretchChild(header, "Toolbar");
            toolbar.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
            var toolbarLayout = toolbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            toolbarLayout.spacing = 12;
            toolbarLayout.childControlWidth = true;
            toolbarLayout.childControlHeight = true;
            toolbarLayout.childForceExpandWidth = true;
            toolbarLayout.childForceExpandHeight = false;

            _moneyText = CreateLayoutText(toolbar, "MoneyText", "자산: 0원",
                TextAlignmentOptions.MidlineLeft, 24, 1.1f);
            _resultText = CreateLayoutText(toolbar, "ResultText", "",
                TextAlignmentOptions.Center, 22, 2f);
            _resultText.enableWordWrapping = false;
            _resultText.overflowMode = TextOverflowModes.Ellipsis;
            _slotText = CreateLayoutText(toolbar, "SlotText", "슬롯: [ ]",
                TextAlignmentOptions.MidlineRight, 22, 0.9f);

            CreateHeaderButton(toolbar, "배달", () => OnDeliveryRequested?.Invoke());

            _craftRoot = UiFactory.CreatePanel(craftContent, "CraftArea",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var scrollPanel = UiFactory.CreatePanel(_craftRoot, "IngredientScroll",
                new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.98f),
                Vector2.zero, Vector2.zero);

            var scroll = scrollPanel.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var viewport = UiFactory.CreateStretchChild(scrollPanel, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            var content = UiFactory.CreateStretchChild(viewport, "Content");
            content.pivot = new Vector2(0.5f, 1f);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200, 64);
            grid.spacing = new Vector2(12, 10);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumns;

            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            foreach (var ing in ingredients)
                CreateIngredientButton(content, ing);

            var actionBar = UiFactory.CreatePanel(_craftRoot, "ActionBar",
                new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.18f),
                Vector2.zero, Vector2.zero);
            var layout = actionBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _clearButton = CreateActionButton(actionBar, "초기화", () => _controller.ClearSelection());
            _craftButton = CreateActionButton(actionBar, "제작!", () => _controller.Craft());

            UnderstandingManager.Instance.OnIngredientUnlocked += OnIngredientUnlocked;
            InventoryManager.Instance.OnStockChanged += RefreshIngredientStates;
            RefreshIngredientStates();
        }

        public void BindMoney(MoneyManager money)
        {
            money.OnMoneyChanged += v => _moneyText.text = $"자산: {v:N0}원";
            _moneyText.text = $"자산: {money.Money:N0}원";
        }

        public void UpdateSlot(IReadOnlyList<IngredientSO> list)
        {
            var sb = new System.Text.StringBuilder("슬롯: [ ");
            foreach (var ing in list) sb.Append(ing.code).Append(' ');
            sb.Append(']');
            _slotText.text = sb.ToString();
        }

        public void ShowResult(CraftResult result, MenuRecipeSO matched)
        {
            _resultText.text = result switch
            {
                CraftResult.Success => $"[성공] {matched.displayName}  (+{matched.price}원)",
                CraftResult.TabooViolation => "[금기위반]  (-100원)",
                CraftResult.WrongOrder => "[오주문]  손님 주문과 다름",
                CraftResult.WrongRecipe => "[오조리]  메뉴 없음",
                _ => "",
            };

            _resultText.color = result switch
            {
                CraftResult.Success => new Color(0.4f, 1f, 0.5f),
                CraftResult.TabooViolation => new Color(1f, 0.4f, 0.4f),
                CraftResult.WrongOrder => new Color(1f, 0.6f, 0.3f),
                CraftResult.WrongRecipe => new Color(1f, 0.9f, 0.3f),
                _ => Color.white,
            };

            _resultText.transform.DOKill();
            _resultText.transform.localScale = Vector3.one;
            _resultText.transform.DOPunchScale(Vector3.one * 0.06f, 0.22f, 3, 0.5f);
        }

        public void ShowDeliveryArrived(string ingredientName, int quantity)
        {
            _resultText.text = $"[배달도착] {ingredientName} x{quantity}";
            _resultText.color = new Color(0.55f, 0.85f, 1f);
            _resultText.transform.DOKill();
            _resultText.transform.DOPunchScale(Vector3.one * 0.06f, 0.22f, 3, 0.5f);
        }

        public void SetInteractable(bool enabled)
        {
            _craftEnabled = enabled;
            if (_craftButton != null) _craftButton.interactable = enabled;
            if (_clearButton != null) _clearButton.interactable = enabled;
            RefreshIngredientStates();
        }

        private void RefreshIngredientStates()
        {
            foreach (var pair in _ingredientButtons)
            {
                bool unlocked = UnderstandingManager.Instance.IsUnlocked(pair.Key);
                bool hasStock = InventoryManager.Instance.GetStock(pair.Key) > 0;
                bool active = _craftEnabled && unlocked && hasStock;
                pair.Value.interactable = active;
                if (_ingredientImages.TryGetValue(pair.Key, out var img))
                    img.color = active
                        ? new Color(0.18f, 0.38f, 0.58f)
                        : new Color(0.25f, 0.25f, 0.28f, 0.6f);
            }
        }

        private void OnIngredientUnlocked(IngredientSO ing)
        {
            RefreshIngredientStates();
            _resultText.text = $"[해금] {ing.displayName}";
            _resultText.color = new Color(0.5f, 0.85f, 1f);
            _resultText.transform.DOKill();
            _resultText.transform.DOPunchScale(Vector3.one * 0.06f, 0.2f, 2, 0.5f);
        }

        private void CreateIngredientButton(Transform parent, IngredientSO ing)
        {
            var go = new GameObject($"Btn_{ing.code}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.38f, 0.58f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            _ingredientButtons[ing.code] = btn;
            _ingredientImages[ing.code] = img;

            var labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            UiFactory.Stretch(labelGo.GetComponent<RectTransform>());

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{ing.displayName}\n({ing.code}) x{InventoryManager.Instance.GetStock(ing.code)}";
            tmp.fontSize = 16;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);

            var captured = ing;
            var capturedGo = go;
            btn.onClick.AddListener(() =>
            {
                _controller.SelectIngredient(captured);
                capturedGo.transform.DOPunchScale(Vector3.one * 0.08f, 0.15f, 4, 0.4f);
            });

            InventoryManager.Instance.OnStockChanged += () =>
            {
                tmp.text = $"{captured.displayName}\n({captured.code}) x{InventoryManager.Instance.GetStock(captured.code)}";
            };
        }

        private static TextMeshProUGUI CreateLayoutText(Transform parent, string name, string text,
            TextAlignmentOptions align, int fontSize, float flex)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            UiFactory.Stretch(go.GetComponent<RectTransform>());
            go.AddComponent<LayoutElement>().flexibleWidth = flex;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
            return tmp;
        }

        private static void CreateHeaderButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 58f;
            le.minWidth = 58f;
            le.preferredHeight = 36f;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.32f, 0.55f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            UiFactory.Stretch(labelGo.GetComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 17;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
        }

        private static Button CreateActionButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = 64;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.48f, 0.18f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());
            var labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            UiFactory.Stretch(labelGo.GetComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 26;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            KoreanUiFont.Apply(tmp);
            return btn;
        }
    }
}
