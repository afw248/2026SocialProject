using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ChangJun.Editor
{
    /// <summary>
    /// Windows 맑은 고딕(malgun.ttf)으로 TMP SDF 폰트 에셋을 생성한다.
    /// 아틀라스·머티리얼을 서브 에셋으로 함께 저장하지 않으면 런타임 MissingReferenceException 이 발생한다.
    /// </summary>
    public static class KoreanFontSetup
    {
        private const string FontDir   = "Assets/Resources/Fonts";
        private const string FontPath  = FontDir + "/malgun.ttf";
        private const string AssetPath = FontDir + "/MalgunSDF.asset";

        [MenuItem("Tools/CupRice/Setup Korean Font")]
        public static void Setup()
        {
            EnsureDirectory(FontDir);
            CopySystemFontIfNeeded();

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (sourceFont == null)
            {
                Debug.LogError("[KoreanFontSetup] malgun.ttf 를 찾지 못했습니다.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath) != null)
                AssetDatabase.DeleteAsset(AssetPath);

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                36,
                5,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            fontAsset.name = "MalgunSDF";

            // 프로토타입 UI 에 쓰이는 한글·기호를 미리 베이크
            fontAsset.TryAddCharacters(BuildCharacterSet(), out _);

            SaveWithSubAssets(fontAsset, AssetPath);

            Debug.Log($"[KoreanFontSetup] 한글 폰트 생성 완료: {AssetPath}");
        }

        private static void SaveWithSubAssets(TMP_FontAsset fontAsset, string path)
        {
            AssetDatabase.CreateAsset(fontAsset, path);

            if (fontAsset.material != null)
            {
                fontAsset.material.name = "MalgunSDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    var tex = fontAsset.atlasTextures[i];
                    if (tex == null) continue;
                    tex.name = $"MalgunSDF Atlas {i}";
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }
            }

            // 빌드 시 Dynamic 데이터 삭제 방지
            var so = new SerializedObject(fontAsset);
            var clearProp = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearProp != null)
            {
                clearProp.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>프로토타입에서 사용하는 한글·숫자·기호 집합</summary>
        private static string BuildCharacterSet()
        {
            var sb = new StringBuilder(512);

            // ASCII 기본
            sb.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
            sb.Append(" .,!?()[]-+:/원");

            // 재료·메뉴·UI
            sb.Append("할랄소고기돼지고기닭고기계란김치무발효두부야채모둠매운양념커리향신료콩나물치즈새우");
            sb.Append("다진돼지고기제육");
            sb.Append("불고기컵밥덮밥삼겹치킨");

            // 손님·대사
            sb.Append("아이샤응웬사라안녕하세요저돼지안돼요밥있어요동물성다빼주세요젓갈도무발효저기돈많이없어요싼거돼요");

            // HUD·버튼·결과
            sb.Append("자산슬롯초기화제작다음손님성공금기위반오조리메뉴없음");

            return sb.ToString();
        }

        private static void CopySystemFontIfNeeded()
        {
            string projectRoot = Application.dataPath[..^"Assets".Length];
            string destFull    = Path.Combine(projectRoot, FontPath.Replace('/', Path.DirectorySeparatorChar));

            string systemFont = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Fonts),
                "malgun.ttf");

            if (!File.Exists(systemFont))
            {
                Debug.LogError($"[KoreanFontSetup] 시스템 폰트 없음: {systemFont}");
                return;
            }

            File.Copy(systemFont, destFull, overwrite: true);
            AssetDatabase.ImportAsset(FontPath);
        }

        private static void EnsureDirectory(string dir)
        {
            if (AssetDatabase.IsValidFolder(dir)) return;

            var parts   = dir.Split('/');
            string curr = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = curr + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(curr, parts[i]);
                curr = next;
            }
        }
    }
}
