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

        void Update()
        {
            // Right-click drag: pan
            if (Input.GetMouseButton(1))
            {
                Vector3 delta = _cam.ScreenToWorldPoint(new Vector3(
                    Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"), _cam.transform.position.y))
                    - _cam.ScreenToWorldPoint(Vector3.zero);
                _targetPos -= delta * _panSpeed;
            }

            // Scroll: zoom
            _targetZoom -= Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed;
            _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);

            // Smooth
            transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * _damping);
            if (_cam.orthographic)
                _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, Time.deltaTime * _damping);
        }

        public void FocusOn(Vector3 pos) { _targetPos = pos; }
    }
}
