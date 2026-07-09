using UnityEngine;
using UnityEngine.InputSystem;

namespace CivVSCiv
{
    /// <summary>
    /// Controle de camera pour mobile et desktop :
    /// - 1 doigt : pan (drag)
    /// - 2 doigts : pinch-to-zoom
    /// - Desktop : clic droit drag (pan), molette (zoom)
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Zoom")]
        [SerializeField] private float _minZoom = 3f;
        [SerializeField] private float _maxZoom = 20f;
        [SerializeField] private float _zoomSpeed = 0.01f;
        [SerializeField] private float _zoomDamping = 5f;
        [SerializeField] private float _defaultZoom = 10f;

        [Header("Pan")]
        [SerializeField] private float _panSpeed = 1f;
        [SerializeField] private float _panDamping = 8f;

        [Header("Bounds")]
        [SerializeField] private float _mapWidth = 60f;
        [SerializeField] private float _mapHeight = 50f;
        [SerializeField] private float _boundsPadding = 2f;

        private Camera _cam;
        private Vector3 _targetPosition;
        private float _targetZoom;
        private Vector3 _dragOrigin;
        private bool _isDragging;

        // Pinch
        private float _previousPinchDistance;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            _targetZoom = _defaultZoom;
            _cam.orthographicSize = _defaultZoom;

            // Position initiale : centree sur la carte
            _targetPosition = new Vector3(_mapWidth / 2f, transform.position.y, _mapHeight / 2f);
            transform.position = _targetPosition;
        }

        private void Update()
        {
            HandleTouchInput();
            HandleDesktopInput();
            ApplySmoothing();
        }

        private void HandleTouchInput()
        {
            if (Touchscreen.current == null) return;

            var touches = Touchscreen.current.touches;

            if (touches.Count == 1)
            {
                // Pan a 1 doigt
                var touch = touches[0];
                if (touch.press.wasPressedThisFrame)
                {
                    _dragOrigin = GetWorldPoint(touch.position.ReadValue());
                    _isDragging = true;
                }
                else if (_isDragging && touch.press.isPressed)
                {
                    Vector3 currentWorld = GetWorldPoint(touch.position.ReadValue());
                    Vector3 delta = _dragOrigin - currentWorld;
                    _targetPosition += delta * _panSpeed;
                    ClampPosition();
                }
                else
                {
                    _isDragging = false;
                }
            }
            else if (touches.Count == 2)
            {
                _isDragging = false;

                // Pinch to zoom
                var t0 = touches[0].position.ReadValue();
                var t1 = touches[1].position.ReadValue();
                float currentDistance = Vector2.Distance(t0, t1);

                if (touches[0].press.wasPressedThisFrame ||
                    touches[1].press.wasPressedThisFrame)
                {
                    _previousPinchDistance = currentDistance;
                }
                else
                {
                    float delta = _previousPinchDistance - currentDistance;
                    _targetZoom += delta * _zoomSpeed;
                    _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
                    _previousPinchDistance = currentDistance;
                }
            }
        }

        private void HandleDesktopInput()
        {
            // Zoom molette
            float scroll = Mouse.current?.scroll.y.ReadValue() ?? 0f;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetZoom -= scroll * 2f;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }

            // Pan clic droit
            if (Mouse.current?.rightButton.wasPressedThisFrame == true)
            {
                _dragOrigin = GetWorldPoint(Mouse.current.position.ReadValue());
                _isDragging = true;
            }
            else if (_isDragging && Mouse.current?.rightButton.isPressed == true)
            {
                Vector3 currentWorld = GetWorldPoint(Mouse.current.position.ReadValue());
                Vector3 delta = _dragOrigin - currentWorld;
                _targetPosition += delta * _panSpeed;
                ClampPosition();
            }
            else if (Mouse.current?.rightButton.wasReleasedThisFrame == true)
            {
                _isDragging = false;
            }
        }

        private Vector3 GetWorldPoint(Vector2 screenPoint)
        {
            // Pour orthographique, le Z n'importe pas mais doit etre coherent
            return _cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, _cam.transform.position.y));
        }

        private void ApplySmoothing()
        {
            // Zoom lisse
            _cam.orthographicSize = Mathf.Lerp(
                _cam.orthographicSize, _targetZoom,
                Time.deltaTime * _zoomDamping);

            // Position lisse
            transform.position = Vector3.Lerp(
                transform.position,
                new Vector3(_targetPosition.x, transform.position.y, _targetPosition.z),
                Time.deltaTime * _panDamping);
        }

        private void ClampPosition()
        {
            // Les limites dependent du zoom (plus on est zoome, moins on peut sortir)
            float vertExtent = _cam.orthographicSize;
            float horzExtent = vertExtent * _cam.aspect;

            _targetPosition.x = Mathf.Clamp(
                _targetPosition.x,
                -_boundsPadding + horzExtent,
                _mapWidth + _boundsPadding - horzExtent);

            _targetPosition.z = Mathf.Clamp(
                _targetPosition.z,
                -_boundsPadding + vertExtent,
                _mapHeight + _boundsPadding - vertExtent);
        }
    }
}
