using TMPro;
using UnityEngine;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 한글 UI용 TMP 폰트를 Resources 에서 로드해 TextMeshProUGUI 에 적용한다.
    /// 폰트 에셋이 손상된 경우 기본 TMP 폰트로 폴백해 MissingReferenceException 을 방지한다.
    /// </summary>
    public static class KoreanUiFont
    {
        private const string FontResourcePath = "Fonts/MalgunSDF";
        private static TMP_FontAsset _cached;
        private static bool _warned;

        public static TMP_FontAsset Get()
        {
            if (_cached != null && IsValid(_cached))
                return _cached;

            _cached = Resources.Load<TMP_FontAsset>(FontResourcePath);
            if (IsValid(_cached))
                return _cached;

            if (!_warned)
            {
                Debug.LogWarning(
                    "[KoreanUiFont] MalgunSDF 가 없거나 손상되었습니다. " +
                    "Tools > CupRice > Setup Korean Font 를 실행하세요. (기본 폰트로 폴백)");
                _warned = true;
            }

            return TMP_Settings.defaultFontAsset;
        }

        public static void Apply(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;

            var font = Get();
            if (font != null)
                tmp.font = font;
        }

        private static bool IsValid(TMP_FontAsset font)
        {
            if (font == null) return false;
            if (font.material == null) return false;

            // atlasTexture 또는 atlasTextures 중 하나라도 유효해야 함
            if (font.atlasTexture != null) return true;

            var textures = font.atlasTextures;
            if (textures == null || textures.Length == 0) return false;

            foreach (var tex in textures)
            {
                if (tex != null) return true;
            }

            return false;
        }
    }
}
