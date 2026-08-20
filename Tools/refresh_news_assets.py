# -*- coding: utf-8 -*-
"""Re-write NewsSO assets without mid-string YAML wraps (preserve Korean text)."""
import os
import uuid

NEWS_DIR = r"C:\Users\user\source\repos\2026GameProgramingTeamProject\2026GameProgramingTeamProject\2026SocialProject\Assets\Resources\Craft\News"
ILLUST_DIR = os.path.join(NEWS_DIR, "Illustrations")
SCRIPT_GUID = "93f2766bd19c85d478e43b4e1b5aafc5"

NEWS = [
    dict(id="Muslim_Positive", culture=2, sentiment=0, mult=1.2, spawn=1.2, boycott=0.0,
         section="할랄", headline="할랄 푸드 열풍, 도심 식당 줄서기",
         sub="무슬림 친화 메뉴 수요가 크게 늘었습니다. '믿을 수 있는 식재료'가 소비의 핵심입니다.",
         article=(
             "도심 곳곳에서 할랄 인증 식당이 줄을 서고 있습니다. 현지 무슬림 커뮤니티는 돼지고기·알코올 미사용 메뉴를 가장 중요하게 꼽았으며, "
             "학교 급식과 기업 구내식당까지 할랄 옵션 도입 논의가 확산되고 있습니다.\n\n"
             "전문가들은 할랄이 단순한 종교적 규범을 넘어 '식품 안전·공급망 투명성'의 문제이기도 하다고 설명합니다. "
             "일부 지역에서는 할랄 인증 과정을 투어로 열어 이해를 돕는 프로그램도 인기를 끌고 있습니다.\n\n"
             "한 식당 주인은 \"손님이 무엇을 먹지 못하는지 아는 것이 예의\"라며, 재료 표시와 교차오염 방지에 힘쓰겠다고 밝혔습니다."
         ),
         sidebar="종교적 식습관을 이해하는 것은 다문화 사회의 기본 소양입니다.",
         summary="오늘은 무슬림 손님의 방문이 늘 수 있습니다. 할랄 메뉴를 준비하면 호응이 좋을 것입니다.",
         stock="HLAL"),
    dict(id="Korean_Negative", culture=1, sentiment=1, mult=0.85, spawn=1.0, boycott=0.0,
         section="경제", headline="물가 안정 속 한식 업계 '가성비' 경쟁",
         sub="농산물 가격이 안정되며 한식 재료 비용 부담이 줄었습니다.",
         article=(
             "농산물 도매 시장에서 배추·콩나물·두부 거래량이 늘면서 가격이 안정세를 보이고 있습니다. "
             "한식 업체들은 원가 부담이 줄었다며 숨통을 트였지만, 소비자들은 여전히 '가성비 있는 한 끼'를 찾고 있다고 합니다.\n\n"
             "반면 외식 물가 전반은 여전히 높아, 젊은 층 사이에서는 컵밥·분식 같은 소형 메뉴가 다시 주목받고 있습니다. "
             "전문가는 \"저가가 곧 품질 저하를 의미하지 않는다\"며, 효율적인 운영과 메뉴 구성이 중요하다고 조언했습니다."
         ),
         sidebar="가격 경쟁 속에서도 정성은 줄일 수 없습니다.",
         summary="한식 메뉴 수요는 회복될 수 있지만, 가격에 민감한 손님이 많아지는 날입니다.",
         stock="KFOOD"),
    dict(id="Hindu_Curry", culture=3, sentiment=0, mult=1.15, spawn=1.1, boycott=0.0,
         section="문화", headline="채식 커리 열풍, '소를 소중히' 메시지 확산",
         sub="인도·남아시아 채식 문화에 대한 관심이 커지고 있습니다.",
         article=(
             "힌두교에서 소는 신성한 동물로 여겨지며, 많은 힌두교 신자는 채식을 실천합니다. "
             "최근 도시에서는 커리 향신료와 채소 위주의 메뉴가 건강식으로 소개되며 인기를 얻고 있습니다.\n\n"
             "현지 커뮤니티 리더는 \"소고기를 피하는 것은 종교만의 문제가 아니라 환경·동물복지와도 연결된다\"고 설명했습니다. "
             "일부 식당은 메뉴판에 '소고기 미사용'을 명시해 신뢰를 쌓고 있습니다."
         ),
         sidebar="다른 문화의 식생활 금기를 알아주는 것이 편견을 줄이는 첫걸음입니다.",
         summary="채식·커리 메뉴에 관심이 몰릴 수 있습니다. 소고기가 들어간 메뉴는 주의하세요.",
         stock="CURRY"),
    dict(id="Vegan_Rise", culture=4, sentiment=0, mult=1.18, spawn=1.15, boycott=0.0,
         section="라이프", headline="비건 인구 증가, '완전 채식' 수요 확대",
         sub="동물성 원료를 배제한 메뉴에 대한 수요가 꾸준히 늘고 있습니다.",
         article=(
             "비건은 단순히 채식을 넘어 난·우유·꿀·젤라틴 등 동물 유래 성분까지 피하는 생활 방식입니다. "
             "SNS에서는 비건 인증 마크와 레시피 공유가 활발하며, 식당들도 교차오염 방지를 강조하고 있습니다.\n\n"
             "영양학자는 \"비건 메뉴는 단백질·비타민B12 보충 설계가 중요하다\"고 덧붙였습니다. "
             "두부·콩·견과류를 활용한 메뉴가 대안으로 떠오르고 있습니다."
         ),
         sidebar="식단 선택은 개인의 신념이자 건강 문제이기도 합니다.",
         summary="완전 채식 손님이 늘 수 있습니다. 계란·치즈·다진고기 성분을 꼼꼼히 확인하세요.",
         stock="VGND"),
    dict(id="SEAsian_Fusion", culture=5, sentiment=0, mult=1.1, spawn=1.0, boycott=0.0,
         section="문화", headline="동남아 이주민 커뮤니티, '고향의 맛' 공유 행사",
         sub="저렴하고 든든한 한 끼에 대한 관심이 이주민·현지인 모두에게 커지고 있습니다.",
         article=(
             "동남아 출신 노동자와 학생들이 모인 지역에서 '고향 밥상' 나눔 행사가 열렸습니다. "
             "향신료와 해산물, 쌀 위주의 메뉴가 소개됐으며, 참가자들은 \"맛이 곧 기억이자 정체성\"이라고 말했습니다.\n\n"
             "일부 상인은 동남아 손님이 많은 시간대에 맞춰 메뉴를 조정하고, 가격대를 낮춘 세트를 준비하기도 합니다."
         ),
         sidebar="이주민의 식문화는 우리 동네 경제와도 연결됩니다.",
         summary="담백·저가 메뉴 수요가 늘 수 있습니다. 해산물 알레르기도 함께 확인하세요.",
         stock="SEAFO"),
    dict(id="Multiculture_Unity", culture=1, sentiment=0, mult=1.25, spawn=1.3, boycott=0.0,
         section="사회", headline="다문화 상생 인증 매장 확대",
         sub="문화 이해를 실천하는 식당이 시범 사례로 소개됐습니다.",
         article=(
             "지자체와 시민단체가 참여한 '다문화 상생 인증' 제도가 본격화되고 있습니다. "
             "인증 매장은 메뉴 설명, 종교·식이 금기 안내, 직원 교육 등을 기준으로 평가받습니다.\n\n"
             "한 참여 상인은 \"손님의 이름과 식습관을 기억하는 것이 매출보다 먼저\"라고 말해 화제가 됐습니다. "
             "전문가들은 음식점이 통합사회 교육의 현장이 될 수 있다고 평가합니다."
         ),
         sidebar="다양성은 장식이 아니라 공존의 조건입니다.",
         summary="다양한 문화권 손님이 함께 찾아올 수 있는 날입니다. 정확한 주문 처리가 평판을 만듭니다.",
         stock="UNITY"),
    dict(id="Discrimination", culture=6, sentiment=2, mult=0.7, spawn=0.8, boycott=0.3,
         section="사회", headline="편견 게시글 확산, 일부 상권 소비 위축",
         sub="인터넷 커뮤니티의 차별적 게시글이 논란을 일으켰습니다.",
         article=(
             "한 인터넷 커뮤니티에 올라온 편견 섞인 게시글이 빠르게 퍼지며 논란이 커지고 있습니다. "
             "일부 상인들은 '이웃을 대하는 태도가 곧 가게의 평판'이라며 차별 없는 영업을 당부했습니다.\n\n"
             "시민단체는 편견 보도가 실제 소비에 영향을 준다고 경고했습니다. "
             "학교에서는 '미디어 리터러시' 수업에서 이번 사건을 사례로 다루기도 했습니다.\n\n"
             "사회학자는 \"익명성 뒤에 숨은 혐오가 경제 활동까지 침투한다\"며, 공공 담론의 책임을 강조했습니다."
         ),
         sidebar="편견은 개인의 문제가 아니라 사회 구조의 문제이기도 합니다.",
         summary="오늘은 분위기 영향으로 일부 손님이 줄어들 수 있습니다. 정확한 주문 처리가 더 중요합니다.",
         stock="UNITY"),
    dict(id="Halal_Scandal_False", culture=2, sentiment=1, mult=0.9, spawn=0.9, boycott=0.0,
         section="속보", headline="가짜 할랄 인증 논란, 소비자 불안 확대",
         sub="일부 업체의 허위 인증 의혹이 제기됐습니다.",
         article=(
             "할랄 인증 마크를 무단으로 사용했다는 제보가 이어지며 소비자 불안이 커지고 있습니다. "
             "진짜 인증과 가짜를 구분하기 어렵다는 목소리도 나옵니다.\n\n"
             "무슬림 커뮤니티는 \"신뢰가 한 번 무너지면 회복이 어렵다\"고 강조했습니다. "
             "정부는 할랄 인증 정보를 한눈에 볼 수 있는 포털 구축을 검토 중입니다."
         ),
         sidebar="문화적 신뢰는 라벨 한 장으로 완성되지 않습니다.",
         summary="무슬림 손님이 신중해질 수 있습니다. 재료 출처를 명확히 안내하면 도움이 됩니다.",
         stock="HLAL"),
    dict(id="School_Food_Edu", culture=1, sentiment=0, mult=1.08, spawn=1.0, boycott=0.0,
         section="교육", headline="통합사회 수업, '식탁에서 배우는 문화' 주목",
         sub="학교에서 식문화를 주제로 한 프로젝트 수업이 확산되고 있습니다.",
         article=(
             "전국 여러 학교에서 '식탁에서 배우는 문화' 프로젝트가 진행되고 있습니다. "
             "학생들은 종교·식이·지역별 음식 차이를 조사하고, 가게를 방문해 인터뷰하기도 합니다.\n\n"
             "교사들은 \"교과서의 다문화가 실제 손님을 만나야 살아난다\"고 말합니다. "
             "일부 학생들은 가족 식단과 학교 급식의 차이를 발표하며 공감대를 형성했습니다."
         ),
         sidebar="음식은 문화를 가르치는 가장 부드러운 매개체입니다.",
         summary="젊은 손님과 학부모의 관심이 높아질 수 있습니다.",
         stock="KFOOD"),
    dict(id="Muslim_Travel_Ban", culture=2, sentiment=1, mult=0.88, spawn=0.95, boycott=0.0,
         section="국제", headline="입국 심사 강화 논란, 할랄 상권도 긴장",
         sub="여행·체류 규제가 강화되며 무슬림 방문객·상권이 움츠러들고 있습니다.",
         article=(
             "일부 국가의 입국·체류 심사 강화 소식이 전해지며 현지 무슬림 커뮤니티와 할랄 외식업계가 긴장하고 있습니다. "
             "관광·유학 수요가 줄어들면 할랄 식당·식재료 유통도 타격을 받을 수 있다는 전망이 나옵니다.\n\n"
             "시민단체는 \"정책과 무관한 이웃까지 편견으로 묶어선 안 된다\"고 경고했습니다. "
             "상인들은 단골 손님과의 신뢰를 지키며 차분히 영업하겠다고 밝혔습니다."
         ),
         sidebar="정책 이슈가 식탁의 편견으로 번지지 않도록 주의가 필요합니다.",
         summary="무슬림 손님 방문이 줄 수 있습니다. 재료 안내를 더 분명히 하면 도움이 됩니다.",
         stock="HLAL"),
    dict(id="Hindu_Temple_Dispute", culture=3, sentiment=1, mult=0.82, spawn=0.9, boycott=0.0,
         section="사회", headline="사원 인근 상권 갈등, 채식 거리도 한산",
         sub="지역 갈등 소식으로 힌두·남아시아 식문화 상권이 위축됐습니다.",
         article=(
             "사원 인근 개발·소음 문제를 둘러싼 갈등이 보도되며 인근 채식·커리 거리가 한산해졌습니다. "
             "상인들은 \"종교 시설과 상권이 함께 성장해 온 동네\"라며 조속한 대화를 촉구했습니다.\n\n"
             "커뮤니티 리더는 \"갈등을 문화 차별로 몰아가면 안 된다\"고 강조했습니다. "
             "일부 손님은 온라인으로만 주문하며 현장을 피하고 있습니다."
         ),
         sidebar="갈등의 본질과 문화를 분리해 읽는 눈이 필요합니다.",
         summary="힌두·채식 메뉴 수요가 줄 수 있습니다. 소고기 메뉴는 특히 신중히 다루세요.",
         stock="CURRY"),
    dict(id="Vegan_Greenwash", culture=4, sentiment=1, mult=0.86, spawn=0.95, boycott=0.0,
         section="소비", headline="비건 라벨 과장 광고 논란 확산",
         sub="동물성 원료가 검출된 '비건' 제품 사례가 잇따르고 있습니다.",
         article=(
             "시중 일부 '비건' 표기 제품에서 동물성 성분이 검출됐다는 조사 결과가 발표되며 소비자 불신이 커지고 있습니다. "
             "SNS에서는 그린워싱 비판이 이어졌고, 비건 식당도 교차오염·성분 표기를 재점검하고 있습니다.\n\n"
             "업계 관계자는 \"라벨보다 조리 과정 투명성이 중요하다\"고 말했습니다. "
             "완전 채식을 지키는 손님일수록 성분 확인을 더 까다롭게 요구할 전망입니다."
         ),
         sidebar="신념을 존중하는 식사는 표시보다 실천에서 드러납니다.",
         summary="비건 손님이 신중해질 수 있습니다. 계란·유제품·육류 교차오염을 철저히 확인하세요.",
         stock="VGND"),
    dict(id="SEAsian_Labor_Strike", culture=5, sentiment=1, mult=0.84, spawn=0.9, boycott=0.0,
         section="노동", headline="식품 가공장 파업, 동남아 식재료 수급 불안",
         sub="임금·처우 개선을 요구하는 파업으로 일부 향신료·해산물 공급이 지연되고 있습니다.",
         article=(
             "동남아 계열 식품 가공장에서 처우 개선을 요구하는 파업이 이어지며 향신료·해산물 가공품 출하가 늦어지고 있습니다. "
             "소규모 식당들은 대체 거래처를 찾는 중입니다.\n\n"
             "이주민 노동자 단체는 \"식탁의 풍요 뒤에는 노동이 있다\"고 강조했습니다. "
             "전문가들은 단기 가격 변동과 함께 장기적으로는 공정 공급망 논의가 필요하다고 지적합니다."
         ),
         sidebar="값싼 한 끼의 뒤에는 누군가의 노동이 있습니다.",
         summary="동남아·해산물 메뉴 재료비가 흔들릴 수 있습니다. 재고를 점검하세요.",
         stock="SEAFO"),
    dict(id="SoulFood_Fest", culture=6, sentiment=0, mult=1.22, spawn=1.2, boycott=0.0,
         section="문화", headline="소울푸드 페스티벌, 상생 상권 '활기'",
         sub="흑인 디아스포라 식문화를 조명하는 축제가 도심을 달궜습니다.",
         article=(
             "시내 광장에서 열린 소울푸드 페스티벌에 시민과 관광객이 몰렸습니다. "
             "프라이드 치킨·검보·콜라도 그린스 등 메뉴가 소개됐고, 참가자들은 음식과 음악·역사를 함께 경험했습니다.\n\n"
             "주최 측은 \"맛으로 만나는 연대\"를 내세웠으며, 인근 상권도 방문객 증가로 활기를 띠었습니다. "
             "다문화 상생 지수에도 긍정적 신호가 관측됩니다."
         ),
         sidebar="축제는 차이를 무대로, 공존을 일상으로 만듭니다.",
         summary="다양한 문화권 손님이 늘 수 있습니다. 따뜻하고 정확한 응대가 평판을 만듭니다.",
         stock="UNITY"),
    dict(id="Korean_Harvest_Fest", culture=1, sentiment=0, mult=1.12, spawn=1.1, boycott=0.0,
         section="생활", headline="추석 맞이 한식 나눔·급식 특식 확대",
         sub="명절을 앞두고 송편·전·나물 등 한식 수요가 크게 늘었습니다.",
         article=(
             "추석을 앞두고 학교 급식과 지역 나눔 행사에서 한식 특식이 늘고 있습니다. "
             "송편·잡채·나물 등 명절 메뉴에 대한 관심이 높아지며 전통 시장 거래량도 증가했습니다.\n\n"
             "영양사들은 \"명절 음식도 알레르기·종교 식이를 함께 고려해야 한다\"고 조언했습니다. "
             "한식 재료 관련 지수에도 온기가 돌고 있습니다."
         ),
         sidebar="명절 밥상은 가족과 이웃을 잇는 다리입니다.",
         summary="한식 메뉴 수요가 살아날 수 있습니다. 가성비와 정성을 함께 챙기세요.",
         stock="KFOOD"),
    dict(id="Halal_Kitchen_Edu", culture=2, sentiment=0, mult=1.14, spawn=1.1, boycott=0.0,
         section="교육", headline="할랄 키친 교실, 식당 사장님도 수강",
         sub="교차오염 방지·재료 구분을 배우는 실습 교육이 인기입니다.",
         article=(
             "지자체와 이슬람 문화센터가 함께하는 '할랄 키친' 교실에 식당 운영자와 조리 전공 학생이 몰리고 있습니다. "
             "돼지고기·알코올 미사용뿐 아니라 도마·칼 분리, 보관 구역 표시까지 실습합니다.\n\n"
             "수강생들은 \"손님의 믿음을 지키는 기술\"이라고 평가했습니다. "
             "할랄 외식 인증을 준비하는 매장도 늘어나는 추세입니다."
         ),
         sidebar="이해는 안내문보다 주방의 습관에서 완성됩니다.",
         summary="할랄·무슬림 친화 메뉴에 관심이 몰릴 수 있습니다.",
         stock="HLAL"),
    dict(id="Seafood_Shortage", culture=5, sentiment=1, mult=0.8, spawn=0.85, boycott=0.0,
         section="경제", headline="해산물 어획량 감소, 가공·외식 업계 비상",
         sub="수급 불안정으로 동남아 해산물 메뉴 원가가 요동치고 있습니다.",
         article=(
             "이상 기후와 조업 제한 여파로 일부 해산물 어획량이 줄며 가공·유통 가격이 상승했습니다. "
             "동남아 퓨전 식당들은 메뉴 구성을 조정하거나 대체 식재료를 검토 중입니다.\n\n"
             "수산업 관계자는 \"단기 급등락보다 안정적 공급망이 과제\"라고 말했습니다. "
             "소비자들은 가격 부담을 호소하며 내륙 식재료 메뉴로 이동하는 모습도 보입니다."
         ),
         sidebar="바다의 변화는 식탁의 가격표에도 나타납니다.",
         summary="해산물 메뉴 원가 부담이 커질 수 있습니다. 대체 메뉴를 준비하세요.",
         stock="SEAFO"),
    dict(id="Unity_City_Campaign", culture=6, sentiment=0, mult=1.16, spawn=1.15, boycott=0.0,
         section="사회", headline="도시 캠페인 '한 식탁의 이웃', 상생 소비 확산",
         sub="편견 없는 외식·쇼핑을 독려하는 시민 캠페인이 확산되고 있습니다.",
         article=(
             "시민단체와 지자체가 함께하는 '한 식탁의 이웃' 캠페인이 도시 전역으로 퍼지고 있습니다. "
             "참여 매장은 차별 없는 응대 서약과 다문화 메뉴 안내를 게시하며, 방문객에게 스탬프 투어를 제공합니다.\n\n"
             "초기 집계에 따르면 참여 상권의 주말 매출이 소폭 상승했으며, "
             "다문화 상생 관련 지수에도 긍정 신호가 포착됐습니다."
         ),
         sidebar="캠페인은 구호가 아니라 손님 한 명을 대하는 태도에서 시작됩니다.",
         summary="다양한 손님이 함께 찾아올 수 있는 날입니다. 정확한 주문이 신뢰를 쌓습니다.",
         stock="UNITY"),
]


def unity_str(s: str) -> str:
    out = []
    for ch in s:
        o = ord(ch)
        if ch == "\\":
            out.append("\\\\")
        elif ch == '"':
            out.append('\\"')
        elif ch == "\n":
            out.append("\\n")
        elif ch == "\r":
            continue
        elif o < 0x20 or o > 0x7E:
            out.append(f"\\u{o:04X}")
        else:
            out.append(ch)
    return "".join(out)


def field(name: str, value: str) -> str:
    return f'  {name}: "{unity_str(value)}"'


def read_guid(meta_path: str) -> str:
    with open(meta_path, encoding="utf-8") as f:
        for line in f:
            if line.startswith("guid:"):
                return line.split(":", 1)[1].strip()
    raise RuntimeError(meta_path)


def write_asset(n: dict, illust_guid: str):
    name = f"News_{n['id']}"
    path = os.path.join(NEWS_DIR, f"{name}.asset")
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        f"  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}",
        f"  m_Name: {name}",
        "  m_EditorClassIdentifier: Assembly-CSharp::ChangJun.Data.NewsSO",
        f"  cultureGroup: {n['culture']}",
        f"  sentiment: {n['sentiment']}",
        f"  priceMultiplier: {n['mult']}",
        f"  spawnWeight: {n['spawn']}",
        f"  boycottWeight: {n['boycott']}",
        field("sectionTag", n["section"]),
        field("headline", n["headline"]),
        field("subheadline", n["sub"]),
        field("body", n["sub"]),
        field("article", n["article"]),
        field("sidebarNote", n["sidebar"]),
        field("summary", n["summary"]),
        f"  illustration: {{fileID: 21300000, guid: {illust_guid}, type: 3}}",
        f"  primaryStockCode: {n['stock']}",
        "",
    ]
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines))
    meta_path = path + ".meta"
    if not os.path.exists(meta_path):
        with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
            f.write(
                "fileFormatVersion: 2\n"
                f"guid: {uuid.uuid4().hex}\n"
                "NativeFormatImporter:\n"
                "  externalObjects: {}\n"
                "  mainObjectFileID: 11400000\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n"
            )
    print("ok", name)


def main():
    for n in NEWS:
        guid = read_guid(os.path.join(ILLUST_DIR, f"News_{n['id']}.png.meta"))
        write_asset(n, guid)
    print("done", len(NEWS))


if __name__ == "__main__":
    main()
