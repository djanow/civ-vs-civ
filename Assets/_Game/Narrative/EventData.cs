using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Type d'evenement narratif.
    /// Micro = petit popup contextuel (15-20s)
    /// Interlude = ecran complet au changement d'ere
    /// KeyMoment = moment cle unique par partie
    /// </summary>
    public enum EventType
    {
        Micro,
        Interlude,
        KeyMoment
    }

    /// <summary>
    /// ScriptableObject contenant les donnees d'un evenement narratif.
    /// </summary>
    [CreateAssetMenu(fileName = "EventData", menuName = "CivVSCiv/Event Data")]
    public class EventData : ScriptableObject
    {
        [Header("Identifiant")]
        public int EventId;
        public string Title;
        [TextArea(5, 10)] public string Description;

        [Header("Type")]
        public EventType Type;

        [Header("Declencheurs")]
        public int TriggerEra = -1;           // -1 = n'importe quelle ere
        public int[] TriggerCivIds;            // -1 = n'importe quelle civ
        public string[] TriggerConditions;     // Conditions lisible par EvaluateCondition

        [Header("Choix (2-3)")]
        public ChoiceData[] Choices;

        /// <summary>
        /// Verifie si les IDs de civ correspondent (ou -1 pour toute civ).
        /// </summary>
        public bool MatchesCiv(int civId)
        {
            if (TriggerCivIds == null || TriggerCivIds.Length == 0)
                return true;
            foreach (int id in TriggerCivIds)
                if (id == -1 || id == civId)
                    return true;
            return false;
        }
    }

    /// <summary>
    /// Une option de choix dans un evenement narratif.
    /// </summary>
    [System.Serializable]
    public class ChoiceData
    {
        [Header("Texte")]
        public string ChoiceText;
        [TextArea(2, 4)] public string ChoiceDescription;

        [Header("Effets")]
        public string[] Effects;              // "+50 gold", "-10 relations with Greece"
        public string LegacyUnlock;           // Nom du legacy a debloquer (optionnel)

        [Header("Narration")]
        [TextArea(3, 6)] public string NarrativeFollowUp;
    }
}
