using UnityEngine;

namespace CivVSCiv
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _zoomSpeed = 5f;
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 40f;

        private Camera _cam;
        private Vector3 _targetPos;
        private float _targetZoom;
        private bool _dragging;

        // Map bounds (computed from grid on first Update)
        private float _minX = -10f, _maxX = 105f;
        private float _minZ = -10f, _maxZ = 55f;
        private bool _boundsComputed;

        void Awake()
        {
            _cam = GetComponent<Camera>() ?? Camera.main;
            _targetPos = transform.position;
            _targetZoom = _cam.orthographic ? _cam.orthographicSize : 20f;
        }

        void Update()
        {
            // Compute map bounds lazily (grid may not exist at Awake)
            if (!_boundsComputed) ComputeBounds();

            // Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            _targetZoom -= scroll * _zoomSpeed;
            _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);

            // Right-click drag: pan in XZ plane
            if (Input.GetMouseButtonDown(1)) _dragging = true;
            if (Input.GetMouseButtonUp(1)) _dragging = false;

            if (_dragging && Input.GetMouseButton(1))
            {
                float s = _targetZoom * 2f / Screen.height;
                float mx = -Input.GetAxis("Mouse X");
                float my = -Input.GetAxis("Mouse Y");
                _targetPos += new Vector3(mx * s, 0, my * s);
            }

            // Clamp to map bounds
            _targetPos.x = Mathf.Clamp(_targetPos.x, _minX, _maxX);
            _targetPos.z = Mathf.Clamp(_targetPos.z, _minZ, _maxZ);

            // Apply
            transform.position = _targetPos;
            _cam.orthographicSize = _targetZoom;
        }

        private void ComputeBounds()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Width > 0 && gm.Height > 0)
            {
                float s3 = Mathf.Sqrt(3f);
                _minX = -10f;
                _maxX = s3 * (gm.Width + gm.Height / 2f) + 10f;
                _minZ = -10f;
                _maxZ = 1.5f * gm.Height + 10f;
                _boundsComputed = true;
            }
        }

        public void FocusOn(Vector3 pos) { _targetPos = pos; }
    }
}
