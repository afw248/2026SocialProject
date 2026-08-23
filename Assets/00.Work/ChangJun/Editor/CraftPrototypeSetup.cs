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
        private const string StockDir = "Assets/Resources/Craft/Stocks";
        private const string DeliveryDir = "Assets/Resources/Craft/Delivery";
        private const string NodeDir = "Assets/Resources/Craft/UnderstandingNodes";
        private const string UpgradeDir = "Assets/Resources/Craft/Upgrades";
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
            EnsureDirectory($"{NewsDir}/Illustrations");
            EnsureDirectory(StockDir);
            EnsureDirectory(DeliveryDir);
            EnsureDirectory(NodeDir);
            EnsureDirectory(UpgradeDir);
            EnsureDirectory("Assets/Resources/Craft/Staff");
            EnsureDirectory("Assets/Resources/Craft/Conflicts");

            CreateDayConfig();
            CreateIngredients();
            CreateMenus();
            CreateCustomers();
            CreateThresholds();
            CreateUnderstandingNodes();
            CreateUpgrades();
            CreateStaff();
            CreateConflictEvents();
            CreateNews();
            CreateStocks();
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
            so.starterStockPerIngredient = 20;
            so.expressDeliveryMinutes = 30;
            so.economyDeliveryMinutes = 60;
            so.expressDeliveryPriceMultiplier = 2f;
            so.inflationIntervalDays = 7;
            so.inflationRatePerTick = 0.015f;
            so.dividendIntervalDays = 5;
            so.dividendRate = 0.005f;
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
            CreateIngredient("BSP", "콩나물", Diet.None, CultureGroup.Vegan, 20, 25, true);
            CreateIngredient("CHS", "치즈", Diet.Vegan, CultureGroup.Korean, 55, 90, false);
            CreateIngredient("SHR", "새우", Diet.Vegan, CultureGroup.SEAsian, 60, 110, false);
            CreateIngredient("PGD", "다진돼지고기", Diet.Halal | Diet.Vegan | Diet.Hindu, CultureGroup.Korean, 45, 75, false);

            SetIngredientFlags("TFU", fairTrade: true);
            SetIngredientFlags("KIM", localSourced: true);
            SetIngredientFlags("VEG", localSourced: true);
            SetIngredientFlags("BSP", fairTrade: true);

            // 제거된 재료 정리 (육수·마늘·계란고명)
            foreach (var code in new[] { "BRT", "GAR", "EGN" })
            {
                string path = $"{IngDir}/Ingredient_{code}.asset";
                if (AssetDatabase.LoadAssetAtPath<IngredientSO>(path) != null)
                    AssetDatabase.DeleteAsset(path);
            }
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

        private static void SetIngredientFlags(string code, bool fairTrade = false, bool localSourced = false)
        {
            var so = AssetDatabase.LoadAssetAtPath<IngredientSO>($"{IngDir}/Ingredient_{code}.asset");
            if (so == null) return;
            so.isFairTrade = fairTrade;
            so.isLocalSourced = localSourced;
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
            CreateMenu("M9", "제육 김치 컵밥", new[] { "PRK", "KIM" }, 350, CultureGroup.Korean);
            CreateMenu("M10", "치즈 김치 컵밥", new[] { "KIM", "CHS", "VEG" }, 450, CultureGroup.Korean);
            CreateMenu("M11", "담백 두부 채소 컵밥", new[] { "TFU", "BSP", "VEG" }, 420, CultureGroup.Vegan);
            CreateMenu("M12", "매운 해물 컵밥", new[] { "SHR", "SPC", "BSP" }, 480, CultureGroup.SEAsian);
            CreateMenu("M13", "고향식 커리 컵밥", new[] { "CUR", "CHK", "BSP" }, 550, CultureGroup.Hindu);
            CreateMenu("M14", "든든 삼겹 컵밥", new[] { "PRK", "EGG", "KIM" }, 500, CultureGroup.Korean);
            CreateMenu("M15", "콩나물 채소 컵밥", new[] { "BSP", "VEG" }, 280, CultureGroup.Vegan);
            CreateMenu("M16", "상생 컵밥", new[] { "TFU", "CUR", "VEG" }, 600, CultureGroup.Korean);
            CreateMenu("M18", "다진돼지 김치 컵밥", new[] { "PGD", "KIM" }, 380, CultureGroup.Korean);
            CreateMenu("M19", "야채 치킨 컵밥", new[] { "CHK", "VEG" }, 300, CultureGroup.Korean);

            CreateFusionMenu("M20", "할랄 김치 컵밥", new[] { "HBF", "KIM" }, 520, CultureGroup.Muslim);
            CreateFusionMenu("M21", "비건 커리 컵밥", new[] { "TFU", "CUR", "VEG" }, 480, CultureGroup.Vegan);
            CreateFusionMenu("M22", "매운 해산 컵밥", new[] { "SHR", "SPC", "VEG" }, 550, CultureGroup.SEAsian);
            CreateFusionMenu("M23", "상생 퓨전 컵밥", new[] { "TFU", "CUR", "BSP" }, 620, CultureGroup.Korean);
            CreateMenu("M24", "소울 두부 컵밥", new[] { "TFU", "VEG", "BSP" }, 360, CultureGroup.AfricanAmerican);
            CreateMenu("M25", "흑미 채소 컵밥", new[] { "VEG", "BSP", "TFU" }, 340, CultureGroup.AfricanAmerican);

            foreach (var code in new[] { "M17" })
            {
                string path = $"{MenuDir}/Menu_{code}.asset";
                if (AssetDatabase.LoadAssetAtPath<MenuRecipeSO>(path) != null)
                    AssetDatabase.DeleteAsset(path);
            }
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

        private static void CreateFusionMenu(string code, string displayName, string[] codes, int price, CultureGroup culture)
        {
            string path = $"{MenuDir}/Menu_{code}.asset";
            var so = LoadOrCreate<MenuRecipeSO>(path);
            so.code = code;
            so.displayName = displayName;
            so.ingredientCodes = codes;
            so.price = price;
            so.cultureGroup = culture;
            so.requiresFusionUnlock = true;
            EditorUtility.SetDirty(so);
        }

        private static void CreateCustomers()
        {
            CreateCustomer("아이샤", "안녕하세요… 저 돼지 안 돼요. 매운 고기 밥 있어요?", Diet.Halal, CultureGroup.Muslim, "M1");
            CreateCustomer("압둘라", "할랄 있어요? 매운 고기 밥 주세요.", Diet.Halal, CultureGroup.Muslim, "M1");
            CreateCustomer("응웬", "저기… 돈 많이 없어요. 밥이랑 계란… 싼 거 돼요?", Diet.None, CultureGroup.SEAsian, "M3");
            CreateCustomer("첸", "두부… 계란… 담백한 거 좋아요.", Diet.None, CultureGroup.Korean, "M4");
            CreateCustomer("김상철", "김치 계란 제육. 빨리요.", Diet.None, CultureGroup.Korean, "M14");
            CreateCustomer("박영자", "늘 먹던 김치 계란으로 줘요.", Diet.None, CultureGroup.Korean, "M5");
            CreateCustomer("미나", "커리 치킨? 그거 인기래요.", Diet.None, CultureGroup.Korean, "M6");
            CreateCustomer("라라", "제육에 김치… 든든한 거요. 힘든 날이에요.", Diet.None, CultureGroup.Korean, "M9");
            CreateCustomer("사라", "동물성은 다 빼주세요. 계란도, 젓갈도요. 김치 되나요?", Diet.Vegan, CultureGroup.Vegan, "M2");
            CreateCustomer("유코", "치즈랑… 새우? 그거 있어요? 인터넷에서 봤어요.", Diet.None, CultureGroup.Korean, "M8");
            CreateCustomer("데비", "소는… 안 돼요. 야채 매운 거 주세요.", Diet.Hindu, CultureGroup.Hindu, "M7");
            CreateCustomer("그린", "완전 채식이요. 두부, 콩나물, 야채. 깔끔하게.", Diet.Vegan, CultureGroup.Vegan, "M11");
            CreateCustomer("마르코", "치즈! 김치! 퓨전 좋아요. 야채도 넣어줘요.", Diet.None, CultureGroup.Korean, "M10");
            CreateCustomer("하산", "저… 돼지 없는 거… 고기 밥…", Diet.Halal, CultureGroup.Muslim, "M1");
            CreateCustomer("왕", "매운 거! 김치 계란 매운 거 좋아.", Diet.None, CultureGroup.Korean, "M5");
            CreateCustomer("로사", "콩나물이랑 야채요. 담백한 거.", Diet.Vegan, CultureGroup.Vegan, "M15");
            CreateCustomer("이수진", "다문화 상생 인증 매장이라 들었어요. 상생 컵밥 주세요.", Diet.None, CultureGroup.Korean, "M16");
            CreateCustomer("현우", "다진돼지에 김치! 그거 한 그릇요.", Diet.None, CultureGroup.Korean, "M18");
            CreateCustomer("나영", "닭고기에 야채 잔뜩. 가볍게 해주세요.", Diet.None, CultureGroup.Korean, "M19");

            CreateCustomer("마커스", "소울푸드 페스티벌에서 왔어요. 두부 든든한 거요.", Diet.Vegan, CultureGroup.AfricanAmerican, "M24");
            CreateCustomer("Jasmine", "흑미 채소밥… 건강한 거 좋아요.", Diet.Vegan, CultureGroup.AfricanAmerican, "M25");
            CreateCustomer("Tyler", "퓨전 좋아해요. 할랄이면서 김치? 그거요.", Diet.Halal, CultureGroup.AfricanAmerican, "M20");
            CreateCustomer("Keisha", "우리 동네 상생 캠페인 중이에요. 퓨전 컵밥 주세요.", Diet.None, CultureGroup.AfricanAmerican, "M23");

            CreateCustomer("준호", "학교 급식 견학 왔어요. 비건 커리 있나요?", Diet.Vegan, CultureGroup.Korean, "M21");
            CreateCustomer("Fatima", "할랄 김치밥… 신기하네요. 주문할게요.", Diet.Halal, CultureGroup.Muslim, "M20");
            CreateCustomer("Priya", "채식 커리 퓨전? 한번 먹어볼게요.", Diet.Hindu, CultureGroup.Hindu, "M21");
            CreateCustomer("Bao", "매운 해산물… 고향 같아요.", Diet.None, CultureGroup.SEAsian, "M22");
            CreateCustomer("Leo", "로컬 채소 든든한 밥이요.", Diet.Vegan, CultureGroup.Vegan, "M15");
            CreateCustomer("Darnell", "소울 두부밥, 따뜻하게요.", Diet.Vegan, CultureGroup.AfricanAmerican, "M24");

            ApplyCustomerSocial("응웬", "베트남에서 와 공장 일을 하는 손님. 한 끼 가격이 중요하다.",
                bargain: true, discount: 0.25f, accessible: false, null,
                Beat(2, 0, "어제 그 계란밥, 고향 아침이랑 비슷했어요."),
                Beat(4, 2, "송금하고 나면 점심값이 빠듯해서요. 조금만 깎아주면 내일도 올게요."));
            ApplyCustomerSocial("첸", "중국에서 와 한국에 정착한 손님. 한식을 배우며 산다.",
                false, 0.2f, false, null,
                Beat(2, 0, "한국 온 지 3년째예요. 두부는 익숙한데 김치는 아직…"),
                Beat(4, 2, "고향에선 아침마다 두부를 했어요. 여기 컵밥이 그 자리를 대신하네요."));
            ApplyCustomerSocial("왕", "중국 동포. 매운 한식을 좋아한다.",
                false, 0.2f, false, null,
                Beat(2, 0, "여기 김치 계란이 제일 익숙해요."));
            ApplyCustomerSocial("유코", "일본에서 유학 온 손님.",
                false, 0.2f, false, null,
                Beat(2, 0, "한국 컵밥에 치즈라니… 신기해서 또 왔어요."),
                Beat(4, 2, "기숙사에서 해 먹기 어려운 맛을 여기서 찾아요."));
            ApplyCustomerSocial("마르코", "이탈리아에서 온 교환학생.",
                false, 0.2f, false, null,
                Beat(2, 0, "김치랑 치즈가 의외로 잘 맞더라고요."));
            ApplyCustomerSocial("박영자", "동네에 오래 사신 손님. 거동이 느리다.",
                false, 0.2f, true, "천천히 해주시고, 글씨는 크게 보여주세요.",
                Beat(2, 0, "여기 사장님은 말을 천천히 해줘서 좋아요."),
                Beat(4, 2, "손주가 다문화 친구랑 논대요. 그 아이 도시락 생각이 나요."));
            ApplyCustomerSocial("아이샤", "한국에 온 지 얼마 안 된 무슬림 손님.",
                false, 0.2f, false, null,
                Beat(2, 0, "여기서는 돼지 없다고 해서 마음이 놓여요."),
                Beat(4, 2, "고국의 불고기 냄새가 나면서도, 안심하고 먹을 수 있어서요."));
            ApplyCustomerSocial("Bao", "동남아에서 온 손님. 해산물이 고향 맛.",
                true, 0.15f, false, null,
                Beat(2, 0, "매운 해산물… 정말 고향 같아요."));

            foreach (var name in new[] { "지우" })
            {
                string path = $"{CusDir}/Customer_{name}.asset";
                if (AssetDatabase.LoadAssetAtPath<CraftCustomerSO>(path) != null)
                    AssetDatabase.DeleteAsset(path);
            }
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

        private static RegularStoryBeat Beat(int visits, int affinity, string line)
        {
            return new RegularStoryBeat
            {
                requiredVisits = visits,
                requiredAffinity = affinity,
                line = line
            };
        }

        private static void ApplyCustomerSocial(string customerName, string originNote,
            bool bargain, float discount, bool accessible, string accessLine,
            params RegularStoryBeat[] beats)
        {
            string path = $"{CusDir}/Customer_{customerName}.asset";
            var so = AssetDatabase.LoadAssetAtPath<CraftCustomerSO>(path);
            if (so == null) return;
            so.originNote = originNote;
            so.canBargain = bargain;
            so.bargainDiscount = discount;
            so.needsAccessibleService = accessible;
            so.accessibleRequestLine = accessLine ?? "";
            so.storyBeats = beats;
            EditorUtility.SetDirty(so);
        }

        private static void CreateStaff()
        {
            const string dir = "Assets/Resources/Craft/Staff";
            CreateStaffAsset(dir, "amin", "아민", CultureGroup.Muslim, 200, 90, 0.25f,
                "돼지고기·알코올은 빼세요. 칼과 도마도 구분하는 게 좋아요.",
                "할랄 주방을 아는 조리원.");
            CreateStaffAsset(dir, "anand", "아난드", CultureGroup.Hindu, 200, 90, 0.25f,
                "소고기는 안 됩니다. 채식 커리면 안심하세요.",
                "남아시아 식문화를 아는 동료.");
            CreateStaffAsset(dir, "linh", "린", CultureGroup.SEAsian, 180, 80, 0.2f,
                "해산물 알레르기와 가격에 민감한 손님이 많아요.",
                "동남아 이주 노동자 커뮤니티와 가깝다.");
            CreateStaffAsset(dir, "michelle", "미셸", CultureGroup.AfricanAmerican, 200, 90, 0.2f,
                "소울푸드는 정성이에요. 대충 내면 바로 티 납니다.",
                "동네 상생 캠페인에서 만났다.");
        }

        private static void CreateStaffAsset(string dir, string id, string display, CultureGroup culture,
            int hire, int wage, float bonus, string hint, string bio)
        {
            string path = $"{dir}/Staff_{id}.asset";
            var so = LoadOrCreate<StaffSO>(path);
            so.staffId = id;
            so.displayName = display;
            so.cultureGroup = culture;
            so.hireCost = hire;
            so.dailyWage = wage;
            so.understandingBonus = bonus;
            so.tabooHint = hint;
            so.bio = bio;
            EditorUtility.SetDirty(so);
        }

        private static void CreateConflictEvents()
        {
            const string dir = "Assets/Resources/Craft/Conflicts";
            CreateConflictAsset(dir, "queue_halal", CultureGroup.Korean, CultureGroup.Muslim,
                "줄에서 한 손님이 다른 손님을 보며 말합니다.",
                "저 사람들 음식은 냄새도 이상하고, 따로 해야 하는 거 아니에요?",
                "개입: 타문화 손님이 안심하고, 이해도가 조금 오릅니다.",
                "방관: 편견이 가게 분위기가 됩니다. 해당 문화권 손님이 줄어듭니다.");
            CreateConflictAsset(dir, "queue_sea", CultureGroup.Korean, CultureGroup.SEAsian,
                "대기줄에서 볼멘소리가 들립니다.",
                "이주 노동자들 때문에 줄이 길어진다니까요.",
                "개입: 동남아 손님의 신뢰가 회복됩니다.",
                "방관: 동남아 손님 방문이 며칠 줄어듭니다.");
            CreateConflictAsset(dir, "queue_vegan", CultureGroup.Korean, CultureGroup.Vegan,
                "한 손님이 채식 주문 카드를 보고 코웃음 칩니다.",
                "고기도 안 먹고 유난이네. 밥이 음식이면 됐지.",
                "개입: 채식 손님이 이 가게를 안전한 곳으로 기억합니다.",
                "방관: 채식 손님 발길이 뜸해집니다.");
        }

        private static void CreateConflictAsset(string dir, string id, CultureGroup speaker, CultureGroup target,
            string prompt, string line, string interveneNote, string ignoreNote)
        {
            string path = $"{dir}/Conflict_{id}.asset";
            var so = LoadOrCreate<ConflictEventSO>(path);
            so.eventId = id;
            so.speakerCulture = speaker;
            so.targetCulture = target;
            so.prompt = prompt;
            so.prejudiceLine = line;
            so.interveneLabel = "개입한다";
            so.ignoreLabel = "모른 척한다";
            so.interveneNote = interveneNote;
            so.ignoreNote = ignoreNote;
            EditorUtility.SetDirty(so);
        }

        private static void CreateThresholds()
        {
            CreateThreshold("Muslim_SPC", CultureGroup.Muslim, 0, "SPC");
            CreateThreshold("Muslim_HBF", CultureGroup.Muslim, 0, "HBF");
            CreateThreshold("Hindu_CUR", CultureGroup.Hindu, 30, "CUR");
            CreateThreshold("Vegan_BSP", CultureGroup.Vegan, 25, "BSP");
            CreateThreshold("Korean_PRK", CultureGroup.Korean, 40, "PRK");
            CreateThreshold("Korean_PGD", CultureGroup.Korean, 45, "PGD");
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

        private static void CreateUnderstandingNodes()
        {
            CreateNode("KOR_1", CultureGroup.Korean, UnderstandingNodeType.Milestone, 0, null,
                "한식 기본", "한국 손님의 식사 문화를 익히기 시작합니다.", 0, null);
            CreateNode("KOR_2", CultureGroup.Korean, UnderstandingNodeType.Milestone, 20, new[] { "KOR_1" },
                "가성비 이해", "저렴하지만 정성스러운 한 끼를 선호합니다.", 1, null);
            CreateNode("KOR_3", CultureGroup.Korean, UnderstandingNodeType.IngredientUnlock, 40, new[] { "KOR_2" },
                "제육·김치", "한식 핵심 재료를 다룰 수 있습니다.", 2, "PRK");
            CreateNode("KOR_4", CultureGroup.Korean, UnderstandingNodeType.Certification, 60, new[] { "KOR_3" },
                "다문화 상생", "다양한 손님을 함께 맞이할 준비가 됩니다.", 3, null);
            CreateNode("KOR_5", CultureGroup.Korean, UnderstandingNodeType.EventUnlock, 80, new[] { "KOR_4" },
                "문화 축제", "한식 문화 축제 이벤트 조건을 충족합니다.", 4, null);
            CreateNode("KOR_6", CultureGroup.Korean, UnderstandingNodeType.Fusion, 100, new[] { "KOR_5" },
                "완전 이해", "한식 메뉴 영구 +5% 보너스.", 5, null);

            CreateNode("MUS_1", CultureGroup.Muslim, UnderstandingNodeType.Milestone, 0, null,
                "할랄 기본", "무슬림 손님의 식습관을 배웁니다.", 0, null);
            CreateNode("MUS_2", CultureGroup.Muslim, UnderstandingNodeType.Milestone, 20, new[] { "MUS_1" },
                "금기 이해", "돼지·알코올 회피가 왜 중요한지 압니다.", 1, null);
            CreateNode("MUS_3", CultureGroup.Muslim, UnderstandingNodeType.IngredientUnlock, 40, new[] { "MUS_2" },
                "할랄 재료", "SPC·HBF 재료를 사용할 수 있습니다.", 2, "SPC");
            CreateNode("MUS_4", CultureGroup.Muslim, UnderstandingNodeType.Certification, 60, new[] { "MUS_3" },
                "할랄 키친", "교차오염 방지 인증 준비.", 3, null);
            CreateNode("MUS_5", CultureGroup.Muslim, UnderstandingNodeType.EventUnlock, 80, new[] { "MUS_4" },
                "문화 축제", "무슬림 문화 축제 조건.", 4, null);
            CreateNode("MUS_6", CultureGroup.Muslim, UnderstandingNodeType.Fusion, 100, new[] { "MUS_5" },
                "완전 이해", "할랄 메뉴 +5% 보너스.", 5, null);

            CreateNode("HIN_1", CultureGroup.Hindu, UnderstandingNodeType.Milestone, 0, null,
                "채식 문화", "힌두·남아시아 채식 문화를 배웁니다.", 0, null);
            CreateNode("HIN_2", CultureGroup.Hindu, UnderstandingNodeType.Milestone, 20, new[] { "HIN_1" },
                "소의 의미", "소고기 금기의 문화적 배경.", 1, null);
            CreateNode("HIN_3", CultureGroup.Hindu, UnderstandingNodeType.IngredientUnlock, 40, new[] { "HIN_2" },
                "커리 향신료", "CUR 재료 해금.", 2, "CUR");
            CreateNode("HIN_4", CultureGroup.Hindu, UnderstandingNodeType.Certification, 60, new[] { "HIN_3" },
                "채식 존중", "채식 손님을 위한 조리 습관.", 3, null);
            CreateNode("HIN_5", CultureGroup.Hindu, UnderstandingNodeType.EventUnlock, 80, new[] { "HIN_4" },
                "문화 축제", "힌두 문화 축제 조건.", 4, null);
            CreateNode("HIN_6", CultureGroup.Hindu, UnderstandingNodeType.Fusion, 100, new[] { "HIN_5" },
                "완전 이해", "채식·커리 메뉴 +5%.", 5, null);

            CreateNode("VEG_1", CultureGroup.Vegan, UnderstandingNodeType.Milestone, 0, null,
                "비건 기본", "완전 채식의 의미.", 0, null);
            CreateNode("VEG_2", CultureGroup.Vegan, UnderstandingNodeType.Milestone, 20, new[] { "VEG_1" },
                "동물성 회피", "계란·유제품·육류 성분 확인.", 1, null);
            CreateNode("VEG_3", CultureGroup.Vegan, UnderstandingNodeType.IngredientUnlock, 40, new[] { "VEG_2" },
                "콩나물·두부", "BSP 재료 해금.", 2, "BSP");
            CreateNode("VEG_4", CultureGroup.Vegan, UnderstandingNodeType.Certification, 60, new[] { "VEG_3" },
                "비건 존", "비건 전용 조리 구역.", 3, null);
            CreateNode("VEG_5", CultureGroup.Vegan, UnderstandingNodeType.EventUnlock, 80, new[] { "VEG_4" },
                "문화 축제", "비건 문화 축제.", 4, null);
            CreateNode("VEG_6", CultureGroup.Vegan, UnderstandingNodeType.Fusion, 100, new[] { "VEG_5" },
                "완전 이해", "비건 메뉴 +5%.", 5, null);

            CreateNode("SEA_1", CultureGroup.SEAsian, UnderstandingNodeType.Milestone, 0, null,
                "동남아 기본", "이주민·향신료 문화.", 0, null);
            CreateNode("SEA_2", CultureGroup.SEAsian, UnderstandingNodeType.Milestone, 20, new[] { "SEA_1" },
                "고향의 맛", "저렴하고 든든한 한 끼.", 1, null);
            CreateNode("SEA_3", CultureGroup.SEAsian, UnderstandingNodeType.IngredientUnlock, 40, new[] { "SEA_2" },
                "해산물", "SHR 재료 해금.", 2, "SHR");
            CreateNode("SEA_4", CultureGroup.SEAsian, UnderstandingNodeType.Certification, 60, new[] { "SEA_3" },
                "공정 공급", "노동·공급망 존중.", 3, null);
            CreateNode("SEA_5", CultureGroup.SEAsian, UnderstandingNodeType.EventUnlock, 80, new[] { "SEA_4" },
                "문화 축제", "동남아 축제.", 4, null);
            CreateNode("SEA_6", CultureGroup.SEAsian, UnderstandingNodeType.Fusion, 100, new[] { "SEA_5" },
                "완전 이해", "동남아 메뉴 +5%.", 5, null);

            CreateNode("AA_1", CultureGroup.AfricanAmerican, UnderstandingNodeType.Milestone, 0, null,
                "소울푸드", "흑인 디아스포라 식문화.", 0, null);
            CreateNode("AA_2", CultureGroup.AfricanAmerican, UnderstandingNodeType.Milestone, 20, new[] { "AA_1" },
                "연대의 식탁", "편견 없는 응대.", 1, null);
            CreateNode("AA_3", CultureGroup.AfricanAmerican, UnderstandingNodeType.IngredientUnlock, 40, new[] { "AA_2" },
                "퓨전 재료", "다문화 재료 조합.", 2, "TFU");
            CreateNode("AA_4", CultureGroup.AfricanAmerican, UnderstandingNodeType.Certification, 60, new[] { "AA_3" },
                "상생 배지", "다문화 상생 인증.", 3, null);
            CreateNode("AA_5", CultureGroup.AfricanAmerican, UnderstandingNodeType.EventUnlock, 80, new[] { "AA_4" },
                "문화 축제", "소울푸드·UNITY 축제.", 4, null);
            CreateNode("AA_6", CultureGroup.AfricanAmerican, UnderstandingNodeType.Fusion, 100, new[] { "AA_5" },
                "완전 이해", "소울·퓨전 메뉴 +5%.", 5, null);
        }

        private static void CreateNode(string id, CultureGroup culture, UnderstandingNodeType type,
            int required, string[] prereqs, string title, string desc, int row, string ingCode)
        {
            string path = $"{NodeDir}/Node_{id}.asset";
            var so = LoadOrCreate<UnderstandingNodeSO>(path);
            so.nodeId = id;
            so.cultureGroup = culture;
            so.nodeType = type;
            so.requiredUnderstanding = required;
            so.prerequisiteNodeIds = prereqs ?? System.Array.Empty<string>();
            so.displayName = title;
            so.description = desc;
            so.gridRow = row;
            if (!string.IsNullOrEmpty(ingCode))
                so.ingredientToUnlock = AssetDatabase.LoadAssetAtPath<IngredientSO>($"{IngDir}/Ingredient_{ingCode}.asset");
            EditorUtility.SetDirty(so);
        }

        private static void CreateUpgrades()
        {
            CreateUpgrade(ShopUpgradeType.HalalKitchen, "할랄 키친 인증", CultureGroup.Muslim,
                600, 0.25f, 0.15f, "교차오염 위험 감소, 무슬림 손님 증가.");
            CreateUpgrade(ShopUpgradeType.VeganZone, "비건 조리 존", CultureGroup.Vegan,
                550, 0.2f, 0.12f, "금기 패널티 완화, 비건 손님 증가.");
            CreateUpgrade(ShopUpgradeType.MulticultureBadge, "다문화 상생 배지", CultureGroup.Korean,
                700, 0.15f, 0.08f, "전 문화권 손님 소폭 증가, 평판 상승.");
        }

        private static void CreateUpgrade(ShopUpgradeType type, string name, CultureGroup culture,
            int cost, float tabooReduce, float spawnBoost, string desc)
        {
            string path = $"{UpgradeDir}/Upgrade_{type}.asset";
            var so = LoadOrCreate<ShopUpgradeSO>(path);
            so.upgradeType = type;
            so.displayName = name;
            so.description = desc;
            so.purchaseCost = cost;
            so.cultureGroup = culture;
            so.tabooPenaltyReduction = tabooReduce;
            so.spawnBoost = spawnBoost;
            EditorUtility.SetDirty(so);
        }

        private static void CreateNews()
        {
            CreateNews("Muslim_Positive", CultureGroup.Muslim, NewsSentiment.Positive, 1.2f,
                "할랄", "할랄 푸드 열풍, 도심 식당 줄서기",
                "무슬림 친화 메뉴 수요가 크게 늘었습니다. '믿을 수 있는 식재료'가 소비의 핵심입니다.",
                "도심 곳곳에서 할랄 인증 식당이 줄을 서고 있습니다. 현지 무슬림 커뮤니티는 돼지고기·알코올 미사용 메뉴를 가장 중요하게 꼽았으며, 학교 급식과 기업 구내식당까지 할랄 옵션 도입 논의가 확산되고 있습니다.\n\n" +
                "전문가들은 할랄이 단순한 종교적 규범을 넘어 '식품 안전·공급망 투명성'의 문제이기도 하다고 설명합니다. 일부 지역에서는 할랄 인증 과정을 투어로 열어 이해를 돕는 프로그램도 인기를 끌고 있습니다.\n\n" +
                "한 식당 주인은 \"손님이 무엇을 먹지 못하는지 아는 것이 예의\"라며, 재료 표시와 교차오염 방지에 힘쓰겠다고 밝혔습니다.",
                "종교적 식습관을 이해하는 것은 다문화 사회의 기본 소양입니다.",
                "오늘은 무슬림 손님의 방문이 늘 수 있습니다. 할랄 메뉴를 준비하면 호응이 좋을 것입니다.",
                "HLAL", 1.2f);

            CreateNews("Korean_Negative", CultureGroup.Korean, NewsSentiment.Negative, 0.85f,
                "경제", "물가 안정 속 한식 업계 '가성비' 경쟁",
                "농산물 가격이 안정되며 한식 재료 비용 부담이 줄었습니다.",
                "농산물 도매 시장에서 배추·콩나물·두부 거래량이 늘면서 가격이 안정세를 보이고 있습니다. 한식 업체들은 원가 부담이 줄었다며 숨통을 트였지만, 소비자들은 여전히 '가성비 있는 한 끼'를 찾고 있다고 합니다.\n\n" +
                "반면 외식 물가 전반은 여전히 높아, 젊은 층 사이에서는 컵밥·분식 같은 소형 메뉴가 다시 주목받고 있습니다. 전문가는 \"저가가 곧 품질 저하를 의미하지 않는다\"며, 효율적인 운영과 메뉴 구성이 중요하다고 조언했습니다.",
                "가격 경쟁 속에서도 정성은 줄일 수 없습니다.",
                "한식 메뉴 수요는 회복될 수 있지만, 가격에 민감한 손님이 많아지는 날입니다.",
                "KFOOD", 1f);

            CreateNews("Hindu_Curry", CultureGroup.Hindu, NewsSentiment.Positive, 1.15f,
                "문화", "채식 커리 열풍, '소를 소중히' 메시지 확산",
                "인도·남아시아 채식 문화에 대한 관심이 커지고 있습니다.",
                "힌두교에서 소는 신성한 동물로 여겨지며, 많은 힌두교 신자는 채식을 실천합니다. 최근 도시에서는 커리 향신료와 채소 위주의 메뉴가 건강식으로 소개되며 인기를 얻고 있습니다.\n\n" +
                "현지 커뮤니티 리더는 \"소고기를 피하는 것은 종교만의 문제가 아니라 환경·동물복지와도 연결된다\"고 설명했습니다. 일부 식당은 메뉴판에 '소고기 미사용'을 명시해 신뢰를 쌓고 있습니다.",
                "다른 문화의 식생활 금기를 알아주는 것이 편견을 줄이는 첫걸음입니다.",
                "채식·커리 메뉴에 관심이 몰릴 수 있습니다. 소고기가 들어간 메뉴는 주의하세요.",
                "CURRY", 1.1f);

            CreateNews("Vegan_Rise", CultureGroup.Vegan, NewsSentiment.Positive, 1.18f,
                "라이프", "비건 인구 증가, '완전 채식' 수요 확대",
                "동물성 원료를 배제한 메뉴에 대한 수요가 꾸준히 늘고 있습니다.",
                "비건은 단순히 채식을 넘어 난·우유·꿀·젤라틴 등 동물 유래 성분까지 피하는 생활 방식입니다. SNS에서는 비건 인증 마크와 레시피 공유가 활발하며, 식당들도 교차오염 방지를 강조하고 있습니다.\n\n" +
                "영양학자는 \"비건 메뉴는 단백질·비타민B12 보충 설계가 중요하다\"고 덧붙였습니다. 두부·콩·견과류를 활용한 메뉴가 대안으로 떠오르고 있습니다.",
                "식단 선택은 개인의 신념이자 건강 문제이기도 합니다.",
                "완전 채식 손님이 늘 수 있습니다. 계란·치즈·다진고기 성분을 꼼꼼히 확인하세요.",
                "VGND", 1.15f);

            CreateNews("SEAsian_Fusion", CultureGroup.SEAsian, NewsSentiment.Positive, 1.1f,
                "문화", "동남아 이주민 커뮤니티, '고향의 맛' 공유 행사",
                "저렴하고 든든한 한 끼에 대한 관심이 이주민·현지인 모두에게 커지고 있습니다.",
                "동남아 출신 노동자와 학생들이 모인 지역에서 '고향 밥상' 나눔 행사가 열렸습니다. 향신료와 해산물, 쌀 위주의 메뉴가 소개됐으며, 참가자들은 \"맛이 곧 기억이자 정체성\"이라고 말했습니다.\n\n" +
                "일부 상인은 동남아 손님이 많은 시간대에 맞춰 메뉴를 조정하고, 가격대를 낮춘 세트를 준비하기도 합니다.",
                "이주민의 식문화는 우리 동네 경제와도 연결됩니다.",
                "담백·저가 메뉴 수요가 늘 수 있습니다. 해산물 알레르기도 함께 확인하세요.",
                "SEAFO", 1f);

            CreateNews("Multiculture_Unity", CultureGroup.Korean, NewsSentiment.Positive, 1.25f,
                "사회", "다문화 상생 인증 매장 확대",
                "문화 이해를 실천하는 식당이 시범 사례로 소개됐습니다.",
                "지자체와 시민단체가 참여한 '다문화 상생 인증' 제도가 본격화되고 있습니다. 인증 매장은 메뉴 설명, 종교·식이 금기 안내, 직원 교육 등을 기준으로 평가받습니다.\n\n" +
                "한 참여 상인은 \"손님의 이름과 식습관을 기억하는 것이 매출보다 먼저\"라고 말해 화제가 됐습니다. 전문가들은 음식점이 통합사회 교육의 현장이 될 수 있다고 평가합니다.",
                "다양성은 장식이 아니라 공존의 조건입니다.",
                "다양한 문화권 손님이 함께 찾아올 수 있는 날입니다. 정확한 주문 처리가 평판을 만듭니다.",
                "UNITY", 1.3f);

            CreateNews("Discrimination", CultureGroup.AfricanAmerican, NewsSentiment.Discrimination, 0.7f,
                "사회", "편견 게시글 확산, 일부 상권 소비 위축",
                "인터넷 커뮤니티의 차별적 게시글이 논란을 일으켰습니다.",
                "한 인터넷 커뮤니티에 올라온 편견 섞인 게시글이 빠르게 퍼지며 논란이 커지고 있습니다. 일부 상인들은 '이웃을 대하는 태도가 곧 가게의 평판'이라며 차별 없는 영업을 당부했습니다.\n\n" +
                "시민단체는 편견 보도가 실제 소비에 영향을 준다고 경고했습니다. 학교에서는 '미디어 리터러시' 수업에서 이번 사건을 사례로 다루기도 했습니다.\n\n" +
                "사회학자는 \"익명성 뒤에 숨은 혐오가 경제 활동까지 침투한다\"며, 공공 담론의 책임을 강조했습니다.",
                "편견은 개인의 문제가 아니라 사회 구조의 문제이기도 합니다.",
                "오늘은 분위기 영향으로 일부 손님이 줄어들 수 있습니다. 정확한 주문 처리가 더 중요합니다.",
                "UNITY", 0.8f, 0.3f);

            CreateNews("Halal_Scandal_False", CultureGroup.Muslim, NewsSentiment.Negative, 0.9f,
                "속보", "가짜 할랄 인증 논란, 소비자 불안 확대",
                "일부 업체의 허위 인증 의혹이 제기됐습니다.",
                "할랄 인증 마크를 무단으로 사용했다는 제보가 이어지며 소비자 불안이 커지고 있습니다. 진짜 인증과 가짜를 구분하기 어렵다는 목소리도 나옵니다.\n\n" +
                "무슬림 커뮤니티는 \"신뢰가 한 번 무너지면 회복이 어렵다\"고 강조했습니다. 정부는 할랄 인증 정보를 한눈에 볼 수 있는 포털 구축을 검토 중입니다.",
                "문화적 신뢰는 라벨 한 장으로 완성되지 않습니다.",
                "무슬림 손님이 신중해질 수 있습니다. 재료 출처를 명확히 안내하면 도움이 됩니다.",
                "HLAL", 0.9f);

            CreateNews("School_Food_Edu", CultureGroup.Korean, NewsSentiment.Positive, 1.08f,
                "교육", "통합사회 수업, '식탁에서 배우는 문화' 주목",
                "학교에서 식문화를 주제로 한 프로젝트 수업이 확산되고 있습니다.",
                "전국 여러 학교에서 '식탁에서 배우는 문화' 프로젝트가 진행되고 있습니다. 학생들은 종교·식이·지역별 음식 차이를 조사하고, 가게를 방문해 인터뷰하기도 합니다.\n\n" +
                "교사들은 \"교과서의 다문화가 실제 손님을 만나야 살아난다\"고 말합니다. 일부 학생들은 가족 식단과 학교 급식의 차이를 발표하며 공감대를 형성했습니다.",
                "음식은 문화를 가르치는 가장 부드러운 매개체입니다.",
                "젊은 손님과 학부모의 관심이 높아질 수 있습니다.",
                "KFOOD", 1f);

            CreateNews("Muslim_Travel_Ban", CultureGroup.Muslim, NewsSentiment.Negative, 0.88f,
                "국제", "입국 심사 강화 논란, 할랄 상권도 긴장",
                "여행·체류 규제가 강화되며 무슬림 방문객·상권이 움츠러들고 있습니다.",
                "일부 국가의 입국·체류 심사 강화 소식이 전해지며 현지 무슬림 커뮤니티와 할랄 외식업계가 긴장하고 있습니다. 관광·유학 수요가 줄어들면 할랄 식당·식재료 유통도 타격을 받을 수 있다는 전망이 나옵니다.\n\n" +
                "시민단체는 \"정책과 무관한 이웃까지 편견으로 묶어선 안 된다\"고 경고했습니다. 상인들은 단골 손님과의 신뢰를 지키며 차분히 영업하겠다고 밝혔습니다.",
                "정책 이슈가 식탁의 편견으로 번지지 않도록 주의가 필요합니다.",
                "무슬림 손님 방문이 줄 수 있습니다. 재료 안내를 더 분명히 하면 도움이 됩니다.",
                "HLAL", 0.95f);

            CreateNews("Hindu_Temple_Dispute", CultureGroup.Hindu, NewsSentiment.Negative, 0.82f,
                "사회", "사원 인근 상권 갈등, 채식 거리도 한산",
                "지역 갈등 소식으로 힌두·남아시아 식문화 상권이 위축됐습니다.",
                "사원 인근 개발·소음 문제를 둘러싼 갈등이 보도되며 인근 채식·커리 거리가 한산해졌습니다. 상인들은 \"종교 시설과 상권이 함께 성장해 온 동네\"라며 조속한 대화를 촉구했습니다.\n\n" +
                "커뮤니티 리더는 \"갈등을 문화 차별로 몰아가면 안 된다\"고 강조했습니다. 일부 손님은 온라인으로만 주문하며 현장을 피하고 있습니다.",
                "갈등의 본질과 문화를 분리해 읽는 눈이 필요합니다.",
                "힌두·채식 메뉴 수요가 줄 수 있습니다. 소고기 메뉴는 특히 신중히 다루세요.",
                "CURRY", 0.9f);

            CreateNews("Vegan_Greenwash", CultureGroup.Vegan, NewsSentiment.Negative, 0.86f,
                "소비", "비건 라벨 과장 광고 논란 확산",
                "동물성 원료가 검출된 '비건' 제품 사례가 잇따르고 있습니다.",
                "시중 일부 '비건' 표기 제품에서 동물성 성분이 검출됐다는 조사 결과가 발표되며 소비자 불신이 커지고 있습니다. SNS에서는 그린워싱 비판이 이어졌고, 비건 식당도 교차오염·성분 표기를 재점검하고 있습니다.\n\n" +
                "업계 관계자는 \"라벨보다 조리 과정 투명성이 중요하다\"고 말했습니다. 완전 채식을 지키는 손님일수록 성분 확인을 더 까다롭게 요구할 전망입니다.",
                "신념을 존중하는 식사는 표시보다 실천에서 드러납니다.",
                "비건 손님이 신중해질 수 있습니다. 계란·유제품·육류 교차오염을 철저히 확인하세요.",
                "VGND", 0.95f);

            CreateNews("SEAsian_Labor_Strike", CultureGroup.SEAsian, NewsSentiment.Negative, 0.84f,
                "노동", "식품 가공장 파업, 동남아 식재료 수급 불안",
                "임금·처우 개선을 요구하는 파업으로 일부 향신료·해산물 공급이 지연되고 있습니다.",
                "동남아 계열 식품 가공장에서 처우 개선을 요구하는 파업이 이어지며 향신료·해산물 가공품 출하가 늦어지고 있습니다. 소규모 식당들은 대체 거래처를 찾는 중입니다.\n\n" +
                "이주민 노동자 단체는 \"식탁의 풍요 뒤에는 노동이 있다\"고 강조했습니다. 전문가들은 단기 가격 변동과 함께 장기적으로는 공정 공급망 논의가 필요하다고 지적합니다.",
                "값싼 한 끼의 뒤에는 누군가의 노동이 있습니다.",
                "동남아·해산물 메뉴 재료비가 흔들릴 수 있습니다. 재고를 점검하세요.",
                "SEAFO", 0.9f);

            CreateNews("SoulFood_Fest", CultureGroup.AfricanAmerican, NewsSentiment.Positive, 1.22f,
                "문화", "소울푸드 페스티벌, 상생 상권 '활기'",
                "흑인 디아스포라 식문화를 조명하는 축제가 도심을 달궜습니다.",
                "시내 광장에서 열린 소울푸드 페스티벌에 시민과 관광객이 몰렸습니다. 프라이드 치킨·검보·콜라도 그린스 등 메뉴가 소개됐고, 참가자들은 음식과 음악·역사를 함께 경험했습니다.\n\n" +
                "주최 측은 \"맛으로 만나는 연대\"를 내세웠으며, 인근 상권도 방문객 증가로 활기를 띠었습니다. 다문화 상생 지수에도 긍정적 신호가 관측됩니다.",
                "축제는 차이를 무대로, 공존을 일상으로 만듭니다.",
                "다양한 문화권 손님이 늘 수 있습니다. 따뜻하고 정확한 응대가 평판을 만듭니다.",
                "UNITY", 1.2f);

            CreateNews("Korean_Harvest_Fest", CultureGroup.Korean, NewsSentiment.Positive, 1.12f,
                "생활", "추석 맞이 한식 나눔·급식 특식 확대",
                "명절을 앞두고 송편·전·나물 등 한식 수요가 크게 늘었습니다.",
                "추석을 앞두고 학교 급식과 지역 나눔 행사에서 한식 특식이 늘고 있습니다. 송편·잡채·나물 등 명절 메뉴에 대한 관심이 높아지며 전통 시장 거래량도 증가했습니다.\n\n" +
                "영양사들은 \"명절 음식도 알레르기·종교 식이를 함께 고려해야 한다\"고 조언했습니다. 한식 재료 관련 지수에도 온기가 돌고 있습니다.",
                "명절 밥상은 가족과 이웃을 잇는 다리입니다.",
                "한식 메뉴 수요가 살아날 수 있습니다. 가성비와 정성을 함께 챙기세요.",
                "KFOOD", 1.1f);

            CreateNews("Halal_Kitchen_Edu", CultureGroup.Muslim, NewsSentiment.Positive, 1.14f,
                "교육", "할랄 키친 교실, 식당 사장님도 수강",
                "교차오염 방지·재료 구분을 배우는 실습 교육이 인기입니다.",
                "지자체와 이슬람 문화센터가 함께하는 '할랄 키친' 교실에 식당 운영자와 조리 전공 학생이 몰리고 있습니다. 돼지고기·알코올 미사용뿐 아니라 도마·칼 분리, 보관 구역 표시까지 실습합니다.\n\n" +
                "수강생들은 \"손님의 믿음을 지키는 기술\"이라고 평가했습니다. 할랄 외식 인증을 준비하는 매장도 늘어나는 추세입니다.",
                "이해는 안내문보다 주방의 습관에서 완성됩니다.",
                "할랄·무슬림 친화 메뉴에 관심이 몰릴 수 있습니다.",
                "HLAL", 1.1f);

            CreateNews("Seafood_Shortage", CultureGroup.SEAsian, NewsSentiment.Negative, 0.8f,
                "경제", "해산물 어획량 감소, 가공·외식 업계 비상",
                "수급 불안정으로 동남아 해산물 메뉴 원가가 요동치고 있습니다.",
                "이상 기후와 조업 제한 여파로 일부 해산물 어획량이 줄며 가공·유통 가격이 상승했습니다. 동남아 퓨전 식당들은 메뉴 구성을 조정하거나 대체 식재료를 검토 중입니다.\n\n" +
                "수산업 관계자는 \"단기 급등락보다 안정적 공급망이 과제\"라고 말했습니다. 소비자들은 가격 부담을 호소하며 내륙 식재료 메뉴로 이동하는 모습도 보입니다.",
                "바다의 변화는 식탁의 가격표에도 나타납니다.",
                "해산물 메뉴 원가 부담이 커질 수 있습니다. 대체 메뉴를 준비하세요.",
                "SEAFO", 0.85f);

            CreateNews("Unity_City_Campaign", CultureGroup.AfricanAmerican, NewsSentiment.Positive, 1.16f,
                "사회", "도시 캠페인 '한 식탁의 이웃', 상생 소비 확산",
                "편견 없는 외식·쇼핑을 독려하는 시민 캠페인이 확산되고 있습니다.",
                "시민단체와 지자체가 함께하는 '한 식탁의 이웃' 캠페인이 도시 전역으로 퍼지고 있습니다. 참여 매장은 차별 없는 응대 서약과 다문화 메뉴 안내를 게시하며, 방문객에게 스탬프 투어를 제공합니다.\n\n" +
                "초기 집계에 따르면 참여 상권의 주말 매출이 소폭 상승했으며, 다문화 상생 관련 지수에도 긍정 신호가 포착됐습니다.",
                "캠페인은 구호가 아니라 손님 한 명을 대하는 태도에서 시작됩니다.",
                "다양한 손님이 함께 찾아올 수 있는 날입니다. 정확한 주문이 신뢰를 쌓습니다.",
                "UNITY", 1.15f);

            CreateNews("Health_Sugar_Warning", CultureGroup.Vegan, NewsSentiment.Negative, 0.92f,
                "건강", "고당도 간식 규제 논의, '가볍게' 메뉴 선호",
                "학교·보건 당국이 고당도 간식 섭취를 줄이자는 캠페인을 벌이고 있습니다.",
                "전국 학교와 보건소가 '가벼운 한 끼'를 권장하는 캠페인을 펼치고 있습니다. 채소·두부·콩 위주의 메뉴가 급식·외식 모두에서 주목받고 있습니다.\n\n" +
                "영양 교사들은 \"건강은 개인 선택이지만 공공 정책과도 연결된다\"고 설명했습니다.",
                "건강한 식습관은 사회적 약속이기도 합니다.",
                "담백·채식 메뉴 수요가 늘 수 있습니다.",
                "VGND", 1.05f);

            CreateNews("Local_Farm_Bonus", CultureGroup.Korean, NewsSentiment.Positive, 1.1f,
                "로컬", "로컬 농가 직거래 확대, 김치·채소 값싸게",
                "지역 농가와 식당을 잇는 직거래가 늘며 로컬 재료 가격이 안정됐습니다.",
                "시·군 농협이 로컬 직거래 플랫폼을 확대하면서 김치·채소류 공급가가 소폭 내렸습니다. 소비자들은 '동네에서 난 재료'에 대한 관심도 높아지고 있습니다.\n\n" +
                "참여 농가는 \"짧은 유통이 신선함과 가격 모두에 이롭다\"고 말했습니다.",
                "로컬 식재료는 지속가능성과 지역 경제를 동시에 살립니다.",
                "로컬 태그 재료 구매 시 비용 이점이 있습니다.",
                "KFOOD", 1.12f);

            CreateNews("Community_Meal_Day", CultureGroup.Korean, NewsSentiment.Positive, 1.08f,
                "나눔", "커뮤니티 밥상의 날, 창고 나눔 캠페인",
                "식당·시민이 남는 재료를 이웃과 나누는 '커뮤니티 밥상' 행사가 확산됩니다.",
                "지역 복지관과 연계한 '커뮤니티 밥상' 캠페인이 주말을 앞두고 화제입니다. 식당은 창고에 남은 재료를 기부하고, 이웃 식당과 연대하는 사례도 늘고 있습니다.\n\n" +
                "사회복지사는 \"나눔은 불평등을 줄이는 작은 실천\"이라고 말했습니다.",
                "음식 나눔은 통합사회의 따뜻한 실험입니다.",
                "정산 시 커뮤니티 밥상을 선택하면 상생 지수가 오릅니다.",
                "UNITY", 1.1f);

            CreateNews("Halal_Cert_Boon", CultureGroup.Muslim, NewsSentiment.Positive, 1.18f,
                "인증", "할랄 인증 매장, 소비자 신뢰 '최고'",
                "정부 인증 할랄 매장에 대한 소비자 신뢰 조사 결과가 발표됐습니다.",
                "할랄 인증을 받은 매장이 비인증 대비 재방문율이 높다는 조사가 공개됐습니다. 무슬림뿐 아니라 일반 소비자도 '투명한 주방'을 중요하게 여긴다고 답했습니다.\n\n" +
                "업계는 인증 교육 수요도 함께 늘고 있다고 전했습니다.",
                "인증은 차별이 아니라 신뢰의 언어입니다.",
                "할랄 키친 업그레이드와 궁합이 좋은 날입니다.",
                "HLAL", 1.2f);

            CreateNews("Fair_Trade_Supply", CultureGroup.Vegan, NewsSentiment.Positive, 1.12f,
                "윤리", "공정무역 두부·콩나물, MZ세대 선택",
                "공정무역 표시 재료를 쓰는 메뉴가 '윤리적 소비' 대표 사례로 소개됐습니다.",
                "공정무역 두부·콩나물을 사용하는 식당이 SNS에서 주목받고 있습니다. 소비자들은 \"값이 조금 더 들어도 생산자의 노동을 존중하고 싶다\"고 답했습니다.\n\n" +
                "NGO는 \"윤리적 재료는 프리미엄 팁으로도 이어질 수 있다\"고 설명했습니다.",
                "공정무역은 멀리 있는 농부와의 연결입니다.",
                "공정무역 재료 메뉴에 프리미엄이 붙을 수 있습니다.",
                "VGND", 1.08f);

            CreateNews("Fusion_Food_Trend", CultureGroup.Korean, NewsSentiment.Positive, 1.2f,
                "퓨전", "퓨전 워크숍 열풍, '두 문화의 밥' 인기",
                "두 문화 이상의 재료를 조합한 퓨전 메뉴가 외식 트렌드로 떠오르고 있습니다.",
                "대학·지역센터에서 열린 '퓨전 워크숍'에 시민 참여가 폭발적으로 늘었습니다. 할랄·비건·한식 재료를 섞은 실험 메뉴가 소개됐고, 참가자들은 \"차이를 섞는 것이 편견을 줄인다\"고 말했습니다.\n\n" +
                "일부 식당은 워크숍 이후 한정 메뉴를 영업에 도입하기도 했습니다.",
                "융합은 문화 상대주의의 맛있는 실습입니다.",
                "퓨전 워크숍 이벤트 시 M20~M23 메뉴를 활용하세요.",
                "UNITY", 1.25f);
        }

        private static void CreateNews(string id, CultureGroup culture, NewsSentiment sentiment,
            float multiplier, string section, string headline, string subheadline, string article,
            string sidebar, string summary, string stockCode, float spawnWeight = 1f, float boycott = 0f)
        {
            string path = $"{NewsDir}/News_{id}.asset";
            var so = LoadOrCreate<NewsSO>(path);
            so.cultureGroup = culture;
            so.sentiment = sentiment;
            so.priceMultiplier = multiplier;
            so.sectionTag = section;
            so.headline = headline;
            so.subheadline = subheadline;
            so.body = subheadline;
            so.article = article;
            so.sidebarNote = sidebar;
            so.summary = summary;
            so.primaryStockCode = stockCode;
            so.boycottWeight = boycott;
            so.spawnWeight = spawnWeight;
            so.illustration = AssetDatabase.LoadAssetAtPath<Sprite>(
                $"{NewsDir}/Illustrations/News_{id}.png");
            EditorUtility.SetDirty(so);
        }

        private static void CreateStocks()
        {
            CreateStock("KFOOD", "한식푸드", CultureGroup.Korean, 12800, 0.10f,
                "김치·한식 재료·분식 체인 지수");
            CreateStock("HLAL", "할랄그룹", CultureGroup.Muslim, 9200, 0.11f,
                "할랄 인증 식품·외식 기업 지수");
            CreateStock("CURRY", "인도식홀딩스", CultureGroup.Hindu, 10500, 0.09f,
                "채식 커리·향신료 유통 지수");
            CreateStock("VGND", "채식테크", CultureGroup.Vegan, 6800, 0.12f,
                "비건·플랜트 기반 식품 지수");
            CreateStock("SEAFO", "동남아식품", CultureGroup.SEAsian, 7600, 0.10f,
                "동남아 향신료·해산물 가공 지수");
            CreateStock("UNITY", "다문화상생", CultureGroup.AfricanAmerican, 11200, 0.09f,
                "다문화 상생·포용 경제 지수");
        }

        private static void CreateStock(string code, string displayName, CultureGroup culture,
            int basePrice, float volatility, string description)
        {
            string path = $"{StockDir}/Stock_{code}.asset";
            var so = LoadOrCreate<StockTickerSO>(path);
            so.code = code;
            so.displayName = displayName;
            so.cultureGroup = culture;
            so.basePrice = basePrice;
            so.volatility = volatility;
            so.description = description;
            EditorUtility.SetDirty(so);
        }

        private static void CreateDeliveryEvents()
        {
            CreateDelivery("Normal", DeliveryEventType.None, 0, 0f,
                "정상 배달", "재료가 무사히 도착했습니다.", 6f);
            CreateDelivery("Delay", DeliveryEventType.Delay, 15, 0f,
                "배달 지연", "교통 체증으로 재료가 늦게 도착했습니다.", 2f);
            CreateDelivery("Theft", DeliveryEventType.Theft, 0, 0.25f,
                "도난 사건", "배달 중 일부 재료가 도난당했습니다.", 0.5f);
            CreateDelivery("Accident", DeliveryEventType.Accident, 25, 0.15f,
                "배달 사고", "사고로 일부 상자가 파손되었습니다.", 1.5f);
            CreateDelivery("LocalFarm", DeliveryEventType.None, 0, 0f,
                "로컬 농가 보너스", "직거래 농가에서 신선한 채소가 추가로 도착했습니다.", 2f, bonusUnits: 3);
            CreateDelivery("EthicalDelay", DeliveryEventType.Delay, 10, 0f,
                "공정무역 배송 지연", "공정무역 공급망 점검으로 배송이 늦어졌습니다.", 1f);
        }

        private static void CreateDelivery(string id, DeliveryEventType type, int freshnessPenalty,
            float stockLoss, string headline, string body, float spawnWeight = 1f, int bonusUnits = 0)
        {
            string path = $"{DeliveryDir}/Delivery_{id}.asset";
            var so = LoadOrCreate<DeliveryEventSO>(path);
            so.eventType = type;
            so.freshnessPenalty = freshnessPenalty;
            so.stockLossRatio = stockLoss;
            so.bonusWarehouseUnitsPerIngredient = bonusUnits;
            so.headline = headline;
            so.body = body;
            so.spawnWeight = spawnWeight;
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
