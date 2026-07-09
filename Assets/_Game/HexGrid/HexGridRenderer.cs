using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CivVSCiv
{
    public class HexGridRenderer : MonoBehaviour
    {
        [SerializeField] private float _hexSize = 1f;
        [SerializeField] private float _tileHeight = 0.4f;
        [SerializeField] private float _mountainHeight = 0.9f;

        private HexCell[,] _cells;
        private int _width, _height;
        private Transform _gridParent;
        private GameObject[] _tilePrefabs; // indexed by TileType
        private bool _useKayKit;

        private static readonly Color[] FallbackColors = {
            new Color(0.2f, 0.6f, 1.0f), new Color(0.1f, 0.3f, 0.7f),
            new Color(0.55f, 0.5f, 0.4f), new Color(0.45f, 0.85f, 0.3f),
            new Color(0.1f, 0.5f, 0.15f), new Color(0.6f, 0.85f, 0.35f),
            new Color(0.95f, 0.85f, 0.5f), new Color(0.45f, 0.55f, 0.3f),
        };

        // KayKit naming conventions for auto-discovery
        private static readonly string[][] KayKitKeywords = {
            new[]{"water", "sea", "ocean_shallow", "coast"},   // Sea
            new[]{"ocean", "deep_water", "sea_deep"},           // Ocean
            new[]{"mountain", "rock", "cliff"},                 // Mountain
            new[]{"hill", "hills", "rocky"},                    // Hill
            new[]{"forest", "woods", "pine", "tree"},           // Forest
            new[]{"grass", "plains", "meadow", "plain"},        // Plain
            new[]{"desert", "sand", "dune"},                    // Desert
            new[]{"marsh", "swamp", "wetland"},                 // Marsh
        };

        private void Awake()
        {
            _gridParent = new GameObject("HexGrid").transform;
            _gridParent.SetParent(transform);
            _tilePrefabs = new GameObject[8];
            AutoDiscoverKayKit();
            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void AutoDiscoverKayKit()
        {
            // Find all GameObjects in the project (only works with loaded assets)
            var allAssets = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => go.scene.name == null && go.transform.parent == null)
                .ToArray();

            int found = 0;
            foreach (var go in allAssets)
            {
                string name = go.name.ToLowerInvariant();
                for (int i = 0; i < 8; i++)
                {
                    if (_tilePrefabs[i] != null) continue;
                    foreach (var kw in KayKitKeywords[i])
                    {
                        if (name.Contains(kw))
                        {
                            _tilePrefabs[i] = go;
                            found++;
                            Debug.Log($"[KayKit] Auto-linked {go.name} → TileType {i}");
                            break;
                        }
                    }
                }
            }

            if (found >= 2)
            {
                _useKayKit = true;
                Debug.Log($"[KayKit] Auto-discovered {found}/{8} tile prefabs!");
            }
            else
            {
                Debug.LogWarning("[KayKit] Could not auto-discover tiles. Using colored cubes.");
            }
        }

        private void OnDestroy() => EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);

        private GameObject GetPrefabFor(TileType t)
        {
            if (!_useKayKit) return null;
            int idx = (int)t;
            if (idx < 0 || idx >= 8) return null;

            // Fallback chain for missing prefabs
            if (_tilePrefabs[idx] != null) return _tilePrefabs[idx];
            // Try similar types
            if (t == TileType.Ocean) return _tilePrefabs[(int)TileType.Sea];
            if (t == TileType.Desert || t == TileType.Marsh) return _tilePrefabs[(int)TileType.Plain];
            if (t == TileType.Forest || t == TileType.Hill) return _tilePrefabs[(int)TileType.Plain];
            return _tilePrefabs[(int)TileType.Plain]; // ultimate fallback
        }

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            _cells = evt.Cells;
            _width = evt.Width;
            _height = evt.Height;
            // Retry auto-discovery (now that assets are loaded)
            if (!_useKayKit) AutoDiscoverKayKit();
            BuildGrid();
        }

        private void BuildGrid()
        {
            foreach (Transform child in _gridParent) Destroy(child.gameObject);

            // Ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GridGround";
            ground.transform.SetParent(_gridParent);
            ground.transform.position = new Vector3(_width * 0.75f, -0.1f, _height * 0.43f);
            ground.transform.localScale = new Vector3(_width * 0.15f + 1.5f, 1f, _height * 0.09f + 1.5f);
            ground.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"))
                { color = new Color(0.04f, 0.04f, 0.08f) };

            int count = 0;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _cells[x, y];
                    var pos = HexToWorld(cell.Coordinates);
                    var prefab = GetPrefabFor(cell.TileType);

                    GameObject go;
                    if (_useKayKit && prefab != null)
                    {
                        go = Instantiate(prefab, _gridParent);
                        go.transform.position = new Vector3(pos.x, 0, pos.z);
                    }
                    else
                    {
                        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.transform.SetParent(_gridParent);
                        go.transform.position = new Vector3(pos.x, 0, pos.z);

                        float h = _tileHeight;
                        if (cell.TileType == TileType.Mountain) h = _mountainHeight;
                        go.transform.localScale = new Vector3(0.85f, h, 0.85f);

                        int idx = (int)cell.TileType;
                        if (idx < 0 || idx >= 8) idx = 0;
                        var mr = go.GetComponent<MeshRenderer>();
                        mr.material = new Material(Shader.Find("Standard")) { color = FallbackColors[idx] };
                    }

                    go.name = $"T_{x}_{y}";
                    count++;
                }
            }
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
