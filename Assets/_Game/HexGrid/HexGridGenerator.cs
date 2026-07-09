using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Genere une carte hexagonale proceduralement.
    /// Utilise le bruit de Perlin pour des biomes naturels et place
    /// les positions de depart equitablement.
    /// </summary>
    public static class HexGridGenerator
    {
        public static HexCell[,] Generate(HexGridData config, int civCount)
        {
            var cells = new HexCell[config.Width, config.Height];

            // Seed le random
            if (config.Seed == 0) config.Seed = System.DateTime.Now.GetHashCode();
            Random.InitState(config.Seed);

            // Etape 1 : generer la topographie (altitude via Perlin)
            float[,] elevation = GenerateElevation(config);
            // Etape 2 : generer l'humidite (pour les forets/marais)
            float[,] moisture = GenerateMoisture(config);

            // Etape 3 : assigner les types de tuile
            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    var coords = HexCoordinates.FromOffset(x, y);
                    var tileType = DetermineTileType(elevation[x, y], moisture[x, y], config);
                    cells[x, y] = new HexCell(coords, tileType);
                }
            }

            // Etape 4 : generer les rivieres
            GenerateRivers(cells, elevation, config);

            // Etape 5 : generer les cols de montagne
            GenerateMountainPasses(cells, config);

            // Etape 6 : placer les ressources
            PlaceResources(cells, config);

            // Etape 7 : placer les positions de depart
            var startPositions = PlaceStartPositions(cells, config, civCount);

            EventBus.Publish(new GameEvents.MapGenerated
            {
                Cells = cells,
                Width = config.Width,
                Height = config.Height
            });

            EventBus.Publish(new GameEvents.CivStartPositions
            {
                StartPositions = startPositions
            });

            return cells;
        }

        private static float[,] GenerateElevation(HexGridData config)
        {
            var elevation = new float[config.Width, config.Height];
            float scale = 0.05f;
            float offsetX = Random.Range(0f, 10000f);
            float offsetY = Random.Range(0f, 10000f);

            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    // Utiliser les coordonnees du monde (position spatiale de l'hex)
                    var coords = HexCoordinates.FromOffset(x, y);
                    float wx = coords.Q * 1.5f * scale + offsetX;
                    float wy = (coords.R + coords.Q * 0.5f) * Mathf.Sqrt(3f) * scale + offsetY;
                    elevation[x, y] = Mathf.PerlinNoise(wx, wy);
                }
            }
            return elevation;
        }

        private static float[,] GenerateMoisture(HexGridData config)
        {
            var moisture = new float[config.Width, config.Height];
            float scale = 0.07f;
            float offsetX = Random.Range(0f, 10000f);
            float offsetY = Random.Range(0f, 10000f);

            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    var coords = HexCoordinates.FromOffset(x, y);
                    float wx = coords.Q * 1.5f * scale + offsetX;
                    float wy = (coords.R + coords.Q * 0.5f) * Mathf.Sqrt(3f) * scale + offsetY;
                    moisture[x, y] = Mathf.PerlinNoise(wx, wy);
                }
            }
            return moisture;
        }

        private static TileType DetermineTileType(float elevation, float moisture, HexGridData config)
        {
            if (elevation < config.WaterLevel - 0.05f)
                return TileType.Ocean;
            if (elevation < config.WaterLevel)
                return TileType.Sea;
            if (elevation > 0.8f)
                return TileType.Mountain;

            // Terrains selon l'humidite
            if (moisture > 0.6f)
                return elevation > 0.5f ? TileType.Forest : TileType.Marsh;
            if (moisture < 0.2f)
                return TileType.Desert;

            return elevation > 0.5f ? TileType.Hill : TileType.Plain;
        }

        private static void GenerateRivers(HexCell[,] cells, float[,] elevation, HexGridData config)
        {
            int riverCount = config.Width * config.Height / 150;
            for (int i = 0; i < riverCount; i++)
            {
                int sx = Random.Range(5, config.Width - 5);
                int sy = Random.Range(5, config.Height - 5);

                // Ne demarre une riviere que sur une colline ou montagne
                if (elevation[sx, sy] < 0.5f) continue;

                int cx = sx, cy = sy;
                int length = 0;
                while (length < 40)
                {
                    cells[cx, cy].HasRiver = true;

                    // Chercher le voisin le plus bas
                    float lowest = elevation[cx, cy];
                    int nx = cx, ny = cy;
                    var coords = HexCoordinates.FromOffset(cx, cy);
                    foreach (var neighbor in coords.GetNeighbors())
                    {
                        var (nx2, ny2) = neighbor.ToOffset();
                        if (nx2 < 0 || nx2 >= config.Width || ny2 < 0 || ny2 >= config.Height)
                            continue;
                        if (elevation[nx2, ny2] < lowest)
                        {
                            lowest = elevation[nx2, ny2];
                            nx = nx2; ny = ny2;
                        }
                    }

                    if (nx == cx && ny == cy) break; // Plus bas que tous les voisins
                    cx = nx; cy = ny;
                    length++;
                }
            }
        }

        private static void GenerateMountainPasses(HexCell[,] cells, HexGridData config)
        {
            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    if (cells[x, y].TileType != TileType.Mountain) continue;

                    var coords = HexCoordinates.FromOffset(x, y);
                    int plainNeighbors = 0;
                    foreach (var n in coords.GetNeighbors())
                    {
                        var (nx, ny) = n.ToOffset();
                        if (nx < 0 || nx >= config.Width || ny < 0 || ny >= config.Height) continue;
                        var neighborType = cells[nx, ny].TileType;
                        if (neighborType != TileType.Mountain && neighborType != TileType.Ocean)
                            plainNeighbors++;
                    }

                    // Un col relie deux zones non-montagneuses
                    if (plainNeighbors >= 2 && Random.value < 0.3f)
                    {
                        cells[x, y].IsMountainPass = true;
                    }
                }
            }
        }

        private static void PlaceResources(HexCell[,] cells, HexGridData config)
        {
            // Pour la phase 1, on place quelques ressources aleatoirement
            // Le systeme complet viendra en phase 2
            int luxuryCount = config.Width * config.Height / 30;
            for (int i = 0; i < luxuryCount; i++)
            {
                int x = Random.Range(0, config.Width);
                int y = Random.Range(0, config.Height);
                if (cells[x, y].TileType != TileType.Sea &&
                    cells[x, y].TileType != TileType.Ocean &&
                    cells[x, y].TileType != TileType.Mountain)
                {
                    cells[x, y].LuxuryResourceId = Random.Range(0, 4); // 4 types de luxe
                }
            }
        }

        private static HexCoordinates[] PlaceStartPositions(
            HexCell[,] cells, HexGridData config, int civCount)
        {
            var positions = new HexCoordinates[civCount];
            for (int i = 0; i < civCount; i++)
            {
                bool valid;
                int attempts = 0;
                int x, y;
                do
                {
                    x = Random.Range(5, config.Width - 5);
                    y = Random.Range(5, config.Height - 5);
                    valid = cells[x, y].MovementCost > 0; // Terrain franchissable

                    // Verifier distance minimale avec les autres departs
                    var coords = HexCoordinates.FromOffset(x, y);
                    for (int j = 0; j < i && valid; j++)
                    {
                        if (coords.DistanceTo(positions[j]) < config.MinDistanceBetweenCivs)
                            valid = false;
                    }

                    // Verifier le biome prefere
                    if (valid && i == 0 && cells[x, y].TileType != config.Civ1PreferredBiome)
                        valid = false;
                    if (valid && i == 1 && cells[x, y].TileType != config.Civ2PreferredBiome)
                        valid = false;

                    attempts++;
                } while (!valid && attempts < 500);

                positions[i] = HexCoordinates.FromOffset(x, y);
            }
            return positions;
        }
    }
}
