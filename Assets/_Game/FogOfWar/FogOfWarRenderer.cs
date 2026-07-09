using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Affiche le brouillard de guerre par-dessus la grille.
    /// Utilise des quads semi-transparents placés au-dessus de chaque hex.
    /// En phase 1, l'overlay est simple (gris pour exploré, noir pour caché).
    /// </summary>
    public class FogOfWarRenderer : MonoBehaviour
    {
        [SerializeField] private Material _hiddenMaterial;    // Noir opaque
        [SerializeField] private Material _exploredMaterial;   // Gris semi-transparent
        [SerializeField] private Material _visibleMaterial;    // Transparent (pas de fog)
        [SerializeField] private float _fogHeight = 0.05f;     // Légèrement au-dessus des tuiles
        [SerializeField] private int _currentPlayerIndex = 0;

        private FogOfWarManager _fogManager;
        private HexGridRenderer _gridRenderer;
        private GameObject _fogParent;
        private GameObject[,] _fogQuads;

        private void Awake()
        {
            _fogManager = GetComponent<FogOfWarManager>();
            if (_fogManager == null)
                _fogManager = gameObject.AddComponent<FogOfWarManager>();

            if (_gridRenderer == null)
                _gridRenderer = FindAnyObjectByType<HexGridRenderer>();

            // Auto-création des matériaux si non assignés
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null)
                unlitShader = Shader.Find("Unlit/Texture");
            if (unlitShader == null)
                unlitShader = Shader.Find("Standard");

            if (_hiddenMaterial == null)
            {
                _hiddenMaterial = new Material(unlitShader);
                _hiddenMaterial.color = Color.black;
                _hiddenMaterial.name = "FogHidden";
            }

            if (_exploredMaterial == null)
            {
                _exploredMaterial = new Material(unlitShader);
                _exploredMaterial.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
                _exploredMaterial.name = "FogExplored";
            }

            if (_visibleMaterial == null)
            {
                _visibleMaterial = new Material(unlitShader);
                _visibleMaterial.color = new Color(1f, 1f, 1f, 0f);
                _visibleMaterial.name = "FogVisible";
            }

            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
            EventBus.Subscribe<GameEvents.PlayerTurnStarted>(OnPlayerTurnStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
            EventBus.Unsubscribe<GameEvents.PlayerTurnStarted>(OnPlayerTurnStarted);
        }

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            BuildFogOverlay(evt.Width, evt.Height);
        }

        private void OnPlayerTurnStarted(GameEvents.PlayerTurnStarted evt)
        {
            _currentPlayerIndex = evt.PlayerIndex;
            UpdateAllFogQuads();
        }

        private void BuildFogOverlay(int width, int height)
        {
            if (_gridRenderer == null)
            {
                _gridRenderer = FindAnyObjectByType<HexGridRenderer>();
                if (_gridRenderer == null)
                {
                    Debug.LogError("[FogOfWarRenderer] Cannot build fog overlay: HexGridRenderer not found");
                    return;
                }
            }

            if (_fogParent != null) Destroy(_fogParent);
            _fogParent = new GameObject("FogOverlay");
            _fogParent.transform.SetParent(transform);
            _fogQuads = new GameObject[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var coords = HexCoordinates.FromOffset(x, y);
                    var worldPos = _gridRenderer.HexToWorld(coords);
                    worldPos.y += _fogHeight;

                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = $"Fog_{coords}";
                    quad.transform.SetParent(_fogParent.transform);
                    quad.transform.position = worldPos;
                    quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    quad.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

                    var mr = quad.GetComponent<MeshRenderer>();
                    // All quads start with hidden material (black); visibility is updated
                    // when units reveal areas via UpdateAllFogQuads()
                    mr.material = _hiddenMaterial;

                    _fogQuads[x, y] = quad;
                }
            }

            Debug.Log($"[FogOfWarRenderer] Built {width}x{height} fog overlay ({width * height} quads)");
        }

        public void UpdateAllFogQuads()
        {
            if (_fogQuads == null) return;

            int width = _fogQuads.GetLength(0);
            int height = _fogQuads.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (_fogQuads[x, y] == null) continue;

                    var coords = HexCoordinates.FromOffset(x, y);
                    var mr = _fogQuads[x, y].GetComponent<MeshRenderer>();

                    if (_fogManager.IsVisible(coords, _currentPlayerIndex))
                    {
                        mr.material = _visibleMaterial;
                        mr.enabled = false;
                    }
                    else if (_fogManager.HasBeenExplored(coords, _currentPlayerIndex))
                    {
                        mr.material = _exploredMaterial;
                        mr.enabled = true;
                    }
                    else
                    {
                        mr.material = _hiddenMaterial;
                        mr.enabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Révèle une zone autour d'une position (appelé par les unités).
        /// </summary>
        public void RevealArea(HexCoordinates center, int range, int playerIndex)
        {
            _fogManager.UpdateVisibility(center, range, playerIndex);
            UpdateAllFogQuads();
        }
    }
}
