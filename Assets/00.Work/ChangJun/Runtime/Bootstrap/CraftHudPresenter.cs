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
    public enum IngredientCategory
    {
        Main,
        Sauce,
        Topping,
    }

    /// <summary>
    /// 재료 그리드·밥그릇 프리뷰·액션 바·결과 텍스트 HUD.
    /// </summary>
    public sealed class CraftHudPresenter
    {
        private const float ToppingRadius = 42f;
        private const float ToppingSize = 72f;

        private readonly CraftController _controller;
        private readonly RectTransform _craftRoot;
        private readonly RectTransform _cookContent;
        private readonly RectTransform _toppingLayer;
        private readonly TextMeshProUGUI _moneyText;
        private readonly TextMeshProUGUI _resultText;
        private readonly TextMeshProUGUI _orderStatusText;
        private readonly Dictionary<string, Button> _ingredientButtons = new();
        private readonly Dictionary<string, Image> _ingredientImages = new();
        private readonly List<Image> _toppingImages = new();
        private Button _craftButton;
        private Button _clearButton;
        private bool _craftEnabled = true;

        public RectTransform CraftRoot => _craftRoot;

        public CraftHudPresenter(RectTransform root, RectTransform craftContent,
            CraftController controller, IReadOnlyList<IngredientSO> ingredients, RectTransform topBar)
        {
            _controller = controller;
            IngredientVisualCatalog.EnsureLoaded();
            IngredientHoverTooltip.Ensure(root);

            // 상단 바에 자산 칩을 배치 (Day/Time 칩 오른쪽)
            var moneyChip = UiTheme.CreateBorderedPanel(topBar, "MoneyChip",
                new Vector2(0f, 0.18f), new Vector2(0f, 0.82f),
                new Vector2(250f, 0f), new Vector2(520f, 0f), UiTheme.CardWhite, 2f);
            _moneyText = UiFactory.CreateText(moneyChip, "MoneyText", "자산: 0원",
                Vector2.zero, Vector2.one, new Vector2(14f, 0f), Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 22, UiTheme.TextDark);

            _craftRoot = UiFactory.CreatePanel(craftContent, "CraftArea",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // 다른 탭이 비쳐 보이지 않도록 불투명 배경
            _craftRoot.gameObject.AddComponent<Image>().color = UiTheme.Background;

            // 결과/배너 텍스트 — 조리 화면 상단 중앙에 떠 있는 배너
            var resultBanner = UiTheme.CreateShadowCard(_craftRoot, "ResultBanner",
                new Vector2(0.28f, 0.94f), new Vector2(0.72f, 0.995f), Vector2.zero, Vector2.zero,
                UiTheme.CardWhite, 3f, 4f);
            _resultText = UiFactory.CreateText(resultBanner, "ResultText", "",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 24, UiTheme.TextDark);
            _resultText.fontStyle = FontStyles.Bold;
            _resultText.textWrappingMode = TextWrappingModes.NoWrap;
            _resultText.overflowMode = TextOverflowModes.Ellipsis;

            // "조리하기" 화면 전체(재료 존·밥그릇·액션바) — 주문받기 화면일 때는 통째로 숨긴다.
            _cookContent = UiFactory.CreatePanel(_craftRoot, "CookContent",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var orderStatusChip = UiTheme.CreateBorderedPanel(_cookContent, "OrderStatusChip",
                new Vector2(0f, 0.94f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(220f, 0f),
                UiTheme.CardWhite, 2f);
            _orderStatusText = UiFactory.CreateText(orderStatusChip, "Text", "주문서 확인 중",
                Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f),
                TextAlignmentOptions.MidlineLeft, 13, UiTheme.TextDark);

            // Cupbap Order UI 목업의 조리 화면처럼 메인/밥/소스/토핑을 밥그릇 둘레에 배치한다.
            var mains = new List<IngredientSO>();
            var sauces = new List<IngredientSO>();
            var toppings = new List<IngredientSO>();
            foreach (var ing in ingredients)
            {
                switch (GetCategory(ing.code))
                {
                    case IngredientCategory.Sauce: sauces.Add(ing); break;
                    case IngredientCategory.Topping: toppings.Add(ing); break;
                    default: mains.Add(ing); break;
                }
            }

            CreateCategoryZone(_cookContent, "메인", UiTheme.Accent,
                new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.92f), mains, vertical: false, cellSize: 74f);

            CreateCategoryZone(_cookContent, "토핑", UiTheme.Accent,
                new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.28f), toppings, vertical: false, cellSize: 74f);

            CreateCategoryZone(_cookContent, "소스", UiTheme.Accent,
                new Vector2(0.80f, 0.30f), new Vector2(0.95f, 0.78f), sauces, vertical: true, cellSize: 88f);

            // 가운데-좌측: 밥 (자동 지급 — 선택 불가, 장식용 표시)
            var riceZone = UiFactory.CreatePanel(_cookContent, "RiceZone",
                new Vector2(0.05f, 0.30f), new Vector2(0.20f, 0.78f), Vector2.zero, Vector2.zero);
            CreateCategoryPill(riceZone, "밥", UiTheme.Accent);
            var riceIconGo = new GameObject("RiceIcon", typeof(RectTransform));
            riceIconGo.transform.SetParent(riceZone, false);
            var riceIconRt = riceIconGo.GetComponent<RectTransform>();
            riceIconRt.anchorMin = new Vector2(0.5f, 0.5f);
            riceIconRt.anchorMax = new Vector2(0.5f, 0.5f);
            riceIconRt.sizeDelta = new Vector2(88f, 88f);
            riceIconRt.anchoredPosition = new Vector2(0f, -18f);
            var riceIconImg = riceIconGo.AddComponent<Image>();
            riceIconImg.sprite = IngredientVisualCatalog.Rice;
            riceIconImg.preserveAspect = true;
            riceIconImg.raycastTarget = false;
            if (riceIconImg.sprite == null)
                riceIconImg.color = new Color(0.95f, 0.92f, 0.85f, 0.9f);

            // 가운데: Rice + 토핑 프리뷰
            var bowlArea = UiTheme.CreateShadowCard(_cookContent, "BowlArea",
                new Vector2(0.22f, 0.30f), new Vector2(0.78f, 0.78f), Vector2.zero, Vector2.zero,
                UiTheme.CardWhite, 3f, 5f);

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
            var actionBar = UiFactory.CreatePanel(_cookContent, "ActionBar",
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

        /// <summary>주문받기 화면일 땐 숨기고, 조리하기 화면일 땐 보여준다.</summary>
        public void SetCookViewVisible(bool visible) => _cookContent.gameObject.SetActive(visible);

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
            ShowBanner($"[배달도착] {ingredientName} x{quantity}");
        }

        public void ShowBanner(string message)
        {
            _resultText.text = message;
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

            // 목업처럼 흰 카드 + 검정 테두리 받침
            var border = go.AddComponent<Image>();
            border.color = UiTheme.Border;

            var plateRt = UiFactory.CreatePanel(go.transform, "Plate",
                Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            var plate = plateRt.gameObject.AddComponent<Image>();
            plate.color = UiTheme.CardWhite;

            var icon = IngredientVisualCatalog.GetButtonIcon(ing.code);
            Image img;
            TextMeshProUGUI tmp;

            if (icon != null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(plateRt, false);
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

        private static Button CreateActionButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = 56;

            bool isPrimary = label == "제작!";
            var borderImg = go.AddComponent<Image>();
            borderImg.color = UiTheme.Border;
            var btn = go.AddComponent<Button>();

            var fillRt = UiFactory.CreatePanel(go.transform, "Fill",
                Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            var fillImg = fillRt.gameObject.AddComponent<Image>();
            fillImg.color = isPrimary ? UiTheme.Accent : UiTheme.CardWhite;
            btn.targetGraphic = fillImg;
            btn.onClick.AddListener(() => onClick());

            var tmp = UiFactory.CreateText(fillRt, "Text", label,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 24, isPrimary ? UiTheme.CardWhite : UiTheme.TextDark);
            tmp.raycastTarget = false;
            return btn;
        }

        /// <summary>재료를 메인/소스/토핑 중 하나로 분류 — 조리 화면 배치용(게임 로직에는 영향 없음).</summary>
        private static IngredientCategory GetCategory(string code) => code switch
        {
            "SPC" or "CUR" => IngredientCategory.Sauce,
            "KIM" or "EGG" or "VEG" or "CHS" or "BSP" => IngredientCategory.Topping,
            _ => IngredientCategory.Main,
        };

        private void CreateCategoryZone(Transform parent, string label, Color pillColor,
            Vector2 anchorMin, Vector2 anchorMax, List<IngredientSO> items, bool vertical, float cellSize)
        {
            var zone = UiFactory.CreatePanel(parent, $"Zone_{label}", anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            CreateCategoryPill(zone, label, pillColor);

            var rowArea = UiFactory.CreatePanel(zone, "Row",
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -34f));
            var grid = rowArea.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cellSize, cellSize);
            grid.spacing = new Vector2(10f, 10f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = vertical
                ? GridLayoutGroup.Constraint.FixedColumnCount
                : GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 1;

            foreach (var ing in items)
                CreateIngredientButton(rowArea, ing);
        }

        private static void CreateCategoryPill(Transform zone, string label, Color pillColor)
        {
            var pill = UiTheme.CreateBorderedPanel(zone, "Pill",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-42f, -28f), new Vector2(42f, 0f),
                pillColor, 2f);
            UiFactory.CreateText(pill, "Text", label,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 13, UiTheme.CardWhite);
        }
    }
}
