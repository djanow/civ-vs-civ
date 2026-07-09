using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Categories d'unites.
    /// </summary>
    public enum UnitCategory
    {
        Recon,      // Eclaireur, Cavalier — vision, rapide, fragile
        Infantry,   // Guerrier, Phalange, Epéiste — polyvalent, bonus terrain
        Cavalry,    // Char, Cavalerie légère — rapide, bonus terrain plat
        Siege,      // Bélier, Catapulte — lent, fragile, anti-villes
        Naval,      // Trière, Quinquérème — controle maritime, blocus
        Support,    // Médecin, Ingénieur — soigne, construit
        Civil       // Colon, Caravane — fonde villes, commerce
    }

    /// <summary>
    /// Doctrine militaire choisie par le joueur.
    /// Applique des bonus/malus symetriques au combat.
    /// </summary>
    public enum Doctrine
    {
        None,        // Pas de doctrine activee
        Aggressive,  // +1 ATK, -1 DEF
        Defensive,   // +1 DEF, -1 ATK
        Guerrilla    // +1 DEF en terrain accidente, -1 ATK en terrain ouvert
    }

    /// <summary>
    /// Donnees statiques d'une unite (ScriptableObject).
    /// Cree via le menu Assets > Create > CivVSCiv > Unit Data.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitData", menuName = "CivVSCiv/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("Identite")]
        public string UnitName;

        [Header("Stats de combat")]
        public UnitCategory Category;
        public int BaseAttack;
        public int BaseDefense;
        public int MaxHealth;
        public int MovementRange;

        [Header("Production")]
        public int ProductionCost;
        public int RequiredTechId;

        [Header("Exclusivite")]
        public bool IsUnique;           // true pour Bireme, Phalange
        public int CivilizationId = -1; // -1 = toutes les civs, sinon ID specifique
    }
}
