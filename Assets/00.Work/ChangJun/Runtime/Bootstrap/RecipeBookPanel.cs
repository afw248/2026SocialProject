using System;
using System.Collections.Generic;
using ChangJun.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 레시피 도감 — 독립 풀스크린 화면. 아이콘 중심 레시피 목록.
    /// </summary>
    public sealed class RecipeBookPanel
    {
        private readonly GameObject _root;

        public event Action OnBack;

        public RecipeBookPanel(IReadOnlyList<MenuRecipeSO> menus,
            IReadOnlyList<IngredientSO> ingredients)
        {
            IngredientVisualCatalog.EnsureLoaded();

            _root = UiFactory.CreateOverlayRoot("RecipeOverlay", 60);
            _root.SetActive(false);
            IngredientHoverTooltip.Ensure(_root.transform);

            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = UiTheme.Background;

            var header = UiTheme.CreateHeaderBar(_root.transform, "컵밥 레시피 도감", 72f, 78f);
            UiTheme.CreateBackButton(header, () => OnBack?.Invoke());

            var body = UiTheme.CreateScreenBody(_root.transform, 72f, 24f);

            UiFactory.CreateText(body, "Hint",
                "재료 2~3개 조합 · 아이콘에 마우스를 올리면 이름이 보입니다",
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 15, UiTheme.TextMuted);

            var scrollRt = UiFactory.CreatePanel(body, "Scroll",
                new Vector2(0f, 0f), new Vector2(1f, 0.92f),
                Vector2.zero, Vector2.zero);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            UiFactory.ConfigureScroll(scroll);

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            var content = UiFactory.CreateStretchChild(viewport, "Content");
            content.pivot = new Vector2(0.5f, 1f);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            var nameMap = BuildIngredientNameMap(ingredients);
            foreach (var menu in menus)
            {
                if (menu == null) continue;
                CreateRecipeCard(content, menu, nameMap);
            }
        }

        public void Show() => _root.SetActive(true);
        public void Hide() => _root.SetActive(false);

        private static void CreateRecipeCard(RectTransform content, MenuRecipeSO menu,
            Dictionary<string, string> nameMap)
        {
            var cardWrap = UiFactory.CreateStretchChild(content, $"Card_{menu.code}");
            var le = cardWrap.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 118;
            le.minHeight = 118;
            var card = UiTheme.CreateBorderedPanel(cardWrap, "Fill",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UiTheme.CardWhite, 3f);

            UiFactory.CreateText(card, "MenuName", menu.displayName,
                new Vector2(0.03f, 0.58f), new Vector2(0.72f, 0.94f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 22, ReceiptUiHelper.InkColor).fontStyle =
                FontStyles.Bold;

            UiFactory.CreateText(card, "Price", $"{menu.price:N0}원",
                new Vector2(0.72f, 0.58f), new Vector2(0.97f, 0.94f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight, 20, ReceiptUiHelper.MutedInk);

            var iconRow = UiFactory.CreatePanel(card, "Icons",
                new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.54f),
                Vector2.zero, Vector2.zero);
            var hlg = iconRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;

            UiFactory.CreateText(iconRow, "Eq", "=",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 22, ReceiptUiHelper.InkColor);

            if (menu.ingredientCodes != null)
            {
                for (int i = 0; i < menu.ingredientCodes.Length; i++)
                {
                    if (i > 0)
                    {
                        UiFactory.CreateText(iconRow, $"Plus{i}", "+",
                            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                            TextAlignmentOptions.Center, 20, ReceiptUiHelper.MutedInk);
                    }

                    string code = menu.ingredientCodes[i];
                    string label = nameMap.TryGetValue(code, out var n) ? n : code;
                    CreateIngredientIcon(iconRow.transform, code, label);
                }
            }
        }

        private static void CreateIngredientIcon(Transform parent, string code, string label)
        {
            var chip = new GameObject($"Icon_{code}", typeof(RectTransform));
            chip.transform.SetParent(parent, false);
            var le = chip.AddComponent<LayoutElement>();
            le.preferredWidth = 52f;
            le.preferredHeight = 52f;

            var img = chip.AddComponent<Image>();
            var sprite = IngredientVisualCatalog.GetButtonIcon(code);
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = sprite != null ? Color.white : new Color(0.35f, 0.38f, 0.45f);

            var trigger = chip.AddComponent<IngredientHoverTrigger>();
            trigger.Setup(label);
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
