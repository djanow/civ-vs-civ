using System.Collections.Generic;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Gere toutes les unites sur la carte : creation, deplacement,
    /// destruction, armees combinees, et visibilite.
    /// </summary>
    public class UnitManager : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject _unitPrefab;

        [Header("References")]
        private HexGridRenderer _gridRenderer;
        private FogOfWarManager _fogManager;
        private FogOfWarRenderer _fogRenderer;

        public List<Unit> AllUnits { get; private set; } = new();

        /// <summary>
        /// Initialise le gestionnaire d'unites pour le nombre de civilisations donne.
        /// </summary>
        public void Initialize(int civCount)
        {
            AllUnits.Clear();
            Debug.Log($"[UnitManager] Initialize for {civCount} players.");
        }

        private void Awake()
        {
            _gridRenderer = FindAnyObjectByType<HexGridRenderer>();
            _fogManager = FindAnyObjectByType<FogOfWarManager>();
            _fogRenderer = FindAnyObjectByType<FogOfWarRenderer>();

            EventBus.Subscribe<GameEvents.CivStartPositions>(OnCivStartPositions);
            EventBus.Subscribe<GameEvents.TurnPhaseChanged>(OnTurnPhaseChanged);
            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.CivStartPositions>(OnCivStartPositions);
            EventBus.Unsubscribe<GameEvents.TurnPhaseChanged>(OnTurnPhaseChanged);
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            AllUnits.Clear();
        }

        private void OnCivStartPositions(GameEvents.CivStartPositions evt)
        {
            SpawnStartingUnits(evt.StartPositions);
        }

        private void OnTurnPhaseChanged(GameEvents.TurnPhaseChanged evt)
        {
            if (evt.Phase == TurnPhase.Movement)
            {
                RefreshMovementForPlayer(evt.PlayerIndex);
                UpdatePlayerVisibility(evt.PlayerIndex);
            }
        }

        // ----------------------------------------------------------------
        // Creation / Destruction
        // ----------------------------------------------------------------

        /// <summary>
        /// Fait apparaitre une unite sur la carte a partir de ses donnees.
        /// </summary>
        public Unit SpawnUnit(UnitData data, HexCoordinates position, int ownerIndex)
        {
            Vector3 worldPos = _gridRenderer != null
                ? _gridRenderer.HexToWorld(position)
                : Vector3.zero;

            GameObject go;
            if (_unitPrefab != null)
            {
                go = Instantiate(_unitPrefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                go = new GameObject(data != null ? data.UnitName : "Unit");
                go.transform.SetParent(transform);
                go.transform.position = worldPos;
            }
            go.name = $"{(data != null ? data.UnitName : "Unit")}_{position}";

            Unit unit = go.GetComponent<Unit>();
            if (unit == null)
                unit = go.AddComponent<Unit>();

            unit.Initialize(data, position, ownerIndex);
            AllUnits.Add(unit);

            return unit;
        }

        /// <summary>
        /// Cree les unites de depart pour chaque civilisation.
        /// Chaque joueur recoit un Guerrier et un Eclaireur pres de sa position initiale.
        /// </summary>
        public void SpawnStartingUnits(HexCoordinates[] startPositions)
        {
            if (startPositions == null || startPositions.Length == 0)
            {
                Debug.LogError("[UnitManager] SpawnStartingUnits: startPositions is null or empty");
                return;
            }

            for (int i = 0; i < startPositions.Length; i++)
            {
                UnitData warriorData = CreateDefaultUnitData("Guerrier", UnitCategory.Infantry, 3, 3, 10, 2, 40);
                UnitData scoutData = CreateDefaultUnitData("Eclaireur", UnitCategory.Recon, 2, 1, 8, 3, 30);

                // Verifier que la position de depart est valide (terrain praticable)
                HexCoordinates warriorPos = startPositions[i];
                if (GameManager.Instance.IsCellInBounds(warriorPos))
                {
                    var cell = GameManager.Instance.GetCell(warriorPos);
                    if (cell == null || cell.MovementCost <= 0)
                    {
                        Debug.LogWarning($"[UnitManager] Position de depart {warriorPos} invalide (cout={cell?.MovementCost}), recherche d'un voisin valide...");
                        warriorPos = FindValidNeighbor(warriorPos);
                    }
                }

                // Placer le guerrier sur la position de depart
                SpawnUnit(warriorData, warriorPos, i);

                // Placer l'eclaireur sur un voisin ou sur la meme case
                var neighbors = startPositions[i].GetNeighbors();
                HexCoordinates scoutPos = startPositions[i];
                for (int n = 0; n < neighbors.Length; n++)
                {
                    if (GameManager.Instance.IsCellInBounds(neighbors[n]))
                    {
                        var cell = GameManager.Instance.GetCell(neighbors[n]);
                        if (cell != null && cell.MovementCost > 0)
                        {
                            scoutPos = neighbors[n];
                            break;
                        }
                    }
                }

                // Si tous les voisins sont invalides, placer le scout sur la meme case que le guerrier
                if (scoutPos == startPositions[i] && GameManager.Instance.IsCellInBounds(startPositions[i]))
                {
                    var cell = GameManager.Instance.GetCell(startPositions[i]);
                    if (cell == null || cell.MovementCost <= 0)
                        scoutPos = warriorPos; // fallback: a cote du guerrier
                }

                SpawnUnit(scoutData, scoutPos, i);
            }

            Debug.Log($"[UnitManager] Unites de depart creees pour {startPositions.Length} joueur(s).");
        }

        /// <summary>
        /// Trouve un voisin valide (terrain praticable) pour une position donnee.
        /// </summary>
        private HexCoordinates FindValidNeighbor(HexCoordinates pos)
        {
            var neighbors = pos.GetNeighbors();
            foreach (var n in neighbors)
            {
                if (GameManager.Instance.IsCellInBounds(n))
                {
                    var cell = GameManager.Instance.GetCell(n);
                    if (cell != null && cell.MovementCost > 0)
                        return n;
                }
            }
            return pos; // fallback: garder la position d'origine
        }

        /// <summary>
        /// Cree un UnitData temporaire pour les unites de depart.
        /// Dans la version finale, ces donnees viendront de ScriptableObjects.
        /// </summary>
        private static UnitData CreateDefaultUnitData(string name, UnitCategory category,
            int atk, int def, int hp, int move, int cost)
        {
            var data = ScriptableObject.CreateInstance<UnitData>();
            data.UnitName = name;
            data.Category = category;
            data.BaseAttack = atk;
            data.BaseDefense = def;
            data.MaxHealth = hp;
            data.MovementRange = move;
            data.ProductionCost = cost;
            data.RequiredTechId = -1;
            data.IsUnique = false;
            data.CivilizationId = -1;
            return data;
        }

        /// <summary>
        /// Deplace une unite le long d'un chemin calcule par A*.
        /// Consomme les points de mouvement selon le cout de chaque case.
        /// </summary>
        public void MoveUnit(Unit unit, List<HexCoordinates> path)
        {
            if (unit == null || path == null || path.Count < 2) return;

            HexCoordinates destination = path[^1];
            var cells = GameManager.Instance.Cells;

            // Calculer le cout total du deplacement
            int totalCost = 0;
            for (int i = 1; i < path.Count; i++)
            {
                var (x, y) = path[i].ToOffset();
                int cost = cells[x, y].MovementCost;
                if (cost < 0) return; // Chemin invalide
                totalCost += cost;
            }

            if (totalCost > unit.MovementRemaining) return;

            unit.MovementRemaining -= totalCost;
            unit.MoveTo(destination);

            // Deplacer le GameObject
            if (_gridRenderer != null)
            {
                unit.transform.position = _gridRenderer.HexToWorld(destination);
            }

            // Mettre a jour la visibilite
            UpdateUnitVisibility(unit);
        }

        /// <summary>
        /// Detruit une unite et la retire de la liste.
        /// </summary>
        public void DestroyUnit(Unit unit)
        {
            if (unit == null) return;
            AllUnits.Remove(unit);
            Destroy(unit.gameObject);
        }

        // ----------------------------------------------------------------
        // Requetes
        // ----------------------------------------------------------------

        /// <summary>
        /// Retourne la premiere unite trouvee a une position donnee, ou null.
        /// </summary>
        public Unit GetUnitAt(HexCoordinates position)
        {
            foreach (var unit in AllUnits)
            {
                if (unit != null && unit.Position == position && !unit.IsDead())
                    return unit;
            }
            return null;
        }

        /// <summary>
        /// Retourne toutes les unites (non mortes) a une position donnee.
        /// Permet le stack (plusieurs unites sur la meme case).
        /// </summary>
        public List<Unit> GetUnitsAt(HexCoordinates position)
        {
            var result = new List<Unit>();
            foreach (var unit in AllUnits)
            {
                if (unit != null && unit.Position == position && !unit.IsDead())
                    result.Add(unit);
            }
            return result;
        }

        /// <summary>
        /// Retourne toutes les unites d'un joueur.
        /// </summary>
        public List<Unit> GetPlayerUnits(int playerIndex)
        {
            var result = new List<Unit>();
            foreach (var unit in AllUnits)
            {
                if (unit != null && unit.OwnerIndex == playerIndex && !unit.IsDead())
                    result.Add(unit);
            }
            return result;
        }

        // ----------------------------------------------------------------
        // Systeme d'Armee (3 unites identiques -> 1 Armee)
        // ----------------------------------------------------------------

        /// <summary>
        /// Verifie si 3 unites peuvent fusionner en une armee.
        /// Conditions : meme position, meme nom, meme proprietaire, aucune deja une armee.
        /// </summary>
        public bool CanFormArmy(Unit a, Unit b, Unit c)
        {
            if (a == null || b == null || c == null) return false;
            if (a == b || a == c || b == c) return false;

            // Meme position
            if (a.Position != b.Position || a.Position != c.Position) return false;

            // Meme nom d'unite de base
            if (a.UnitName != b.UnitName || a.UnitName != c.UnitName) return false;

            // Meme proprietaire
            if (a.OwnerIndex != b.OwnerIndex || a.OwnerIndex != c.OwnerIndex) return false;

            // Aucune deja une armee
            if (a.IsArmy || b.IsArmy || c.IsArmy) return false;

            return true;
        }

        /// <summary>
        /// Fusionne 3 unites identiques en une Armee combinee.
        /// Les PV, ATK et DEF sont additionnes. La meilleure veterance est conservee.
        /// L'armee recoit un nom genere automatiquement.
        /// </summary>
        public Unit FormArmy(Unit a, Unit b, Unit c)
        {
            if (!CanFormArmy(a, b, c)) return null;

            // Generer un nom d'armee
            string armyName = GenerateArmyName(a.UnitName);

            // Calculer les stats combinees
            int combinedMaxHealth = a.MaxHealth + b.MaxHealth + c.MaxHealth;
            int healthSum = a.CurrentHealth + b.CurrentHealth + c.CurrentHealth;

            // Prendre le meilleur rang de veterance
            int bestRank = Mathf.Max(a.VeterancyRank, b.VeterancyRank, c.VeterancyRank);

            // Creer la nouvelle unite-armee
            GameObject go;
            if (_unitPrefab != null)
            {
                Vector3 worldPos = _gridRenderer != null
                    ? _gridRenderer.HexToWorld(a.Position)
                    : Vector3.zero;
                go = Instantiate(_unitPrefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                go = new GameObject($"Army_{armyName}");
                go.transform.SetParent(transform);
            }

            Unit army = go.AddComponent<Unit>();
            army.InitializeAsArmy(armyName, a, combinedMaxHealth, healthSum);

            // Restaurer le rang de veterance
            army.VeterancyRank = bestRank;

            AllUnits.Add(army);

            // Detruire les 3 unites originales
            DestroyUnit(a);
            DestroyUnit(b);
            DestroyUnit(c);

            // Publier l'evenement
            EventBus.Publish(new GameEvents.ArmyFormed
            {
                Location = army.Position,
                ArmyName = armyName
            });

            Debug.Log($"[UnitManager] Armee formee : {armyName} (stats combinees : ATK {army.BaseAttack}, DEF {army.BaseDefense}, PV {army.CurrentHealth}/{army.MaxHealth})");

            return army;
        }

        /// <summary>
        /// Genere un nom d'armee a partir du nom d'unite de base.
        /// </summary>
        private static string GenerateArmyName(string unitName)
        {
            string[] prefixes = { "Garde", "Legion", "Bataillon", "Phalange", "Cohorte", "Escadron" };
            string[] suffixes = { "de Tyr", "de Sparte", "d'Athenes", "de Carthage", "du Levant", "du Couchant" };

            string prefix = prefixes[Random.Range(0, prefixes.Length)];
            string suffix = suffixes[Random.Range(0, suffixes.Length)];

            return $"{prefix} {suffix}";
        }

        // ----------------------------------------------------------------
        // Visibilite
        // ----------------------------------------------------------------

        /// <summary>
        /// Met a jour la visibilite (fog of war) pour un joueur en fonction
        /// de la position de ses unites.
        /// </summary>
        public void UpdatePlayerVisibility(int playerIndex)
        {
            if (_fogManager == null) return;

            // Effacer l'ancienne visibilite
            _fogManager.ClearVisibility(playerIndex);

            // Recalculer a partir de chaque unite
            var playerUnits = GetPlayerUnits(playerIndex);
            foreach (var unit in playerUnits)
            {
                if (unit != null && !unit.IsDead())
                {
                    int visionRange = GetVisionRange(unit);
                    _fogManager.UpdateVisibility(unit.Position, visionRange, playerIndex);
                }
            }
        }

        /// <summary>
        /// Met a jour la visibilite pour une seule unite.
        /// </summary>
        private void UpdateUnitVisibility(Unit unit)
        {
            if (_fogManager == null || unit == null) return;

            int visionRange = GetVisionRange(unit);
            _fogManager.UpdateVisibility(unit.Position, visionRange, unit.OwnerIndex);
        }

        /// <summary>
        /// Retourne le rayon de vision d'une unite selon sa categorie.
        /// </summary>
        private static int GetVisionRange(Unit unit)
        {
            return unit.Category switch
            {
                UnitCategory.Recon => 3,
                UnitCategory.Naval => 3,
                UnitCategory.Civil => 1,
                _ => 2
            };
        }

        // ----------------------------------------------------------------
        // Mouvement
        // ----------------------------------------------------------------

        /// <summary>
        /// Reinitialise les points de mouvement de toutes les unites d'un joueur.
        /// </summary>
        public void RefreshMovementForPlayer(int playerIndex)
        {
            var playerUnits = GetPlayerUnits(playerIndex);
            foreach (var unit in playerUnits)
            {
                if (unit != null)
                    unit.RefreshMovement();
            }
        }
    }
}
