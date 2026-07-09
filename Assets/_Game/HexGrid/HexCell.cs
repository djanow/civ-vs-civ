using UnityEngine;

namespace CivVSCiv
{
    public enum TileType
    {
        Sea,        // Cotiere, impraticable sauf navires
        Ocean,      // Haute mer, impraticable sauf navires avances
        Mountain,   // Infranchissable sauf cols
        Hill,       // +1 defense, bonus production
        Forest,     // +1 defense, -1 mouvement, cache les unites
        Plain,      // Neutre, ideale agriculture
        Desert,     // -1 nourriture, bonus or
        Marsh       // -1 mouvement, malus defense
    }

    /// <summary>
    /// Donnees d'une cellule hexagonale sur la carte.
    /// Serialisable pour la sauvegarde.
    /// </summary>
    [System.Serializable]
    public class HexCell
    {
        public HexCoordinates Coordinates;
        public TileType TileType;
        public int OwnerIndex = -1;        // -1 = neutre
        public bool IsVisible;             // Actuellement revele (pas dans le fog)
        public bool HasBeenExplored;       // Deja decouvert (brouillard persistant)
        public bool HasRiver;
        public bool IsMountainPass;        // Col de montagne
        public int LuxuryResourceId = -1;  // -1 = pas de ressource de luxe
        public int StrategicResourceId = -1;

        public HexCell(HexCoordinates coords, TileType tileType)
        {
            Coordinates = coords;
            TileType = tileType;
        }

        /// <summary>
        /// Cout de mouvement pour entrer dans cette cellule.
        /// Retourne -1 si infranchissable.
        /// </summary>
        public int MovementCost
        {
            get
            {
                switch (TileType)
                {
                    case TileType.Sea:
                    case TileType.Ocean:
                    case TileType.Mountain when !IsMountainPass:
                        return -1; // Infranchissable
                    case TileType.Forest:
                    case TileType.Marsh:
                        return 2;
                    case TileType.Hill:
                        return 2;
                    default:
                        return 1;
                }
            }
        }

        /// <summary>
        /// Bonus de defense confere par le terrain.
        /// </summary>
        public int DefenseBonus
        {
            get
            {
                switch (TileType)
                {
                    case TileType.Forest:
                        return 2;
                    case TileType.Hill:
                        return 1;
                    case TileType.Marsh:
                        return -1;
                    default:
                        return 0;
                }
            }
        }
    }
}
