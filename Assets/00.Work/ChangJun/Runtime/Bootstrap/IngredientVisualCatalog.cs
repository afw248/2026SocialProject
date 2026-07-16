using System.Collections.Generic;
using UnityEngine;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 재료 코드 → SteelFood(버튼) / Food(밥 위 토핑) 스프라이트 매핑.
    /// SteelFood 라벨(좌→우, 위→아래 0~15):
    ///  0 Cooked Chicken Strips  1 Cheesy Sauce  2 Halal Beef Bulgogi  3 Porkbelly
    ///  4 Spicy Gochujang        5 White Tofu    6 Cooked Peeled Shrimp 7 Assorted Vegetables
    ///  8 Fried Egg              9 Curry Sauce  10 Egg Garnish(미사용)  11 Kimchi
    /// 12 Kongnamul             13 Vegetables(미사용) 14 Pork Crumbles  15 Garlic(미사용)
    /// </summary>
    public static class IngredientVisualCatalog
    {
        private static Sprite _rice;
        private static Sprite[] _steelFood;
        private static Sprite[] _foodToppings;
        private static Dictionary<string, int> _buttonIndex;
        private static Dictionary<string, int> _toppingIndex;
        private static bool _loaded;

        public static Sprite Rice
        {
            get
            {
                EnsureLoaded();
                return _rice;
            }
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            _steelFood = Resources.LoadAll<Sprite>("Craft/Sprites/SteelFood");
            _foodToppings = Resources.LoadAll<Sprite>("Craft/Sprites/Food");
            var riceSprites = Resources.LoadAll<Sprite>("Craft/Sprites/Rice");
            _rice = riceSprites != null && riceSprites.Length > 0 ? riceSprites[0] : null;

            if (_steelFood != null) System.Array.Sort(_steelFood, CompareSpriteName);
            if (_foodToppings != null) System.Array.Sort(_foodToppings, CompareSpriteName);

            _buttonIndex = new Dictionary<string, int>
            {
                ["CHK"] = 0,
                ["CHS"] = 1,
                ["HBF"] = 2,
                ["PRK"] = 3,
                ["SPC"] = 4,
                ["TFU"] = 5,
                ["SHR"] = 6,
                ["VEG"] = 7,
                ["EGG"] = 8,
                ["CUR"] = 9,
                ["KIM"] = 11,
                ["BSP"] = 12,
                ["PGD"] = 14,
            };

            // Food: 4~5=카레원, 6~7=계란조각
            _toppingIndex = new Dictionary<string, int>
            {
                ["CHK"] = 8,
                ["CHS"] = 3,
                ["HBF"] = 14,
                ["PRK"] = 22,
                ["SPC"] = 29,
                ["TFU"] = 52,
                ["SHR"] = 20,
                ["VEG"] = 55,
                ["EGG"] = 6,
                ["CUR"] = 4,
                ["KIM"] = 16,
                ["BSP"] = 0,
                ["PGD"] = 15,
            };
        }

        public static Sprite GetButtonIcon(string code)
        {
            EnsureLoaded();
            if (_buttonIndex == null || !_buttonIndex.TryGetValue(code, out int idx))
                return null;
            return GetAt(_steelFood, idx);
        }

        public static Sprite GetToppingIcon(string code)
        {
            EnsureLoaded();
            if (_toppingIndex != null && _toppingIndex.TryGetValue(code, out int foodIdx))
            {
                var food = GetAt(_foodToppings, foodIdx);
                if (food != null) return food;
            }

            return GetButtonIcon(code);
        }

        private static Sprite GetAt(Sprite[] arr, int idx)
        {
            if (arr == null || idx < 0 || idx >= arr.Length) return null;
            return arr[idx];
        }

        private static int CompareSpriteName(Sprite a, Sprite b)
        {
            return ExtractTrailingIndex(a.name).CompareTo(ExtractTrailingIndex(b.name));
        }

        private static int ExtractTrailingIndex(string name)
        {
            int underscore = name.LastIndexOf('_');
            if (underscore < 0 || underscore >= name.Length - 1) return 0;
            return int.TryParse(name[(underscore + 1)..], out int n) ? n : 0;
        }
    }
}
