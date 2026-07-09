using System.Collections;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Gère les phases d'un tour de jeu pour chaque joueur.
    /// Le début de tour commence par la phase NarrativeEvent (conditionnelle),
    /// puis cycle : Movement -> CityManagement -> Research -> EndOfTurn -> (joueur suivant).
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        [SerializeField] private int _playerCount = 2;
        [SerializeField] private float _phaseTransitionDelay = 0.1f;

        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.NarrativeEvent;
        public int CurrentTurn { get; private set; } = 1;
        public int CurrentPlayerIndex { get; private set; } = 0;

        private static readonly TurnPhase[] PlayerPhases =
        {
            TurnPhase.Movement,
            TurnPhase.CityManagement,
            TurnPhase.Research,
            TurnPhase.EndOfTurn
        };

        /// <summary>Phase initiale (NarrativeEvent) avant le cycle normal.</summary>
        private const TurnPhase INITIAL_PHASE = TurnPhase.NarrativeEvent;

        private int _phaseIndex;
        private UnitManager _unitManager;
        private EventManager _eventManager;
        private ResearchManager _researchManager;
        private DiplomacyManager _diplomacyManager;

        private void Start()
        {
            _unitManager = FindAnyObjectByType<UnitManager>();
            _eventManager = FindAnyObjectByType<EventManager>();
            _researchManager = FindAnyObjectByType<ResearchManager>();
            _diplomacyManager = FindAnyObjectByType<DiplomacyManager>();
            StartCoroutine(StartFirstTurn());
        }

        private IEnumerator StartFirstTurn()
        {
            yield return null; // Attendre un frame pour que tout soit initialisé

            // Verifier que les unites de depart ont ete creees
            if (_unitManager != null)
            {
                _unitManager.RefreshMovementForPlayer(0);
            }

            BeginPlayerTurn(0);
        }

        /// <summary>
        /// Appelé par le bouton "Fin de tour" de l'UI.
        /// Passe à la phase suivante ou au joueur suivant.
        /// </summary>
        public void EndTurn()
        {
            StopAllCoroutines();
            StartCoroutine(AdvancePhase());
        }

        private IEnumerator AdvancePhase()
        {
            _phaseIndex++;

            while (true)
            {
                if (_phaseIndex >= PlayerPhases.Length)
                {
                    // Fin du tour du joueur courant
                    yield return ProcessEndOfTurn();

                    _phaseIndex = 0;

                    // Passer au joueur suivant
                    CurrentPlayerIndex++;
                    if (CurrentPlayerIndex >= _playerCount)
                    {
                        CurrentPlayerIndex = 0;
                        CurrentTurn++;
                    }

                    yield return new WaitForSeconds(_phaseDelay);
                    BeginPlayerTurn(CurrentPlayerIndex);

                    // BeginPlayerTurn démarre sa propre coroutine narrative ;
                    // on sort de la boucle ici et on attend le clic "Fin de tour"
                    // pour lancer un nouveau AdvancePhase.
                    yield break;
                }
                else
                {
                    CurrentPhase = PlayerPhases[_phaseIndex];

                    // Traitement automatique selon la phase
                    yield return ProcessPhase(CurrentPhase);

                    EventBus.Publish(new GameEvents.TurnPhaseChanged
                    {
                        Phase = CurrentPhase,
                        TurnNumber = CurrentTurn,
                        PlayerIndex = CurrentPlayerIndex
                    });

                    yield return new WaitForSeconds(_phaseDelay);

                    // Movement nécessite l'interaction du joueur humain → on s'arrête
                    if (CurrentPhase == TurnPhase.Movement && CurrentPlayerIndex == 0)
                    {
                        yield break;
                    }

                    // Phases non-interactives : avance automatique après un court délai
                    yield return new WaitForSeconds(0.5f);
                    _phaseIndex++;
                }
            }
        }

        /// <summary>
        /// Traite automatiquement les phases qui n'ont pas besoin d'interaction joueur.
        /// </summary>
        private IEnumerator ProcessPhase(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.Research:
                    ProcessResearchPhase();
                    break;
                case TurnPhase.EndOfTurn:
                    ProcessAIDiplomacy();
                    break;
            }
            yield return null;
        }

        /// <summary>
        /// Traite la phase de recherche : accumule la science et avance la recherche.
        /// </summary>
        private void ProcessResearchPhase()
        {
            if (_researchManager == null) return;

            // Utiliser le revenu scientifique du joueur courant
            int scienceIncome = 0;
            if (GameManager.Instance != null)
            {
                scienceIncome = GameManager.Instance.PlayerScience[CurrentPlayerIndex];
            }

            // Science de base minimale (même sans infrastructure)
            if (scienceIncome <= 0) scienceIncome = 1;

            _researchManager.ProcessResearch(CurrentPlayerIndex, scienceIncome);
        }

        /// <summary>
        /// Traite les décisions diplomatiques de l'IA pendant l'EndOfTurn.
        /// </summary>
        private void ProcessAIDiplomacy()
        {
            if (_diplomacyManager == null) return;

            // Pour chaque joueur IA, décider d'une action diplomatique
            for (int i = 1; i < _playerCount; i++) // Skip player 0 (human)
            {
                _diplomacyManager.AIDecideAction(i);
            }
        }

        /// <summary>
        /// Traite la fin de tour complète d'un joueur.
        /// Inclut la production des cités, la croissance démographique,
        /// l'accumulation des ressources (or, science, culture),
        /// et la mise à jour du brouillard de guerre.
        /// </summary>
        private IEnumerator ProcessEndOfTurn()
        {
            // Traiter la production, les yields et la croissance des cités
            ProcessCityProductionAndGrowth();

            // Mettre à jour le brouillard de guerre après la fin du tour
            RefreshFogForCurrentPlayer();

            EventBus.Publish(new GameEvents.TurnEnded
            {
                TurnNumber = CurrentTurn,
                PlayerIndex = CurrentPlayerIndex
            });

            yield return null;
        }

        /// <summary>
        /// Traite la production, les yields (ressources) et la croissance
        /// démographique pour toutes les cités du joueur courant.
        /// </summary>
        private void ProcessCityProductionAndGrowth()
        {
            var cityManager = GameManager.Instance?.CityManager;
            if (cityManager == null) return;

            var runtimeCities = cityManager.GetRuntimeCities();
            var cells = GameManager.Instance?.Cells;
            if (cells == null) return;

            foreach (var city in runtimeCities)
            {
                // Vérifier que la cité appartient au joueur courant
                var cityData = cityManager.GetAllCities().Find(c => c.CityName == city.CityName);
                if (cityData == null || cityData.OwnerIndex != CurrentPlayerIndex)
                    continue;

                // ---- Yields par tour ----
                int foodYield = city.CalculateFoodYield(cells);
                int goldYield = city.CalculateGoldYield(cells);
                int scienceYield = city.Population;        // Science = Population
                int cultureYield = Mathf.Max(1, city.Population / 2); // Culture = Pop/2 (min 1)

                // Nourriture : accumulation et croissance démographique
                city.FoodStored += foodYield;
                while (city.FoodStored >= city.FoodThreshold)
                {
                    city.FoodStored -= city.FoodThreshold;
                    cityData.Population++;
                    city.Population = cityData.Population;
                    Debug.Log($"[TurnManager] {city.CityName} a grandi ! Population : {city.Population}");
                }

                // Or : accumulation dans le GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ModifyGold(CurrentPlayerIndex, goldYield);
                    GameManager.Instance.ModifyScience(CurrentPlayerIndex, scienceYield);
                    GameManager.Instance.ModifyCulture(CurrentPlayerIndex, cultureYield);
                }

                // ---- Production ----
                if (!string.IsNullOrEmpty(city.CurrentProduction))
                {
                    ProductionManager.ProcessCityProduction(city, cells);
                }
            }
        }

        /// <summary>
        /// Démarre le tour d'un joueur en commençant par la phase NarrativeEvent.
        /// Si un événement est en attente, le cycle normal est suspendu.
        /// Rafraîchit également le brouillard de guerre et les mouvements.
        /// </summary>
        private void BeginPlayerTurn(int playerIndex)
        {
            _phaseIndex = -1; // Sera incremente a 0 par CheckNarrativeEvent -> AdvanceAfterNarrative
            CurrentPhase = INITIAL_PHASE;
            CurrentPlayerIndex = playerIndex;

            EventBus.Publish(new GameEvents.PlayerTurnStarted
            {
                PlayerIndex = playerIndex
            });

            // Rafraîchir les points de mouvement des unités du joueur
            if (_unitManager != null)
                _unitManager.RefreshMovementForPlayer(playerIndex);

            // Mettre à jour la visibilité (brouillard de guerre)
            RefreshFogForCurrentPlayer();

            // Verifier les evenements narratifs en attente
            StartCoroutine(CheckNarrativeEvent(playerIndex));
        }

        /// <summary>
        /// Rafraîchit le brouillard de guerre pour le joueur courant :
        /// recalcule les cellules visibles à partir de toutes ses unités,
        /// puis met à jour l'affichage des quads de brouillard.
        /// </summary>
        private void RefreshFogForCurrentPlayer()
        {
            int playerIndex = CurrentPlayerIndex;
            if (playerIndex < 0) return;

            if (_unitManager != null)
                _unitManager.UpdatePlayerVisibility(playerIndex);

            var fogRenderer = FindAnyObjectByType<FogOfWarRenderer>();
            if (fogRenderer != null)
                fogRenderer.UpdateAllFogQuads();
        }

        /// <summary>
        /// Vérifie s'il y a des événements narratifs en attente pour ce joueur.
        /// Si oui, on reste en phase NarrativeEvent jusqu'à résolution.
        /// Sinon, on passe directement au cycle normal.
        /// </summary>
        private IEnumerator CheckNarrativeEvent(int playerIndex)
        {
            yield return new WaitForSeconds(_phaseDelay);

            if (_eventManager != null && _eventManager.ProcessNextEvent(playerIndex))
            {
                // Evenement en cours : la UI NarrativeScreen prend le relais.
                // Quand elle est fermee, elle appele OnNarrativeEventDismissed.
                CurrentPhase = TurnPhase.NarrativeEvent;
                EventBus.Publish(new GameEvents.TurnPhaseChanged
                {
                    Phase = TurnPhase.NarrativeEvent,
                    TurnNumber = CurrentTurn,
                    PlayerIndex = CurrentPlayerIndex
                });
            }
            else
            {
                // Pas d'evenement : passer directement au cycle normal
                AdvanceAfterNarrative();
            }
        }

        /// <summary>
        /// Appelé par l'EventManager ou la NarrativeScreen quand l'événement
        /// narratif est terminé. Passe au cycle normal des phases.
        /// </summary>
        public void OnNarrativeEventDismissed()
        {
            if (CurrentPhase == TurnPhase.NarrativeEvent)
            {
                AdvanceAfterNarrative();
            }
        }

        /// <summary>
        /// Passe du NarrativeEvent à la première phase du cycle normal.
        /// </summary>
        private void AdvanceAfterNarrative()
        {
            _phaseIndex = 0;
            CurrentPhase = PlayerPhases[0];

            EventBus.Publish(new GameEvents.TurnPhaseChanged
            {
                Phase = CurrentPhase,
                TurnNumber = CurrentTurn,
                PlayerIndex = CurrentPlayerIndex
            });
        }

        private float _phaseDelay => _phaseTransitionDelay;
    }
}
