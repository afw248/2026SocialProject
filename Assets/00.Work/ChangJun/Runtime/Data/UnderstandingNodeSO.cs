using UnityEngine;

namespace ChangJun.Data
{
    public enum UnderstandingNodeType
    {
        Milestone,
        IngredientUnlock,
        EventUnlock,
        Certification,
        Fusion,
    }

    public enum UnderstandingNodeState
    {
        Locked,
        InProgress,
        Unlocked,
    }

    [CreateAssetMenu(fileName = "Node_", menuName = "CupRice/Data/UnderstandingNode")]
    public class UnderstandingNodeSO : ScriptableObject
    {
        public string nodeId;
        public CultureGroup cultureGroup;
        public UnderstandingNodeType nodeType;
        [Range(0, 100)] public int requiredUnderstanding = 20;
        public string[] prerequisiteNodeIds;
        public int gridRow;
        public string displayName;
        [TextArea(1, 3)] public string description;
        public IngredientSO ingredientToUnlock;
        public Sprite icon;
    }
}
