using UnityEngine;

namespace ChangJun.Data
{
    [CreateAssetMenu(fileName = "Conflict_", menuName = "CupRice/Data/ConflictEvent")]
    public class ConflictEventSO : ScriptableObject
    {
        public string eventId;
        public CultureGroup speakerCulture;
        public CultureGroup targetCulture;
        [TextArea(2, 4)] public string prompt;
        [TextArea(2, 4)] public string prejudiceLine;
        public string interveneLabel = "개입한다";
        public string ignoreLabel = "모른 척한다";
        [TextArea(1, 3)] public string interveneNote;
        [TextArea(1, 3)] public string ignoreNote;
    }
}
