using System.IO;
using ChangJun.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChangJun.Editor
{
    /// <summary>
    /// 에디터 메뉴 Tools > CupRice > Build Craft Prototype 실행 시:
    ///   1. Resources/Craft/{Ingredients,Menus,Customers} 에 SO 에셋 일괄 생성
    ///   2. Assets/Scenes/Craft.unity 씬 생성 후 Bootstrap GameObject 배치
    ///   3. 씬 저장 (EditorBuildSettings 에 등록 포함)
    /// </summary>
    public static class CraftPrototypeSetup
    {
        private const string IngDir  = "Assets/Resources/Craft/Ingredients";
        private const string MenuDir = "Assets/Resources/Craft/Menus";
        private const string CusDir  = "Assets/Resources/Craft/Customers";
        private const string ScenePath = "Assets/Scenes/Craft.unity";

        [MenuItem("Tools/CupRice/Build Craft Prototype")]
        public static void Build()
        {
            EnsureDirectory(IngDir);
            EnsureDirectory(MenuDir);
            EnsureDirectory(CusDir);

            CreateIngredients();
            CreateMenus();
            CreateCustomers();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildScene();

            Debug.Log("[CraftPrototypeSetup] 완료! Craft.unity 씬을 열고 플레이하세요.");
        }

        // ── 재료 SO ──────────────────────────────────────────────
        // 기획서 §2-1 재료 마스터 (RICE 베이스 제외, 총 8종)
        private static void CreateIngredients()
        {
            CreateIngredient("HBF", "할랄 소고기",  Diet.Vegan | Diet.Hindu);
            CreateIngredient("PRK", "돼지고기",     Diet.Halal | Diet.Vegan | Diet.Hindu);
            CreateIngredient("CHK", "닭고기",       Diet.Vegan);
            CreateIngredient("EGG", "계란",         Diet.Vegan);
            CreateIngredient("KIM", "김치(젓갈⽆)", Diet.None);
            CreateIngredient("TFU", "두부",         Diet.None);
            CreateIngredient("VEG", "야채모둠",     Diet.None);
            CreateIngredient("SPC", "매운양념",     Diet.None);
            CreateIngredient("CUR", "커리향신료",   Diet.None);
            CreateIngredient("BRT", "육수(채수)",   Diet.None);
            CreateIngredient("BSP", "콩나물",       Diet.None);
            CreateIngredient("CHS", "치즈",         Diet.Vegan);
            CreateIngredient("SHR", "새우",         Diet.Vegan);
        }

        private static void CreateIngredient(string code, string displayName, Diet forbidden)
        {
            string path = $"{IngDir}/Ingredient_{code}.asset";
            if (File.Exists(Path.Combine(Application.dataPath[..^"Assets".Length], path))) return;

            var so          = ScriptableObject.CreateInstance<IngredientSO>();
            so.code         = code;
            so.displayName  = displayName;
            so.forbiddenIn  = forbidden;
            AssetDatabase.CreateAsset(so, path);
        }

        // ── 메뉴 SO ──────────────────────────────────────────────
        // 기획서 §2-2 — M1~M3 + M9(금기위반 시연)
        private static void CreateMenus()
        {
            CreateMenu("M1", "할랄 불고기 컵밥", new[] { "HBF", "SPC" }, 300);
            CreateMenu("M2", "김치 두부 컵밥",   new[] { "KIM", "TFU" }, 320);
            CreateMenu("M3", "계란 덮밥",         new[] { "EGG", "VEG" }, 220);
            CreateMenu("M9", "돼지국밥풍 컵밥",  new[] { "PRK", "BRT" }, 350);
        }

        private static void CreateMenu(string code, string displayName, string[] codes, int price)
        {
            string path = $"{MenuDir}/Menu_{code}.asset";
            if (File.Exists(Path.Combine(Application.dataPath[..^"Assets".Length], path))) return;

            var so               = ScriptableObject.CreateInstance<MenuRecipeSO>();
            so.code              = code;
            so.displayName       = displayName;
            so.ingredientCodes   = codes;
            so.price             = price;
            AssetDatabase.CreateAsset(so, path);
        }

        // ── 손님 SO ──────────────────────────────────────────────
        // 1주차 대표 3명 — 아이샤(할랄·M1), 사라(비건·M2), 응웬(제약없음·M3)
        private static void CreateCustomers()
        {
            CreateCustomer("아이샤",
                "안녕하세요… 저 돼지 안 돼요. 매운 고기 밥 있어요?",
                Diet.Halal, "M1");

            CreateCustomer("사라",
                "동물성은 다 빼주세요. 계란도, 젓갈도요. 김치 되나요?",
                Diet.Vegan, "M2");

            CreateCustomer("응웬",
                "저기… 돈 많이 없어요. 밥이랑 계란… 싼 거 돼요?",
                Diet.None, "M3");
        }

        private static void CreateCustomer(string customerName, string orderLine, Diet diet, string menuCode)
        {
            string path = $"{CusDir}/Customer_{customerName}.asset";
            if (File.Exists(Path.Combine(Application.dataPath[..^"Assets".Length], path))) return;

            var menu = AssetDatabase.LoadAssetAtPath<MenuRecipeSO>($"{MenuDir}/Menu_{menuCode}.asset");

            var so             = ScriptableObject.CreateInstance<CraftCustomerSO>();
            so.customerName    = customerName;
            so.orderLine       = orderLine;
            so.diet            = diet;
            so.requiredMenu    = menu;
            AssetDatabase.CreateAsset(so, path);
        }

        // ── 씬 빌드 ──────────────────────────────────────────────
        private static void BuildScene()
        {
            // 기존 씬에 미저장 변경이 있으면 먼저 저장
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 어두운 배경
            scene.name = "Craft";

            // Camera
            var camGo = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(camGo, scene);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.08f, 0.08f, 0.12f);
            cam.orthographic     = true;
            cam.tag              = "MainCamera";

            // Bootstrap
            var bootGo = new GameObject("CraftSceneBootstrap");
            SceneManager.MoveGameObjectToScene(bootGo, scene);
            bootGo.AddComponent<ChangJun.Bootstrap.CraftSceneBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            // EditorBuildSettings 에 씬 추가 (없는 경우만)
            var scenes = EditorBuildSettings.scenes;
            bool exists = false;
            foreach (var s in scenes)
                if (s.path == ScenePath) { exists = true; break; }

            if (!exists)
            {
                var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
                System.Array.Copy(scenes, newScenes, scenes.Length);
                newScenes[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
                EditorBuildSettings.scenes = newScenes;
            }

            Debug.Log($"[CraftPrototypeSetup] 씬 저장됨: {ScenePath}");
        }

        private static void EnsureDirectory(string dir)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parts = dir.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }
}
