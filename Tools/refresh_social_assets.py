# -*- coding: utf-8 -*-
"""Generate UnderstandingNode + ShopUpgrade YAML assets (no Unity Editor required)."""
import os
import uuid

ROOT = r"C:\Users\user\source\repos\2026GameProgramingTeamProject\2026GameProgramingTeamProject\2026SocialProject"
NODE_DIR = os.path.join(ROOT, "Assets", "Resources", "Craft", "UnderstandingNodes")
UPGRADE_DIR = os.path.join(ROOT, "Assets", "Resources", "Craft", "Upgrades")
ING_DIR = os.path.join(ROOT, "Assets", "Resources", "Craft", "Ingredients")

NODE_SCRIPT = "7957fad32280bb74394128774b777c09"
UPGRADE_SCRIPT = "93e8f8384110e3e488df0d6b2ed0a63a"

CULTURE = {
    "Korean": 1, "Muslim": 2, "Hindu": 3, "Vegan": 4, "SEAsian": 5, "AfricanAmerican": 6,
}
NODE_TYPE = {
    "Milestone": 0, "IngredientUnlock": 1, "EventUnlock": 2, "Certification": 3, "Fusion": 4,
}


def read_guid(code: str) -> str | None:
    path = os.path.join(ING_DIR, f"Ingredient_{code}.asset.meta")
    if not os.path.isfile(path):
        return None
    with open(path, encoding="utf-8") as f:
        for line in f:
            if line.startswith("guid:"):
                return line.split(":", 1)[1].strip()
    return None


def write_meta(asset_path: str):
    meta = asset_path + ".meta"
    if os.path.isfile(meta):
        return
    g = uuid.uuid4().hex
    with open(meta, "w", encoding="utf-8") as f:
        f.write(
            "fileFormatVersion: 2\n"
            f"guid: {g}\n"
            "NativeFormatImporter:\n"
            "  externalObjects: {}\n"
            "  mainObjectFileID: 11400000\n"
            "  userData:\n"
            "  assetBundleName:\n"
            "  assetBundleVariant:\n"
        )


def write_node(node_id, culture, ntype, req, prereqs, title, desc, row, ing_code=None):
    os.makedirs(NODE_DIR, exist_ok=True)
    path = os.path.join(NODE_DIR, f"Node_{node_id}.asset")
    ing_ref = "  ingredientToUnlock: {fileID: 0}"
    if ing_code:
        g = read_guid(ing_code)
        if g:
            ing_ref = f"  ingredientToUnlock: {{fileID: 11400000, guid: {g}, type: 2}}"
    prereq_lines = ""
    if prereqs:
        prereq_lines = "  prerequisiteNodeIds:\n" + "".join(f"  - {p}\n" for p in prereqs)
    else:
        prereq_lines = "  prerequisiteNodeIds: []\n"
    yaml = (
        "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"
        "--- !u!114 &11400000\nMonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  m_GameObject: {fileID: 0}\n"
        "  m_Enabled: 1\n"
        "  m_EditorHideFlags: 0\n"
        f"  m_Script: {{fileID: 11500000, guid: {NODE_SCRIPT}, type: 3}}\n"
        f"  m_Name: Node_{node_id}\n"
        "  m_EditorClassIdentifier: Assembly-CSharp::ChangJun.Data.UnderstandingNodeSO\n"
        f"  nodeId: {node_id}\n"
        f"  cultureGroup: {CULTURE[culture]}\n"
        f"  nodeType: {NODE_TYPE[ntype]}\n"
        f"  requiredUnderstanding: {req}\n"
        f"{prereq_lines}"
        f"  gridRow: {row}\n"
        f"  displayName: {title}\n"
        f"  description: {desc}\n"
        f"{ing_ref}\n"
        "  icon: {fileID: 0}\n"
    )
    with open(path, "w", encoding="utf-8") as f:
        f.write(yaml)
    write_meta(path)
    print("node", node_id)


def write_upgrade(upgrade_type, name, culture, cost, taboo, spawn, desc):
    os.makedirs(UPGRADE_DIR, exist_ok=True)
    path = os.path.join(UPGRADE_DIR, f"Upgrade_{upgrade_type}.asset")
    type_map = {"HalalKitchen": 0, "VeganZone": 1, "MulticultureBadge": 2}
    yaml = (
        "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"
        "--- !u!114 &11400000\nMonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  m_GameObject: {fileID: 0}\n"
        "  m_Enabled: 1\n"
        "  m_EditorHideFlags: 0\n"
        f"  m_Script: {{fileID: 11500000, guid: {UPGRADE_SCRIPT}, type: 3}}\n"
        f"  m_Name: Upgrade_{upgrade_type}\n"
        "  m_EditorClassIdentifier: Assembly-CSharp::ChangJun.Data.ShopUpgradeSO\n"
        f"  upgradeType: {type_map[upgrade_type]}\n"
        f"  displayName: {name}\n"
        f"  description: {desc}\n"
        f"  purchaseCost: {cost}\n"
        f"  cultureGroup: {CULTURE[culture]}\n"
        f"  tabooPenaltyReduction: {taboo}\n"
        f"  spawnBoost: {spawn}\n"
    )
    with open(path, "w", encoding="utf-8") as f:
        f.write(yaml)
    write_meta(path)
    print("upgrade", upgrade_type)


def main():
    chains = [
        ("KOR", "Korean", [
            ("KOR_1", "Milestone", 0, None, "한식 기본", "한국 손님의 식사 문화를 익히기 시작합니다.", 0, None),
            ("KOR_2", "Milestone", 20, ["KOR_1"], "가성비 이해", "저렴하지만 정성스러운 한 끼를 선호합니다.", 1, None),
            ("KOR_3", "IngredientUnlock", 40, ["KOR_2"], "제육·김치", "한식 핵심 재료를 다룰 수 있습니다.", 2, "PRK"),
            ("KOR_4", "Certification", 60, ["KOR_3"], "다문화 상생", "다양한 손님을 함께 맞이할 준비가 됩니다.", 3, None),
            ("KOR_5", "EventUnlock", 80, ["KOR_4"], "문화 축제", "한식 문화 축제 이벤트 조건을 충족합니다.", 4, None),
            ("KOR_6", "Fusion", 100, ["KOR_5"], "완전 이해", "한식 메뉴 영구 +5% 보너스.", 5, None),
        ]),
        ("MUS", "Muslim", [
            ("MUS_1", "Milestone", 0, None, "할랄 기본", "무슬림 손님의 식습관을 배웁니다.", 0, None),
            ("MUS_2", "Milestone", 20, ["MUS_1"], "금기 이해", "돼지·알코올 회피가 왜 중요한지 압니다.", 1, None),
            ("MUS_3", "IngredientUnlock", 40, ["MUS_2"], "할랄 재료", "SPC·HBF 재료를 사용할 수 있습니다.", 2, "SPC"),
            ("MUS_4", "Certification", 60, ["MUS_3"], "할랄 키친", "교차오염 방지 인증 준비.", 3, None),
            ("MUS_5", "EventUnlock", 80, ["MUS_4"], "문화 축제", "무슬림 문화 축제 조건.", 4, None),
            ("MUS_6", "Fusion", 100, ["MUS_5"], "완전 이해", "할랄 메뉴 +5% 보너스.", 5, None),
        ]),
        ("HIN", "Hindu", [
            ("HIN_1", "Milestone", 0, None, "채식 문화", "힌두·남아시아 채식 문화를 배웁니다.", 0, None),
            ("HIN_2", "Milestone", 20, ["HIN_1"], "소의 의미", "소고기 금기의 문화적 배경.", 1, None),
            ("HIN_3", "IngredientUnlock", 40, ["HIN_2"], "커리 향신료", "CUR 재료 해금.", 2, "CUR"),
            ("HIN_4", "Certification", 60, ["HIN_3"], "채식 존중", "채식 손님을 위한 조리 습관.", 3, None),
            ("HIN_5", "EventUnlock", 80, ["HIN_4"], "문화 축제", "힌두 문화 축제 조건.", 4, None),
            ("HIN_6", "Fusion", 100, ["HIN_5"], "완전 이해", "채식·커리 메뉴 +5%.", 5, None),
        ]),
        ("VEG", "Vegan", [
            ("VEG_1", "Milestone", 0, None, "비건 기본", "완전 채식의 의미.", 0, None),
            ("VEG_2", "Milestone", 20, ["VEG_1"], "동물성 회피", "계란·유제품·육류 성분 확인.", 1, None),
            ("VEG_3", "IngredientUnlock", 40, ["VEG_2"], "콩나물·두부", "BSP 재료 해금.", 2, "BSP"),
            ("VEG_4", "Certification", 60, ["VEG_3"], "비건 존", "비건 전용 조리 구역.", 3, None),
            ("VEG_5", "EventUnlock", 80, ["VEG_4"], "문화 축제", "비건 문화 축제.", 4, None),
            ("VEG_6", "Fusion", 100, ["VEG_5"], "완전 이해", "비건 메뉴 +5%.", 5, None),
        ]),
        ("SEA", "SEAsian", [
            ("SEA_1", "Milestone", 0, None, "동남아 기본", "이주민·향신료 문화.", 0, None),
            ("SEA_2", "Milestone", 20, ["SEA_1"], "고향의 맛", "저렴하고 든든한 한 끼.", 1, None),
            ("SEA_3", "IngredientUnlock", 40, ["SEA_2"], "해산물", "SHR 재료 해금.", 2, "SHR"),
            ("SEA_4", "Certification", 60, ["SEA_3"], "공정 공급", "노동·공급망 존중.", 3, None),
            ("SEA_5", "EventUnlock", 80, ["SEA_4"], "문화 축제", "동남아 축제.", 4, None),
            ("SEA_6", "Fusion", 100, ["SEA_5"], "완전 이해", "동남아 메뉴 +5%.", 5, None),
        ]),
        ("AA", "AfricanAmerican", [
            ("AA_1", "Milestone", 0, None, "소울푸드", "흑인 디아스포라 식문화.", 0, None),
            ("AA_2", "Milestone", 20, ["AA_1"], "연대의 식탁", "편견 없는 응대.", 1, None),
            ("AA_3", "IngredientUnlock", 40, ["AA_2"], "퓨전 재료", "다문화 재료 조합.", 2, "TFU"),
            ("AA_4", "Certification", 60, ["AA_3"], "상생 배지", "다문화 상생 인증.", 3, None),
            ("AA_5", "EventUnlock", 80, ["AA_4"], "문화 축제", "소울푸드·UNITY 축제.", 4, None),
            ("AA_6", "Fusion", 100, ["AA_5"], "완전 이해", "소울·퓨전 메뉴 +5%.", 5, None),
        ]),
    ]

    for _, culture, nodes in chains:
        for node_id, ntype, req, prereqs, title, desc, row, ing in nodes:
            write_node(node_id, culture, ntype, req, prereqs, title, desc, row, ing)

    write_upgrade("HalalKitchen", "할랄 키친 인증", "Muslim", 600, 0.25, 0.15,
                    "교차오염 위험 감소, 무슬림 손님 증가.")
    write_upgrade("VeganZone", "비건 조리 존", "Vegan", 550, 0.2, 0.12,
                  "금기 패널티 완화, 비건 손님 증가.")
    write_upgrade("MulticultureBadge", "다문화 상생 배지", "Korean", 700, 0.15, 0.08,
                  "전 문화권 손님 소폭 증가, 평판 상승.")
    print("done")


if __name__ == "__main__":
    main()
