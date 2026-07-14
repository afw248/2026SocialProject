using ChangJun.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChangJun.Editor
{
    public static class CraftPrototypeSetup
    {
        private const string IngDir = "Assets/Resources/Craft/Ingredients";
        private const string MenuDir = "Assets/Resources/Craft/Menus";
        private const string CusDir = "Assets/Resources/Craft/Customers";
        private const string ThresholdDir = "Assets/Resources/Craft/Thresholds";
        private const string NewsDir = "Assets/Resources/Craft/News";
        private const string DeliveryDir = "Assets/Resources/Craft/Delivery";
        private const string ConfigPath = "Assets/Resources/Craft/DayConfig.asset";
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
            EnsureDirectory(ThresholdDir);
            EnsureDirectory(NewsDir);
            EnsureDirectory(DeliveryDir);

            CreateDayConfig();
            CreateIngredients();
            CreateMenus();
            CreateCustomers();
            CreateThresholds();
            CreateNews();
            CreateDeliveryEvents();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CraftPrototypeSetup] 레시피·손님·진행 데이터 갱신 완료.");
        }

        private static void CreateDayConfig()
        {
            var so = AssetDatabase.LoadAssetAtPath<DayConfigSO>(ConfigPath);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<DayConfigSO>();
                AssetDatabase.CreateAsset(so, ConfigPath);
            }
            so.openHour = 10;
            so.closeHour = 21;
            so.morningHour = 8;
            so.passiveMinutesPerRealSecond = 0.45f;
            so.minutesPerCustomerMin = 10;
            so.minutesPerCustomerMax = 18;
            so.minutesPerCustomer = 12;
            so.starterStockPerIngredient = 5;
            so.expressDeliveryMinutes = 30;
            so.economyDeliveryMinutes = 60;
            so.expressDeliveryPriceMultiplier = 2f;
            EditorUtility.SetDirty(so);
        }

        private static void CreateIngredients()
        {
            CreateIngredient("HBF", "할랄 소고기", Diet.Vegan | Diet.Hindu, CultureGroup.Muslim, 40, 120, false);
            CreateIngredient("PRK", "돼지고기", Diet.Halal | Diet.Vegan | Diet.Hindu, CultureGroup.Korean, 50, 80, false);
            CreateIngredient("CHK", "닭고기", Diet.Vegan, CultureGroup.Korean, 30, 70, true);
            CreateIngredient("EGG", "계란", Diet.Vegan, CultureGroup.Korean, 0, 40, true);
            CreateIngredient("KIM", "김치(무발효)", Diet.None, CultureGroup.Korean, 0, 50, true);
            CreateIngredient("TFU", "두부", Diet.None, CultureGroup.Vegan, 0, 45, true);
            CreateIngredient("VEG", "야채모둠", Diet.None, CultureGroup.Korean, 0, 35, true);
            CreateIngredient("SPC", "매운양념", Diet.None, CultureGroup.Muslim, 35, 30, false);
            CreateIngredient("CUR", "커리향신료", Diet.None, CultureGroup.Hindu, 45, 55, false);
            CreateIngredient("BRT", "육수(채수)", Diet.None, CultureGroup.Korean, 25, 40, true);
            CreateIngredient("BSP", "콩나물", Diet.None, CultureGroup.Vegan, 20, 25, true);
            CreateIngredient("CHS", "치즈", Diet.Vegan, CultureGroup.Korean, 55, 90, false);
            CreateIngredient("SHR", "새우", Diet.Vegan, CultureGroup.SEAsian, 60, 110, false);
        }

        private static void CreateIngredient(string code, string displayName, Diet forbidden,
            CultureGroup culture, int unlockThreshold, int price, bool starter)
        {
            string path = $"{IngDir}/Ingredient_{code}.asset";
            var so = LoadOrCreate<IngredientSO>(path);
            so.code = code;
            so.displayName = displayName;
            so.forbiddenIn = forbidden;
            so.cultureGroup = culture;
            so.unlockThreshold = unlockThreshold;
            so.purchasePrice = price;
            so.isStarterUnlocked = starter;
            EditorUtility.SetDirty(so);
        }

        private static void CreateMenus()
        {
            CreateMenu("M1", "할랄 불고기 컵밥", new[] { "HBF", "SPC" }, 300, CultureGroup.Muslim);
            CreateMenu("M2", "김치 두부 컵밥", new[] { "KIM", "TFU" }, 320, CultureGroup.Vegan);
            CreateMenu("M3", "계란 덮밥", new[] { "EGG", "VEG" }, 220, CultureGroup.SEAsian);
            CreateMenu("M4", "두부 계란 컵밥", new[] { "TFU", "EGG" }, 300, CultureGroup.Korean);
            CreateMenu("M5", "김치 계란 컵밥", new[] { "KIM", "EGG" }, 300, CultureGroup.Korean);
            CreateMenu("M6", "커리 치킨 컵밥", new[] { "CUR", "CHK" }, 300, CultureGroup.Korean);
            CreateMenu("M7", "채식 커리 컵밥", new[] { "CUR", "VEG" }, 350, CultureGroup.Hindu);
            CreateMenu("M8", "치즈 새우 컵밥", new[] { "SHR", "CHS" }, 400, CultureGroup.Korean);
            CreateMenu("M9", "돼지국밥풍 컵밥", new[] { "PRK", "BRT" }, 350, CultureGroup.Korean);
            CreateMenu("M10", "치즈 김치 컵밥", new[] { "KIM", "CHS", "VEG" }, 450, CultureGroup.Korean);
            CreateMenu("M11", "담백 두부 채소 컵밥", new[] { "TFU", "BSP", "VEG" }, 420, CultureGroup.Vegan);
            CreateMenu("M12", "매운 해물 컵밥", new[] { "SHR", "SPC", "BSP" }, 480, CultureGroup.SEAsian);
            CreateMenu("M13", "고향식 커리 컵밥", new[] { "CUR", "CHK", "BSP" }, 550, CultureGroup.Hindu);
            CreateMenu("M14", "든든 삼겹 컵밥", new[] { "PRK", "EGG", "KIM" }, 500, CultureGroup.Korean);
            CreateMenu("M15", "채소 국밥 컵밥", new[] { "BRT", "VEG", "BSP" }, 420, CultureGroup.Vegan);
            CreateMenu("M16", "상생 컵밥", new[] { "TFU", "CUR", "VEG" }, 600, CultureGroup.Korean);
        }

        private static void CreateMenu(string code, string displayName, string[] codes, int price, CultureGroup culture)
        {
            string path = $"{MenuDir}/Menu_{code}.asset";
            var so = LoadOrCreate<MenuRecipeSO>(path);
            so.code = code;
            so.displayName = displayName;
            so.ingredientCodes = codes;
            so.price = price;
            so.cultureGroup = culture;
            EditorUtility.SetDirty(so);
        }

        private static void CreateCustomers()
        {
            CreateCustomer("아이샤", "안녕하세요… 저 돼지 안 돼요. 매운 고기 밥 있어요?", Diet.Halal, CultureGroup.Muslim, "M1");
            CreateCustomer("압둘라", "할랄 있어요? 매운 고기 밥 주세요.", Diet.Halal, CultureGroup.Muslim, "M1");
            CreateCustomer("응웬", "저기… 돈 많이 없어요. 밥이랑 계란… 싼 거 돼요?", Diet.None, CultureGroup.SEAsian, "M3");
            CreateCustomer("첸", "두부… 계란… 담백한 거 좋아요.", Diet.None, CultureGroup.Korean, "M4");
            CreateCustomer("김상철", "김치 계란 제육. 빨리요.", Diet.None, CultureGroup.Korean, "M5");
            CreateCustomer("박영자", "늘 먹던 김치 계란으로 줘요.", Diet.None, CultureGroup.Korean, "M5");
            CreateCustomer("미나", "커리 치킨? 그거 인기래요.", Diet.None, CultureGroup.Korean, "M6");
            CreateCustomer("라라", "따뜻한 국물… 고기… 힘든 날이에요.", Diet.None, CultureGroup.Korean, "M9");
            CreateCustomer("사라", "동물성은 다 빼주세요. 계란도, 젓갈도요. 김치 되나요?", Diet.Vegan, CultureGroup.Vegan, "M2");
            CreateCustomer("유코", "치즈랑… 새우? 그거 있어요? 인터넷에서 봤어요.", Diet.None, CultureGroup.Korean, "M8");
            CreateCustomer("데비", "소는… 안 돼요. 야채 매운 거 주세요.", Diet.Hindu, CultureGroup.Hindu, "M7");
            CreateCustomer("그린", "완전 채식이요. 두부, 콩나물, 야채. 깔끔하게.", Diet.Vegan, CultureGroup.Vegan, "M11");
            CreateCustomer("마르코", "치즈! 김치! 퓨전 좋아요. 야채도 넣어줘요.", Diet.None, CultureGroup.Korean, "M10");
            CreateCustomer("하산", "저… 돼지 없는 거… 고기 밥…", Diet.Halal, CultureGroup.Muslim, "M1");
            CreateCustomer("왕", "매운 거! 김치 계란 매운 거 좋아.", Diet.None, CultureGroup.Korean, "M5");
            CreateCustomer("로사", "채소 국밥 따뜻한 거요. 오늘 추워서.", Diet.Vegan, CultureGroup.Vegan, "M15");
            CreateCustomer("이수진", "다문화 상생 인증 매장이라 들었어요. 상생 컵밥 주세요.", Diet.None, CultureGroup.Korean, "M16");
        }

        private static void CreateCustomer(string customerName, string orderLine, Diet diet,
            CultureGroup culture, string menuCode)
        {
            string path = $"{CusDir}/Customer_{customerName}.asset";
            var menu = AssetDatabase.LoadAssetAtPath<MenuRecipeSO>($"{MenuDir}/Menu_{menuCode}.asset");
            var so = LoadOrCreate<CraftCustomerSO>(path);
            so.customerName = customerName;
            so.orderLine = orderLine;
            so.diet = diet;
            so.cultureGroup = culture;
            so.requiredMenu = menu;
            EditorUtility.SetDirty(so);
        }

        private static void CreateThresholds()
        {
            CreateThreshold("Muslim_SPC", CultureGroup.Muslim, 0, "SPC");
            CreateThreshold("Muslim_HBF", CultureGroup.Muslim, 0, "HBF");
            CreateThreshold("Hindu_CUR", CultureGroup.Hindu, 30, "CUR");
            CreateThreshold("Vegan_BSP", CultureGroup.Vegan, 25, "BSP");
            CreateThreshold("Korean_PRK", CultureGroup.Korean, 40, "PRK");
            CreateThreshold("Korean_CHS", CultureGroup.Korean, 55, "CHS");
            CreateThreshold("SEAsian_SHR", CultureGroup.SEAsian, 35, "SHR");
        }

        private static void CreateThreshold(string id, CultureGroup culture, int threshold, string ingCode)
        {
            string path = $"{ThresholdDir}/Threshold_{id}.asset";
            var ing = AssetDatabase.LoadAssetAtPath<IngredientSO>($"{IngDir}/Ingredient_{ingCode}.asset");
            var so = LoadOrCreate<UnderstandingThresholdSO>(path);
            so.cultureGroup = culture;
            so.threshold = threshold;
            so.ingredientToUnlock = ing;
            EditorUtility.SetDirty(so);
        }

        private static void CreateNews()
        {
            CreateNews("Muslim_Positive", CultureGroup.Muslim, NewsSentiment.Positive, 1.2f,
                "할랄 푸드 열풍",
                "무슬림 친화 메뉴 수요가 크게 늘었습니다.",
                "도심 곳곳에서 할랄 인증 식당이 줄을 서고 있습니다. 현지 무슬림 커뮤니티는 '믿을 수 있는 식재료'를 가장 중요하게 꼽았으며, 돼지고기·알코올 미사용 메뉴에 대한 관심이 평소보다 높다고 전했습니다.",
                "오늘은 무슬림 손님의 방문이 늘 수 있습니다. 할랄 메뉴를 준비하면 호응이 좋을 것입니다.");
            CreateNews("Korean_Negative", CultureGroup.Korean, NewsSentiment.Negative, 0.85f,
                "물가 상승",
                "한식 재료 가격이 소폭 하락했습니다.",
                "농산물 도매 시장에서 배추·콩나물·두부 거래량이 늘면서 가격이 안정세를 보이고 있습니다. 한식 업체들은 원가 부담이 줄었다며 숨통을 트였지만, 소비자들은 여전히 '가성비 있는 한 끼'를 찾고 있다고 합니다.",
                "한식 메뉴 수요는 회복될 수 있지만, 가격에 민감한 손님이 많아지는 날입니다.");
            CreateNews("Discrimination", CultureGroup.AfricanAmerican, NewsSentiment.Discrimination, 0.7f,
                "차별 보도",
                "편견이 커지며 일부 손님이 가게를 기피합니다.",
                "한 인터넷 커뮤니티에 올라온 편견 섞인 게시글이 빠르게 퍼지며 논란이 커지고 있습니다. 일부 상인들은 '이웃을 대하는 태도가 곧 가게의 평판'이라며 차별 없는 영업을 당부했습니다. 시민단체는 편견 보도가 실제 소비에 영향을 준다고 경고했습니다.",
                "오늘은 분위기 영향으로 일부 손님이 줄어들 수 있습니다. 정확한 주문 처리가 더 중요합니다.",
                0.3f);
        }

        private static void CreateNews(string id, CultureGroup culture, NewsSentiment sentiment,
            float multiplier, string headline, string body, string article, string summary,
            float boycott = 0f)
        {
            string path = $"{NewsDir}/News_{id}.asset";
            var so = LoadOrCreate<NewsSO>(path);
            so.cultureGroup = culture;
            so.sentiment = sentiment;
            so.priceMultiplier = multiplier;
            so.headline = headline;
            so.body = body;
            so.article = article;
            so.summary = summary;
            so.boycottWeight = boycott;
            so.spawnWeight = 1f;
            EditorUtility.SetDirty(so);
        }

        private static void CreateDeliveryEvents()
        {
            CreateDelivery("Delay", DeliveryEventType.Delay, 15, 0f,
                "배달 지연", "교통 체증으로 재료가 늦게 도착했습니다.");
            CreateDelivery("Theft", DeliveryEventType.Theft, 0, 0.25f,
                "도난 사건", "배달 중 일부 재료가 도난당했습니다.");
            CreateDelivery("Accident", DeliveryEventType.Accident, 25, 0.15f,
                "배달 사고", "사고로 일부 상자가 파손되었습니다.");
        }

        private static void CreateDelivery(string id, DeliveryEventType type, int freshnessPenalty,
            float stockLoss, string headline, string body)
        {
            string path = $"{DeliveryDir}/Delivery_{id}.asset";
            var so = LoadOrCreate<DeliveryEventSO>(path);
            so.eventType = type;
            so.freshnessPenalty = freshnessPenalty;
            so.stockLossRatio = stockLoss;
            so.headline = headline;
            so.body = body;
            so.spawnWeight = 1f;
            EditorUtility.SetDirty(so);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var so = AssetDatabase.LoadAssetAtPath<T>(path);
            if (so != null) return so;
            so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static void BuildScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Craft";

            var camGo = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(camGo, scene);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
            cam.orthographic = true;
            cam.tag = "MainCamera";

            var bootGo = new GameObject("CraftSceneBootstrap");
            SceneManager.MoveGameObjectToScene(bootGo, scene);
            bootGo.AddComponent<ChangJun.Bootstrap.CraftSceneBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);

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
        }

        private static void EnsureDirectory(string dir)
        {
            if (AssetDatabase.IsValidFolder(dir)) return;
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
