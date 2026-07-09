using UnityEngine;
using UnityEngine.UI;

namespace CivVSCiv
{
    public class MinimapWidget : MonoBehaviour
    {
        [SerializeField] private RawImage _minimapImage;
        [SerializeField] private RectTransform _viewportRect;
        [SerializeField] private int _minimapResolution = 256;

        private Camera _minimapCamera;
        private RenderTexture _minimapRT;
        private Camera _mainCamera;
        private bool _visible = false;
        private Button _toggleBtn;
        private GameObject _minimapGO;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _visible = false;

            // Minimap desactivee : ne pas creer les elements UI
            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void ToggleMinimap()
        {
            _visible = !_visible;
            if (_minimapGO != null) _minimapGO.SetActive(_visible);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
            if (_minimapRT != null) _minimapRT.Release();
        }

        private void SetupMinimapCamera()
        {
            var camGo = new GameObject("MinimapCamera");
            camGo.transform.SetParent(transform);
            _minimapCamera = camGo.AddComponent<Camera>();
            _minimapCamera.orthographic = true;
            _minimapCamera.orthographicSize = 35f;
            _minimapCamera.cullingMask = 1 << LayerMask.NameToLayer("Default");
            _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            _minimapCamera.backgroundColor = Color.black;
            _minimapCamera.transform.position = new Vector3(30f, 100f, 26f);
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _minimapRT = new RenderTexture(_minimapResolution, _minimapResolution, 16);
            _minimapCamera.targetTexture = _minimapRT;
            if (_minimapImage != null) _minimapImage.texture = _minimapRT;
        }

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            if (_minimapGO != null) _minimapGO.SetActive(_visible);
            if (_minimapCamera == null) return;
            float w = evt.Width * 1.5f, h = evt.Height * Mathf.Sqrt(3f);
            _minimapCamera.transform.position = new Vector3(w / 2f, 100f, h / 2f);
            _minimapCamera.orthographicSize = Mathf.Max(w, h) / 2f;
        }

        private void LateUpdate() { if (_visible) UpdateViewport(); }

        private void UpdateViewport()
        {
            if (_viewportRect == null || _mainCamera == null) return;
            float mw = GameManager.Instance?.Width ?? 40;
            float mh = GameManager.Instance?.Height ?? 30;
            float ww = mw * 1.5f, wh = mh * Mathf.Sqrt(3f);

            // Camera world position on the map plane
            Vector3 camPos = _mainCamera.transform.position;

            // Normalized map coordinates (0-1)
            float nx = Mathf.Clamp01(camPos.x / ww);
            float nz = Mathf.Clamp01(camPos.z / wh);

            // Flip Z for minimap Y (world Z up -> screen Y down in minimap)
            float my = 1f - nz;

            // Indicator size correlates with camera height (closer = bigger indicator)
            float zoomFactor = Mathf.Clamp01(1f - (camPos.y - 10f) / 50f);
            float s = 0.03f + zoomFactor * 0.07f;

            _viewportRect.anchorMin = new Vector2(Mathf.Clamp01(nx - s), Mathf.Clamp01(my - s));
            _viewportRect.anchorMax = new Vector2(Mathf.Clamp01(nx + s), Mathf.Clamp01(my + s));
        }

        public void SetUIRefs(RawImage img, RectTransform vp) { _minimapImage = img; _viewportRect = vp; if (_minimapImage != null) _minimapImage.texture = _minimapRT; }
    }
}
