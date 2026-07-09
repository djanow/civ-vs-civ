using System;

namespace CivVSCiv
{
    /// <summary>
    /// Coordonnées hexagonales cubiques (q, r, s) avec q + r + s = 0.
    /// Implémentation basée sur le guide redblobgames.
    /// </summary>
    [System.Serializable]
    public struct HexCoordinates : IEquatable<HexCoordinates>
    {
        public int Q { get; }
        public int R { get; }
        public int S => -Q - R;

        /// <summary>
        /// Les 6 directions dans l'ordre : E, NE, NW, W, SW, SE
        /// </summary>
        private static readonly HexCoordinates[] Directions = {
            new(1, 0), new(1, -1), new(0, -1),
            new(-1, 0), new(-1, 1), new(0, 1)
        };

        public HexCoordinates(int q, int r)
        {
            Q = q;
            R = r;
            // S est calculé automatiquement via la propriété
        }

        /// <summary>
        /// Distance hexagonale entre deux cellules (nombre de pas minimum).
        /// </summary>
        public int DistanceTo(HexCoordinates other)
        {
            int dq = Math.Abs(Q - other.Q);
            int dr = Math.Abs(R - other.R);
            int ds = Math.Abs(S - other.S);
            return (dq + dr + ds) / 2;
        }

        /// <summary>
        /// Retourne les 6 voisins de cette cellule.
        /// </summary>
        public HexCoordinates[] GetNeighbors()
        {
            var result = new HexCoordinates[6];
            for (int i = 0; i < 6; i++)
            {
                result[i] = new HexCoordinates(
                    Q + Directions[i].Q,
                    R + Directions[i].R);
            }
            return result;
        }

        /// <summary>
        /// Retourne les 6 voisins en wrap horizontal pour une carte cylindrique.
        /// Les coordonnees dont la colonne depasse les bornes sont ramenees
        /// de l'autre cote de la carte. Les voisins hors limites en Y sont exclus.
        /// </summary>
        public HexCoordinates[] GetNeighborsWrapped(int mapWidth, int mapHeight)
        {
            var raw = GetNeighbors();
            var result = new System.Collections.Generic.List<HexCoordinates>(6);
            foreach (var n in raw)
            {
                var (col, row) = n.ToOffset();
                if (row < 0 || row >= mapHeight) continue;
                // Wrap horizontal
                col = ((col % mapWidth) + mapWidth) % mapWidth;
                result.Add(FromOffset(col, row));
            }
            return result.ToArray();
        }

        /// <summary>
        /// Retourne la coordonnee en wrapant la colonne horizontalement
        /// dans les limites [0, mapWidth[. Utilise pour traverser le globe.
        /// </summary>
        public HexCoordinates Wrap(int mapWidth)
        {
            var (col, row) = ToOffset();
            col = ((col % mapWidth) + mapWidth) % mapWidth;
            return FromOffset(col, row);
        }

        /// <summary>
        /// Distance minimale entre deux cellules en tenant compte du wrap
        /// horizontal (carte cylindrique). Essaye la position originale et
        /// la position wrappee pour trouver le chemin le plus court.
        /// </summary>
        public int WrappedDistanceTo(HexCoordinates other, int mapWidth)
        {
            // Distance directe
            int direct = DistanceTo(other);

            // Distance en wrapant other d'une largeur a gauche et a droite
            var (oc, or) = other.ToOffset();
            var left = FromOffset(oc - mapWidth, or);
            var right = FromOffset(oc + mapWidth, or);

            int dLeft = DistanceTo(left);
            int dRight = DistanceTo(right);

            return Math.Min(direct, Math.Min(dLeft, dRight));
        }

        /// <summary>
        /// Conversion d'un offset "odd-r" (col, row) vers coordonnées cubiques.
        /// odd-r : les lignes impaires sont décalées vers la droite.
        /// </summary>
        public static HexCoordinates FromOffset(int col, int row)
        {
            int q = col - (row - (row & 1)) / 2;
            int r = row;
            return new HexCoordinates(q, r);
        }

        /// <summary>
        /// Conversion de coordonnées cubiques vers offset "odd-r".
        /// </summary>
        public (int col, int row) ToOffset()
        {
            int col = Q + (R - (R & 1)) / 2;
            int row = R;
            return (col, row);
        }

        public override string ToString() => $"({Q}, {R}, {S})";

        public bool Equals(HexCoordinates other)
        {
            return Q == other.Q && R == other.R;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoordinates other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Q, R);
        }

        public static bool operator ==(HexCoordinates a, HexCoordinates b) => a.Equals(b);
        public static bool operator !=(HexCoordinates a, HexCoordinates b) => !a.Equals(b);
    }
}
