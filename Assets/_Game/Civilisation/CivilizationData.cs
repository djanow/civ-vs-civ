using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Donnees statiques d'une civilisation : nom, description, leaders, bonus signature.
    /// </summary>
    [CreateAssetMenu(fileName = "CivData", menuName = "CivVSCiv/Civilization Data")]
    public class CivilizationData : ScriptableObject
    {
        [Header("Identite")]
        public int CivId;
        public string CivName;
        [TextArea(2, 4)] public string CivDescription;

        [Header("Leaders")]
        public LeaderData[] Leaders;

        [Header("Bonus Signature")]
        public string SignatureBonusName;
        [TextArea(2, 4)] public string SignatureBonusDescription;

        public LeaderData GetLeaderForEra(int eraIndex)
        {
            if (eraIndex < 0 || eraIndex >= Leaders.Length)
                return null;
            return Leaders[eraIndex];
        }
    }
}
