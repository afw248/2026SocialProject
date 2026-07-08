using System.Collections.Generic;
using ChangJun.Craft;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Judge;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// Craft 씬의 모든 런타임 UI 를 코드로 생성·연결한다.
    /// 씬에는 이 컴포넌트를 가진 GameObject 하나만 배치하면 된다.
    ///
    /// Resources/Craft/ 에 있는 SO 에셋을 로드하고,
    /// CraftController 에 RecipeBook 과 첫 번째 손님을 주입한다.
    /// </summary>
    public sealed class CraftSceneBootstrap : MonoBehaviour
    {
        // ── UI 레이아웃 상수 ──────────────────────────────────────
        private const float BtnW = 180f, BtnH = 60f, Gap = 12f;

        // ── 런타임 참조 ──────────────────────────────────────────
        private CraftController _controller;
        private TextMeshProUGUI _moneyText;
        private TextMeshProUGUI _resultText;
        private TextMeshProUGUI _orderText;
        private TextMeshProUGUI _slotText;

        private List<IngredientSO> _ingredients;
        private List<CraftCustomerSO> _customers;
        private int _customerIndex = 0;

        // ── 생명주기 ─────────────────────────────────────────────
        private void Start()
        {
            _ingredients = new List<IngredientSO>(Resources.LoadAll<IngredientSO>("Craft/Ingredients"));
            var menus      = new List<MenuRecipeSO>(Resources.LoadAll<MenuRecipeSO>("Craft/Menus"));
            _customers     = new List<CraftCustomerSO>(Resources.LoadAll<CraftCustomerSO>("Craft/Customers"));

            if (_ingredients.Count == 0 || menus.Count == 0 || _customers.Count == 0)
            {
                Debug.LogError("[Bootstrap] Resources/Craft 폴더에 SO 에셋이 없습니다. " +
                               "Tools > CupRice > Build Craft Prototype 을 먼저 실행하세요.");
                return;
            }

            BuildManagers();
            BuildUI(menus);
            LoadCustomer(_customerIndex);
        }

        // ── 매니저 생성 ──────────────────────────────────────────
        private void BuildManagers()
        {
            // MoneyManager
            if (MoneyManager.Instance == null)
                new GameObject("MoneyManager").AddComponent<MoneyManager>();

            // CraftController
            _controller = gameObject.AddComponent<CraftController>();
            _controller.OnCraftJudged  += HandleCraftJudged;
            _controller.OnSelectionChanged += HandleSelectionChanged;
        }

        // ── UI 빌드 ──────────────────────────────────────────────
        private void BuildUI(List<MenuRecipeSO> menus)
        {
            // 캔버스
            var canvasGo = new GameObject("UI_Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var root = canvas.transform;

            // ─ 상단 HUD ─────────────────────────────────────────
            _moneyText  = CreateLabel(root, "자산: 3000 원", new Vector2(0, 1), new Vector2(200, 50), new Vector2(110, -30));
            _orderText  = CreateLabel(root, "손님: ...", new Vector2(0.5f, 1), new Vector2(500, 50), new Vector2(0, -30));
            _slotText   = CreateLabel(root, "슬롯: [ ]", new Vector2(1, 1), new Vector2(300, 50), new Vector2(-160, -30));
            _resultText = CreateLabel(root, "", new Vector2(0.5f, 0.5f), new Vector2(600, 80), new Vector2(0, 80));

            // MoneyManager 구독
            MoneyManager.Instance.OnMoneyChanged += v => _moneyText.text = $"자산: {v} 원";
            _moneyText.text = $"자산: {MoneyManager.Instance.Money} 원";

            // ─ 재료 버튼 그리드 ──────────────────────────────────
            float startX = -((_ingredients.Count * (BtnW + Gap)) / 2f) + BtnW / 2f;
            float y = -160f;

            for (int i = 0; i < _ingredients.Count; i++)
            {
                var ing = _ingredients[i];
                float x = startX + i * (BtnW + Gap);
                CreateIngredientButton(root, ing, new Vector2(0.5f, 0.5f), new Vector2(x, y));
            }

            // ─ 슬롯 해제(Clear) 버튼 ─────────────────────────────
            CreateActionButton(root, "초기화", new Vector2(-80, -280), () =>
            {
                _controller.ClearSelection();
            });

            // ─ 제작 버튼 ─────────────────────────────────────────
            CreateActionButton(root, "제작!", new Vector2(80, -280), () =>
            {
                _controller.Craft();
            });

            // ─ 다음 손님 버튼 ────────────────────────────────────
            CreateActionButton(root, "다음 손님 ▶", new Vector2(0, -380), () =>
            {
                _customerIndex = (_customerIndex + 1) % _customers.Count;
                LoadCustomer(_customerIndex);
            });
        }

        // ── 손님 로드 ────────────────────────────────────────────
        private void LoadCustomer(int index)
        {
            if (_customers.Count == 0) return;
            var customer = _customers[index];

            var menus = new List<MenuRecipeSO>(Resources.LoadAll<MenuRecipeSO>("Craft/Menus"));
            _controller.Initialize(new RecipeBook(menus), customer);

            _orderText.text = $"[{customer.customerName}] {customer.orderLine}";
            _resultText.text = "";
        }

        // ── 이벤트 핸들러 ────────────────────────────────────────
        private void HandleSelectionChanged(System.Collections.Generic.IReadOnlyList<IngredientSO> list)
        {
            var codes = new System.Text.StringBuilder("슬롯: [ ");
            foreach (var ing in list) codes.Append(ing.code).Append(' ');
            codes.Append(']');
            _slotText.text = codes.ToString();
        }

        private void HandleCraftJudged(CraftResult result, MenuRecipeSO matched)
        {
            string label = result switch
            {
                CraftResult.Success        => $"✓ 성공! {matched.displayName} (+{matched.price}원)",
                CraftResult.TabooViolation => $"✗ 금기위반! (-100원)",
                CraftResult.WrongRecipe    => "✗ 오조리 — 메뉴 없음",
                _                          => "",
            };

            _resultText.text = label;

            // DOTween 팝업 연출
            _resultText.transform.localScale = Vector3.zero;
            _resultText.transform
                .DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack);

            Color color = result == CraftResult.Success ? Color.green
                        : result == CraftResult.TabooViolation ? Color.red
                        : Color.yellow;
            _resultText.color = color;
        }

        // ── UI 헬퍼 ──────────────────────────────────────────────
        private TextMeshProUGUI CreateLabel(Transform parent, string text,
            Vector2 anchor, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            var go  = new GameObject("Label_" + text[..Mathf.Min(8, text.Length)]);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot     = anchor;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text     = text;
            tmp.fontSize = 22;
            tmp.color    = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private void CreateIngredientButton(Transform parent, IngredientSO ing,
            Vector2 anchor, Vector2 anchoredPos)
        {
            var go  = new GameObject($"Btn_{ing.code}");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot     = anchor;
            rt.sizeDelta = new Vector2(BtnW, BtnH);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.6f, 1f);

            var btn = go.AddComponent<Button>();

            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{ing.displayName}\n({ing.code})";
            tmp.fontSize = 16;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            var capturedIng = ing;
            btn.onClick.AddListener(() =>
            {
                _controller.SelectIngredient(capturedIng);
                // DOTween 버튼 눌림 연출
                go.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 5, 0.5f);
            });
        }

        private void CreateActionButton(Transform parent, string label, Vector2 anchoredPos,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(BtnW + 20, BtnH);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.55f, 0.15f, 1f);

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }
}
