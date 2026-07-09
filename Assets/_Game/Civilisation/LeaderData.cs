using UnityEngine;

namespace CivVSCiv
{
    [CreateAssetMenu(fileName = "LeaderData", menuName = "CivVSCiv/Leader Data")]
    public class LeaderData : ScriptableObject
    {
        [Header("Identite")]
        public string LeaderName;

        [Header("Ere")]
        public int Era;

        [Header("Bonus")]
        public string EraBonusName;
        [TextArea(2, 4)] public string EraBonusDescription;

        [Header("Legs")]
        public string LegacyName;
        [TextArea(2, 4)] public string LegacyDescription;
    }
}
