using UnityEngine;
using UnityEngine.UI;

namespace CivVSCiv
{
    /// <summary>
    /// Widget minimap affiché en bas à gauche.
    /// Utilise une RenderTexture + caméra secondaire pour le rendu.
    /// </summary>
    public class MinimapWidget : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage _minimapImage;
        [SerializeField] private RectTransform _viewportRect;
        [SerializeField] private Vector2 _minimapSize = new Vector2(180f, 140f);
        [SerializeField] private int _minimapResolution = 256;

        [Header("Colors")]
        [SerializeField] private Color _seaColor = new Color(0.29f, 0.56f, 0.85f);
        [SerializeField] private Color _landColor = new Color(0.56f, 0.78f, 0.43f);
        [SerializeField] private Color _mountainColor = new Color(0.54f, 0.54f, 0.54f);
        [SerializeField] private Color _viewportColor = Color.white;

        private Camera _minimapCamera;
        private RenderTexture _minimapRT;
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;

            // Auto-trouver les refs UI si non assignées (zero-setup)
            if (_minimapImage == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    var mm = canvas.transform.Find("Minimap");
                    if (mm != null)
                    {
                        _minimapImage = mm.GetComponent<RawImage>();
                        _viewportRect = mm.Find("ViewportRect")?.GetComponent<RectTransform>();
                    }
                }
            }

            SetupMinimapCamera();
            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
            if (_minimapRT != null)
                _minimapRT.Release();
        }

        private void SetupMinimapCamera()
        {
            var camGo = new GameObject("MinimapCamera");
            camGo.transform.SetParent(transform);

            _minimapCamera = camGo.AddComponent<Camera>();
            _minimapCamera.orthographic = true;
            _minimapCamera.orthographicSize = 35f;
            _minimapCamera.cullingMask = LayerMask.GetMask("Minimap");
            _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            _minimapCamera.backgroundColor = Color.black;
            _minimapCamera.transform.position = new Vector3(0, 100f, 0);
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            _minimapRT = new RenderTexture(_minimapResolution, _minimapResolution, 16);
            _minimapCamera.targetTexture = _minimapRT;

            if (_minimapImage != null)
                _minimapImage.texture = _minimapRT;
        }

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            // Ajuster la taille ortho de la caméra minimap
            float mapWorldWidth = evt.Width * 1.5f;
            float mapWorldHeight = evt.Height * Mathf.Sqrt(3f);
            _minimapCamera.orthographicSize = Mathf.Max(mapWorldWidth, mapWorldHeight) / 2f;
        }

        private void LateUpdate()
        {
            UpdateViewportRect();
        }

        private void UpdateViewportRect()
        {
            if (_viewportRect == null || _mainCamera == null) return;

            // Calculer la position et taille du viewport en proportion de la carte
            float mapW = GameManager.Instance?.Width ?? 40;
            float mapH = GameManager.Instance?.Height ?? 30;

            float camHalfW = _mainCamera.orthographicSize * _mainCamera.aspect;
            float camHalfH = _mainCamera.orthographicSize;

            float mapWorldW = mapW * 1.5f;
            float mapWorldH = mapH * Mathf.Sqrt(3f);

            float normX = _mainCamera.transform.position.x / mapWorldW;
            float normZ = _mainCamera.transform.position.z / mapWorldH;
            float normW = (camHalfW * 2f) / mapWorldW;
            float normH = (camHalfH * 2f) / mapWorldH;

            _viewportRect.anchorMin = new Vector2(normX - normW / 2f, 1f - normZ + normH / 2f);
            _viewportRect.anchorMax = new Vector2(normX + normW / 2f, 1f - normZ - normH / 2f);
        }

        /// <summary>
        /// Permet d'assigner les références UI depuis l'extérieur (zero-setup).
        /// </summary>
        public void SetUIRefs(RawImage minimapImage, RectTransform viewportRect)
        {
            _minimapImage = minimapImage;
            _viewportRect = viewportRect;
            if (_minimapImage != null)
                _minimapImage.texture = _minimapRT;
        }

        /// <summary>
        /// Convertit un clic sur la minimap en position monde.
        /// </summary>
        public Vector3 MinimapToWorld(Vector2 minimapClickUV)
        {
            float mapWorldW = (GameManager.Instance?.Width ?? 40) * 1.5f;
            float mapWorldH = (GameManager.Instance?.Height ?? 30) * Mathf.Sqrt(3f);

            return new Vector3(
                minimapClickUV.x * mapWorldW,
                0f,
                (1f - minimapClickUV.y) * mapWorldH);
        }
    }
}
