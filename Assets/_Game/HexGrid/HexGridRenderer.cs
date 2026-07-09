using UnityEngine;
using System.Collections.Generic;

namespace CivVSCiv
{
    public class HexGridRenderer : MonoBehaviour
    {
        [SerializeField] private float _hexSize = 1f;
        [SerializeField] private float _tileHeight = 0.4f;

        private HexCell[,] _cells;
        private int _width, _height;
        private Transform _gridParent;
        private GameObject[] _tilePrefabs;
        private GameObject[] _decoPrefabs; // mountains, trees, hills placed on top
        private bool _useKayKit;

        private static readonly Color[] FallbackColors = {
            new Color(0.2f, 0.6f, 1.0f), new Color(0.1f, 0.3f, 0.7f),
            new Color(0.55f, 0.5f, 0.4f), new Color(0.45f, 0.85f, 0.3f),
            new Color(0.1f, 0.5f, 0.15f), new Color(0.6f, 0.85f, 0.35f),
            new Color(0.95f, 0.85f, 0.5f), new Color(0.45f, 0.55f, 0.3f),
        };

        // Exact KayKit paths (without .fbx extension, relative to Resources/)
        private static readonly (string tile, string deco)[] KayKitMapping = {
            ("KayKit/tiles/base/hex_water",        null),                                    // 0 Sea
            ("KayKit/tiles/base/hex_water",        null),                                    // 1 Ocean
            ("KayKit/tiles/base/hex_grass",        "KayKit/decoration/nature/mountain_A"),   // 2 Mountain
            ("KayKit/tiles/base/hex_grass",        "KayKit/decoration/nature/hills_A"),      // 3 Hill
            ("KayKit/tiles/base/hex_grass",        "KayKit/decoration/nature/trees_A_medium"),// 4 Forest
            ("KayKit/tiles/base/hex_grass",        null),                                    // 5 Plain
            ("KayKit/tiles/base/hex_grass",        null),                                    // 6 Desert (fallback: grass)
            ("KayKit/tiles/base/hex_grass",        null),                                    // 7 Marsh (fallback: grass)
        };

        private void Awake()
        {
            _gridParent = new GameObject("HexGrid").transform;
            _gridParent.SetParent(transform);
            _tilePrefabs = new GameObject[8];
            _decoPrefabs = new GameObject[8];
            LoadKayKit();
            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void LoadKayKit()
        {
            int found = 0;
            for (int i = 0; i < 8; i++)
            {
                var (tilePath, decoPath) = KayKitMapping[i];
                _tilePrefabs[i] = Resources.Load<GameObject>(tilePath);
                if (!string.IsNullOrEmpty(decoPath))
                    _decoPrefabs[i] = Resources.Load<GameObject>(decoPath);

                if (_tilePrefabs[i] != null)
                {
                    found++;
                    string deco = _decoPrefabs[i] != null ? $" + {_decoPrefabs[i].name}" : "";
                    Debug.Log($"[KayKit] Type {i}: {_tilePrefabs[i].name}{deco}");
                }
            }

            _useKayKit = found >= 2;
            Debug.Log(_useKayKit
                ? $"[KayKit] Loaded! ({found}/8 tile types)"
                : "[KayKit] Not enough tiles, using colored cubes");
        }

        private void OnDestroy() => EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            _cells = evt.Cells;
            _width = evt.Width;
            _height = evt.Height;
            if (!_useKayKit) LoadKayKit();
            BuildGrid();
        }

        private void BuildGrid()
        {
            foreach (Transform child in _gridParent) Destroy(child.gameObject);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GridGround"; ground.transform.SetParent(_gridParent);
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
                    int idx = (int)cell.TileType;
                    if (idx < 0 || idx >= 8) idx = 0;

                    if (_useKayKit && _tilePrefabs[idx] != null)
                    {
                        var tile = Instantiate(_tilePrefabs[idx], _gridParent);
                        tile.name = $"T_{x}_{y}";
                        tile.transform.position = new Vector3(pos.x, 0, pos.z);

                        // Mountains: bigger scale
                        if (idx == 2) // Mountain
                            tile.transform.localScale = Vector3.one * 1.5f;

                        // Add decoration (trees, hills) on top
                        if (_decoPrefabs[idx] != null)
                        {
                            var deco = Instantiate(_decoPrefabs[idx], tile.transform);
                            deco.transform.localPosition = Vector3.zero;
                            deco.name = $"D_{x}_{y}";
                        }
                    }
                    else
                    {
                        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.transform.SetParent(_gridParent);
                        go.transform.position = new Vector3(pos.x, 0, pos.z);
                        go.transform.localScale = new Vector3(0.85f, _tileHeight, 0.85f);
                        go.name = $"T_{x}_{y}";
                        go.GetComponent<MeshRenderer>().material =
                            new Material(Shader.Find("Standard")) { color = FallbackColors[idx] };
                    }

                    count++;
                }
            }
        }

        public Vector3 HexToWorld(HexCoordinates hex)
            => new(_hexSize * 1.5f * hex.Q, 0f, _hexSize * Mathf.Sqrt(3f) * (hex.R + hex.Q * 0.5f));

        public HexCoordinates WorldToHex(Vector3 wp)
        {
            float q = (2f / 3f) * wp.x / _hexSize;
            float r = (-1f / 3f * wp.x + Mathf.Sqrt(3f) / 3f * wp.z) / _hexSize;
            return HexRound(q, r);
        }

        private static HexCoordinates HexRound(float q, float r)
        {
            float s = -q - r; int rq = Mathf.RoundToInt(q), rr = Mathf.RoundToInt(r), rs = Mathf.RoundToInt(s);
            float qd = Mathf.Abs(rq - q), rd = Mathf.Abs(rr - r), sd = Mathf.Abs(rs - s);
            if (qd > rd && qd > sd) rq = -rr - rs; else if (rd > sd) rr = -rq - rs;
            return new HexCoordinates(rq, rr);
        }
    }
}
