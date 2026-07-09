using System.Collections.Generic;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Gère la progression de la recherche pour chaque joueur.
    /// S'occupe de la séléction des techs, de l'accumulation des points de science,
    /// et du déblocage des techs terminées.
    /// </summary>
    public class ResearchManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TechTreeData _techTree;

        [Header("Debug")]
        [SerializeField] private bool _logProgress = true;

        public TechTreeData TechTree => _techTree;

        // Per-player state
        private int[] _currentResearchId;
        private int[] _researchProgress;
        private List<int>[] _completedTechs;
        private int[] _playerEra;

        private int _playerCount;

        private void Awake()
        {
            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
        }

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            Initialize(2); // MVP: 2 joueurs
        }

        /// <summary>
        /// Initialise l'état de recherche pour le nombre de joueurs donné.
        /// </summary>
        public void Initialize(int playerCount)
        {
            _playerCount = playerCount;
            _currentResearchId = new int[playerCount];
            _researchProgress = new int[playerCount];
            _completedTechs = new List<int>[playerCount];
            _playerEra = new int[playerCount];

            for (int i = 0; i < playerCount; i++)
            {
                _currentResearchId[i] = -1;
                _researchProgress[i] = 0;
                _completedTechs[i] = new List<int>();
                _playerEra[i] = 0; // Antiquité
            }

            if (_logProgress)
                Debug.Log($"[ResearchManager] Initialized for {playerCount} players.");
        }

        /// <summary>
        /// Définit la tech à rechercher pour un joueur.
        /// </summary>
        public void SetResearch(int playerIndex, int techId)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount) return;

            if (techId == -1)
            {
                _currentResearchId[playerIndex] = -1;
                _researchProgress[playerIndex] = 0;
                return;
            }

            if (!CanResearch(playerIndex, techId))
            {
                Debug.LogWarning($"[ResearchManager] Player {playerIndex} cannot research tech ID {techId}.");
                return;
            }

            _currentResearchId[playerIndex] = techId;
            _researchProgress[playerIndex] = 0;

            var tech = _techTree.GetTech(techId);

            EventBus.Publish(new GameEvents.ResearchStarted
            {
                PlayerIndex = playerIndex,
                TechId = techId,
                TechName = tech.TechName
            });

            if (_logProgress)
                Debug.Log($"[ResearchManager] Player {playerIndex} started researching '{tech.TechName}'.");
        }

        /// <summary>
        /// Traite la progression de la recherche pour un joueur avec le revenu scientifique donné.
        /// Appelé à chaque tour (phase Research).
        /// </summary>
        public void ProcessResearch(int playerIndex, int scienceIncome)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount) return;

            // Si aucune tech sélectionnée, essayer d'en choisir une automatiquement
            if (_currentResearchId[playerIndex] == -1)
            {
                var available = GetAvailableTechs(playerIndex);
                if (available.Count > 0)
                {
                    SetResearch(playerIndex, available[0]);
                }
                return;
            }

            var currentTech = _techTree.GetTech(_currentResearchId[playerIndex]);
            if (currentTech.TechId == 0 && string.IsNullOrEmpty(currentTech.TechName))
            {
                // Tech invalide
                _currentResearchId[playerIndex] = -1;
                _researchProgress[playerIndex] = 0;
                return;
            }

            _researchProgress[playerIndex] += scienceIncome;

            if (_logProgress && scienceIncome > 0)
            {
                int remaining = currentTech.ScienceCost - _researchProgress[playerIndex];
                if (remaining < 0) remaining = 0;
                Debug.Log($"[ResearchManager] Player {playerIndex}: +{scienceIncome} science toward " +
                    $"'{currentTech.TechName}' ({_researchProgress[playerIndex]}/{currentTech.ScienceCost}, {remaining} remaining).");
            }

            // Vérifier si la tech est complétée
            if (_researchProgress[playerIndex] >= currentTech.ScienceCost)
            {
                CompleteTech(playerIndex, currentTech);
            }
        }

        /// <summary>
        /// Marque une tech comme complétée pour un joueur.
        /// </summary>
        private void CompleteTech(int playerIndex, TechNodeData tech)
        {
            _completedTechs[playerIndex].Add(tech.TechId);
            _currentResearchId[playerIndex] = -1;
            _researchProgress[playerIndex] = 0;

            // Vérifier le changement d'ère
            if (tech.IsEraGate)
            {
                int newEra = tech.Era + 1;
                if (newEra > _playerEra[playerIndex])
                {
                    _playerEra[playerIndex] = newEra;

                    if (_logProgress)
                        Debug.Log($"[ResearchManager] Player {playerIndex} advanced to Era {newEra}!");
                }
            }

            EventBus.Publish(new GameEvents.TechCompleted
            {
                PlayerIndex = playerIndex,
                TechId = tech.TechId,
                TechName = tech.TechName
            });

            if (_logProgress)
                Debug.Log($"[ResearchManager] Player {playerIndex} completed research: '{tech.TechName}'.");
        }

        /// <summary>
        /// Vérifie si un joueur peut rechercher une tech donnée.
        /// </summary>
        public bool CanResearch(int playerIndex, int techId)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount) return false;
            if (_techTree == null) return false;

            var tech = _techTree.GetTech(techId);
            if (tech.TechId == 0 && string.IsNullOrEmpty(tech.TechName)) return false;

            // Déjà complétée ?
            if (_completedTechs[playerIndex].Contains(techId)) return false;

            // Tech de l'ère actuelle ou suivante ?
            if (tech.Era > _playerEra[playerIndex] + 1) return false;

            // Tous les prérequis sont-ils complétés ?
            if (tech.PrerequisiteIds != null)
            {
                for (int i = 0; i < tech.PrerequisiteIds.Length; i++)
                {
                    if (!_completedTechs[playerIndex].Contains(tech.PrerequisiteIds[i]))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Vérifie si une tech a déjà été complétée par un joueur.
        /// </summary>
        public bool IsTechCompleted(int playerIndex, int techId)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount) return false;
            return _completedTechs[playerIndex].Contains(techId);
        }

        /// <summary>
        /// Retourne la liste des techs disponibles pour la recherche (prérequis remplis, pas encore complétées).
        /// </summary>
        public List<int> GetAvailableTechs(int playerIndex)
        {
            var available = new List<int>();

            if (playerIndex < 0 || playerIndex >= _playerCount) return available;
            if (_techTree == null) return available;

            var nodes = _techTree.TechNodes;
            if (nodes == null) return available;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (CanResearch(playerIndex, nodes[i].TechId))
                {
                    available.Add(nodes[i].TechId);
                }
            }

            return available;
        }

        /// <summary>
        /// Vérifie si un joueur peut passer à l'ère suivante.
        /// </summary>
        public bool CanAdvanceEra(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount) return false;
            if (_techTree == null) return false;

            // Condition 1 : toutes les techs de l'ère actuelle sont complétées
            var currentEraTechs = _techTree.GetTechsByEra(_playerEra[playerIndex]);
            bool allCompleted = true;
            for (int i = 0; i < currentEraTechs.Length; i++)
            {
                if (!_completedTechs[playerIndex].Contains(currentEraTechs[i].TechId))
                {
                    allCompleted = false;
                    break;
                }
            }

            if (allCompleted) return true;

            // Condition 2 : au moins 3 techs de l'ère suivante sont complétées
            var nextEraTechs = _techTree.GetTechsByEra(_playerEra[playerIndex] + 1);
            int nextEraCount = 0;
            for (int i = 0; i < nextEraTechs.Length; i++)
            {
                if (_completedTechs[playerIndex].Contains(nextEraTechs[i].TechId))
                {
                    nextEraCount++;
                }
            }

            return nextEraCount >= 3;
        }

        /// <summary>
        /// Passe manuellement à l'ère suivante si possible.
        /// </summary>
        public bool AdvanceEra(int playerIndex)
        {
            if (!CanAdvanceEra(playerIndex)) return false;

            _playerEra[playerIndex]++;

            if (_logProgress)
                Debug.Log($"[ResearchManager] Player {playerIndex} manually advanced to Era {_playerEra[playerIndex]}.");

            EventBus.Publish(new GameEvents.EraAdvanced
            {
                PlayerIndex = playerIndex,
                NewEra = _playerEra[playerIndex]
            });

            return true;
        }

        /// <summary>
        /// Retourne l'ère actuelle d'un joueur.
        /// </summary>
        public int GetPlayerEra(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount) return 0;
            return _playerEra[playerIndex];
        }

        /// <summary>
        /// Retourne l'ID de la tech en cours de recherche pour un joueur (-1 si aucune).
        /// </summary>
        public int GetCurrentResearchId(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount) return -1;
            return _currentResearchId[playerIndex];
        }

        /// <summary>
        /// Retourne la progression (points de science accumulés) vers la tech en cours.
        /// </summary>
        public int GetResearchProgress(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount) return 0;
            return _researchProgress[playerIndex];
        }

        /// <summary>
        /// Retourne la liste des techs complétées pour un joueur.
        /// </summary>
        public List<int> GetCompletedTechs(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount) return new List<int>();
            return new List<int>(_completedTechs[playerIndex]);
        }

        /// <summary>
        /// Vérifie si un joueur a complété une technologie par son nom.
        /// </summary>
        public bool HasTech(int playerIndex, string techName)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount)
                return false;
            if (_techTree == null || _techTree.TechNodes == null)
                return false;

            // Trouver l'ID de la tech par son nom
            int techId = -1;
            foreach (var node in _techTree.TechNodes)
            {
                if (node.TechName.Equals(techName, System.StringComparison.OrdinalIgnoreCase))
                {
                    techId = node.TechId;
                    break;
                }
            }

            if (techId < 0) return false;

            // Vérifier si la tech est complétée
            return _completedTechs[playerIndex].Contains(techId);
        }

        /// <summary>
        /// Ajoute un bonus de science direct (événements narratifs, etc.).
        /// </summary>
        public void AddScienceBonus(int playerIndex, int amount)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount || amount <= 0) return;
            _researchProgress[playerIndex] += amount;

            // Vérifier complétion immédiate
            if (_currentResearchId[playerIndex] != -1)
            {
                var currentTech = _techTree.GetTech(_currentResearchId[playerIndex]);
                if (currentTech.TechId != 0 || !string.IsNullOrEmpty(currentTech.TechName))
                {
                    if (_researchProgress[playerIndex] >= currentTech.ScienceCost)
                    {
                        CompleteTech(playerIndex, currentTech);
                    }
                }
            }
        }

        /// <summary>
        /// Réinitialise l'état pour une nouvelle partie.
        /// </summary>
        public void ResetState()
        {
            Initialize(_playerCount > 0 ? _playerCount : 2);
        }
    }
}
