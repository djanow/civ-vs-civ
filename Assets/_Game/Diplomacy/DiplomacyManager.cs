using System.Collections.Generic;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Gère les relations diplomatiques entre civilisations.
    /// Relations, mémoire diplomatique, propositions et décisions IA.
    /// </summary>
    public class DiplomacyManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int _playerCount = 2;

        [Header("Debug")]
        [SerializeField] private bool _logActions = true;

        // Relations matrix: relations[a][b] = -100 to +100
        private int[][] _relations;

        // AI personalities for NPC civs
        private AIPersonalityType[] _aiPersonalities;

        // Per-player diplomatic memory
        private List<string>[] _diplomaticMemory;

        // Pending diplomatic actions awaiting player response
        private List<DiplomaticAction> _pendingActions;

        // Turn counter for various cooldowns
        private int _currentTurn;

        /// <summary>
        /// Nombre de joueurs configuré.
        /// </summary>
        public int PlayerCount => _playerCount;

        /// <summary>
        /// Relations entre deux civs (lecture seule).
        /// </summary>
        public int[][] Relations => _relations;

        /// <summary>
        /// Personnalités IA des joueurs non-humains.
        /// </summary>
        public AIPersonalityType[] AIPersonalities => _aiPersonalities;

        /// <summary>
        /// Mémoire diplomatique par joueur.
        /// </summary>
        public List<string>[] DiplomaticMemory => _diplomaticMemory;

        /// <summary>
        /// Actions en attente de réponse.
        /// </summary>
        public List<DiplomaticAction> PendingActions => _pendingActions;

        private void Awake()
        {
            _pendingActions = new List<DiplomaticAction>();
            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
            EventBus.Subscribe<GameEvents.TurnEnded>(OnTurnEnded);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
            EventBus.Unsubscribe<GameEvents.TurnEnded>(OnTurnEnded);
        }

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            Initialize(_playerCount);
        }

        private void OnTurnEnded(GameEvents.TurnEnded evt)
        {
            _currentTurn = evt.TurnNumber;
        }

        /// <summary>
        /// Initialise les relations et personnalités pour le nombre de joueurs donné.
        /// </summary>
        public void Initialize(int playerCount)
        {
            _playerCount = playerCount;
            _currentTurn = 0;

            // Relations matrix
            _relations = new int[playerCount][];
            for (int i = 0; i < playerCount; i++)
            {
                _relations[i] = new int[playerCount];
                for (int j = 0; j < playerCount; j++)
                {
                    _relations[i][j] = i == j ? 100 : 0; // Neutre entre civs differentes
                }
            }

            // AI personalities (alternating for variety)
            _aiPersonalities = new AIPersonalityType[playerCount];
            var personalities = (AIPersonalityType[])System.Enum.GetValues(typeof(AIPersonalityType));
            for (int i = 0; i < playerCount; i++)
            {
                // Player 0 is human (no AI personality needed), players 1+ get AI
                if (i > 0)
                {
                    _aiPersonalities[i] = personalities[(i - 1) % personalities.Length];
                }
                else
                {
                    _aiPersonalities[i] = AIPersonalityType.Diplomatic; // Human default
                }
            }

            // Diplomatic memory
            _diplomaticMemory = new List<string>[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                _diplomaticMemory[i] = new List<string>();
            }

            _pendingActions.Clear();

            if (_logActions)
                Debug.Log($"[DiplomacyManager] Initialized for {playerCount} players.");
        }

        /// <summary>
        /// Modifie les relations entre deux civilisations.
        /// </summary>
        public void ModifyRelations(int from, int to, int delta, string reason)
        {
            if (from < 0 || from >= _playerCount || to < 0 || to >= _playerCount) return;
            if (from == to) return;

            _relations[from][to] = Mathf.Clamp(_relations[from][to] + delta, -100, 100);

            // La reciproque peut etre asymetrique selon le contexte
            // Par defaut, symetrique pour les actions directes
            if (delta > 0)
            {
                // Un geste positif ameliore aussi la reciproque (legerement)
                _relations[to][from] = Mathf.Clamp(_relations[to][from] + delta / 2, -100, 100);
            }

            // Log dans la memoire diplomatique
            string log = $"Tour {_currentTurn}: {reason} ({delta:+0;-0;0})";
            _diplomaticMemory[from].Add(log);
            _diplomaticMemory[to].Add(log);

            if (_logActions)
                Debug.Log($"[Diplomacy] Relations J{from}->J{to}: {delta} ({reason}). Now: {_relations[from][to]}");
        }

        /// <summary>
        /// Retourne le niveau de relation entre deux civilisations.
        /// </summary>
        public int GetRelations(int civA, int civB)
        {
            if (civA < 0 || civA >= _playerCount || civB < 0 || civB >= _playerCount)
                return 0;
            return _relations[civA][civB];
        }

        /// <summary>
        /// Propose un echange commercial entre deux civilisations.
        /// </summary>
        public DiplomaticAction ProposeTrade(int proposer, int target, TradeOffer offer)
        {
            if (!ValidateIndices(proposer, target))
                return DiplomaticAction.Create(DiploActionType.TradeRoute, proposer, target);

            var action = DiplomaticAction.CreateWithOffer(DiploActionType.TradeRoute, proposer, target, offer);

            // Si la cible est une IA, evaluer automatiquement
            if (target != 0) // Player 0 is human
            {
                float willingness = AIPersonalityManager.GetTradeWillingness(
                    _aiPersonalities[target], _relations[proposer][target]);

                action.Accepted = Random.value < willingness;

                if (action.Accepted)
                {
                    ApplyTrade(proposer, target, offer);
                    AddMemory(proposer, $"Route commerciale acceptee par J{target} ({offer})");
                    AddMemory(target, $"Route commerciale acceptee de J{proposer} ({offer})");

                    if (_logActions)
                        Debug.Log($"[Diplomacy] AI J{target} accepted trade from J{proposer}: {offer}");
                }
                else
                {
                    AddMemory(proposer, $"Route commerciale refusee par J{target} ({offer})");

                    if (_logActions)
                        Debug.Log($"[Diplomacy] AI J{target} declined trade from J{proposer}: {offer}");
                }
            }
            else
            {
                // Human target -- add to pending
                _pendingActions.Add(action);

                if (_logActions)
                    Debug.Log($"[Diplomacy] Trade offered to human player: {action}");
            }

            return action;
        }

        /// <summary>
        /// Declare la guerre entre deux civilisations.
        /// </summary>
        public void DeclareWar(int attacker, int defender)
        {
            if (!ValidateIndices(attacker, defender)) return;

            // Chute massive de relations
            ModifyRelations(attacker, defender, -50, "Declaration de guerre");
            ModifyRelations(defender, attacker, -40, "Declaration de guerre recue");

            AddMemory(attacker, $"A declare la guerre a J{defender}");
            AddMemory(defender, $"A recu une declaration de guerre de J{attacker}");

            if (_logActions)
                Debug.Log($"[Diplomacy] WAR: J{attacker} declared war on J{defender}!");

            EventBus.Publish(new GameEvents.WarDeclared
            {
                AttackerIndex = attacker,
                DefenderIndex = defender
            });
        }

        /// <summary>
        /// Propose la paix entre deux civilisations en guerre.
        /// </summary>
        public DiplomaticAction ProposePeace(int proposer, int target)
        {
            if (!ValidateIndices(proposer, target))
                return DiplomaticAction.Create(DiploActionType.PeaceTreaty, proposer, target);

            var action = DiplomaticAction.Create(DiploActionType.PeaceTreaty, proposer, target);

            // AI evaluation
            if (target != 0)
            {
                int currentRelations = _relations[proposer][target];
                bool accepts = currentRelations > -30 || Random.value < 0.4f;
                action.Accepted = accepts;

                if (accepts)
                {
                    ApplyPeace(proposer, target);

                    if (_logActions)
                        Debug.Log($"[Diplomacy] AI J{target} accepted peace with J{proposer}.");
                }
            }
            else
            {
                _pendingActions.Add(action);
            }

            return action;
        }

        /// <summary>
        /// Propose une alliance entre deux civilisations.
        /// </summary>
        public DiplomaticAction ProposeAlliance(int proposer, int target)
        {
            if (!ValidateIndices(proposer, target))
                return DiplomaticAction.Create(DiploActionType.Alliance, proposer, target);

            var action = DiplomaticAction.Create(DiploActionType.Alliance, proposer, target);

            if (target != 0)
            {
                int relations = _relations[proposer][target];
                int commonEnemies = CountCommonEnemies(proposer, target);
                float desire = AIPersonalityManager.GetAllianceDesire(
                    _aiPersonalities[target], relations, commonEnemies);
                action.Accepted = Random.value < desire;

                if (action.Accepted)
                {
                    ApplyAlliance(proposer, target);

                    if (_logActions)
                        Debug.Log($"[Diplomacy] AI J{target} accepted alliance with J{proposer}.");
                }
            }
            else
            {
                _pendingActions.Add(action);
            }

            return action;
        }

        /// <summary>
        /// Fait un don (or ou science) a une autre civilisation.
        /// </summary>
        public DiplomaticAction MakeGift(int giver, int receiver, TradeOffer gift)
        {
            if (!ValidateIndices(giver, receiver))
                return DiplomaticAction.Create(DiploActionType.Gift, giver, receiver);

            var action = DiplomaticAction.CreateWithOffer(DiploActionType.Gift, giver, receiver, gift);

            // Appliquer le don
            if (gift.GoldAmount > 0)
            {
                if (GameManager.Instance != null)
                {
                    int giverGold = GameManager.Instance.PlayerGold[giver];
                    int actualGold = Mathf.Min(gift.GoldAmount, giverGold);
                    GameManager.Instance.PlayerGold[giver] = giverGold - actualGold;
                    GameManager.Instance.PlayerGold[receiver] += actualGold;
                }
            }

            if (gift.ScienceAmount > 0)
            {
                if (GameManager.Instance != null)
                {
                    int giverScience = GameManager.Instance.PlayerScience[giver];
                    int actualScience = Mathf.Min(gift.ScienceAmount, giverScience);
                    GameManager.Instance.PlayerScience[giver] = giverScience - actualScience;
                    GameManager.Instance.PlayerScience[receiver] += actualScience;
                }
            }

            // Bonus de relations pour un geste genereux
            int relationBonus = 5 + (gift.GoldAmount > 0 ? gift.GoldAmount / 10 : 0)
                + (gift.ScienceAmount > 0 ? gift.ScienceAmount / 5 : 0);
            ModifyRelations(giver, receiver, Mathf.Min(relationBonus, 30), $"Cadeau genereux ({gift})");

            AddMemory(giver, $"A offert un cadeau a J{receiver} ({gift})");
            AddMemory(receiver, $"A recu un cadeau de J{giver} ({gift})");

            action.Accepted = true;

            return action;
        }

        /// <summary>
        /// Traite la reponse humaine a une action diplomatique en attente.
        /// </summary>
        public void RespondToAction(int actionIndex, bool accepted)
        {
            if (actionIndex < 0 || actionIndex >= _pendingActions.Count) return;

            var action = _pendingActions[actionIndex];
            _pendingActions.RemoveAt(actionIndex);

            if (!accepted)
            {
                ModifyRelations(action.TargetIndex, action.ProposerIndex, -5,
                    $"A refuse l'action {action.Type}");

                if (_logActions)
                    Debug.Log($"[Diplomacy] Player {action.TargetIndex} declined {action.Type} " +
                        $"from {action.ProposerIndex}.");

                EventBus.Publish(new GameEvents.DiplomaticInteraction
                {
                    Action = new DiplomaticAction
                    {
                        Type = action.Type,
                        ProposerIndex = action.ProposerIndex,
                        TargetIndex = action.TargetIndex,
                        Offer = action.Offer,
                        Accepted = false
                    }
                });
                return;
            }

            // Accepter selon le type
            switch (action.Type)
            {
                case DiploActionType.TradeRoute:
                    ApplyTrade(action.ProposerIndex, action.TargetIndex, action.Offer);
                    break;
                case DiploActionType.Alliance:
                    ApplyAlliance(action.ProposerIndex, action.TargetIndex);
                    break;
                case DiploActionType.PeaceTreaty:
                    ApplyPeace(action.ProposerIndex, action.TargetIndex);
                    break;
                case DiploActionType.NonAggressionPact:
                    ApplyNonAggressionPact(action.ProposerIndex, action.TargetIndex);
                    break;
                case DiploActionType.Gift:
                    // Already handled at creation
                    break;
            }

            action.Accepted = true;

            EventBus.Publish(new GameEvents.DiplomaticInteraction
            {
                Action = action
            });

            if (_logActions)
                Debug.Log($"[Diplomacy] Player {action.TargetIndex} accepted {action.Type} " +
                    $"from {action.ProposerIndex}.");
        }

        /// <summary>
        /// Soumet une contre-offre a une action diplomatique en attente.
        /// </summary>
        public void CounterOffer(int actionIndex, TradeOffer counterOffer)
        {
            if (actionIndex < 0 || actionIndex >= _pendingActions.Count) return;

            var action = _pendingActions[actionIndex];
            _pendingActions.RemoveAt(actionIndex);

            if (action.ProposerIndex != 0)
            {
                // AI evalue la contre-offre
                float willingness = AIPersonalityManager.GetTradeWillingness(
                    _aiPersonalities[action.ProposerIndex],
                    _relations[action.ProposerIndex][action.TargetIndex]);

                bool accepted = Random.value < willingness * 0.7f; // Legerement plus dur

                if (accepted)
                {
                    action.Offer = counterOffer;
                    action.Accepted = true;
                    ApplyTrade(action.ProposerIndex, action.TargetIndex, counterOffer);

                    EventBus.Publish(new GameEvents.DiplomaticInteraction { Action = action });
                }
                else
                {
                    ModifyRelations(action.TargetIndex, action.ProposerIndex, -3,
                        "Contre-offre refusee");
                }
            }
        }

        /// <summary>
        /// L'IA decide d'une action diplomatique pour son tour.
        /// Appele pendant la phase Diplomatie.
        /// </summary>
        public DiplomaticAction AIDecideAction(int aiPlayer)
        {
            if (aiPlayer < 0 || aiPlayer >= _playerCount || aiPlayer == 0)
                return default; // Player 0 is human

            var personality = _aiPersonalities[aiPlayer];

            // Ne pas agir tous les tours (30% de chance)
            if (Random.value > 0.3f) return default;

            // Choisir une cible (pour l'instant, l'autre joueur)
            int target = aiPlayer == 1 ? 0 : 1;
            if (target >= _playerCount) return default;

            int relations = _relations[aiPlayer][target];

            // Decision basee sur la personnalite et les relations
            float warRoll = Random.value;
            float tradeRoll = Random.value;
            float allianceRoll = Random.value;

            float warThreshold = AIPersonalityManager.GetWarThreshold(personality,
                EvaluatePowerRatio(aiPlayer, target));

            if (warRoll < warThreshold && relations < 0)
            {
                DeclareWar(aiPlayer, target);
                return DiplomaticAction.Create(DiploActionType.WarDeclaration, aiPlayer, target);
            }

            if (relations > -20)
            {
                float tradeWillingness = AIPersonalityManager.GetTradeWillingness(personality, relations);
                if (tradeRoll < tradeWillingness && GameManager.Instance != null)
                {
                    int gold = GameManager.Instance.PlayerGold[aiPlayer];
                    if (gold > 20)
                    {
                        var offer = new TradeOffer
                        {
                            GoldAmount = Random.Range(5, gold / 2),
                            ScienceAmount = 0,
                            Turns = 5
                        };

                        if (offer.GoldAmount > 0)
                        {
                            return ProposeTrade(aiPlayer, target, offer);
                        }
                    }
                }

                if (relations > 30)
                {
                    int commonEnemies = CountCommonEnemies(aiPlayer, target);
                    float allianceDesire = AIPersonalityManager.GetAllianceDesire(
                        personality, relations, commonEnemies);
                    if (allianceRoll < allianceDesire)
                    {
                        return ProposeAlliance(aiPlayer, target);
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Trouve le joueur par nom de civilisation (pour les effets narratifs).
        /// </summary>
        public int FindPlayerByCivName(string civName)
        {
            var gameManager = GameManager.Instance;
            if (gameManager?.CivManager == null) return -1;

            for (int i = 0; i < _playerCount; i++)
            {
                var civ = gameManager.CivManager.GetCivData(i);
                if (civ != null && civ.CivName == civName)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Reinitialise l'etat pour une nouvelle partie.
        /// </summary>
        public void ResetState()
        {
            Initialize(_playerCount);
        }

        // ---- Private helpers ----

        private bool ValidateIndices(int a, int b)
        {
            if (a < 0 || a >= _playerCount || b < 0 || b >= _playerCount)
            {
                Debug.LogWarning($"[DiplomacyManager] Invalid player indices: {a}, {b}.");
                return false;
            }
            if (a == b)
            {
                Debug.LogWarning("[DiplomacyManager] Cannot interact with self.");
                return false;
            }
            return true;
        }

        private void AddMemory(int playerIndex, string entry)
        {
            if (playerIndex < 0 || playerIndex >= _playerCount) return;

            _diplomaticMemory[playerIndex].Add($"Turn {_currentTurn}: {entry}");

            // Garder les 50 dernieres entrees max
            if (_diplomaticMemory[playerIndex].Count > 50)
            {
                _diplomaticMemory[playerIndex].RemoveAt(0);
            }
        }

        private void ApplyTrade(int proposer, int target, TradeOffer offer)
        {
            if (GameManager.Instance == null) return;

            // Transfert d'or du proposeur vers la cible
            if (offer.GoldAmount > 0)
            {
                int actual = Mathf.Min(offer.GoldAmount, GameManager.Instance.PlayerGold[proposer]);
                GameManager.Instance.PlayerGold[proposer] -= actual;
                GameManager.Instance.PlayerGold[target] += actual;
            }

            // Bonus de relations pour commerce
            ModifyRelations(proposer, target, 5, "Route commerciale etablie");
            ModifyRelations(target, proposer, 5, "Route commerciale etablie");

            AddMemory(proposer, $"Route commerciale avec J{target} ({offer})");
            AddMemory(target, $"Route commerciale avec J{proposer} ({offer})");
        }

        private void ApplyPeace(int partyA, int partyB)
        {
            ModifyRelations(partyA, partyB, 20, "Traite de paix signe");
            ModifyRelations(partyB, partyA, 20, "Traite de paix signe");

            AddMemory(partyA, $"Paix signee avec J{partyB}");
            AddMemory(partyB, $"Paix signee avec J{partyA}");

            EventBus.Publish(new GameEvents.PeaceSigned
            {
                PartyA = partyA,
                PartyB = partyB
            });
        }

        private void ApplyAlliance(int partyA, int partyB)
        {
            ModifyRelations(partyA, partyB, 30, "Alliance formee");
            ModifyRelations(partyB, partyA, 30, "Alliance formee");

            AddMemory(partyA, $"Alliance avec J{partyB}");
            AddMemory(partyB, $"Alliance avec J{partyA}");
        }

        private void ApplyNonAggressionPact(int partyA, int partyB)
        {
            ModifyRelations(partyA, partyB, 15, "Pacte de non-agression");
            ModifyRelations(partyB, partyA, 15, "Pacte de non-agression");

            AddMemory(partyA, $"Pacte de non-agression avec J{partyB}");
            AddMemory(partyB, $"Pacte de non-agression avec J{partyA}");
        }

        private int CountCommonEnemies(int civA, int civB)
        {
            int count = 0;
            for (int i = 0; i < _playerCount; i++)
            {
                if (i == civA || i == civB) continue;
                if (_relations[civA][i] < -30 && _relations[civB][i] < -30)
                    count++;
            }
            return count;
        }

        private float EvaluatePowerRatio(int aiPlayer, int target)
        {
            // Evaluation simple basee sur l'or disponible (proxy de puissance)
            if (GameManager.Instance == null) return 1f;

            int aiGold = GameManager.Instance.PlayerGold[aiPlayer];
            int targetGold = GameManager.Instance.PlayerGold[target];

            if (targetGold <= 0) return 2f; // L'IA est infiniment plus puissante
            return (float)aiGold / targetGold;
        }
    }
}
