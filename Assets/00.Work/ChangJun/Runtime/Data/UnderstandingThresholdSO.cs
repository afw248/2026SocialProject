using UnityEngine;

namespace ChangJun.Data
{
    [CreateAssetMenu(fileName = "Threshold_", menuName = "CupRice/Data/UnderstandingThreshold")]
    public class UnderstandingThresholdSO : ScriptableObject
    {
        public CultureGroup cultureGroup;
        [Range(0, 100)] public int threshold = 30;
        public IngredientSO ingredientToUnlock;
    }
}
