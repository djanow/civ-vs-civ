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
        private TechTreeUI _techTreeUI;

        private void Start()
        {
            _unitManager = FindAnyObjectByType<UnitManager>();
            _eventManager = FindAnyObjectByType<EventManager>();
            _researchManager = FindAnyObjectByType<ResearchManager>();
            _diplomacyManager = FindAnyObjectByType<DiplomacyManager>();
            _techTreeUI = FindAnyObjectByType<TechTreeUI>();
            if (_techTreeUI == null)
            {
                var go = new GameObject("TechTreeUI");
                _techTreeUI = go.AddComponent<TechTreeUI>();
            }

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

                    // Verifier victoire apres la fin du tour
                    CheckVictoryCondition();

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
                    // on sort de la boucle ici.
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

                    // Phases interactives pour le joueur humain : s'arrêter
                    if (CurrentPlayerIndex == 0)
                    {
                        if (CurrentPhase == TurnPhase.Movement)
                        {
                            yield break;
                        }
                        if (CurrentPhase == TurnPhase.Research)
                        {
                            ShowTechTreeForPlayer();
                            yield break;
                        }
                    }

                    // Phases non-interactives (IA ou phases automatiques) : avance
                    yield return new WaitForSeconds(0.3f);
                    _phaseIndex++;
                }
            }
        }

        // ----------------------------------------------------------------
        // Tech Tree UI (Task 1)
        // ----------------------------------------------------------------

        /// <summary>
        /// Affiche l'arbre technologique pour le joueur humain et attend son choix.
        /// </summary>
        private void ShowTechTreeForPlayer()
        {
            if (_techTreeUI == null) return;

            // Si aucune tech disponible, avancer directement
            if (_researchManager != null && _researchManager.GetAvailableTechs(CurrentPlayerIndex).Count == 0)
            {
                Debug.Log("[TurnManager] Aucune tech disponible pour le joueur humain, avancement automatique.");
                StartCoroutine(AdvancePhase());
                return;
            }

            _techTreeUI.OnTechClicked = OnTechSelected;
            _techTreeUI.Show(CurrentPlayerIndex);
        }

        /// <summary>
        /// Appelé quand le joueur sélectionne une tech dans l'UI.
        /// -1 signifie fermeture sans sélection.
        /// </summary>
        private void OnTechSelected(int techId)
        {
            if (techId >= 0 && _researchManager != null)
            {
                // Appliquer la recherche et la progression du tour
                ProcessResearchPhase();
            }

            // Continuer le cycle des phases
            StartCoroutine(AdvancePhase());
        }

        /// <summary>
        /// Traite automatiquement les phases qui n'ont pas besoin d'interaction joueur.
        /// </summary>
        private IEnumerator ProcessPhase(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.Research:
                    // Pour le joueur IA, auto-rechercher
                    if (CurrentPlayerIndex > 0)
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

            EventBus.Publish(new GameEvents.TurnEnded
            {
                TurnNumber = CurrentTurn,
                PlayerIndex = CurrentPlayerIndex
            });

            yield return null;
        }

        // ----------------------------------------------------------------
        // Victoire (Task 2)
        // ----------------------------------------------------------------

        /// <summary>
        /// Vérifie les conditions de victoire après chaque fin de tour.
        /// Victoire militaire : un joueur possède toutes les villes.
        /// </summary>
        private void CheckVictoryCondition()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CityManager == null) return;

            var allCities = gm.CityManager.GetAllCities();
            if (allCities == null || allCities.Count == 0) return;

            int[] cityCount = new int[_playerCount];
            foreach (var city in allCities)
            {
                if (city.OwnerIndex >= 0 && city.OwnerIndex < _playerCount)
                    cityCount[city.OwnerIndex]++;
            }

            int playersWithCities = 0;
            int lastPlayerWithCities = -1;
            for (int i = 0; i < _playerCount; i++)
            {
                if (cityCount[i] > 0)
                {
                    playersWithCities++;
                    lastPlayerWithCities = i;
                }
            }

            if (playersWithCities <= 1 && _playerCount > 1 && lastPlayerWithCities >= 0)
            {
                // Audio feedback for victory
                GameManager.Instance?.AudioManager?.PlayVictory();

                Debug.Log($"[TurnManager] Victoire! Joueur {lastPlayerWithCities} gagne la partie!");
                gm.SetGameOver();

                // Afficher un texte de victoire simple
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    var victoryText = new GameObject("VictoryText", typeof(UnityEngine.UI.Text));
                    victoryText.transform.SetParent(canvas.transform, false);
                    var vt = victoryText.GetComponent<UnityEngine.UI.Text>();
                    vt.text = $"VICTOIRE! Joueur {lastPlayerWithCities} remporte la partie!";
                    vt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    vt.fontSize = 48;
                    vt.color = Color.yellow;
                    vt.alignment = TextAnchor.MiddleCenter;
                    var vtRT = vt.GetComponent<RectTransform>();
                    vtRT.anchorMin = Vector2.zero;
                    vtRT.anchorMax = Vector2.one;
                    vtRT.offsetMin = vtRT.offsetMax = Vector2.zero;
                }
            }
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

            // Audio feedback for turn start
            GameManager.Instance?.AudioManager?.PlayTurnStart();

            EventBus.Publish(new GameEvents.PlayerTurnStarted
            {
                PlayerIndex = playerIndex
            });

            // Rafraîchir les points de mouvement des unités du joueur
            if (_unitManager != null)
                _unitManager.RefreshMovementForPlayer(playerIndex);

            // Mettre à jour la visibilité (brouillard de guerre)

            // Verifier les evenements narratifs en attente
            StartCoroutine(CheckNarrativeEvent(playerIndex));
        }

        /// <summary>
        /// Vérifie s'il y a des événements narratifs en attente pour ce joueur.
        /// Si oui, on reste en phase NarrativeEvent jusqu'à résolution.
        /// Sinon, on tente un micro-événement spontané (50% de chance).
        /// </summary>
        private IEnumerator CheckNarrativeEvent(int playerIndex)
        {
            yield return new WaitForSeconds(_phaseDelay);

            // Étape 1 : événements déjà dans la file (provenant de conditions remplies)
            if (_eventManager != null && _eventManager.ProcessNextEvent(playerIndex))
            {
                CurrentPhase = TurnPhase.NarrativeEvent;
                EventBus.Publish(new GameEvents.TurnPhaseChanged
                {
                    Phase = TurnPhase.NarrativeEvent,
                    TurnNumber = CurrentTurn,
                    PlayerIndex = CurrentPlayerIndex
                });
                yield break;
            }

            // Étape 2 : micro-événement spontané (50% de chance pour le joueur humain)
            if (playerIndex == 0 && _eventManager != null && Random.value < 0.5f)
            {
                CreateSpontaneousEvent(playerIndex);
                if (_eventManager.ProcessNextEvent(playerIndex))
                {
                    CurrentPhase = TurnPhase.NarrativeEvent;
                    EventBus.Publish(new GameEvents.TurnPhaseChanged
                    {
                        Phase = TurnPhase.NarrativeEvent,
                        TurnNumber = CurrentTurn,
                        PlayerIndex = CurrentPlayerIndex
                    });
                    yield break;
                }
            }

            // Pas d'evenement : passer directement au cycle normal
            AdvanceAfterNarrative();
        }

        /// <summary>
        /// Crée un micro-événement narratif spontané avec des choix aléatoires.
        /// </summary>
        private void CreateSpontaneousEvent(int playerIndex)
        {
            if (_eventManager == null) return;

            string[] events = {
                "Un marchand erranger", "Un evenement mysterieux",
                "Le destin sourit", "Un messager arrive"
            };
            string[] descs = {
                "Un voyageur apporte des nouvelles lointaines et des marchandises exotiques.",
                "Un phenomene etrange illumine le ciel. Les pretres cherchent a l' interpreter.",
                "La fortune semble vous sourire aujourd'hui. Une opportunite se presente.",
                "Un messager couvert de poussiere arrive au galop. Il apporte des nouvelles."
            };
            int idx = Random.Range(0, events.Length);

            ChoiceData[] choices;
            if (Random.value < 0.5f)
            {
                choices = new ChoiceData[] {
                    new ChoiceData { ChoiceText = "Saisir l'opportunite", Effects = new[] { "+30 gold", "+10 science" }, NarrativeFollowUp = "Une decision payante!" },
                    new ChoiceData { ChoiceText = "Agir avec prudence", Effects = new[] { "+15 culture" }, NarrativeFollowUp = "La sagesse est une vertu." }
                };
            }
            else
            {
                choices = new ChoiceData[] {
                    new ChoiceData { ChoiceText = "Investir dans la recherche", Effects = new[] { "+25 science" }, NarrativeFollowUp = "La connaissance progresse." },
                    new ChoiceData { ChoiceText = "Renforcer l'economie", Effects = new[] { "+40 gold" }, NarrativeFollowUp = "Les caisses se remplissent." },
                    new ChoiceData { ChoiceText = "Encourager les arts", Effects = new[] { "+20 culture" }, NarrativeFollowUp = "Les artistes celebrent votre nom." }
                };
            }

            _eventManager.CreateProceduralEvent(events[idx], descs[idx], playerIndex, choices);
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
        /// Si le joueur est une IA, démarre automatiquement le cycle.
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

            // Si c'est l'IA, démarrer automatiquement le cycle des phases
            if (CurrentPlayerIndex > 0)
            {
                StartCoroutine(AIAutoPlay());
            }
        }

        // ----------------------------------------------------------------
        // IA Auto-play (Task 6)
        // ----------------------------------------------------------------

        /// <summary>
        /// Déroule automatiquement toutes les phases pour le joueur IA
        /// avec de courtes pauses pour que le joueur humain voie les changements.
        /// </summary>
        private IEnumerator AIAutoPlay()
        {
            yield return new WaitForSeconds(0.5f);

            // Phase Mouvement (IA : sauter)
            CurrentPhase = TurnPhase.Movement;
            EventBus.Publish(new GameEvents.TurnPhaseChanged
            {
                Phase = TurnPhase.Movement,
                TurnNumber = CurrentTurn,
                PlayerIndex = CurrentPlayerIndex
            });
            yield return new WaitForSeconds(0.3f);

            // Phase Gestion de Ville (IA : sauter)
            CurrentPhase = TurnPhase.CityManagement;
            EventBus.Publish(new GameEvents.TurnPhaseChanged
            {
                Phase = TurnPhase.CityManagement,
                TurnNumber = CurrentTurn,
                PlayerIndex = CurrentPlayerIndex
            });
            yield return new WaitForSeconds(0.3f);

            // Phase Recherche (IA : auto-recherche)
            CurrentPhase = TurnPhase.Research;
            ProcessResearchPhase();
            EventBus.Publish(new GameEvents.TurnPhaseChanged
            {
                Phase = TurnPhase.Research,
                TurnNumber = CurrentTurn,
                PlayerIndex = CurrentPlayerIndex
            });
            yield return new WaitForSeconds(0.3f);

            // Phase Fin de tour
            CurrentPhase = TurnPhase.EndOfTurn;
            EventBus.Publish(new GameEvents.TurnPhaseChanged
            {
                Phase = TurnPhase.EndOfTurn,
                TurnNumber = CurrentTurn,
                PlayerIndex = CurrentPlayerIndex
            });

            yield return ProcessEndOfTurn();
            CheckVictoryCondition();

            // Passer au joueur suivant (revenir au joueur humain)
            CurrentPlayerIndex = 0;
            CurrentTurn++;
            _phaseIndex = 0;

            yield return new WaitForSeconds(0.5f);
            BeginPlayerTurn(0);
        }

        private float _phaseDelay => _phaseTransitionDelay;
    }
}
