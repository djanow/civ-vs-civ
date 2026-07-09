using UnityEngine;

namespace CivVSCiv
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _panSpeed = 1f;
        [SerializeField] private float _zoomSpeed = 5f;
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 40f;
        [SerializeField] private float _damping = 8f;

        private Camera _cam;
        private Vector3 _targetPos;
        private float _targetZoom;

        void Awake()
        {
            _cam = GetComponent<Camera>() ?? Camera.main;
            _targetPos = transform.position;
            _targetZoom = _cam.orthographic ? _cam.orthographicSize : 20f;
        }

        private Vector3 _lastMouse;

        void Update()
        {
            // Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            _targetZoom -= scroll * _zoomSpeed;
            _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);

            // Right-click drag: pan
            if (Input.GetMouseButtonDown(1))
                _lastMouse = Input.mousePosition;
            else if (Input.GetMouseButton(1))
            {
                Vector3 worldDelta = Input.mousePosition - _lastMouse;
                _lastMouse = Input.mousePosition;
                float scale = _targetZoom * 2f / Screen.height;
                _targetPos -= new Vector3(worldDelta.x * scale, 0, worldDelta.y * scale);
            }

            // Apply
            transform.position = _targetPos;
            _cam.orthographicSize = _targetZoom;
        }

        public void FocusOn(Vector3 pos) { _targetPos = pos; }
    }
}
