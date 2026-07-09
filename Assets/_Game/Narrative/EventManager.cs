using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Gere tous les evenements narratifs : verification des conditions,
    /// file d'attente, declenchement, et routage vers l'UI appropriee.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        [Header("Donnees")]
        [SerializeField] private List<EventData> _allEvents = new List<EventData>();

        [Header("UI")]
        [SerializeField] private NarrativeScreen _narrativeScreen;
        [SerializeField] private NarrativePopup _microEventPopup;

        /// <summary>File d'attente des evenements en attente (pour la phase NarrativeEvent).</summary>
        private Queue<EventData> _pendingEvents = new Queue<EventData>();

        /// <summary>Evenements deja declenches (IDs) pour eviter les doublons.</summary>
        private HashSet<int> _triggeredEvents = new HashSet<int>();

        /// <summary>Prochain ID d'evenement auto-genere.</summary>
        private int _nextAutoEventId = 9000;

        /// <summary>Liste publique des evenements (lecture seule).</summary>
        public IReadOnlyList<EventData> AllEvents => _allEvents.AsReadOnly();

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.PlayerTurnStarted>(OnPlayerTurnStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.PlayerTurnStarted>(OnPlayerTurnStarted);
        }

        private void OnPlayerTurnStarted(GameEvents.PlayerTurnStarted evt)
        {
            // Verifier les evenements contextuels au debut du tour
            CheckAndTriggerEvents(evt.PlayerIndex);
        }

        // ----------------------------------------------------------------
        // Gestion de la file d'attente
        // ----------------------------------------------------------------

        /// <summary>
        /// Enregistre un evenement dans la liste (utilise pour le contenu procedural).
        /// </summary>
        public void RegisterEvent(EventData evt)
        {
            if (evt == null || _allEvents.Any(e => e != null && e.EventId == evt.EventId))
                return;
            _allEvents.Add(evt);
            Debug.Log($"[EventManager] Evenement enregistre : {evt.Title} (ID {evt.EventId})");
        }

        /// <summary>
        /// Verifie les conditions de tous les evenements disponibles
        /// et les ajoute a la file s'ils se declenchent.
        /// </summary>
        public void CheckAndTriggerEvents(int playerIndex)
        {
            var civData = GameManager.Instance?.CivManager?.GetCivData(playerIndex);
            if (civData == null) return;

            int civId = civData.CivId;
            int era = GameManager.Instance.CivManager.GetPlayerEra(playerIndex);

            foreach (var evt in _allEvents)
            {
                if (evt == null) continue;
                if (_triggeredEvents.Contains(evt.EventId)) continue; // Deja declenche
                if (!evt.MatchesCiv(civId)) continue;

                // Verifier l'ere
                if (evt.TriggerEra >= 0 && evt.TriggerEra != era) continue;

                // Verifier les conditions
                if (!EvaluateConditions(evt.TriggerConditions, playerIndex)) continue;

                // Ajouter a la file
                _pendingEvents.Enqueue(evt);
                _triggeredEvents.Add(evt.EventId);

                Debug.Log($"[EventManager] Evenement ajoute a la file : {evt.Title} (ID {evt.EventId})");
            }
        }

        /// <summary>
        /// Verifie s'il y a des evenements en attente pour un joueur.
        /// Appele par TurnManager pendant la phase NarrativeEvent.
        /// Retourne true si un evenement a ete declenche.
        /// </summary>
        public bool ProcessNextEvent(int playerIndex)
        {
            if (_pendingEvents.Count == 0)
                return false;

            EventData evt = _pendingEvents.Dequeue();
            TriggerEvent(evt, playerIndex);
            return true;
        }

        // ----------------------------------------------------------------
        // Déclenchement
        // ----------------------------------------------------------------

        /// <summary>
        /// Declenche un evenement specifique par son ID.
        /// </summary>
        public void TriggerEvent(int eventId, int playerIndex)
        {
            var evt = _allEvents.FirstOrDefault(e => e != null && e.EventId == eventId);
            if (evt == null)
            {
                Debug.LogWarning($"[EventManager] Evenement {eventId} introuvable.");
                return;
            }
            TriggerEvent(evt, playerIndex);
        }

        /// <summary>
        /// Declenche un evenement : routage vers l'ecran approprie selon le type.
        /// </summary>
        public void TriggerEvent(EventData evt, int playerIndex)
        {
            if (evt == null) return;

            // Publier l'evenement systeme
            EventBus.Publish(new GameEvents.NarrativeEventTriggered
            {
                PlayerIndex = playerIndex,
                EventId = evt.EventId,
                Title = evt.Title
            });

            Debug.Log($"[EventManager] Déclenchement : {evt.Title} (joueur {playerIndex}, type {evt.Type})");

            switch (evt.Type)
            {
                case EventType.Micro:
                    ShowMicroEvent(evt, playerIndex);
                    break;

                case EventType.Interlude:
                    ShowInterlude(evt, playerIndex);
                    break;

                case EventType.KeyMoment:
                    ShowInterlude(evt, playerIndex); // Meme ecran que les interludes
                    break;
            }
        }

        /// <summary>
        /// Cree et ajoute un evenement procedural (micro) avec des donnees dynamiques.
        /// </summary>
        public int CreateProceduralEvent(string title, string description, int playerIndex,
            ChoiceData[] choices, EventType type = EventType.Micro)
        {
            var go = new GameObject($"ProcEvent_{title}");
            go.transform.SetParent(transform);

            var evt = ScriptableObject.CreateInstance<EventData>();
            evt.EventId = _nextAutoEventId++;
            evt.Title = title;
            evt.Description = description;
            evt.Type = type;
            evt.TriggerEra = -1;
            evt.TriggerCivIds = new[] { -1 };
            evt.TriggerConditions = System.Array.Empty<string>();
            evt.Choices = choices;

            _allEvents.Add(evt);
            _triggeredEvents.Add(evt.EventId);

            // Ajouter directement a la file
            _pendingEvents.Enqueue(evt);

            return evt.EventId;
        }

        // ----------------------------------------------------------------
        // Gestion des choix
        // ----------------------------------------------------------------

        /// <summary>
        /// Appele par l'UI NarrativeScreen ou NarrativePopup quand un joueur fait un choix.
        /// </summary>
        public void OnEventChoice(EventData evt, int choiceIndex, int playerIndex)
        {
            if (evt == null || evt.Choices == null || choiceIndex < 0 || choiceIndex >= evt.Choices.Length)
                return;

            ChoiceData choice = evt.Choices[choiceIndex];

            // Appliquer les effets
            ChoiceResolver.ResolveChoice(playerIndex, choice);

            // Legacy optionnel
            if (!string.IsNullOrEmpty(choice.LegacyUnlock))
            {
                var interlude = GameManager.Instance?.InterludeManager;
                if (interlude != null)
                    interlude.ApplyLegacy(playerIndex, choice.LegacyUnlock);
            }

            // Publier l'evenement de choix
            EventBus.Publish(new GameEvents.NarrativeChoiceMade
            {
                PlayerIndex = playerIndex,
                EventId = evt.EventId,
                ChoiceIndex = choiceIndex,
                EffectsDescription = ChoiceResolver.GetEffectPreview(choice, playerIndex)
            });
        }

        // ----------------------------------------------------------------
        // Affichage UI
        // ----------------------------------------------------------------

        private void ShowMicroEvent(EventData evt, int playerIndex)
        {
            if (_microEventPopup != null)
            {
                _microEventPopup.Show(evt, playerIndex);
            }
            else
            {
                Debug.LogWarning("[EventManager] NarrativePopup non assigne.");
            }
        }

        private void ShowInterlude(EventData evt, int playerIndex)
        {
            if (_narrativeScreen != null)
            {
                _narrativeScreen.Show(evt, playerIndex);
                GameManager.Instance.CurrentState = GameState.Interlude;
            }
            else
            {
                Debug.LogWarning("[EventManager] NarrativeScreen non assigne.");
            }
        }

        /// <summary>
        /// Cache l'ecran narratif et reprend le jeu.
        /// </summary>
        public void DismissNarrative()
        {
            if (_narrativeScreen != null)
                _narrativeScreen.Hide();
            if (_microEventPopup != null)
                _microEventPopup.Hide();

            GameManager.Instance.CurrentState = GameState.Playing;
        }

        // ----------------------------------------------------------------
        // Evaluation des conditions
        // ----------------------------------------------------------------

        /// <summary>
        /// Evalue toutes les conditions d'un tableau (ET logique).
        /// </summary>
        private bool EvaluateConditions(string[] conditions, int playerIndex)
        {
            if (conditions == null || conditions.Length == 0)
                return true;

            foreach (string condition in conditions)
            {
                if (!EvaluateCondition(condition, playerIndex))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Evalue une condition unique.
        /// Syntaxes supportees :
        ///   "HasCityOnCoast"           -> true si le joueur a une ville cotiere
        ///   "AtWar"                     -> true si en guerre
        ///   "RelationsBelow:-50"       -> true si relations avec un ennemi < -50
        ///   "RelationsAbove:50"        -> true si relations avec allie > 50
        ///   "HasScience:50"            -> true si science >= 50
        ///   "HasGold:100"              -> true si gold >= 100
        ///   "TurnCount:10"             -> true si tour >= 10
        ///   "Era:1"                    -> true si le joueur est a l'ere 1
        ///   "HasTech:Navigation"       -> true si la tech est debloquee
        ///   "HasUnlock:Colony"         -> true si le joueur a le flag Colony
        /// </summary>
        public bool EvaluateCondition(string condition, int playerIndex)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return true;

            string trimmed = condition.Trim();

            // Conditions sans parametre
            if (trimmed.Equals("AtWar", System.StringComparison.OrdinalIgnoreCase))
            {
                var diplomacy = GameManager.Instance?.DiplomacyManager;
                if (diplomacy == null) return false;
                for (int i = 0; i < 4; i++)
                {
                    if (i != playerIndex && diplomacy.GetRelations(playerIndex, i) < -50)
                        return true;
                }
                return false;
            }

            if (trimmed.Equals("HasCityOnCoast", System.StringComparison.OrdinalIgnoreCase))
            {
                var cityManager = GameManager.Instance?.CityManager;
                return cityManager != null && cityManager.HasCoastalCity(playerIndex);
            }

            // Conditions avec parametre "Key:Value"
            int colonIndex = trimmed.IndexOf(':');
            if (colonIndex < 0)
            {
                Debug.LogWarning($"[EventManager] Condition inconnue : \"{trimmed}\"");
                return false;
            }

            string key = trimmed.Substring(0, colonIndex).Trim();
            string value = trimmed.Substring(colonIndex + 1).Trim();
            var gm = GameManager.Instance;

            switch (key.ToLowerInvariant())
            {
                case "relationsbelow":
                    if (int.TryParse(value, out int relBelow))
                    {
                        var diplomacy = gm?.DiplomacyManager;
                        if (diplomacy == null) return false;
                        for (int i = 0; i < 4; i++)
                        {
                            if (i != playerIndex && diplomacy.GetRelations(playerIndex, i) < relBelow)
                                return true;
                        }
                        return false;
                    }
                    return false;

                case "relationsabove":
                    if (int.TryParse(value, out int relAbove))
                    {
                        var diplomacy = gm?.DiplomacyManager;
                        if (diplomacy == null) return false;
                        for (int i = 0; i < 4; i++)
                        {
                            if (i != playerIndex && diplomacy.GetRelations(playerIndex, i) > relAbove)
                                return true;
                        }
                        return false;
                    }
                    return false;

                case "hasscience":
                    if (int.TryParse(value, out int sci))
                        return gm != null && gm.GetPlayerScience(playerIndex) >= sci;
                    return false;

                case "hasgold":
                    if (int.TryParse(value, out int gold))
                        return gm != null && gm.GetPlayerGold(playerIndex) >= gold;
                    return false;

                case "turncount":
                    if (int.TryParse(value, out int turns))
                    {
                        var turnManager = FindAnyObjectByType<TurnManager>();
                        return turnManager != null && turnManager.CurrentTurn >= turns;
                    }
                    return false;

                case "era":
                    if (int.TryParse(value, out int era))
                        return gm?.CivManager?.GetPlayerEra(playerIndex) == era;
                    return false;

                case "hastech":
                    var research = FindAnyObjectByType<ResearchManager>();
                    return research != null && research.HasTech(playerIndex, value);

                case "hasunlock":
                    return gm != null && gm.HasUnlock(playerIndex, value);

                default:
                    Debug.LogWarning($"[EventManager] Condition inconnue : \"{trimmed}\"");
                    return false;
            }
        }
    }
}
