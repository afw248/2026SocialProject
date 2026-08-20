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
    /// 재료 그리드·밥그릇 프리뷰·액션 바·결과 텍스트 HUD.
    /// </summary>
    public sealed class CraftHudPresenter
    {
        private const int GridColumns = 5;
        private const float ToppingRadius = 42f;
        private const float ToppingSize = 72f;

        private readonly CraftController _controller;
        private readonly RectTransform _craftRoot;
        private readonly RectTransform _toppingLayer;
        private readonly TextMeshProUGUI _moneyText;
        private readonly TextMeshProUGUI _resultText;
        private readonly Dictionary<string, Button> _ingredientButtons = new();
        private readonly Dictionary<string, Image> _ingredientImages = new();
        private readonly List<Image> _toppingImages = new();
        private Button _craftButton;
        private Button _clearButton;
        private bool _craftEnabled = true;

        public RectTransform CraftRoot => _craftRoot;
        public GameObject HeaderRoot { get; }

        public CraftHudPresenter(RectTransform root, RectTransform craftContent,
            CraftController controller, IReadOnlyList<IngredientSO> ingredients)
        {
            _controller = controller;
            IngredientVisualCatalog.EnsureLoaded();
            IngredientHoverTooltip.Ensure(root);

            var header = UiFactory.CreatePanel(root, "Header",
                new Vector2(0.26f, 0.68f), new Vector2(0.88f, 0.92f), Vector2.zero, Vector2.zero);
            HeaderRoot = header.gameObject;
            header.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.09f, 0.13f, 0.94f);

            var toolbar = UiFactory.CreateStretchChild(header, "Toolbar");
            var toolbarLayout = toolbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            toolbarLayout.spacing = 16;
            toolbarLayout.padding = new RectOffset(18, 18, 10, 10);
            toolbarLayout.childControlWidth = true;
            toolbarLayout.childControlHeight = true;
            toolbarLayout.childForceExpandWidth = true;
            toolbarLayout.childForceExpandHeight = true;
            toolbarLayout.childAlignment = TextAnchor.MiddleCenter;

            _moneyText = CreateLayoutText(toolbar, "MoneyText", "자산: 0원",
                TextAlignmentOptions.MidlineLeft, 26, 0.85f);
            _resultText = CreateLayoutText(toolbar, "ResultText", "",
                TextAlignmentOptions.Center, 30, 2.2f);
            _resultText.fontStyle = FontStyles.Bold;
            _resultText.textWrappingMode = TextWrappingModes.NoWrap;
            _resultText.overflowMode = TextOverflowModes.Ellipsis;

            _craftRoot = UiFactory.CreatePanel(craftContent, "CraftArea",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // 다른 탭이 비쳐 보이지 않도록 불투명 배경
            _craftRoot.gameObject.AddComponent<Image>().color =
                new Color(0.07f, 0.08f, 0.11f, 1f);

            // 위: 가로 도마 배경 + 재료 그리드 (픽셀 비율 유지)
            var scrollPanel = UiFactory.CreatePanel(_craftRoot, "IngredientScroll",
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.99f),
                Vector2.zero, Vector2.zero);

            // 재료 영역 어두운 베이스
            scrollPanel.gameObject.AddComponent<Image>().color =
                new Color(0.04f, 0.03f, 0.02f, 0.92f);

            var boardGo = new GameObject("CuttingBoard", typeof(RectTransform));
            boardGo.transform.SetParent(scrollPanel, false);
            var boardRt = boardGo.GetComponent<RectTransform>();
            boardRt.anchorMin = Vector2.zero;
            boardRt.anchorMax = Vector2.one;
            boardRt.offsetMin = new Vector2(6f, 6f);
            boardRt.offsetMax = new Vector2(-6f, -6f);
            var boardImg = boardGo.AddComponent<Image>();
            var boardSprites = Resources.LoadAll<Sprite>("Craft/Sprites/CuttingBoard");
            boardImg.sprite = boardSprites != null && boardSprites.Length > 0
                ? boardSprites[0]
                : Resources.Load<Sprite>("Craft/Sprites/CuttingBoard");
            boardImg.preserveAspect = true;
            boardImg.raycastTarget = false;
            boardImg.color = boardImg.sprite != null
                ? Color.white
                : new Color(0.72f, 0.58f, 0.38f, 0.95f);

            // 스프라이트가 이미 재료칸 비율(~2.4:1)이라 FitInParent로도 픽셀이 안 늘어남
            float boardAspect = 2.4f;
            if (boardImg.sprite != null)
            {
                var rect = boardImg.sprite.rect;
                if (rect.height > 0.01f)
                    boardAspect = rect.width / rect.height;
            }
            var boardFitter = boardGo.AddComponent<AspectRatioFitter>();
            boardFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            boardFitter.aspectRatio = boardAspect;

            var scroll = scrollPanel.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            UiFactory.ConfigureScroll(scroll);

            var viewport = UiFactory.CreateStretchChild(scrollPanel, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            var content = UiFactory.CreateStretchChild(viewport, "Content");
            content.pivot = new Vector2(0.5f, 1f);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            // 5칸이 도마 안쪽에 들어가도록 셀/패딩 조정
            grid.cellSize = new Vector2(92, 92);
            grid.spacing = new Vector2(8, 8);
            grid.padding = new RectOffset(28, 28, 18, 18);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumns;
            grid.childAlignment = TextAnchor.UpperCenter;

            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            foreach (var ing in ingredients)
                CreateIngredientButton(content, ing);

            // 가운데: Rice + 토핑 프리뷰 (도마는 재료 영역에만)
            var bowlArea = UiFactory.CreatePanel(_craftRoot, "BowlArea",
                new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.50f),
                Vector2.zero, Vector2.zero);

            var riceGo = new GameObject("Rice", typeof(RectTransform));
            riceGo.transform.SetParent(bowlArea, false);
            var riceRt = riceGo.GetComponent<RectTransform>();
            riceRt.anchorMin = new Vector2(0.5f, 0.5f);
            riceRt.anchorMax = new Vector2(0.5f, 0.5f);
            riceRt.sizeDelta = new Vector2(220, 220);
            riceRt.anchoredPosition = Vector2.zero;
            var riceImg = riceGo.AddComponent<Image>();
            riceImg.sprite = IngredientVisualCatalog.Rice;
            riceImg.preserveAspect = true;
            riceImg.raycastTarget = false;
            if (riceImg.sprite == null)
                riceImg.color = new Color(0.95f, 0.92f, 0.85f, 0.9f);

            _toppingLayer = UiFactory.CreateStretchChild(bowlArea, "ToppingLayer");

            // 아래: 초기화 / 제작!
            var actionBar = UiFactory.CreatePanel(_craftRoot, "ActionBar",
                new Vector2(0.12f, 0.01f), new Vector2(0.88f, 0.14f),
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
            RefreshBowlToppings(list);
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
            _resultText.transform.DOPunchScale(Vector3.one * 0.08f, 0.25f, 4, 0.5f);
        }

        public void ShowDeliveryArrived(string ingredientName, int quantity)
        {
            _resultText.text = $"[배달도착] {ingredientName} x{quantity}";
            _resultText.color = new Color(0.55f, 0.85f, 1f);
            _resultText.transform.DOKill();
            _resultText.transform.localScale = Vector3.one;
            _resultText.transform.DOPunchScale(Vector3.one * 0.08f, 0.25f, 4, 0.5f);
        }

        public void SetInteractable(bool enabled)
        {
            _craftEnabled = enabled;
            if (_craftButton != null) _craftButton.interactable = enabled;
            if (_clearButton != null) _clearButton.interactable = enabled;
            RefreshIngredientStates();
        }

        private void RefreshBowlToppings(IReadOnlyList<IngredientSO> list)
        {
            foreach (var img in _toppingImages)
            {
                if (img != null)
                    UnityEngine.Object.Destroy(img.gameObject);
            }
            _toppingImages.Clear();

            int count = list.Count;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var ing = list[i];
                var sprite = IngredientVisualCatalog.GetToppingIcon(ing.code);
                if (sprite == null)
                    sprite = IngredientVisualCatalog.GetButtonIcon(ing.code);

                var go = new GameObject($"Topping_{ing.code}", typeof(RectTransform));
                go.transform.SetParent(_toppingLayer, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(ToppingSize, ToppingSize);
                rt.anchoredPosition = FanPosition(i, count);
                // 나중에 올린 토핑이 위에 보이도록 sibling 순서 = 선택 순서
                go.transform.SetAsLastSibling();

                var img = go.AddComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                if (sprite == null)
                    img.color = new Color(0.9f, 0.75f, 0.4f, 0.85f);

                img.transform.localScale = Vector3.zero;
                img.transform.DOScale(1f, 0.18f).SetEase(Ease.OutBack);
                _toppingImages.Add(img);
            }
        }

        /// <summary>
        /// 선택 개수에 따라 밥 위 원형/부채꼴로 살짝 어긋나게 배치.
        /// </summary>
        private static Vector2 FanPosition(int index, int count)
        {
            if (count == 1)
                return new Vector2(0f, 8f);

            // 위쪽을 중심으로 한 부채꼴 (약 -70° ~ +70°)
            float span = Mathf.Min(140f, 40f * (count - 1));
            float start = -span * 0.5f;
            float step = count == 1 ? 0f : span / (count - 1);
            float deg = start + step * index;
            float rad = deg * Mathf.Deg2Rad;
            float radius = ToppingRadius + (count > 2 ? 6f : 0f);
            return new Vector2(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius * 0.55f + 6f);
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
                {
                    bool hasIcon = IngredientVisualCatalog.GetButtonIcon(pair.Key) != null;
                    if (hasIcon)
                    {
                        // 보유 재료는 완전 불투명, 없는 재료만 살짝 어둡게
                        img.color = active
                            ? Color.white
                            : new Color(0.55f, 0.55f, 0.55f, 0.55f);
                    }
                    else
                    {
                        img.color = active
                            ? new Color(0.18f, 0.38f, 0.58f, 1f)
                            : new Color(0.25f, 0.25f, 0.28f, 0.7f);
                    }
                }
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

            // 도마 위에서도 아이콘이 선명하도록 어두운 받침
            var plate = go.AddComponent<Image>();
            plate.color = new Color(0.08f, 0.07f, 0.05f, 0.82f);

            var icon = IngredientVisualCatalog.GetButtonIcon(ing.code);
            Image img;
            TextMeshProUGUI tmp;

            if (icon != null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(go.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.08f, 0.22f);
                iconRt.anchorMax = new Vector2(0.92f, 0.96f);
                iconRt.offsetMin = Vector2.zero;
                iconRt.offsetMax = Vector2.zero;

                img = iconGo.AddComponent<Image>();
                img.sprite = icon;
                img.preserveAspect = true;
                img.color = Color.white;
                img.raycastTarget = false;
                _ingredientImages[ing.code] = img;

                var badgeGo = new GameObject("StockBadge", typeof(RectTransform));
                badgeGo.transform.SetParent(go.transform, false);
                var badgeRt = badgeGo.GetComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(0f, 0f);
                badgeRt.anchorMax = new Vector2(1f, 0.26f);
                badgeRt.offsetMin = Vector2.zero;
                badgeRt.offsetMax = Vector2.zero;
                var badgeBg = badgeGo.AddComponent<Image>();
                badgeBg.color = new Color(0f, 0f, 0f, 0.72f);
                badgeBg.raycastTarget = false;

                var labelGo = new GameObject("Stock", typeof(RectTransform));
                labelGo.transform.SetParent(badgeGo.transform, false);
                UiFactory.Stretch(labelGo.GetComponent<RectTransform>());
                tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text = $"x{InventoryManager.Instance.GetStock(ing.code)}";
                tmp.fontSize = 14;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                KoreanUiFont.Apply(tmp);
            }
            else
            {
                img = plate;
                img.color = new Color(0.18f, 0.38f, 0.58f, 1f);
                _ingredientImages[ing.code] = img;

                var labelGo = new GameObject("Text", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                UiFactory.Stretch(labelGo.GetComponent<RectTransform>());

                tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text = $"{ing.displayName}\n({ing.code}) x{InventoryManager.Instance.GetStock(ing.code)}";
                tmp.fontSize = 13;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                KoreanUiFont.Apply(tmp);
            }

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = plate;
            btn.transition = Selectable.Transition.None;
            _ingredientButtons[ing.code] = btn;

            var hover = go.AddComponent<IngredientHoverTrigger>();
            hover.Setup(ing.displayName);

            var captured = ing;
            var capturedGo = go;
            var hasIcon = icon != null;
            btn.onClick.AddListener(() =>
            {
                _controller.SelectIngredient(captured);
                capturedGo.transform.DOPunchScale(Vector3.one * 0.08f, 0.15f, 4, 0.4f);
            });

            InventoryManager.Instance.OnStockChanged += () =>
            {
                int stock = InventoryManager.Instance.GetStock(captured.code);
                if (hasIcon)
                    tmp.text = $"x{stock}";
                else
                    tmp.text = $"{captured.displayName}\n({captured.code}) x{stock}";
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

        private static Button CreateActionButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = 56;
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
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            KoreanUiFont.Apply(tmp);
            return btn;
        }
    }
}
