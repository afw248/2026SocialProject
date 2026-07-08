using System.Collections.Generic;
using ChangJun.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 전체 메뉴 레시피 목록을 오버레이 패널로 보여준다.
    /// </summary>
    public sealed class RecipeBookOverlay
    {
        private readonly GameObject _root;

        public RecipeBookOverlay(RectTransform canvasRoot,
            IReadOnlyList<MenuRecipeSO> menus,
            IReadOnlyList<IngredientSO> ingredients)
        {
            var nameMap = BuildIngredientNameMap(ingredients);
            string body   = BuildRecipeText(menus, nameMap);

            // 별도 Canvas 로 최상위 렌더링 (재료 버튼에 가려지지 않음)
            _root = new GameObject("RecipeBookOverlay", typeof(RectTransform));
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode      = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder    = 100;
            canvas.overrideSorting = true;

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            _root.AddComponent<GraphicRaycaster>();

            var overlayRt = _root.GetComponent<RectTransform>();
            Stretch(overlayRt);

            // 반투명 배경 (탭하면 닫힘)
            var dimRt = CreateChild(overlayRt, "Dim", Vector2.zero, Vector2.one);
            var dim = dimRt.gameObject.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.72f);
            var dimBtn = dimRt.gameObject.AddComponent<Button>();
            dimBtn.targetGraphic = dim;
            dimBtn.onClick.AddListener(Hide);

            // 패널 (배경 클릭으로 닫히지 않도록 별도 Image)
            var panel = CreateChild(overlayRt, "Panel",
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.14f, 0.2f, 0.98f);
            panelImg.raycastTarget = true;

            CreateTitle(panel, "컵밥 레시피 도감");
            CreateCloseButton(panel);

            var scrollRt = CreateChild(panel, "Scroll",
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.88f));
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical   = true;

            var viewport = CreateChild(scrollRt, "Viewport", Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            var content = CreateChild(viewport, "Content",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(0, 0));
            content.pivot = new Vector2(0.5f, 1f);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bodyText = content.gameObject.AddComponent<TextMeshProUGUI>();
            bodyText.text = body;
            bodyText.fontSize = 22;
            bodyText.color = new Color(0.92f, 0.94f, 1f);
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.enableWordWrapping = true;
            bodyText.raycastTarget = false;
            KoreanUiFont.Apply(bodyText);

            content.sizeDelta = new Vector2(0, bodyText.preferredHeight + 24);

            scroll.viewport = viewport;
            scroll.content  = content;

            _root.SetActive(false);
        }

        public void Toggle()
        {
            if (_root.activeSelf) Hide();
            else Show();
        }

        public void Show() => _root.SetActive(true);

        public void Hide() => _root.SetActive(false);

        private void CreateTitle(RectTransform panel, string title)
        {
            var titleRt = CreateChild(panel, "Title",
                new Vector2(0.04f, 0.9f), new Vector2(0.8f, 0.98f));
            var tmp = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = 30;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
        }

        private void CreateCloseButton(RectTransform panel)
        {
            var btnRt = CreateChild(panel, "BtnClose",
                new Vector2(0.82f, 0.9f), new Vector2(0.96f, 0.98f));
            var img = btnRt.gameObject.AddComponent<Image>();
            img.color = new Color(0.45f, 0.15f, 0.15f);
            var btn = btnRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(Hide);

            var labelRt = CreateChild(btnRt, "Text", Vector2.zero, Vector2.one);
            var tmp = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = "닫기";
            tmp.fontSize = 22;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            KoreanUiFont.Apply(tmp);
        }

        private static string BuildRecipeText(IReadOnlyList<MenuRecipeSO> menus,
            Dictionary<string, string> nameMap)
        {
            var sb = new System.Text.StringBuilder(menus.Count * 64);
            sb.AppendLine("재료 2~3개를 골라 조합하세요. (밥 베이스는 기본 포함)");
            sb.AppendLine();

            foreach (var menu in menus)
            {
                if (menu == null) continue;
                sb.Append($"[{menu.code}] {menu.displayName}");
                sb.Append("  —  ");
                sb.Append(FormatIngredients(menu.ingredientCodes, nameMap));
                sb.Append($"  ({menu.price:N0}원)");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string FormatIngredients(string[] codes, Dictionary<string, string> nameMap)
        {
            if (codes == null || codes.Length == 0) return "-";
            var parts = new string[codes.Length];
            for (int i = 0; i < codes.Length; i++)
            {
                string code = codes[i];
                parts[i] = nameMap.TryGetValue(code, out var name)
                    ? $"{name}({code})"
                    : code;
            }
            return string.Join(" + ", parts);
        }

        private static Dictionary<string, string> BuildIngredientNameMap(IReadOnlyList<IngredientSO> ingredients)
        {
            var map = new Dictionary<string, string>();
            foreach (var ing in ingredients)
            {
                if (ing != null && !string.IsNullOrEmpty(ing.code))
                    map[ing.code] = ing.displayName;
            }
            return map;
        }

        private static RectTransform CreateChild(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin = default, Vector2 offsetMax = default)
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

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
