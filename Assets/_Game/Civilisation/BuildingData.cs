using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Donnees d'un batiment constructible dans une cite.
    /// Les batiments uniques par civilisation heritent du meme ScriptableObject
    /// avec le flag IsUnique = true.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingData", menuName = "CivVSCiv/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Identite")]
        public string BuildingName;
        public string Description;

        [Header("Couts")]
        public int ProductionCost = 30;
        public int MaintenanceCost; // cout en or par tour

        [Header("Produits")]
        public int ScienceOutput;
        public int CultureOutput;
        public int GoldOutput;
        public int FoodOutput;

        [Header("Exclusivite")]
        public bool IsUnique; // true = batiment unique d'une civilisation

        [Header("Technologie requise")]
        public int RequiredTechId = -1; // -1 = accessible des le depart
    }
}
