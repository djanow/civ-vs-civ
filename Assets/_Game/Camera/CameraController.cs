using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Camera orbit globe — style Civilization Revolution 2.
    /// Orbite autour d'un point central avec une vue perspective.
    /// Zoom avant/arriere, rotation gauche/droite, inclinaison.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _distance = 30f;     // Distance au centre
        [SerializeField] private float _minDist = 8f;        // Zoom max (rapproche)
        [SerializeField] private float _maxDist = 60f;       // Zoom max (eloigne — vue globe)
        [SerializeField] private float _yaw = 0f;            // Rotation autour de Y
        [SerializeField] private float _pitch = 60f;         // Angle par rapport a l'horizontale
        [SerializeField] private float _orbitSpeed = 2f;
        [SerializeField] private float _zoomSpeed = 5f;
        [SerializeField] private float _damping = 8f;
        [SerializeField] private Vector3 _center = new Vector3(30, 0, 26);

        private Camera _cam;
        private float _targetDistance;
        private float _targetYaw;
        private float _targetPitch;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            _targetDistance = _distance;
            _targetYaw = _yaw;
            _targetPitch = _pitch;

            ApplyPosition();
        }

        private void Update()
        {
            // Orbite avec clic droit maintenu
            if (Input.GetMouseButton(1))
            {
                _targetYaw += Input.GetAxis("Mouse X") * _orbitSpeed;
                if (_targetDistance < 40f)
                    _targetPitch -= Input.GetAxis("Mouse Y") * _orbitSpeed;
                _targetPitch = Mathf.Clamp(_targetPitch, 20f, 85f);
            }

            // Zoom avec la molette
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _targetDistance -= scroll * _zoomSpeed * 5f;
                _targetDistance = Mathf.Clamp(_targetDistance, _minDist, _maxDist);
            }

            // Appliquer avec damping
            ApplySmooth();
        }

        private void ApplySmooth()
        {
            _yaw = Mathf.Lerp(_yaw, _targetYaw, Time.deltaTime * _damping);
            _pitch = Mathf.Lerp(_pitch, _targetPitch, Time.deltaTime * _damping);
            _distance = Mathf.Lerp(_distance, _targetDistance, Time.deltaTime * _damping);

            // Forcer la distance cible apres le damping pour eviter le depassement
            if (Mathf.Abs(_distance - _targetDistance) < 0.01f)
                _distance = _targetDistance;

            ApplyPosition();
        }

        private void ApplyPosition()
        {
            float pitchRad = _pitch * Mathf.Deg2Rad;
            float yawRad = _yaw * Mathf.Deg2Rad;

            Vector3 targetPos = _center + new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad) * _distance,
                Mathf.Sin(pitchRad) * _distance,
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad) * _distance
            );

            transform.position = targetPos;
            transform.LookAt(_center);
        }

        /// <summary>
        /// Reinitialise la camera a une vue globe par defaut.
        /// </summary>
        public void ResetToGlobeView()
        {
            _targetDistance = 35f;
            _targetYaw = 0f;
            _targetPitch = 55f;
        }

        /// <summary>
        /// Centre la camera sur des coordonnees hex donnees.
        /// </summary>
        public void FocusOnHex(HexCoordinates coords)
        {
            var (col, row) = coords.ToOffset();
            float wx = col * 1.5f;
            float wz = row * Mathf.Sqrt(3f) + (col % 2 == 1 ? Mathf.Sqrt(3f) * 0.5f : 0f);

            _center = new Vector3(wx, 0, wz);
            _targetDistance = 15f;
            _targetPitch = 40f;
        }
    }
}
