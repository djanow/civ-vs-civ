using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Camera orbit simple et fiable.
    /// - Molette : zoom avant/arriere
    /// - Clic milieu maintenu OU Ctrl+Clic droit : orbite autour du centre
    /// - Clic gauche libre pour interaction unite
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 _center = new Vector3(30, 0, 30);
        [SerializeField] private float _distance = 35f;
        [SerializeField] private float _minDist = 10f;
        [SerializeField] private float _maxDist = 70f;
        [SerializeField] private float _pitch = 55f; // 0=horizontal, 90=top-down
        [SerializeField] private float _yaw = 0f;

        private Camera _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            ApplyPosition();
        }

        void Update()
        {
            bool orbit = Input.GetMouseButton(2) // Middle mouse button
                || (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftControl)); // Ctrl+right click

            if (orbit)
            {
                _yaw += Input.GetAxis("Mouse X") * 3f;
                _pitch -= Input.GetAxis("Mouse Y") * 3f;
                _pitch = Mathf.Clamp(_pitch, 15f, 80f);
            }

            _distance -= Input.GetAxis("Mouse ScrollWheel") * 5f;
            _distance = Mathf.Clamp(_distance, _minDist, _maxDist);

            // Smooth interpolation vers la position calculee
            float p = _pitch * Mathf.Deg2Rad;
            float y = _yaw * Mathf.Deg2Rad;
            Vector3 targetPos = _center + new Vector3(
                Mathf.Sin(y) * Mathf.Cos(p) * _distance,
                Mathf.Sin(p) * _distance,
                Mathf.Cos(y) * Mathf.Cos(p) * _distance
            );
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
            transform.LookAt(_center);
        }

        private void ApplyPosition()
        {
            float p = _pitch * Mathf.Deg2Rad;
            float y = _yaw * Mathf.Deg2Rad;
            transform.position = _center + new Vector3(
                Mathf.Sin(y) * Mathf.Cos(p) * _distance,
                Mathf.Sin(p) * _distance,
                Mathf.Cos(y) * Mathf.Cos(p) * _distance
            );
            transform.LookAt(_center);
        }

        /// <summary>
        /// Centre progressivement la camera sur un point du monde.
        /// </summary>
        public void FocusOn(Vector3 worldPos)
        {
            _center = Vector3.Lerp(_center, worldPos, 0.3f);
        }

        /// <summary>
        /// Tourne la camera vers le territoire d'un joueur.
        /// </summary>
        public void LookAtPlayer(int playerIndex)
        {
            _yaw = playerIndex == 0 ? -30f : 150f;
            _distance = 35f;
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
            _distance = 15f;
            _pitch = 40f;
        }

        /// <summary>
        /// Reinitialise la camera a une vue globe par defaut.
        /// </summary>
        public void ResetToGlobeView()
        {
            _distance = 35f;
            _yaw = 0f;
            _pitch = 55f;
        }
    }
}
