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

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Toggle button (small circle/icon, always visible)
            var btnGo = new GameObject("MinimapToggle", typeof(Image), typeof(Button));
            btnGo.transform.SetParent(canvas.transform, false);
            var btnRT = btnGo.GetComponent<RectTransform>();
            btnRT.anchorMin = btnRT.anchorMax = new Vector2(0, 0);
            btnRT.pivot = new Vector2(0, 0);
            btnRT.anchoredPosition = new Vector2(8, 8);
            btnRT.sizeDelta = new Vector2(32, 32);
            btnGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            _toggleBtn = btnGo.GetComponent<Button>();
            _toggleBtn.onClick.AddListener(ToggleMinimap);

            // Label on toggle
            var lbl = new GameObject("Label", typeof(Text));
            lbl.transform.SetParent(btnGo.transform, false);
            var txt = lbl.GetComponent<Text>();
            txt.text = "🗺";
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = lblRT.anchorMax = Vector2.zero;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
            lblRT.sizeDelta = new Vector2(32, 32);

            // Minimap container (hidden by default until MapGenerated)
            _minimapGO = new GameObject("MinimapContainer");
            _minimapGO.transform.SetParent(canvas.transform, false);
            _minimapGO.SetActive(false);

            var bg = _minimapGO.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);
            var bgRT = _minimapGO.GetComponent<RectTransform>();
            bgRT.anchorMin = bgRT.anchorMax = new Vector2(0, 0);
            bgRT.pivot = new Vector2(0, 0);
            bgRT.anchoredPosition = new Vector2(8, 44);
            bgRT.sizeDelta = new Vector2(188, 148);

            // Minimap RawImage
            var mm = new GameObject("Minimap", typeof(RawImage));
            mm.transform.SetParent(_minimapGO.transform, false);
            _minimapImage = mm.GetComponent<RawImage>();
            var mmRT = _minimapImage.GetComponent<RectTransform>();
            mmRT.anchorMin = mmRT.anchorMax = Vector2.zero;
            mmRT.offsetMin = new Vector2(4, 4);
            mmRT.offsetMax = new Vector2(0, 0);
            mmRT.sizeDelta = new Vector2(180, 140);

            // Viewport
            var vp = new GameObject("ViewportRect", typeof(Image));
            vp.transform.SetParent(_minimapGO.transform, false);
            vp.GetComponent<Image>().color = new Color(1, 1, 1, 0.25f);
            _viewportRect = vp.GetComponent<RectTransform>();
            _viewportRect.anchorMin = _viewportRect.anchorMax = Vector2.zero;
            _viewportRect.offsetMin = new Vector2(4, 4);
            _viewportRect.offsetMax = new Vector2(0, 0);
            _viewportRect.sizeDelta = new Vector2(180, 140);

            SetupMinimapCamera();
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
