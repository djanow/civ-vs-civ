using UnityEngine;
using System.Collections.Generic;

namespace CivVSCiv
{
    public class HexGridRenderer : MonoBehaviour
    {
        [Header("KayKit Prefabs (glisser depuis Assets/KayKit.../Prefabs/)")]
        [SerializeField] private GameObject _prefabPlains;
        [SerializeField] private GameObject _prefabForest;
        [SerializeField] private GameObject _prefabHill;
        [SerializeField] private GameObject _prefabMountain;
        [SerializeField] private GameObject _prefabWater;
        [SerializeField] private GameObject _prefabDesert;
        [SerializeField] private GameObject _prefabMarsh;
        [SerializeField] private GameObject _prefabOcean;

        [SerializeField] private float _hexSize = 1f;

        private HexCell[,] _cells;
        private int _width, _height;
        private Transform _gridParent;
        private static Material[] _fallbackMats;
        private bool _useKayKit;

        private static readonly Color[] FallbackColors = {
            new Color(0.2f, 0.6f, 1.0f), new Color(0.1f, 0.3f, 0.7f),
            new Color(0.55f, 0.5f, 0.4f), new Color(0.45f, 0.85f, 0.3f),
            new Color(0.1f, 0.5f, 0.15f), new Color(0.6f, 0.85f, 0.35f),
            new Color(0.95f, 0.85f, 0.5f), new Color(0.45f, 0.55f, 0.3f),
        };

        private void Awake()
        {
            _gridParent = new GameObject("HexGrid").transform;
            _gridParent.SetParent(transform);
            _useKayKit = (_prefabPlains != null || _prefabWater != null);

            if (!_useKayKit)
            {
                _fallbackMats = new Material[8];
                var shader = Shader.Find("Standard");
                for (int i = 0; i < 8; i++)
                    _fallbackMats[i] = new Material(shader) { color = FallbackColors[i] };
            }

            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnDestroy() => EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            _cells = evt.Cells;
            _width = evt.Width;
            _height = evt.Height;
            BuildGrid();
        }

        GameObject PrefabFor(TileType t)
        {
            switch (t)
            {
                case TileType.Sea: return _prefabWater;
                case TileType.Ocean: return _prefabOcean ?? _prefabWater;
                case TileType.Mountain: return _prefabMountain ?? _prefabHill;
                case TileType.Hill: return _prefabHill;
                case TileType.Forest: return _prefabForest;
                case TileType.Plain: return _prefabPlains;
                case TileType.Desert: return _prefabDesert ?? _prefabPlains;
                case TileType.Marsh: return _prefabMarsh ?? _prefabPlains;
                default: return _prefabPlains;
            }
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

                    GameObject go;
                    var kayPrefab = PrefabFor(cell.TileType);

                    if (_useKayKit && kayPrefab != null)
                    {
                        go = Instantiate(kayPrefab, _gridParent);
                        go.transform.position = new Vector3(pos.x, 0, pos.z);
                    }
                    else
                    {
                        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.transform.SetParent(_gridParent);
                        go.transform.position = new Vector3(pos.x, 0, pos.z);
                        go.transform.localScale = new Vector3(0.85f, 0.4f, 0.85f);

                        int idx = (int)cell.TileType;
                        if (idx < 0 || idx >= 8) idx = 0;
                        var mr = go.GetComponent<MeshRenderer>();
                        mr.material = _fallbackMats[idx];

                        if (cell.TileType == TileType.Mountain)
                            go.transform.localScale = new Vector3(0.85f, 0.9f, 0.85f);
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
