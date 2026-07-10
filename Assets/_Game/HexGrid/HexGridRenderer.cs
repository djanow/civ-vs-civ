using UnityEngine;

namespace CivVSCiv
{
    public class HexGridRenderer : MonoBehaviour
    {
        private const float _hexSize = 1.0f; // KayKit tiles are radius=1, pointy-top spacing = sqrt(3) by 1.5

        private HexCell[,] _cells;
        private int _width, _height;
        private Transform _gridParent;
        private GameObject[] _tilePrefabs;
        private GameObject[] _decoPrefabs;
        private bool _useKayKit;
        private static Material[] _sharedMats;

        private static readonly Color[] FallbackColors = {
            new Color(0.2f, 0.6f, 1.0f), new Color(0.1f, 0.3f, 0.7f),
            new Color(0.44f, 0.4f, 0.32f), new Color(0.36f, 0.68f, 0.24f),
            new Color(0.08f, 0.4f, 0.12f), new Color(0.48f, 0.68f, 0.28f),
            new Color(0.76f, 0.68f, 0.4f), new Color(0.36f, 0.44f, 0.24f),
            new Color(0.64f, 0.72f, 0.8f), // Ice - desature
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

                        // Override KayKit embedded colors with our palette
                        foreach (var mr in tile.GetComponentsInChildren<MeshRenderer>())
                            mr.material = GetSharedMaterial(idx);

                        if (_decoPrefabs[idx] != null)
                        {
                            var deco = Instantiate(_decoPrefabs[idx], tile.transform);
                            deco.transform.localPosition = new Vector3(0, 0.05f, 0);
                            foreach (var mr in deco.GetComponentsInChildren<MeshRenderer>())
                                mr.material = GetSharedMaterial(idx);
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
            // Pointy-top layout: x = size * sqrt(3) * (q + r/2),  z = size * 3/2 * r
            float x = _hexSize * (Mathf.Sqrt(3f) * hex.Q + Mathf.Sqrt(3f) / 2f * hex.R);
            float z = _hexSize * (1.5f * hex.R);
            return new Vector3(x, 0f, z);
        }

        public HexCoordinates WorldToHex(Vector3 wp)
        {
            // Pointy-top inverse:  q = (sqrt(3)/3 * x - 1/3 * z) / size,  r = (2/3 * z) / size
            float q = (Mathf.Sqrt(3f) / 3f * wp.x - 1f / 3f * wp.z) / _hexSize;
            float r = (2f / 3f * wp.z) / _hexSize;
            return HexRound(q, r);
        }

        private static HexCoordinates HexRound(float q, float r)
        {
            float s = -q - r; int rq = Mathf.RoundToInt(q), rr = Mathf.RoundToInt(r), rs = Mathf.RoundToInt(s);
            float qd = Mathf.Abs(rq - q), rd = Mathf.Abs(rr - r), sd = Mathf.Abs(rs - s);
            if (qd > rd && qd > sd) rq = -rr - rs; else if (rd > sd) rr = -rq - rs;
            return new HexCoordinates(rq, rr);
        }
        private static Material GetSharedMaterial(int idx)
        {
            if (_sharedMats == null)
            {
                _sharedMats = new Material[9];
                var shader = Shader.Find("Standard");
                Color[] colors = {
                    new Color(0.18f, 0.45f, 0.70f), // Sea - muted blue
                    new Color(0.10f, 0.25f, 0.50f), // Ocean - deep blue
                    new Color(0.42f, 0.38f, 0.32f), // Mountain - brown-gray
                    new Color(0.35f, 0.60f, 0.25f), // Hill - muted green
                    new Color(0.12f, 0.38f, 0.15f), // Forest - dark green
                    new Color(0.45f, 0.60f, 0.28f), // Plain - green
                    new Color(0.72f, 0.62f, 0.38f), // Desert - sandy
                    new Color(0.32f, 0.40f, 0.20f), // Marsh - brown-green
                    new Color(0.65f, 0.72f, 0.80f), // Ice - blue-white
                };
                for (int i = 0; i < 9; i++)
                    _sharedMats[i] = new Material(shader) { color = colors[i] };
            }
            if (idx < 0 || idx >= 9) idx = 0;
            return _sharedMats[idx];
        }
    }
}
