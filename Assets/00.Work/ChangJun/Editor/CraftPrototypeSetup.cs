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
            RefreshCraftData();
            BuildScene();
            KoreanFontSetup.Setup();
            Debug.Log("[CraftPrototypeSetup] 완료! Craft.unity 씬을 열고 플레이하세요.");
        }

        [MenuItem("Tools/CupRice/Refresh Craft Data")]
        public static void RefreshCraftData()
        {
            EnsureDirectory(IngDir);
            EnsureDirectory(MenuDir);
            EnsureDirectory(CusDir);

            CreateIngredients();
            CreateMenus();
            CreateCustomers();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CraftPrototypeSetup] 레시피·손님 데이터 갱신 완료.");
        }

        // ── 재료 SO ──────────────────────────────────────────────
        // 기획서 §2-1 재료 마스터 (RICE 베이스 제외, 총 8종)
        private static void CreateIngredients()
        {
            CreateIngredient("HBF", "할랄 소고기",  Diet.Vegan | Diet.Hindu);
            CreateIngredient("PRK", "돼지고기",     Diet.Halal | Diet.Vegan | Diet.Hindu);
            CreateIngredient("CHK", "닭고기",       Diet.Vegan);
            CreateIngredient("EGG", "계란",         Diet.Vegan);
            CreateIngredient("KIM", "김치(무발효)", Diet.None);
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
            var so = AssetDatabase.LoadAssetAtPath<IngredientSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<IngredientSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            so.code        = code;
            so.displayName = displayName;
            so.forbiddenIn = forbidden;
            EditorUtility.SetDirty(so);
        }

        // ── 메뉴 SO (M1~M16 전체) ─────────────────────────────────
        private static void CreateMenus()
        {
            CreateMenu("M1",  "할랄 불고기 컵밥",   new[] { "HBF", "SPC" },           300);
            CreateMenu("M2",  "김치 두부 컵밥",     new[] { "KIM", "TFU" },           320);
            CreateMenu("M3",  "계란 덮밥",           new[] { "EGG", "VEG" },           220);
            CreateMenu("M4",  "두부 계란 컵밥",     new[] { "TFU", "EGG" },           300);
            CreateMenu("M5",  "김치 계란 컵밥",     new[] { "KIM", "EGG" },           300);
            CreateMenu("M6",  "커리 치킨 컵밥",     new[] { "CUR", "CHK" },           300);
            CreateMenu("M7",  "채식 커리 컵밥",     new[] { "CUR", "VEG" },           350);
            CreateMenu("M8",  "치즈 새우 컵밥",     new[] { "SHR", "CHS" },           400);
            CreateMenu("M9",  "돼지국밥풍 컵밥",    new[] { "PRK", "BRT" },           350);
            CreateMenu("M10", "치즈 김치 컵밥",     new[] { "KIM", "CHS", "VEG" },    450);
            CreateMenu("M11", "담백 두부 채소 컵밥", new[] { "TFU", "BSP", "VEG" },   420);
            CreateMenu("M12", "매운 해물 컵밥",     new[] { "SHR", "SPC", "BSP" },    480);
            CreateMenu("M13", "고향식 커리 컵밥",   new[] { "CUR", "CHK", "BSP" },    550);
            CreateMenu("M14", "든든 삼겹 컵밥",     new[] { "PRK", "EGG", "KIM" },    500);
            CreateMenu("M15", "채소 국밥 컵밥",     new[] { "BRT", "VEG", "BSP" },    420);
            CreateMenu("M16", "상생 컵밥",          new[] { "TFU", "CUR", "VEG" },    600);
        }

        private static void CreateMenu(string code, string displayName, string[] codes, int price)
        {
            string path = $"{MenuDir}/Menu_{code}.asset";
            var so = AssetDatabase.LoadAssetAtPath<MenuRecipeSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<MenuRecipeSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            so.code            = code;
            so.displayName     = displayName;
            so.ingredientCodes = codes;
            so.price           = price;
            EditorUtility.SetDirty(so);
        }

        // ── 손님 SO (1주차 10명) ─────────────────────────────────
        private static void CreateCustomers()
        {
            CreateCustomer("아이샤",   "안녕하세요… 저 돼지 안 돼요. 매운 고기 밥 있어요?",           Diet.Halal, "M1");
            CreateCustomer("압둘라",   "할랄 있어요? 매운 고기 밥 주세요.",                           Diet.Halal, "M1");
            CreateCustomer("응웬",     "저기… 돈 많이 없어요. 밥이랑 계란… 싼 거 돼요?",               Diet.None,  "M3");
            CreateCustomer("첸",       "두부… 계란… 담백한 거 좋아요.",                               Diet.None,  "M4");
            CreateCustomer("김상철",   "김치 계란 제육. 빨리요.",                                      Diet.None,  "M5");
            CreateCustomer("박영자",   "늘 먹던 김치 계란으로 줘요.",                                  Diet.None,  "M5");
            CreateCustomer("미나",     "커리 치킨? 그거 인기래요.",                                    Diet.None,  "M6");
            CreateCustomer("라라",     "따뜻한 국물… 고기… 힘든 날이에요.",                            Diet.None,  "M9");
            CreateCustomer("사라",     "동물성은 다 빼주세요. 계란도, 젓갈도요. 김치 되나요?",         Diet.Vegan, "M2");
            CreateCustomer("유코",     "치즈랑… 새우? 그거 있어요? 인터넷에서 봤어요.",               Diet.None,  "M8");
            CreateCustomer("데비",     "소는… 안 돼요. 야채 매운 거 주세요.",                          Diet.Hindu, "M7");
            CreateCustomer("그린",     "완전 채식이요. 두부, 콩나물, 야채. 깔끔하게.",                 Diet.Vegan, "M11");
            CreateCustomer("마르코",   "치즈! 김치! 퓨전 좋아요. 야채도 넣어줘요.",                     Diet.None,  "M10");
            CreateCustomer("하산",     "저… 돼지 없는 거… 고기 밥…",                                   Diet.Halal, "M1");
            CreateCustomer("왕",       "매운 거! 김치 계란 매운 거 좋아.",                             Diet.None,  "M5");
            CreateCustomer("로사",     "채소 국밥 따뜻한 거요. 오늘 추워서.",                          Diet.Vegan, "M15");
            CreateCustomer("이수진",   "다문화 상생 인증 매장이라 들었어요. 상생 컵밥 주세요.",         Diet.None,  "M16");
        }

        private static void CreateCustomer(string customerName, string orderLine, Diet diet, string menuCode)
        {
            string path = $"{CusDir}/Customer_{customerName}.asset";
            var menu = AssetDatabase.LoadAssetAtPath<MenuRecipeSO>($"{MenuDir}/Menu_{menuCode}.asset");

            var so = AssetDatabase.LoadAssetAtPath<CraftCustomerSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<CraftCustomerSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            so.customerName = customerName;
            so.orderLine    = orderLine;
            so.diet         = diet;
            so.requiredMenu = menu;
            EditorUtility.SetDirty(so);
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
