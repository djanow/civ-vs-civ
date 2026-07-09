using UnityEngine;
using System.Collections.Generic;

namespace CivVSCiv
{
    /// <summary>
    /// Rendu de la grille avec de vrais prismes hexagonaux 3D low-poly.
    /// Chaque tuile est un prisme hexagonal (top + bottom + 6 cotes) colore
    /// selon le type de terrain.
    /// </summary>
    public class HexGridRenderer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _hexSize = 1f;
        [SerializeField] private float _tileHeight = 0.3f;
        [SerializeField] private float _mountainHeight = 0.8f;
        [SerializeField] private float _hillHeight = 0.5f;

        private HexCell[,] _cells;
        private int _width, _height;
        private Transform _gridParent;
        private static Material[] _sharedMaterials;
        private static Mesh _hexPrismMesh;

        private static readonly Color[] TileColors =
        {
            new Color(0.25f, 0.65f, 0.95f),  // Sea - bleu
            new Color(0.12f, 0.35f, 0.65f),  // Ocean - bleu profond
            new Color(0.58f, 0.53f, 0.47f),  // Mountain - brun-gris
            new Color(0.40f, 0.82f, 0.35f),  // Hill - vert vif
            new Color(0.12f, 0.52f, 0.18f),  // Forest - vert fonce
            new Color(0.60f, 0.82f, 0.38f),  // Plain - vert clair
            new Color(0.93f, 0.82f, 0.55f),  // Desert - sable
            new Color(0.42f, 0.56f, 0.28f),  // Marsh - vert-brun
        };

        private static readonly Color[] SideColors =
        {
            new Color(0.18f, 0.48f, 0.72f), new Color(0.08f, 0.24f, 0.48f),
            new Color(0.45f, 0.40f, 0.35f), new Color(0.30f, 0.62f, 0.26f),
            new Color(0.08f, 0.38f, 0.13f), new Color(0.45f, 0.62f, 0.28f),
            new Color(0.72f, 0.64f, 0.42f), new Color(0.32f, 0.42f, 0.21f),
        };

        private void Awake()
        {
            _gridParent = new GameObject("HexGrid").transform;
            _gridParent.SetParent(transform);
            EnsureSharedResources();
            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private static void EnsureSharedResources()
        {
            if (_sharedMaterials != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            _sharedMaterials = new Material[8];

            // Generate procedural textures for each terrain type
            var textures = new Texture2D[8];

            // Wrap in try-catch so missing shaders don't crash everything
            try
            {
                textures[0] = TerrainTextureGenerator.GenerateSea();
                textures[1] = TerrainTextureGenerator.GenerateOcean();
                textures[2] = TerrainTextureGenerator.GenerateMountain();
                textures[3] = TerrainTextureGenerator.GenerateHill();
                textures[4] = TerrainTextureGenerator.GenerateForest();
                textures[5] = TerrainTextureGenerator.GeneratePlain();
                textures[6] = TerrainTextureGenerator.GenerateDesert();
                textures[7] = TerrainTextureGenerator.GenerateMarsh();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HexGridRenderer] Failed to generate textures: {e}");
            }

            for (int i = 0; i < 8; i++)
            {
                var mat = new Material(shader) { color = TileColors[i], name = $"TileMat_{i}" };

                // Assign procedural texture if available; fall back to flat color
                if (textures[i] != null)
                {
                    mat.mainTexture = textures[i];
                    // URP / HDRP compatible texture property
                    if (mat.HasProperty("_BaseMap"))
                        mat.SetTexture("_BaseMap", textures[i]);
                    if (mat.HasProperty("_BaseColorMap"))
                        mat.SetTexture("_BaseColorMap", textures[i]);
                }

                _sharedMaterials[i] = mat;
            }

            _hexPrismMesh = CreateHexPrismMesh();
        }

        private static Mesh CreateHexPrismMesh()
        {
            var mesh = new Mesh { name = "HexPrism" };
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();

            float h = 0.5f; // half-height (total height = 1, scaled by _tileHeight)
            int segments = 6;

            // Top and bottom center + rim vertices
            verts.Add(new Vector3(0, h, 0));  // 0: top center
            verts.Add(new Vector3(0, -h, 0)); // 1: bottom center

            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i - 30f);
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);
                verts.Add(new Vector3(x, h, z));   // top rim: 2,4,6,8,10,12
                verts.Add(new Vector3(x, -h, z));  // bottom rim: 3,5,7,9,11,13
            }

            // Top face (fan from center)
            for (int i = 0; i < segments; i++)
            {
                tris.Add(0);
                tris.Add(2 + i * 2);
                tris.Add(2 + ((i + 1) % segments) * 2);
            }

            // Bottom face (fan from center, reversed winding)
            for (int i = 0; i < segments; i++)
            {
                tris.Add(1);
                tris.Add(2 + ((i + 1) % segments) * 2 + 1);
                tris.Add(2 + i * 2 + 1);
            }

            // Side faces (quads = 2 triangles)
            for (int i = 0; i < segments; i++)
            {
                int tr = 2 + i * 2;       // top-right
                int tl = 2 + ((i + 1) % segments) * 2; // top-left
                int br = 2 + i * 2 + 1;   // bottom-right
                int bl = 2 + ((i + 1) % segments) * 2 + 1; // bottom-left

                tris.Add(tr); tris.Add(bl); tris.Add(tl);
                tris.Add(tr); tris.Add(br); tris.Add(bl);
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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
            // Clean old
            foreach (Transform child in _gridParent)
                Destroy(child.gameObject);

            // Dark ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GridGround";
            ground.transform.SetParent(_gridParent);
            ground.transform.position = new Vector3(_width * 0.75f, -0.06f, _height * 0.43f);
            ground.transform.localScale = new Vector3(_width * 0.15f + 0.5f, 1f, _height * 0.09f + 0.5f);
            var grndMat = new Material(Shader.Find("Standard"));
            grndMat.color = new Color(0.06f, 0.06f, 0.10f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = grndMat;

            EnsureSharedResources();

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _cells[x, y];
                    var pos = HexToWorld(cell.Coordinates);
                    CreateTile(cell, pos);
                }
            }
        }

        private void CreateTile(HexCell cell, Vector3 position)
        {
            var go = new GameObject($"Tile_{cell.Coordinates}");
            go.transform.SetParent(_gridParent);
            go.transform.position = new Vector3(position.x, 0f, position.z);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = _hexPrismMesh;

            var mr = go.AddComponent<MeshRenderer>();
            int idx = (int)cell.TileType;
            if (idx < 0 || idx >= 8) idx = 0;
            mr.sharedMaterial = _sharedMaterials[idx];

            // Scale: hex prism is unit size, scale by hexSize and height
            float height = _tileHeight;
            if (cell.TileType == TileType.Mountain && !cell.IsMountainPass)
                height = _mountainHeight;
            else if (cell.TileType == TileType.Hill)
                height = _hillHeight;

            go.transform.localScale = new Vector3(_hexSize * 0.93f, height, _hexSize * 0.93f);
        }

        public Vector3 HexToWorld(HexCoordinates hex)
        {
            float x = _hexSize * (1.5f * hex.Q);
            float z = _hexSize * (Mathf.Sqrt(3f) * (hex.R + hex.Q * 0.5f));
            return new Vector3(x, 0f, z);
        }

        public HexCoordinates WorldToHex(Vector3 worldPos)
        {
            float q = (2f / 3f) * worldPos.x / _hexSize;
            float r = (-1f / 3f * worldPos.x + Mathf.Sqrt(3f) / 3f * worldPos.z) / _hexSize;
            return HexRound(q, r);
        }

        private static HexCoordinates HexRound(float q, float r)
        {
            float s = -q - r;
            int rq = Mathf.RoundToInt(q), rr = Mathf.RoundToInt(r), rs = Mathf.RoundToInt(s);
            float qd = Mathf.Abs(rq - q), rd = Mathf.Abs(rr - r), sd = Mathf.Abs(rs - s);
            if (qd > rd && qd > sd) rq = -rr - rs;
            else if (rd > sd) rr = -rq - rs;
            return new HexCoordinates(rq, rr);
        }
    }
}
