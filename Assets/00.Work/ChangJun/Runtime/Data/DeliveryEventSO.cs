using UnityEngine;

namespace ChangJun.Data
{
    public enum DeliveryEventType
    {
        None,
        Delay,
        Theft,
        Accident,
    }

    [CreateAssetMenu(fileName = "Delivery_", menuName = "CupRice/Data/DeliveryEvent")]
    public class DeliveryEventSO : ScriptableObject
    {
        public DeliveryEventType eventType;
        [Range(0f, 1f)] public float spawnWeight = 1f;
        [Range(0, 100)] public int freshnessPenalty;
        [Range(0f, 1f)] public float stockLossRatio;
        [Tooltip("창고 도착 시 추가 보너스 (로컬 농가 등)")]
        public int bonusWarehouseUnitsPerIngredient;
        public string headline;
        [TextArea] public string body;
    }
}
