using UnityEngine;
using System.Collections.Generic;

namespace CivVSCiv
{
    public class HexGridRenderer : MonoBehaviour
    {
        [SerializeField] private float _hexSize = 1f;

        private HexCell[,] _cells;
        private int _width, _height;
        private Transform _gridParent;
        private static Material[] _mats;
        private bool _built;

        private static readonly Color[] Colors = {
            new Color(0.2f, 0.6f, 1.0f),   // Sea
            new Color(0.1f, 0.3f, 0.7f),   // Ocean
            new Color(0.55f, 0.5f, 0.4f),  // Mountain
            new Color(0.45f, 0.85f, 0.3f), // Hill
            new Color(0.1f, 0.5f, 0.15f),  // Forest
            new Color(0.6f, 0.85f, 0.35f), // Plain
            new Color(0.95f, 0.85f, 0.5f), // Desert
            new Color(0.45f, 0.55f, 0.3f), // Marsh
        };

        private void Awake()
        {
            Debug.Log("[HexGridRenderer] Awake");
            _gridParent = new GameObject("HexGrid").transform;
            _gridParent.SetParent(transform);

            if (_mats == null)
            {
                _mats = new Material[8];
                var shader = Shader.Find("Standard");
                Debug.Log($"[HexGridRenderer] Using shader: {(shader != null ? shader.name : "NULL")}");
                for (int i = 0; i < 8; i++)
                {
                    _mats[i] = new Material(shader) { color = Colors[i] };
                }
            }

            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
            Debug.Log("[HexGridRenderer] Subscribed to MapGenerated");
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            Debug.Log($"[HexGridRenderer] OnMapGenerated received: {evt.Width}x{evt.Height}");

            _cells = evt.Cells;
            _width = evt.Width;
            _height = evt.Height;
            BuildGridDirect();
        }

        private void BuildGridDirect()
        {
            Debug.Log($"[HexGridRenderer] BuildGridDirect START: {_width}x{_height}");

            foreach (Transform child in _gridParent)
                Destroy(child.gameObject);

            // Dark ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GridGround";
            ground.transform.SetParent(_gridParent);
            ground.transform.position = new Vector3(_width * 0.75f, -0.06f, _height * 0.43f);
            ground.transform.localScale = new Vector3(_width * 0.15f + 1f, 1f, _height * 0.09f + 1f);
            var gm = ground.GetComponent<MeshRenderer>();
            gm.material = new Material(Shader.Find("Standard")) { color = new Color(0.05f, 0.05f, 0.08f) };

            int count = 0;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _cells[x, y];
                    var pos = HexToWorld(cell.Coordinates);

                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = $"T_{x}_{y}";
                    go.transform.SetParent(_gridParent);
                    go.transform.position = new Vector3(pos.x, 0, pos.z);
                    go.transform.localScale = new Vector3(0.85f, 0.4f, 0.85f);

                    int idx = (int)cell.TileType;
                    if (idx < 0 || idx >= 8) idx = 0;

                    var mr = go.GetComponent<MeshRenderer>();
                    mr.material = _mats[idx];

                    // Mountains taller
                    if (cell.TileType == TileType.Mountain)
                        go.transform.localScale = new Vector3(0.85f, 0.8f, 0.85f);

                    count++;
                }
            }

            _built = true;
            Debug.Log($"[HexGridRenderer] BuildGridDirect DONE: {count} cubes created");
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
