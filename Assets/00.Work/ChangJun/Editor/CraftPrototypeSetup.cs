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
            EnsureDirectory(StockDir);
            EnsureDirectory(DeliveryDir);

            CreateDayConfig();
            CreateIngredients();
            CreateMenus();
            CreateCustomers();
            CreateThresholds();
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
            CreateCustomer("김상철", "김치 계란 제육. 빨리요.", Diet.None, CultureGroup.Korean, "M14");
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
                "완전 채식 손님이 늘 수 있습니다. 계란·치즈·육수 성분을 꼼꼼히 확인하세요.",
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
            EditorUtility.SetDirty(so);
        }

        private static void CreateStocks()
        {
            CreateStock("KFOOD", "한식푸드", CultureGroup.Korean, 12800,
                "김치·한식 재료·분식 체인 지수");
            CreateStock("HLAL", "할랄그룹", CultureGroup.Muslim, 9200,
                "할랄 인증 식품·외식 기업 지수");
            CreateStock("CURRY", "인도식홀딩스", CultureGroup.Hindu, 10500,
                "채식 커리·향신료 유통 지수");
            CreateStock("VGND", "채식테크", CultureGroup.Vegan, 6800,
                "비건·플랜트 기반 식품 지수");
            CreateStock("SEAFO", "동남아식품", CultureGroup.SEAsian, 7600,
                "동남아 향신료·해산물 가공 지수");
            CreateStock("UNITY", "다문화상생", CultureGroup.AfricanAmerican, 11200,
                "다문화 상생·포용 경제 지수");
        }

        private static void CreateStock(string code, string displayName, CultureGroup culture,
            int basePrice, string description)
        {
            string path = $"{StockDir}/Stock_{code}.asset";
            var so = LoadOrCreate<StockTickerSO>(path);
            so.code = code;
            so.displayName = displayName;
            so.cultureGroup = culture;
            so.basePrice = basePrice;
            so.volatility = 0.035f;
            so.description = description;
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
