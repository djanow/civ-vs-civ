using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// ScriptableObject contenant toute la configuration statique d'une partie.
    /// Charge au demarrage dans GameManager.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSetup", menuName = "CivVSCiv/Game Setup")]
    public class GameSetupData : ScriptableObject
    {
        [Header("Civilisations disponibles")]
        public CivilizationData[] AvailableCivs;

        [Header("Definitions d'unites")]
        public UnitData[] UnitDefinitions;

        [Header("Definitions de batiments")]
        public BuildingData[] BuildingDefinitions;

        [Header("Arbre technologique")]
        public TechTreeData TechTree;

        [Header("Evenements narratifs")]
        public EventData[] AllEvents;

        [Header("Parametres de depart")]
        public int StartingGold = 100;
        public int StartingScience = 10;
        public int StartingCulture = 5;
        public int StartingUnits = 2;

        [Header("Noms d'unites par defaut")]
        public string[] DefaultUnitNames; // "Warrior", "Scout"
    }

    /// <summary>
    /// Donnees minimales pour un batiment (placeholder).
    /// </summary>
    [System.Serializable]
    public class BuildingData
    {
        public string BuildingName;
        public string Description;
        public int Cost;
        public string Era;
    }

    /// <summary>
    /// Donnees minimales pour l'arbre tech.
    /// </summary>
    [System.Serializable]
    public class TechTreeData
    {
        public TechNodeData[] AllTechs;
    }

    [System.Serializable]
    public class TechNodeData
    {
        public string TechName;
        public int Era;
        public int Cost;
        public string[] Prerequisites;
    }
}
