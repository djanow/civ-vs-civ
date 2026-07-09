using System.Collections.Generic;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Gère les cités-états neutres présentes sur la carte.
    /// Chaque cité-état peut être influencée par les joueurs pour devenir alliée.
    /// </summary>
    public class CityStateManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int _cityStateCount = 3;

        [Header("Debug")]
        [SerializeField] private bool _showDebugLogs = true;

        // Toutes les cités-états de la carte
        private List<CityState> _cityStates;

        /// <summary>
        /// Nombre de cités-états actuellement sur la carte.
        /// </summary>
        public int CityStateCount => _cityStates?.Count ?? 0;

        /// <summary>
        /// Accès aux données des cités-états (lecture seule).
        /// </summary>
        public List<CityState> CityStates => _cityStates;

        private void Awake()
        {
            _cityStates = new List<CityState>();
            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            GenerateCityStates(evt.Cells, _cityStateCount);
        }

        /// <summary>
        /// Génère les cités-états sur la carte à des positions valides.
        /// </summary>
        public void GenerateCityStates(HexCell[,] cells, int count)
        {
            _cityStates.Clear();

            if (cells == null || count <= 0) return;

            int width = cells.GetLength(0);
            int height = cells.GetLength(1);

            // Noms possibles pour les cités-états
            string[] names = {
                "Sidon", "Byblos", "Corinthe", "Sparte", "Argos",
                "Rhodes", "Cnossos", "Mycènes", "Milet", "Éphèse",
                "Smyrne", "Halicarnasse", "Paphos", "Salamine", "Thèbes"
            };

            int attempts = 0;
            int maxAttempts = 500;
            int placed = 0;

            // Mélanger les noms pour varier
            ShuffleNames(names);

            while (placed < count && attempts < maxAttempts)
            {
                attempts++;

                int x = Random.Range(5, width - 5);
                int y = Random.Range(5, height - 5);
                var cell = cells[x, y];

                // Une cité-état doit être sur du terrain franchissable
                if (cell.MovementCost <= 0) continue;
                if (cell.TileType == TileType.Desert) continue; // Pas en plein desert
                if (cell.OwnerIndex != -1) continue; // Deja possedee

                var coords = HexCoordinates.FromOffset(x, y);

                // Verifier distance minimale avec les autres cites-etats et les departs
                bool tooClose = false;
                for (int i = 0; i < _cityStates.Count; i++)
                {
                    if (coords.DistanceTo(_cityStates[i].Location) < 6)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                // Verifier distance minimale avec les departs des civs
                if (GameManager.Instance != null && GameManager.Instance.Cells != null)
                {
                    // On verifie autour de la position
                    for (int dx = -3; dx <= 3 && !tooClose; dx++)
                    {
                        for (int dy = -3; dy <= 3 && !tooClose; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (cells[nx, ny].OwnerIndex != -1)
                                    tooClose = true;
                            }
                        }
                    }
                }

                if (tooClose) continue;

                // Placement valide
                string name = names[placed % names.Length];
                var cityState = new CityState
                {
                    Name = name,
                    Location = coords,
                    AllyIndex = -1,
                    Influence = 0
                };

                _cityStates.Add(cityState);
                placed++;

                if (_showDebugLogs)
                    Debug.Log($"[CityStateManager] Placee: {name} at {coords}.");
            }

            if (_showDebugLogs)
                Debug.Log($"[CityStateManager] {_cityStates.Count} cites-etats generees sur la carte.");
        }

        /// <summary>
        /// Ajoute de l'influence a une cite-etat.
        /// </summary>
        public void AddInfluence(int playerIndex, HexCoordinates cityStateLocation, int amount)
        {
            var cityState = GetCityStateAt(cityStateLocation);
            if (cityState == null)
            {
                Debug.LogWarning($"[CityStateManager] No city state at {cityStateLocation}.");
                return;
            }

            // Appliquer le bonus grec : -30% du cout d'influence
            int actualAmount = GetInfluenceCost(cityStateLocation, playerIndex);
            if (actualAmount <= 0) actualAmount = amount;

            cityState.Influence += actualAmount;

            // Verifier si le joueur devient l'allié
            const int allianceThreshold = 100;
            if (cityState.Influence >= allianceThreshold)
            {
                cityState.AllyIndex = playerIndex;

                if (_showDebugLogs)
                    Debug.Log($"[CityStateManager] Joueur {playerIndex} est maintenant allie de {cityState.Name}!");

                // Bonus : +1 science pour la Grece par cite-etat alliee
                // (applique dans GameManager via l'evenement)
            }
        }

        /// <summary>
        /// Retourne la cité-état à une position donnée, ou null si absente.
        /// </summary>
        public CityState GetCityStateAt(HexCoordinates position)
        {
            for (int i = 0; i < _cityStates.Count; i++)
            {
                if (_cityStates[i].Location == position)
                    return _cityStates[i];
            }
            return null;
        }

        /// <summary>
        /// Retourne le cout d'influence pour une cité-état.
        /// La Grece a -30% (bonus Polis).
        /// </summary>
        public int GetInfluenceCost(HexCoordinates cityStateLocation, int playerIndex)
        {
            // Cout de base : 10 points d'influence par point
            int baseCost = 10;

            // Bonus grec : -30%
            if (playerIndex == 1) // Player 1 = Grece
            {
                baseCost = Mathf.Max(1, Mathf.RoundToInt(baseCost * 0.7f));
            }

            return baseCost;
        }

        /// <summary>
        /// Retourne le nombre de cités-états alliées à un joueur.
        /// </summary>
        public int GetAlliedCount(int playerIndex)
        {
            int count = 0;
            for (int i = 0; i < _cityStates.Count; i++)
            {
                if (_cityStates[i].AllyIndex == playerIndex)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Retourne true si une cité-état existe à une position donnée.
        /// </summary>
        public bool HasCityStateAt(HexCoordinates position)
        {
            return GetCityStateAt(position) != null;
        }

        /// <summary>
        /// Réinitialise pour une nouvelle partie.
        /// </summary>
        public void ResetState()
        {
            _cityStates.Clear();
        }

        private void ShuffleNames(string[] names)
        {
            for (int i = names.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                string temp = names[i];
                names[i] = names[j];
                names[j] = temp;
            }
        }

        /// <summary>
        /// Données d'une cité-état individuelle.
        /// </summary>
        [System.Serializable]
        public class CityState
        {
            [Tooltip("Nom de la cité-état.")]
            public string Name;

            [Tooltip("Position sur la carte.")]
            public HexCoordinates Location;

            [Tooltip("Index du joueur allié (-1 = neutre).")]
            public int AllyIndex = -1;

            [Tooltip("Points d'influence accumulés.")]
            public int Influence;

            /// <summary>
            /// Retourne true si cette cité-état est alliée à un joueur.
            /// </summary>
            public bool IsAllied => AllyIndex >= 0;

            /// <summary>
            /// Retourne true si cette cité-état est neutre.
            /// </summary>
            public bool IsNeutral => AllyIndex < 0;

            public override string ToString()
            {
                string status = IsNeutral ? "Neutre" : $"Alliee (J{AllyIndex})";
                return $"{Name} [{status}] Influence: {Influence}";
            }
        }
    }
}
