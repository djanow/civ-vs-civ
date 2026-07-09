using UnityEngine;

namespace CivVSCiv
{
    public class HexGridRenderer : MonoBehaviour
    {
        private const float _hexSize = 1.1547f; // = 2/sqrt(3) — matches KayKit tile circumradius

        private HexCell[,] _cells;
        private int _width, _height;
        private Transform _gridParent;
        private GameObject[] _tilePrefabs;
        private GameObject[] _decoPrefabs;
        private bool _useKayKit;

        private static readonly Color[] FallbackColors = {
            new Color(0.2f, 0.6f, 1.0f), new Color(0.1f, 0.3f, 0.7f),
            new Color(0.55f, 0.5f, 0.4f), new Color(0.45f, 0.85f, 0.3f),
            new Color(0.1f, 0.5f, 0.15f), new Color(0.6f, 0.85f, 0.35f),
            new Color(0.95f, 0.85f, 0.5f), new Color(0.45f, 0.55f, 0.3f),
            new Color(0.8f, 0.9f, 1.0f), // Ice - blanc-bleute
        };

        private static readonly (string tile, string deco)[] KayKitMapping = {
            ("KayKit/tiles/base/hex_water",        null),
            ("KayKit/tiles/base/hex_water",        null),
            ("KayKit/tiles/base/hex_grass",        "KayKit/decoration/nature/mountain_A"),
            ("KayKit/tiles/base/hex_grass",        "KayKit/decoration/nature/hills_A"),
            ("KayKit/tiles/base/hex_grass",        "KayKit/decoration/nature/trees_A_medium"),
            ("KayKit/tiles/base/hex_grass",        null),
            ("KayKit/tiles/base/hex_grass",        null),
            ("KayKit/tiles/base/hex_grass",        null),
            ("KayKit/tiles/base/hex_water",        null), // Ice -> hex_water (rendu comme glace)
        };

        private void Awake()
        {
            _gridParent = new GameObject("HexGrid").transform;
            _gridParent.SetParent(transform);
            _tilePrefabs = new GameObject[9];
            _decoPrefabs = new GameObject[9];
            LoadKayKit();
            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void LoadKayKit()
        {
            int found = 0;
            for (int i = 0; i < 9; i++)
            {
                var (tilePath, decoPath) = KayKitMapping[i];
                _tilePrefabs[i] = Resources.Load<GameObject>(tilePath);
                if (!string.IsNullOrEmpty(decoPath))
                    _decoPrefabs[i] = Resources.Load<GameObject>(decoPath);
                if (_tilePrefabs[i] != null) found++;
            }
            _useKayKit = found >= 2;
            Debug.Log(_useKayKit ? $"[KayKit] Loaded {found}/9 types" : "[KayKit] Fallback to cubes");
        }

        private void OnDestroy() => EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            _cells = evt.Cells; _width = evt.Width; _height = evt.Height;
            if (!_useKayKit) LoadKayKit();
            BuildGrid();
        }

        private void BuildGrid()
        {
            foreach (Transform child in _gridParent) Destroy(child.gameObject);

            int count = 0;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _cells[x, y];
                    var pos = HexToWorld(cell.Coordinates);
                    int idx = (int)cell.TileType;
                    if (idx < 0 || idx >= 9) idx = 0;

                    if (_useKayKit && _tilePrefabs[idx] != null)
                    {
                        var tile = Instantiate(_tilePrefabs[idx], _gridParent);
                        tile.name = $"T_{x}_{y}";
                        tile.transform.position = new Vector3(pos.x, 0, pos.z);
                        tile.transform.localScale = new Vector3(0.95f, 1f, 0.95f);
                        tile.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                        // KayKit tiles are pointy-top; rotate 90deg to match flat-top grid layout

                        if (_decoPrefabs[idx] != null)
                        {
                            var deco = Instantiate(_decoPrefabs[idx], tile.transform);
                            deco.transform.localPosition = new Vector3(0, 0.05f, 0);
                        }
                    }
                    else
                    {
                        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.transform.SetParent(_gridParent);
                        go.transform.position = new Vector3(pos.x, 0, pos.z);
                        go.transform.localScale = new Vector3(0.85f, 0.4f, 0.85f);
                        go.name = $"T_{x}_{y}";
                        go.GetComponent<MeshRenderer>().material =
                            new Material(Shader.Find("Standard")) { color = FallbackColors[idx] };
                    }
                    count++;
                }
            }
        }

        public Vector3 HexToWorld(HexCoordinates hex)
        {
            float x = _hexSize * (1.5f * hex.Q);
            float z = _hexSize * (Mathf.Sqrt(3f) / 2f * hex.Q + Mathf.Sqrt(3f) * hex.R);
            return new Vector3(x, 0f, z);
        }

        public HexCoordinates WorldToHex(Vector3 wp)
        {
            // Flat-top inverse
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
