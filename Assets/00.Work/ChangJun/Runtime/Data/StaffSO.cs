using UnityEngine;

namespace ChangJun.Data
{
    [CreateAssetMenu(fileName = "Staff_", menuName = "CupRice/Data/Staff")]
    public class StaffSO : ScriptableObject
    {
        public string staffId;
        public string displayName;
        public CultureGroup cultureGroup;
        public int hireCost = 200;
        public int dailyWage = 90;
        [Range(0f, 1f)] public float understandingBonus = 0.25f;
        [TextArea(2, 4)] public string tabooHint;
        [TextArea(1, 3)] public string bio;
    }
}
