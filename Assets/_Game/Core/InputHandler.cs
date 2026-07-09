using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CivVSCiv
{
    /// <summary>
    /// Gère les clics souris sur la carte hexagonale :
    /// - Sélection d'unité (clic gauche sur unité alliée)
    /// - Déplacement d'unité (clic gauche sur case vide après sélection)
    /// - Aperçu combat (clic sur unité ennemie adjacente)
    /// - Ouverture panneau de cité (clic sur cité)
    /// - Désélection (clic droit ou Escape)
    ///
    /// Créé automatiquement par GameManager.ResolveManagers().
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        private Unit _selectedUnit;
        private List<HexCoordinates> _currentPath;
        private GameObject _selectionRing;
        private readonly List<GameObject> _pathMarkers = new List<GameObject>();

        private HexGridRenderer _gridRenderer;
        private UnitManager _unitManager;
        private CityPanel _cityPanel;
        private Camera _mainCamera;

        private static readonly Color FriendlyUnitColor = new Color(0.2f, 0.9f, 0.2f);  // Vert
        private static readonly Color PathMarkerColor = new Color(1f, 0.9f, 0.3f);      // Jaune
        private static readonly Color CityOwnerPurple = new Color(0.6f, 0.2f, 0.8f);    // Violet
        private static readonly Color CityOwnerBlue   = new Color(0.2f, 0.5f, 0.9f);    // Bleu

        private void Awake()
        {
            _mainCamera = Camera.main;
            _gridRenderer = FindAnyObjectByType<HexGridRenderer>();
            _unitManager = FindAnyObjectByType<UnitManager>();
        }

        /// <summary>
        /// Trouve ou crée la CityPanel (appelé une fois que le Canvas existe).
        /// </summary>
        public void EnsureCityPanel()
        {
            if (_cityPanel != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.GetComponentInChildren<CityPanel>(true);
            if (existing != null)
            {
                _cityPanel = existing;
                return;
            }

            var cpGo = new GameObject("CityPanel", typeof(RectTransform));
            cpGo.transform.SetParent(canvas.transform, false);
            _cityPanel = cpGo.AddComponent<CityPanel>();
        }

        private void Update()
        {
            if (GameManager.Instance == null ||
                GameManager.Instance.CurrentState != GameState.Playing)
                return;

            // S'assurer que la CityPanel existe
            if (_cityPanel == null)
                EnsureCityPanel();

            HandleInput();
        }

        // ----------------------------------------------------------------
        // Gestion des clics
        // ----------------------------------------------------------------

        private void HandleInput()
        {
            // Clic droit ou Escape → désélectionner
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                DeselectUnit();
                return;
            }

            // Clic gauche
            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClick();
            }
        }

        private void HandleLeftClick()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            // Intersection avec le plan y=0 (le sol de la grille)
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter))
                return;

            Vector3 worldPoint = ray.GetPoint(enter);
            HexCoordinates clickedHex = _gridRenderer.WorldToHex(worldPoint);

            // Valider les limites
            if (!GameManager.Instance.IsCellInBounds(clickedHex))
                return;

            var cells = GameManager.Instance.Cells;
            int currentPlayer = GameManager.Instance.TurnManager?.CurrentPlayerIndex ?? -1;

            // Vérifier s'il y a une unité à cette position
            Unit unitAtHex = _unitManager?.GetUnitAt(clickedHex);

            // ---- Cas 1 : Clic sur une unité alliée → sélectionner ----
            if (unitAtHex != null && unitAtHex.OwnerIndex == currentPlayer && !unitAtHex.IsDead())
            {
                SelectUnit(unitAtHex);
                return;
            }

            // ---- Cas 2 : Une unité est sélectionnée ----
            if (_selectedUnit != null)
            {
                // Clic sur une unité ennemie adjacente → exécuter le combat
                if (unitAtHex != null && unitAtHex.OwnerIndex != currentPlayer && !unitAtHex.IsDead())
                {
                    if (_selectedUnit.Position.DistanceTo(clickedHex) == 1)
                    {
                        ExecuteCombat(unitAtHex);
                    }
                    return;
                }

                // Clic sur une case vide et accessible → déplacement
                var (cx, cy) = clickedHex.ToOffset();
                var cell = cells[cx, cy];

                if (cell.MovementCost >= 0 && _selectedUnit.CanMoveTo(clickedHex, cells))
                {
                    MoveUnitTo(clickedHex);
                }
                return;
            }

            // ---- Cas 3 : Clic sur une cité → ouvrir le panneau ----
            if (_cityPanel != null)
            {
                var cityAtHex = FindCityAt(clickedHex);
                if (cityAtHex != null)
                {
                    ShowCityPanel(cityAtHex);
                }
            }
        }

        // ----------------------------------------------------------------
        // Sélection / Désélection
        // ----------------------------------------------------------------

        /// <summary>
        /// Sélectionne une unité alliée et affiche un anneau de sélection.
        /// </summary>
        private void SelectUnit(Unit unit)
        {
            if (_selectedUnit == unit) return;

            // Nettoyer la sélection précédente
            DeselectUnit();

            _selectedUnit = unit;

            // Créer l'anneau de sélection (disque vert sous l'unité)
            if (_selectionRing == null)
            {
                _selectionRing = CreateSelectionRing();
            }

            Vector3 unitPos = unit.transform.position;
            _selectionRing.transform.position = new Vector3(unitPos.x, 0.02f, unitPos.z);
            _selectionRing.transform.SetParent(unit.transform, true);
            _selectionRing.SetActive(true);

            Debug.Log($"[Input] Unité sélectionnée : {unit.UnitName} (propriétaire {unit.OwnerIndex})");
        }

        /// <summary>
        /// Désélectionne l'unité courante et nettoie les marqueurs de chemin.
        /// </summary>
        private void DeselectUnit()
        {
            _selectedUnit = null;
            ClearPath();

            if (_selectionRing != null)
            {
                _selectionRing.transform.SetParent(null, true);
                _selectionRing.SetActive(false);
            }

            // Masquer le panneau de cité si visible
            if (_cityPanel != null && _cityPanel.isActiveAndEnabled)
            {
                _cityPanel.Hide();
            }
        }

        // ----------------------------------------------------------------
        // Déplacement
        // ----------------------------------------------------------------

        /// <summary>
        /// Déplace l'unité sélectionnée vers la cible en utilisant UnitManager.
        /// </summary>
        private void MoveUnitTo(HexCoordinates target)
        {
            if (_selectedUnit == null) return;

            var cells = GameManager.Instance.Cells;
            int width = GameManager.Instance.Width;
            int height = GameManager.Instance.Height;

            var path = HexPathfinding.FindPath(cells, width, height, _selectedUnit.Position, target);
            if (path == null || path.Count < 2)
            {
                Debug.Log("[Input] Aucun chemin trouvé vers la destination.");
                return;
            }

            // Utiliser UnitManager pour le déplacement
            _unitManager.MoveUnit(_selectedUnit, path);

            // Mettre à jour la position de l'anneau de sélection
            if (_selectionRing != null && _selectedUnit != null)
            {
                Vector3 newPos = _selectedUnit.transform.position;
                _selectionRing.transform.SetParent(null, true);
                _selectionRing.transform.position = new Vector3(newPos.x, 0.02f, newPos.z);
                _selectionRing.transform.SetParent(_selectedUnit.transform, true);
            }

            // Nettoyer les marqueurs de chemin
            ClearPath();

            // Mettre à jour le brouillard de guerre
            UpdateFogAfterMovement();

            Debug.Log($"[Input] {_selectedUnit.UnitName} déplacé vers {target}");
        }

        /// <summary>
        /// Met à jour le fog of war après un déplacement d'unité.
        /// </summary>
        private void UpdateFogAfterMovement()
        {
            int currentPlayer = GameManager.Instance.TurnManager?.CurrentPlayerIndex ?? -1;
            if (currentPlayer < 0) return;

            if (_unitManager != null)
            {
                _unitManager.UpdatePlayerVisibility(currentPlayer);
                var fogRenderer = FindAnyObjectByType<FogOfWarRenderer>();
                if (fogRenderer != null)
                    fogRenderer.UpdateAllFogQuads();
            }
        }

        /// <summary>
        /// Calcule et affiche le chemin de l'unité sélectionnée vers la cible.
        /// </summary>
        private void ShowPathPreview(HexCoordinates target)
        {
            if (_selectedUnit == null) return;

            ClearPath();

            var cells = GameManager.Instance.Cells;
            int width = GameManager.Instance.Width;
            int height = GameManager.Instance.Height;

            _currentPath = HexPathfinding.FindPath(cells, width, height, _selectedUnit.Position, target);
            if (_currentPath == null || _currentPath.Count < 2) return;

            // Créer des marqueurs (petites sphères) le long du chemin
            for (int i = 1; i < _currentPath.Count; i++)
            {
                Vector3 worldPos = _gridRenderer.HexToWorld(_currentPath[i]);
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"PathMarker_{i}";
                marker.transform.position = new Vector3(worldPos.x, 0.1f, worldPos.z);
                marker.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                var mr = marker.GetComponent<MeshRenderer>();
                mr.material = new Material(Shader.Find("Standard"));
                mr.material.color = PathMarkerColor;

                Destroy(marker.GetComponent<Collider>());
                _pathMarkers.Add(marker);
            }
        }

        /// <summary>
        /// Supprime les marqueurs de chemin affichés.
        /// </summary>
        private void ClearPath()
        {
            foreach (var marker in _pathMarkers)
            {
                if (marker != null) Destroy(marker);
            }
            _pathMarkers.Clear();
            _currentPath = null;
        }

        // ----------------------------------------------------------------
        // Exécution du combat
        // ----------------------------------------------------------------

        /// <summary>
        /// Exécute le combat entre l'unité sélectionnée et une unité ennemie adjacente.
        /// Applique les dégâts, détruit les unités mortes, gère la promotion,
        /// et met à jour le brouillard de guerre.
        /// </summary>
        private void ExecuteCombat(Unit enemyUnit)
        {
            if (_selectedUnit == null || enemyUnit == null) return;

            var cells = GameManager.Instance.Cells;
            var (ex, ey) = enemyUnit.Position.ToOffset();
            var defenderCell = cells[ex, ey];

            // Exécuter le combat via CombatResolver
            var result = CombatResolver.ExecuteCombat(_selectedUnit, enemyUnit, defenderCell);

            string resultText = result.AttackerWins ? "VICTOIRE" : "DÉFAITE";
            Debug.Log(
                $"<color=orange>[COMBAT]</color>\n" +
                $"{_selectedUnit.UnitName} ({result.AttackerTotalPower}) vs {enemyUnit.UnitName} ({result.DefenderTotalPower})\n" +
                $"Dégâts subis : {result.AttackerDamage} | Dégâts infligés : {result.DefenderDamage}\n" +
                $"Résultat : <b>{resultText}</b>"
            );

            // Détruire le défenseur si mort
            if (result.DefenderKilled)
            {
                Debug.Log($"<color=orange>[COMBAT] {enemyUnit.UnitName} a été détruit !</color>");
                _unitManager.DestroyUnit(enemyUnit);
            }

            // Si l'attaquant est mort
            if (_selectedUnit.IsDead())
            {
                Debug.Log($"<color=red>[COMBAT] {_selectedUnit.UnitName} a été détruit !</color>");
                _unitManager.DestroyUnit(_selectedUnit);
                _selectedUnit = null;
                DeselectUnit();
            }
            else if (result.DefenderKilled && _selectedUnit.VeterancyRank >= 1)
            {
                // Promotion obtenue via CombatResolver, logger le nouveau rang
                Debug.Log($"<color=yellow>[COMBAT] {_selectedUnit.UnitName} promu au rang {_selectedUnit.VeterancyRank} !</color>");
            }

            // Mettre à jour le brouillard de guerre
            UpdateFogAfterMovement();
        }

        // ----------------------------------------------------------------
        // Gestion des cités
        // ----------------------------------------------------------------

        /// <summary>
        /// Trouve une cité à une position donnée.
        /// </summary>
        private CityData FindCityAt(HexCoordinates coords)
        {
            var cm = GameManager.Instance?.CityManager;
            if (cm == null) return null;

            var cities = cm.GetAllCities();
            foreach (var c in cities)
            {
                if (c.Location == coords)
                    return c;
            }
            return null;
        }

        /// <summary>
        /// Ouvre le panneau de gestion de la cité.
        /// </summary>
        private void ShowCityPanel(CityData cityData)
        {
            if (_cityPanel == null) return;

            // Construire une instance City runtime à partir des données persistantes
            var city = new City(cityData);

            // Restaurer les données de production depuis les City runtime du CityManager
            var runtimeCities = GameManager.Instance?.CityManager?.GetRuntimeCities();
            if (runtimeCities != null)
            {
                foreach (var rc in runtimeCities)
                {
                    if (rc.CityName == cityData.CityName)
                    {
                        city.CurrentProduction = rc.CurrentProduction;
                        city.CurrentProductionCost = rc.CurrentProductionCost;
                        city.ProductionStored = rc.ProductionStored;
                        break;
                    }
                }
            }

            int currentPlayer = GameManager.Instance.TurnManager?.CurrentPlayerIndex ?? -1;
            if (cityData.OwnerIndex == currentPlayer)
            {
                _cityPanel.Show(city);
            }
        }

        // ----------------------------------------------------------------
        // Helpers visuels
        // ----------------------------------------------------------------

        /// <summary>
        /// Crée l'anneau de sélection (cylindre jaune aplati).
        /// </summary>
        private static GameObject CreateSelectionRing()
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "SelectionRing";

            var mr = ring.GetComponent<MeshRenderer>();

            // Jaune vif semi-transparent
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.92f, 0.2f, 0.6f);
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mr.sharedMaterial = mat;

            ring.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);

            // Supprimer le collider pour ne pas interférer avec les clics
            Destroy(ring.GetComponent<Collider>());

            ring.SetActive(false);
            return ring;
        }

        /// <summary>
        /// Retourne l'unité actuellement sélectionnée (pour autres scripts).
        /// </summary>
        public Unit GetSelectedUnit() => _selectedUnit;
    }
}
