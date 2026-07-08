namespace ChangJun.Judge
{
    /// <summary>
    /// 조합 판정 결과 — §3-3 의사코드의 세 분기를 열거형으로 표현한다.
    /// </summary>
    public enum CraftResult
    {
        /// <summary>레시피 일치 AND 금기 없음 → 돈+price, 신뢰도+</summary>
        Success,

        /// <summary>레시피는 유효하나 금기 재료 포함 → 환불 −100, 신뢰도−−</summary>
        TabooViolation,

        /// <summary>선택 재료가 어느 메뉴와도 일치하지 않음 → 재료비 손실만</summary>
        WrongRecipe,
    }
}
