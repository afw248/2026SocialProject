using System;

namespace ChangJun.Data
{
    /// <summary>
    /// 손님 문화·식이 분류 — 재료의 forbiddenIn 필드와 비트 AND 로 금기 충돌을 감지한다.
    /// None(0) = 제약 없음 / 조합 자유.
    /// </summary>
    [Flags]
    public enum Diet
    {
        None  = 0,
        Halal = 1 << 0,   // 무슬림: 돼지고기·알코올 금지
        Vegan = 1 << 1,   // 비건: 모든 동물성(고기·계란·유제품·젓갈) 금지
        Hindu = 1 << 2,   // 힌두 채식: 소고기 절대 금지, 다수 채식
    }
}
