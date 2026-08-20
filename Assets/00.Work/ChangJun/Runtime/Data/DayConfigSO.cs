using UnityEngine;

namespace ChangJun.Data
{
    [CreateAssetMenu(fileName = "DayConfig", menuName = "CupRice/Data/DayConfig")]
    public class DayConfigSO : ScriptableObject
    {
        [Header("영업 시간")]
        public int openHour = 10;
        public int closeHour = 21;
        public int morningHour = 8;

        [Header("시간 진행")]
        [Tooltip("영업 중 실시간 1초당 흐르는 게임 분 (배경 흐름)")]
        public float passiveMinutesPerRealSecond = 0.45f;
        [Tooltip("손님 1명 처리 완료 시 추가되는 보너스 분")]
        public int minutesPerCustomerMin = 10;
        public int minutesPerCustomerMax = 18;
        public int minutesPerCustomer = 12;

        [Header("이해도")]
        public int understandingGainOnSuccess = 8;
        public int understandingLossOnWrongOrder = 5;
        public int understandingLossOnTaboo = 5;
        public int understandingLossOnWrongRecipe = 2;

        [Header("시작 재고 (1일차)")]
        public int starterStockPerIngredient = 20;

        [Header("영업 중 배달")]
        public int expressDeliveryMinutes = 30;
        public int economyDeliveryMinutes = 60;
        public float expressDeliveryPriceMultiplier = 2f;
    }
}
