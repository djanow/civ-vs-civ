using UnityEngine;
using System.Collections.Generic;

namespace CivVSCiv
{
    /// <summary>
    /// Rend la grille hexagonale avec des cubes plats colores.
    /// Chaque tuile est un cube PrimitiveType.Cube, positionne via HexToWorld,
    /// et colore selon son type de terrain avec un materiau partage.
    /// S'abonne a MapGenerated pour reconstruire la grille a chaque nouvelle carte.
    /// </summary>
    public class HexGridRenderer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _hexSize = 1f;

        private HexCell[,] _cells;
        private int _width, _height;
        private Transform _gridParent;
        private Dictionary<HexCoordinates, GameObject> _tileObjects = new();

        // Un seul materiau par type de terrain (8 au total), partage entre toutes les tuiles
        private static Material[] _sharedTileMaterials;

        // Couleurs vives et saturees par type de terrain (indice = TileType)
        private static readonly Color[] TileColors = new Color[]
        {
            new Color(0.2f, 0.6f, 1.0f),   // 0 Sea       - bleu vif
            new Color(0.1f, 0.3f, 0.7f),   // 1 Ocean     - bleu profond
            new Color(0.55f, 0.5f, 0.45f), // 2 Mountain  - brun-gris
            new Color(0.4f, 0.85f, 0.3f),  // 3 Hill      - vert vif
            new Color(0.1f, 0.5f, 0.15f),  // 4 Forest    - vert fonce
            new Color(0.55f, 0.8f, 0.35f), // 5 Plain     - vert clair
            new Color(0.95f, 0.85f, 0.5f), // 6 Desert    - jaune sable
            new Color(0.4f, 0.55f, 0.25f), // 7 Marsh     - vert-brun terne
        };

        private const int TerrainTypeCount = 8;

        private void Awake()
        {
            _gridParent = new GameObject("HexGrid").transform;
            _gridParent.SetParent(transform);

            EnsureSharedMaterials();

            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        /// <summary>
        /// Cree ou recupere les materiaux partages pour chaque type de terrain.
        /// Tente "Universal Render Pipeline/Unlit", puis "Unlit/Color", puis "Standard".
        /// </summary>
        private static void EnsureSharedMaterials()
        {
            if (_sharedTileMaterials != null && _sharedTileMaterials.Length == TerrainTypeCount)
                return;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");

            _sharedTileMaterials = new Material[TerrainTypeCount];

            for (int i = 0; i < TerrainTypeCount; i++)
            {
                var mat = new Material(shader);
                mat.color = TileColors[i];
                mat.name = $"TileMat_{i}";
                _sharedTileMaterials[i] = mat;
            }
        }

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            _cells = evt.Cells;
            _width = evt.Width;
            _height = evt.Height;
            BuildGrid();
        }

        private void BuildGrid()
        {
            // Nettoyer l'ancienne grille
            foreach (var go in _tileObjects.Values)
            {
                if (go != null) Destroy(go);
            }
            _tileObjects.Clear();

            // Plan de fond sombre pour faire apparaitre les bordures entre tuiles
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GridGround";
            ground.transform.SetParent(_gridParent);
            ground.transform.position = new Vector3(_width * 0.75f, -0.05f, _height * 0.43f);
            float gw = _width * 1.5f / 10f + 0.5f;
            float gh = _height * Mathf.Sqrt(3f) / 10f + 0.5f;
            ground.transform.localScale = new Vector3(gw, 1f, gh);
            var grndMR = ground.GetComponent<MeshRenderer>();
            var grndMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            grndMat.color = new Color(0.08f, 0.08f, 0.12f);
            grndMR.sharedMaterial = grndMat;

            // S'assurer que les materiaux partages existent
            EnsureSharedMaterials();

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _cells[x, y];
                    var worldPos = HexToWorld(cell.Coordinates);
                    CreateTile(cell, worldPos, x, y);
                }
            }
        }

        private void CreateTile(HexCell cell, Vector3 position, int gridX, int gridY)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Tile_{gridX}_{gridY}";
            go.transform.SetParent(_gridParent);
            go.transform.position = new Vector3(position.x, 0f, position.z);

            // Appliquer l'echelle : cube plat avec un gap visible entre les tuiles
            go.transform.localScale = new Vector3(0.85f, 0.1f, 0.85f);

            // Appliquer le materiau partage correspondant au type de terrain
            int matIndex = (int)cell.TileType;
            if (matIndex < 0 || matIndex >= TerrainTypeCount)
                matIndex = 0;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _sharedTileMaterials[matIndex];

            _tileObjects[cell.Coordinates] = go;
        }

        /// <summary>
        /// Convertit des coordonnees hex cubiques en position monde 3D.
        /// Utilise le layout "pointy-top" : les hexagones ont une pointe vers le haut.
        /// </summary>
        public Vector3 HexToWorld(HexCoordinates hex)
        {
            float x = _hexSize * (1.5f * hex.Q);
            float z = _hexSize * (Mathf.Sqrt(3f) * (hex.R + hex.Q * 0.5f));
            return new Vector3(x, 0f, z);
        }

        /// <summary>
        /// Convertit une position monde en coordonnees hexagonales.
        /// </summary>
        public HexCoordinates WorldToHex(Vector3 worldPos)
        {
            float q = (2f / 3f) * worldPos.x / _hexSize;
            float r = (-1f / 3f * worldPos.x + Mathf.Sqrt(3f) / 3f * worldPos.z) / _hexSize;
            return HexRound(q, r);
        }

        private static HexCoordinates HexRound(float q, float r)
        {
            float s = -q - r;
            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);

            float qDiff = Mathf.Abs(rq - q);
            float rDiff = Mathf.Abs(rr - r);
            float sDiff = Mathf.Abs(rs - s);

            if (qDiff > rDiff && qDiff > sDiff)
                rq = -rr - rs;
            else if (rDiff > sDiff)
                rr = -rq - rs;

            return new HexCoordinates(rq, rr);
        }
    }
}
