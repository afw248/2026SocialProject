using System.Collections.Generic;
using ChangJun.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 탭 콘텐츠 영역에 표시되는 임베디드 레시피 도감.
    /// </summary>
    public sealed class RecipeBookPanel
    {
        private readonly GameObject _root;

        public GameObject Root => _root;

        public RecipeBookPanel(RectTransform parent,
            IReadOnlyList<MenuRecipeSO> menus,
            IReadOnlyList<IngredientSO> ingredients)
        {
            _root = UiFactory.CreatePanel(parent, "RecipePanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            _root.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.98f);

            UiFactory.CreateText(_root.transform, "Title", "컵밥 레시피 도감",
                new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.99f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 28);

            var scrollRt = UiFactory.CreatePanel(_root.transform, "Scroll",
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.9f),
                Vector2.zero, Vector2.zero);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            var content = UiFactory.CreateStretchChild(viewport, "Content");
            content.pivot = new Vector2(0.5f, 1f);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);

            var bodyText = content.gameObject.AddComponent<TextMeshProUGUI>();
            bodyText.text = BuildRecipeText(menus, BuildIngredientNameMap(ingredients));
            bodyText.fontSize = 22;
            bodyText.color = new Color(0.92f, 0.94f, 1f);
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            KoreanUiFont.Apply(bodyText);

            content.sizeDelta = new Vector2(0, bodyText.preferredHeight + 24);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
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

        private static Dictionary<string, string> BuildIngredientNameMap(
            IReadOnlyList<IngredientSO> ingredients)
        {
            var map = new Dictionary<string, string>();
            foreach (var ing in ingredients)
            {
                if (ing != null && !string.IsNullOrEmpty(ing.code))
                    map[ing.code] = ing.displayName;
            }
            return map;
        }
    }
}
