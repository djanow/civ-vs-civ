using UnityEngine;

namespace CivVSCiv
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _minZoom = 3f, _maxZoom = 20f, _zoomSpeed = 0.01f, _zoomDamping = 5f, _defaultZoom = 10f;
        [SerializeField] private float _panSpeed = 1f, _panDamping = 8f;
        [SerializeField] private float _mapWidth = 60f, _mapHeight = 50f, _boundsPadding = 2f;

        private Camera _cam;
        private Vector3 _targetPos;
        private float _targetZoom;
        private Vector3 _dragOrigin;
        private bool _dragging;
        private float _prevPinch;

        private void Awake()
        {
            _cam = GetComponent<Camera>() ?? Camera.main;
            _targetZoom = _defaultZoom; _cam.orthographicSize = _defaultZoom;
            _targetPos = new Vector3(_mapWidth / 2f, transform.position.y, _mapHeight / 2f);
            transform.position = _targetPos;
        }
        private void Update() { Touch(); Desktop(); Smooth(); }

        private void Touch()
        {
            if (Input.touchCount == 0) return;
            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) { _dragOrigin = W(t.position); _dragging = true; }
                else if (_dragging && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
                { _targetPos += (_dragOrigin - W(t.position)) * _panSpeed; Clip(); }
                else if (t.phase >= TouchPhase.Ended) _dragging = false;
            }
            else { _dragging = false; var a = Input.GetTouch(0); var b = Input.GetTouch(1);
                if (a.phase == TouchPhase.Began || b.phase == TouchPhase.Began) _prevPinch = Vector2.Distance(a.position,b.position);
                else if (a.phase == TouchPhase.Moved || b.phase == TouchPhase.Moved)
                { float d = Vector2.Distance(a.position,b.position); _targetZoom += (_prevPinch - d) * _zoomSpeed; _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom); _prevPinch = d; }
            }
        }

        private void Desktop()
        {
            float s = Input.GetAxis("Mouse ScrollWheel"); if (Mathf.Abs(s) > 0.001f) { _targetZoom -= s * 10f; _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom); }
            if (Input.GetMouseButtonDown(1)) { _dragOrigin = W(Input.mousePosition); _dragging = true; }
            else if (_dragging && Input.GetMouseButton(1)) { _targetPos += (_dragOrigin - W(Input.mousePosition)) * _panSpeed; Clip(); }
            else if (Input.GetMouseButtonUp(1)) _dragging = false;
        }

        private Vector3 W(Vector2 s) => _cam.ScreenToWorldPoint(new Vector3(s.x, s.y, _cam.transform.position.y));
        private void Smooth() { _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, Time.deltaTime * _zoomDamping); transform.position = Vector3.Lerp(transform.position, new Vector3(_targetPos.x, transform.position.y, _targetPos.z), Time.deltaTime * _panDamping); }
        private void Clip() { float v = _cam.orthographicSize, h = v * _cam.aspect; _targetPos.x = Mathf.Clamp(_targetPos.x, -_boundsPadding + h, _mapWidth + _boundsPadding - h); _targetPos.z = Mathf.Clamp(_targetPos.z, -_boundsPadding + v, _mapHeight + _boundsPadding - v); }
    }
}
