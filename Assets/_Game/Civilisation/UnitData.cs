using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Donnees de base d'une unite. Utilise par CivilizationData pour les unites uniques.
    /// Le systeme de combat complet sera implemente en Phase 3.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitData", menuName = "CivVSCiv/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("Identite")]
        public string UnitName;
        public string Description;

        [Header("Epoque")]
        public int Era; // 0 = Antiquite, 1 = Classique, 2 = Medievale

        [Header("Stats de base")]
        public int MovementPoints = 2;
        public int CombatStrength = 10;
        public int ProductionCost = 40;

        [Header("Categorie")]
        public UnitCategory Category = UnitCategory.Infantry;

        [Header("Unique")]
        public bool IsUnique;
        public string UniqueAbility;
    }

    /// <summary>
    /// Categorie d'unite pour le systeme de combat et les bonus contextuels.
    /// </summary>
    public enum UnitCategory
    {
        Recon,       // Eclaireur, Cavalier
        Infantry,    // Guerrier, Phalange, Episte
        Cavalry,     // Char, Cavalerie legere
        Siege,       // Belier, Catapulte
        Naval,       // Triere, Quinquerme
        Support,     // Medecin, Ingenieur
        Civilian     // Colon, Caravane
    }
}
