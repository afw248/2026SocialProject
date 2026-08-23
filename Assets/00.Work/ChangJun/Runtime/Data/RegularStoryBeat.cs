using System;
using UnityEngine;

namespace ChangJun.Data
{
    [Serializable]
    public class RegularStoryBeat
    {
        [Min(1)] public int requiredVisits = 2;
        [Min(0)] public int requiredAffinity;
        [TextArea(2, 4)] public string line;
    }
}
