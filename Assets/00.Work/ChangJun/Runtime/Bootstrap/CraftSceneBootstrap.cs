using System.Collections;
using System.Collections.Generic;
using ChangJun.Craft;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Judge;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// Craft 씬 런타임 UI — EventSystem + 앵커 기반 레이아웃으로 구성한다.
    /// </summary>
    public sealed class CraftSceneBootstrap : MonoBehaviour
    {
        private const int GridColumns = 4;

        private CraftController _controller;
        private TextMeshProUGUI _moneyText;
        private TextMeshProUGUI _resultText;
        private TextMeshProUGUI _orderText;
        private TextMeshProUGUI _slotText;

        private List<IngredientSO> _ingredients;
        private List<MenuRecipeSO> _menus;
        private List<CraftCustomerSO> _customers;
        private RecipeBookOverlay _recipeBook;
        private CraftCustomerSO _lastCustomer;
        private Coroutine _nextCustomerRoutine;

        private void Start()
        {
            _ingredients = new List<IngredientSO>(Resources.LoadAll<IngredientSO>("Craft/Ingredients"));
            _menus       = new List<MenuRecipeSO>(Resources.LoadAll<MenuRecipeSO>("Craft/Menus"));
            _customers     = new List<CraftCustomerSO>(Resources.LoadAll<CraftCustomerSO>("Craft/Customers"));
            _menus.Sort((a, b) => string.CompareOrdinal(a.code, b.code));

            if (_ingredients.Count == 0 || _menus.Count == 0 || _customers.Count == 0)
            {
                Debug.LogError("[Bootstrap] SO 에셋이 없습니다. Tools > CupRice > Build Craft Prototype 실행.");
                return;
            }

            EnsureEventSystem();
            BuildManagers();
            BuildUI();
            LoadRandomCustomer();
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildManagers()
        {
            if (MoneyManager.Instance == null)
                new GameObject("MoneyManager").AddComponent<MoneyManager>();

            _controller = gameObject.AddComponent<CraftController>();
            _controller.OnCraftJudged += HandleCraftJudged;
            _controller.OnSelectionChanged += HandleSelectionChanged;
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("UI_Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var root = canvasGo.transform as RectTransform;

            // ── 상단 HUD: VerticalLayout 으로 자산/슬롯/주문 영역 분리 ──
            var header = CreatePanel(root, "Header", new Vector2(0, 0.66f), Vector2.one, Vector2.zero, Vector2.zero);
            var headerBg = header.gameObject.AddComponent<Image>();
            headerBg.color = new Color(0.08f, 0.1f, 0.15f, 0.92f);
            headerBg.raycastTarget = false;

            var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(16, 16, 10, 10);
            headerLayout.spacing = 8;
            headerLayout.childAlignment = TextAnchor.UpperLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            var toolbar = CreateStretchChild(header, "Toolbar");
            toolbar.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
            var toolbarLayout = toolbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            toolbarLayout.spacing = 12;
            toolbarLayout.childAlignment = TextAnchor.MiddleCenter;
            toolbarLayout.childControlWidth = true;
            toolbarLayout.childControlHeight = true;
            toolbarLayout.childForceExpandWidth = true;
            toolbarLayout.childForceExpandHeight = true;

            _moneyText = CreateLayoutText(toolbar, "MoneyText", "자산: 3000원",
                TextAlignmentOptions.MidlineLeft, 26, flex: 1.2f);

            _slotText = CreateLayoutText(toolbar, "SlotText", "슬롯: [ ]",
                TextAlignmentOptions.Center, 24, flex: 1f);

            CreateHeaderLayoutButton(toolbar, "레시피 보기", 200f, () => _recipeBook?.Toggle());

            var orderBox = CreateStretchChild(header, "OrderBox");
            var orderLayout = orderBox.gameObject.AddComponent<LayoutElement>();
            orderLayout.minHeight = 88;
            orderLayout.flexibleHeight = 1f;
            var orderBg = orderBox.gameObject.AddComponent<Image>();
            orderBg.color = new Color(0.1f, 0.12f, 0.18f, 0.9f);
            orderBg.raycastTarget = false;

            _orderText = CreateLayoutText(orderBox, "OrderText", "손님 주문",
                TextAlignmentOptions.TopLeft, 22, flex: 1f, margin: new Vector4(12f, 8f, 12f, 8f));
            _orderText.enableWordWrapping = true;
            _orderText.overflowMode = TextOverflowModes.Ellipsis;
            _orderText.maxVisibleLines = 4;

            // ── 결과 텍스트 (재료 그리드 위) ───────────────────────
            _resultText = CreateText(root, "ResultText", "",
                new Vector2(0.1f, 0.54f), new Vector2(0.9f, 0.64f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 32);

            MoneyManager.Instance.OnMoneyChanged += v => _moneyText.text = $"자산: {v:N0}원";
            _moneyText.text = $"자산: {MoneyManager.Instance.Money:N0}원";

            // ── 재료 그리드 (4열, 스크롤) ──────────────────────────
            var scrollPanel = CreatePanel(root, "IngredientScroll",
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.52f),
                Vector2.zero, Vector2.zero);

            var scroll = scrollPanel.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical   = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreatePanel(scrollPanel, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var viewportImg = viewport.gameObject.AddComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0.01f);

            var content = CreatePanel(viewport, "Content", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(0, 0));
            content.pivot = new Vector2(0.5f, 1f);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize        = new Vector2(200, 64);
            grid.spacing         = new Vector2(12, 10);
            grid.padding         = new RectOffset(8, 8, 8, 8);
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumns;
            grid.childAlignment  = TextAnchor.UpperCenter;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content  = content;

            foreach (var ing in _ingredients)
                CreateIngredientButton(content, ing);

            // ── 하단 액션 버튼 ─────────────────────────────────────
            var actionBar = CreatePanel(root, "ActionBar",
                new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.18f),
                Vector2.zero, Vector2.zero);

            var layout = actionBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing            = 16;
            layout.childAlignment     = TextAnchor.MiddleCenter;
            layout.childControlWidth  = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = true;

            CreateActionButton(actionBar, "초기화", () => _controller.ClearSelection());
            CreateActionButton(actionBar, "제작!", () => _controller.Craft());
            CreateActionButton(actionBar, "다음 손님", LoadRandomCustomer);

            // 도감은 모든 UI 위에 그려지도록 마지막에 생성
            _recipeBook = new RecipeBookOverlay(root, _menus, _ingredients);
        }

        private void LoadRandomCustomer()
        {
            if (_customers.Count == 0) return;

            CraftCustomerSO next;
            if (_customers.Count == 1)
            {
                next = _customers[0];
            }
            else
            {
                do
                {
                    next = _customers[Random.Range(0, _customers.Count)];
                } while (next == _lastCustomer);
            }

            _lastCustomer = next;
            _controller.Initialize(new RecipeBook(_menus), next);

            string dietLabel = next.diet == Diet.None ? "" : $" [{next.diet}]";
            _orderText.text  = $"<b>[{next.customerName}]{dietLabel}</b>\n{next.orderLine}";
            _resultText.text = "";
            _resultText.color = Color.white;
            _controller.ClearSelection();
        }

        private void ScheduleNextRandomCustomer(float delaySeconds = 1.2f)
        {
            if (_nextCustomerRoutine != null)
                StopCoroutine(_nextCustomerRoutine);
            _nextCustomerRoutine = StartCoroutine(NextCustomerAfterDelay(delaySeconds));
        }

        private IEnumerator NextCustomerAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadRandomCustomer();
            _nextCustomerRoutine = null;
        }

        private void HandleSelectionChanged(IReadOnlyList<IngredientSO> list)
        {
            var codes = new System.Text.StringBuilder("슬롯: [ ");
            foreach (var ing in list) codes.Append(ing.code).Append(' ');
            codes.Append(']');
            _slotText.text = codes.ToString();
        }

        private void HandleCraftJudged(CraftResult result, MenuRecipeSO matched)
        {
            _resultText.text = result switch
            {
                CraftResult.Success        => $"[성공] {matched.displayName}  (+{matched.price}원)",
                CraftResult.TabooViolation => "[금기위반]  (-100원)",
                CraftResult.WrongRecipe    => "[오조리]  메뉴 없음",
                _                          => "",
            };

            _resultText.color = result switch
            {
                CraftResult.Success        => new Color(0.4f, 1f, 0.5f),
                CraftResult.TabooViolation => new Color(1f, 0.4f, 0.4f),
                CraftResult.WrongRecipe    => new Color(1f, 0.9f, 0.3f),
                _                          => Color.white,
            };

            _resultText.transform.localScale = Vector3.zero;
            _resultText.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

            ScheduleNextRandomCustomer();
        }

        // ── UI 헬퍼 ──────────────────────────────────────────────

        private static RectTransform CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        private static RectTransform CreateStretchChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            return go.GetComponent<RectTransform>();
        }

        private TextMeshProUGUI CreateLayoutText(Transform parent, string name, string text,
            TextAlignmentOptions align, int fontSize, float flex = 1f, Vector4? margin = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());

            var layout = go.AddComponent<LayoutElement>();
            layout.flexibleWidth = flex;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.margin = margin ?? Vector4.zero;
            KoreanUiFont.Apply(tmp);
            return tmp;
        }

        private static void CreateHeaderLayoutButton(Transform parent, string label, float width,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.flexibleWidth = 0f;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.32f, 0.55f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            Stretch(labelGo.GetComponent<RectTransform>());

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            TextAlignmentOptions align, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
            return tmp;
        }

        private void CreateIngredientButton(Transform parent, IngredientSO ing)
        {
            var go = new GameObject($"Btn_{ing.code}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.38f, 0.58f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.highlightedColor = new Color(0.28f, 0.52f, 0.75f);
            colors.pressedColor     = new Color(0.12f, 0.28f, 0.45f);
            btn.colors = colors;

            var labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            Stretch(labelGo.GetComponent<RectTransform>());

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = $"{ing.displayName}\n({ing.code})";
            tmp.fontSize  = 18;
            tmp.color     = Color.white;
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
        }

        private static void CreateHeaderButton(Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(4, 4);
            rt.offsetMax = new Vector2(-4, -4);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.32f, 0.55f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            Stretch(labelGo.GetComponent<RectTransform>());

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
        }

        private static void CreateActionButton(Transform parent, string label,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 64;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.48f, 0.18f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            Stretch(labelGo.GetComponent<RectTransform>());

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 26;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);

            btn.onClick.AddListener(onClick);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
