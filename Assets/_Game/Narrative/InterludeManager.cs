using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Gere les interludes narratifs entre les transitions d'ere.
    /// Coordonne le changement de leader, l'affichage narratif,
    /// et le systeme de legs (legacy) accumules.
    /// </summary>
    public class InterludeManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private NarrativeScreen _narrativeScreen;

        [Header("Sons")]
        [SerializeField] private AudioClip _eraTransitionSound;

        [Header("Debug")]
        [SerializeField] private bool _skipInterludes = false;

        /// <summary>
        /// Legs actifs par joueur (indexes par nom).
        /// </summary>
        private Dictionary<int, List<string>> _activeLegacies = new Dictionary<int, List<string>>();

        private EventManager _eventManager;

        private void Awake()
        {
            _eventManager = GameManager.Instance?.GetComponent<EventManager>();
            if (_eventManager == null)
                _eventManager = FindAnyObjectByType<EventManager>();
        }

        // ----------------------------------------------------------------
        // Transition d'ere
        // ----------------------------------------------------------------

        /// <summary>
        /// Declenche une transition d'ere avec interlude narratif.
        /// newEra = -1 pour passer a l'ere suivante automatiquement.
        /// </summary>
        public void TriggerEraTransition(int playerIndex, int newEra)
        {
            var civManager = GameManager.Instance?.CivManager;
            if (civManager == null)
            {
                Debug.LogError("[InterludeManager] CivManager introuvable.");
                return;
            }

            int currentEra = civManager.GetPlayerEra(playerIndex);
            int targetEra = newEra >= 0 ? newEra : currentEra + 1;

            // Verifier qu'on avance bien
            if (targetEra <= currentEra)
            {
                Debug.LogWarning($"[InterludeManager] Ere {targetEra} <= actuelle {currentEra}. Ignore.");
                return;
            }

            // Recuperer le legacy du leader sortant
            string legacy = civManager.AdvanceEra(playerIndex);
            if (!string.IsNullOrEmpty(legacy))
            {
                ApplyLegacy(playerIndex, legacy);
                Debug.Log($"[InterludeManager] Legacy obtenu pour joueur {playerIndex} : {legacy}");
            }

            // Trouver l'interlude correspondant a cette transition
            EventData interlude = FindEraTransitionEvent(playerIndex, currentEra, targetEra);

            if (interlude != null && !_skipInterludes)
            {
                // Afficher l'interlude
                ShowInterlude(interlude, playerIndex);
            }
            else
            {
                // Pas d'interlude : continuer directement
                Debug.Log($"[InterludeManager] Transition ere {currentEra} -> {targetEra} (pas d'interlude)");
                if (_eraTransitionSound != null)
                    AudioSource.PlayClipAtPoint(_eraTransitionSound, Vector3.zero);

                EventBus.Publish(new GameEvents.EraAdvanced
                {
                    PlayerIndex = playerIndex,
                    OldEra = currentEra,
                    NewEra = targetEra,
                    NewLeaderName = civManager.GetCurrentLeader(playerIndex)?.LeaderName ?? "Inconnu"
                });
            }
        }

        // ----------------------------------------------------------------
        // Affichage d'interlude
        // ----------------------------------------------------------------

        /// <summary>
        /// Affiche un interlude narratif pour un joueur.
        /// </summary>
        public void ShowInterlude(EventData interludeEvent, int playerIndex)
        {
            if (interludeEvent == null)
            {
                Debug.LogWarning("[InterludeManager] interludeEvent null.");
                return;
            }

            // Si on a un ecran narratif, l'utiliser
            if (_narrativeScreen != null)
            {
                _narrativeScreen.Show(interludeEvent, playerIndex);
                GameManager.Instance.CurrentState = GameState.Interlude;
            }
            // Sinon, passer par l'EventManager
            else if (_eventManager != null)
            {
                _eventManager.TriggerEvent(interludeEvent, playerIndex);
            }
            else
            {
                Debug.LogWarning("[InterludeManager] Aucun ecran narratif disponible.");
            }
        }

        /// <summary>
        /// Appele par l'UI NarrativeScreen quand un choix est fait.
        /// </summary>
        public void OnInterludeChoice(int choiceIndex)
        {
            if (_narrativeScreen != null)
            {
                _narrativeScreen.OnChoiceSelected(choiceIndex);
            }
        }

        // ----------------------------------------------------------------
        // Systeme de legs (legacy)
        // ----------------------------------------------------------------

        /// <summary>
        /// Retourne la liste des legs actifs pour un joueur.
        /// </summary>
        public List<string> GetActiveLegacies(int playerIndex)
        {
            if (_activeLegacies.ContainsKey(playerIndex))
                return new List<string>(_activeLegacies[playerIndex]);
            return new List<string>();
        }

        /// <summary>
        /// Verifie si un joueur possede un leg specifique.
        /// </summary>
        public bool HasLegacy(int playerIndex, string legacyName)
        {
            return _activeLegacies.ContainsKey(playerIndex)
                && _activeLegacies[playerIndex].Contains(legacyName);
        }

        /// <summary>
        /// Applique un leg a un joueur.
        /// </summary>
        public void ApplyLegacy(int playerIndex, string legacyName)
        {
            if (!_activeLegacies.ContainsKey(playerIndex))
                _activeLegacies[playerIndex] = new List<string>();

            if (!_activeLegacies[playerIndex].Contains(legacyName))
            {
                _activeLegacies[playerIndex].Add(legacyName);
                Debug.Log($"[InterludeManager] Legacy \"{legacyName}\" applique au joueur {playerIndex}");

                // Synchroniser avec CivManager
                GameManager.Instance?.CivManager?.AddLegacy(playerIndex, legacyName);
            }
        }

        // ----------------------------------------------------------------
        // Prive
        // ----------------------------------------------------------------

        /// <summary>
        /// Trouve l'evenement d'interlude correspondant a une transition d'ere.
        /// Cherche dans l'EventManager un evenement de type Interlude
        /// avec TriggerEra = oldEra (l'ere qu'on quitte).
        /// </summary>
        private EventData FindEraTransitionEvent(int playerIndex, int oldEra, int newEra)
        {
            var civData = GameManager.Instance?.CivManager?.GetCivData(playerIndex);
            if (civData == null) return null;

            int civId = civData.CivId;

            // Chercher dans les evenements disponibles
            var allEvents = _eventManager?.GetType()
                ?.GetField("_allEvents", System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_eventManager) as List<EventData>;

            if (allEvents == null)
            {
                // Fallback: chercher via FindObjectsOfType
                var loadedEvents = Resources.FindObjectsOfTypeAll<EventData>();
                foreach (var evt in loadedEvents)
                {
                    if (evt.Type == EventType.Interlude
                        && evt.TriggerEra == oldEra
                        && evt.MatchesCiv(civId))
                    {
                        return evt;
                    }
                }
                return null;
            }

            foreach (var evt in allEvents)
            {
                if (evt == null) continue;
                if (evt.Type == EventType.Interlude
                    && evt.TriggerEra == oldEra
                    && evt.MatchesCiv(civId))
                {
                    return evt;
                }
            }

            return null;
        }

        // ----------------------------------------------------------------
        // Utilitaires
        // ----------------------------------------------------------------

        /// <summary>Nombre de legs d'un joueur.</summary>
        public int LegacyCount(int playerIndex)
        {
            return _activeLegacies.ContainsKey(playerIndex)
                ? _activeLegacies[playerIndex].Count
                : 0;
        }

        /// <summary>Liste lisible des legs.</summary>
        public string LegacySummary(int playerIndex)
        {
            var legacies = GetActiveLegacies(playerIndex);
            if (legacies.Count == 0)
                return "Aucun leg";
            return string.Join(", ", legacies);
        }
    }
}
