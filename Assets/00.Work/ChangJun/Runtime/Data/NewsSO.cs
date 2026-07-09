using UnityEngine;

namespace ChangJun.Data
{
    public enum NewsSentiment
    {
        Positive,
        Negative,
        Discrimination,
    }

    [CreateAssetMenu(fileName = "News_", menuName = "CupRice/Data/News")]
    public class NewsSO : ScriptableObject
    {
        public CultureGroup cultureGroup;
        public NewsSentiment sentiment;
        [Range(0.5f, 2f)] public float priceMultiplier = 1f;
        [Range(0f, 1f)] public float spawnWeight = 1f;
        [Range(0f, 1f)] public float boycottWeight;
        public string headline;
        [TextArea(3, 8)] public string body;
        [TextArea(3, 6)] public string article;
        [TextArea(2, 4)] public string summary;
    }
}
