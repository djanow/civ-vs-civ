using UnityEngine;
using System.Collections.Generic;

namespace CivVSCiv
{
    /// <summary>
    /// Rend la grille hexagonale en 3D low-poly.
    /// S'abonne à MapGenerated pour reconstruire le mesh à chaque nouvelle carte.
    /// </summary>
    public class HexGridRenderer : MonoBehaviour
    {
        [Header("Prefabs & Materials")]
        [SerializeField] private Material[] _tileMaterials; // Indexé par TileType
        [SerializeField] private Material _riverMaterial;
        [SerializeField] private float _hexSize = 1f;
        [SerializeField] private float _mountainHeight = 0.4f;
        [SerializeField] private float _hillHeight = 0.15f;

        private HexCell[,] _cells;
        private int _width, _height;
        private Transform _gridParent;
        private Dictionary<HexCoordinates, GameObject> _tileObjects = new();

        // Cache du mesh hexagonal
        private static Mesh _hexMesh;

        private void Awake()
        {
            _gridParent = new GameObject("HexGrid").transform;
            _gridParent.SetParent(transform);

            // Auto-création des matériaux si aucun n'est assigné
            if (_tileMaterials == null || _tileMaterials.Length == 0)
            {
                _tileMaterials = new Material[8];
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Standard");

                Color[] tileColors = new Color[]
                {
                    new Color(0.35f, 0.70f, 0.95f),  // 0 Sea - bleu vif
                    new Color(0.15f, 0.35f, 0.65f),  // 1 Ocean - bleu profond
                    new Color(0.65f, 0.60f, 0.55f),  // 2 Mountain - gris chaud
                    new Color(0.45f, 0.80f, 0.30f),  // 3 Hill - vert vif
                    new Color(0.15f, 0.55f, 0.15f),  // 4 Forest - vert fonce
                    new Color(0.65f, 0.85f, 0.40f),  // 5 Plain - vert clair
                    new Color(0.90f, 0.80f, 0.50f),  // 6 Desert - sable dore
                    new Color(0.50f, 0.60f, 0.30f),  // 7 Marsh - vert-brun
                };

                for (int i = 0; i < 8; i++)
                {
                    var mat = new Material(shader);
                    mat.color = tileColors[i];
                    mat.name = $"TileMat_{i}";
                    _tileMaterials[i] = mat;
                }
            }

            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
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

            if (_hexMesh == null)
                _hexMesh = CreateHexMesh();

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _cells[x, y];
                    var worldPos = HexToWorld(cell.Coordinates);
                    CreateTile(cell, worldPos);
                }
            }
        }

        private void CreateTile(HexCell cell, Vector3 position)
        {
            var go = new GameObject($"Hex_{cell.Coordinates}");
            go.transform.SetParent(_gridParent);
            go.transform.position = position;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = _hexMesh;

            var mr = go.AddComponent<MeshRenderer>();
            int matIndex = (int)cell.TileType;
            mr.sharedMaterial = matIndex < _tileMaterials.Length
                ? _tileMaterials[matIndex]
                : _tileMaterials[0];

            // Leger retrecissement pour creer des bordures visibles entre tuiles
            float gap = 0.93f;

            // Surélévation pour collines et montagnes
            if (cell.TileType == TileType.Mountain && !cell.IsMountainPass)
            {
                go.transform.localScale = new Vector3(gap, gap + _mountainHeight, gap);
            }
            else if (cell.TileType == TileType.Hill)
            {
                go.transform.localScale = new Vector3(gap, gap + _hillHeight, gap);
            }
            else
            {
                go.transform.localScale = new Vector3(gap, 1f, gap);
            }

            _tileObjects[cell.Coordinates] = go;
        }

        /// <summary>
        /// Convertit des coordonnées hex cubiques en position monde 3D.
        /// Utilise le layout "pointy-top" : les hexagones ont une pointe vers le haut.
        /// </summary>
        public Vector3 HexToWorld(HexCoordinates hex)
        {
            float x = _hexSize * (1.5f * hex.Q);
            float z = _hexSize * (Mathf.Sqrt(3f) * (hex.R + hex.Q * 0.5f));
            return new Vector3(x, 0f, z);
        }

        /// <summary>
        /// Convertit une position monde en coordonnées hexagonales.
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

        /// <summary>
        /// Crée un mesh hexagonal "pointy-top" de taille unitaire.
        /// 6 triangles du centre vers les 6 sommets, commençant à 30°.
        /// </summary>
        private static Mesh CreateHexMesh()
        {
            var mesh = new Mesh { name = "HexMesh" };
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            // Centre
            verts.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // 6 sommets (pointy-top : commencer à 30°)
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i - 30f);
                verts.Add(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
                uvs.Add(new Vector2(
                    Mathf.Cos(angle) * 0.5f + 0.5f,
                    Mathf.Sin(angle) * 0.5f + 0.5f));
            }

            // 6 triangles (centre → bord)
            for (int i = 1; i <= 6; i++)
            {
                tris.Add(0);
                tris.Add(i);
                tris.Add(i == 6 ? 1 : i + 1);
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
