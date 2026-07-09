using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Configuration de generation de la carte.
    /// Instance creee dans Assets/_Game/Data/ et referencee par HexGridGenerator.
    /// </summary>
    [CreateAssetMenu(fileName = "HexGridData", menuName = "CivVSCiv/Hex Grid Data")]
    public class HexGridData : ScriptableObject
    {
        [Header("Dimensions")]
        public int Width = 40;
        public int Height = 30;

        [Header("Generation")]
        public int Seed;
        [Range(0f, 1f)]
        public float WaterLevel = 0.3f;
        [Range(0f, 1f)]
        public float MountainDensity = 0.1f;
        [Range(0f, 1f)]
        public float ForestDensity = 0.2f;

        [Header("Placement des civilisations")]
        public int MinDistanceBetweenCivs = 10;
        public TileType Civ1PreferredBiome = TileType.Plain;
        public TileType Civ2PreferredBiome = TileType.Hill;
    }
}
