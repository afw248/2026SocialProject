using System.Collections.Generic;
using ChangJun.Data;
using UnityEngine;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 손님 이름 → 픽셀 초상화. Resources/Craft/Sprites/Customers 에서 로드한다.
    /// </summary>
    public static class CustomerPortraitCatalog
    {
        private static readonly Dictionary<string, string> Keys = new()
        {
            ["아이샤"] = "aisha",
            ["압둘라"] = "abdullah",
            ["응웬"] = "nguyen",
            ["첸"] = "chen",
            ["김상철"] = "kimsangcheol",
            ["박영자"] = "parkyoungja",
            ["미나"] = "mina",
            ["라라"] = "lara",
            ["사라"] = "sara",
            ["유코"] = "yuko",
            ["데비"] = "devi",
            ["그린"] = "green",
            ["마르코"] = "marco",
            ["하산"] = "hasan",
            ["왕"] = "wang",
            ["로사"] = "rosa",
            ["이수진"] = "leesujin",
            ["현우"] = "hyunwoo",
            ["나영"] = "nayoung",
            ["마커스"] = "marcus",
            ["Jasmine"] = "jasmine",
            ["Tyler"] = "tyler",
            ["Keisha"] = "keisha",
            ["준호"] = "junho",
            ["Fatima"] = "fatima",
            ["Priya"] = "priya",
            ["Bao"] = "bao",
            ["Leo"] = "leo",
            ["Darnell"] = "darnell",
        };

        private static Dictionary<string, Sprite> _sprites;
        private static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _sprites = new Dictionary<string, Sprite>();
            var loaded = Resources.LoadAll<Sprite>("Craft/Sprites/Customers");
            if (loaded == null) return;
            foreach (var sprite in loaded)
            {
                if (sprite != null)
                    _sprites[sprite.name] = sprite;
            }
        }

        public static Sprite Get(CraftCustomerSO customer)
        {
            EnsureLoaded();
            if (customer == null || _sprites == null) return null;
            if (!Keys.TryGetValue(customer.customerName, out string key))
                return null;
            return _sprites.TryGetValue($"portrait_{key}", out var sprite) ? sprite : null;
        }

        public static string CultureCaption(CraftCustomerSO customer)
        {
            if (customer == null) return "";
            return customer.cultureGroup switch
            {
                CultureGroup.Korean => "한식",
                CultureGroup.Muslim => "무슬림 · 할랄",
                CultureGroup.Hindu => "힌두",
                CultureGroup.Vegan => "비건",
                CultureGroup.SEAsian => "동남아",
                CultureGroup.AfricanAmerican => "소울푸드",
                _ => "",
            };
        }

        public static Color CultureColor(CultureGroup group) => group switch
        {
            CultureGroup.Korean => new Color(0.92f, 0.42f, 0.34f),
            CultureGroup.Muslim => new Color(0.22f, 0.68f, 0.52f),
            CultureGroup.Hindu => new Color(0.92f, 0.68f, 0.22f),
            CultureGroup.Vegan => new Color(0.40f, 0.78f, 0.38f),
            CultureGroup.SEAsian => new Color(0.38f, 0.60f, 0.92f),
            CultureGroup.AfricanAmerican => new Color(0.68f, 0.45f, 0.88f),
            _ => new Color(0.5f, 0.45f, 0.4f),
        };
    }
}
