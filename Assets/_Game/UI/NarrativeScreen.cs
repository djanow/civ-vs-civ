using UnityEngine;
using UnityEngine.UI;

namespace CivVSCiv
{
    /// <summary>
    /// Ecran narratif plein ecran pour les interludes d'ere et les moments cles.
    /// Affiche le titre, la description narrative, et 2-3 choix avec apercu des effets.
    /// </summary>
    public class NarrativeScreen : MonoBehaviour
    {
        [Header("Textes")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _legacyText;           // Legacy du leader sortant (optionnel)
        [SerializeField] private Text _instructionText;       // "Choisissez votre destin"

        [Header("Boutons de choix")]
        [SerializeField] private Button[] _choiceButtons;     // 3 boutons max
        [SerializeField] private Text[] _choiceTexts;          // Texte du choix
        [SerializeField] private Text[] _choiceEffectTexts;    // Apercu des effets
        [SerializeField] private Text[] _choiceLegacyTexts;    // Legacy unlock (optionnel)

        [Header("Animations")]
        [SerializeField] private Animator _animator;
        [SerializeField] private float _typewriterSpeed = 0.02f;
        [SerializeField] private float _autoHideDelay = 60f;  // Timeout de securite

        [Header("Transition")]
        [SerializeField] private string _showTrigger = "Show";
        [SerializeField] private string _hideTrigger = "Hide";

        /// <summary>Evenement en cours d'affichage.</summary>
        private EventData _currentEvent;

        /// <summary>Joueur concerne par l'evenement.</summary>
        private int _currentPlayerIndex;

        /// <summary>Les boutons etaient-ils deja actives.</summary>
        private bool _choicesRevealed;

        private void Awake()
        {
            // Initialiser l'etat cache
            gameObject.SetActive(false);
            _choicesRevealed = false;
        }

        // ----------------------------------------------------------------
        // Affichage
        // ----------------------------------------------------------------

        /// <summary>
        /// Affiche un evenement narratif sur l'ecran plein.
        /// </summary>
        public void Show(EventData eventData, int playerIndex)
        {
            if (eventData == null)
            {
                Debug.LogWarning("[NarrativeScreen] eventData null.");
                return;
            }

            _currentEvent = eventData;
            _currentPlayerIndex = playerIndex;
            _choicesRevealed = false;

            // Activer le GameObject
            gameObject.SetActive(true);

            // Remplir les textes
            if (_titleText != null)
                _titleText.text = eventData.Title;

            if (_descriptionText != null)
                _descriptionText.text = eventData.Description;

            // Afficher le legacy du leader sortant (pour les interludes d'ere)
            if (_legacyText != null)
            {
                var civManager = GameManager.Instance?.CivManager;
                if (civManager != null)
                {
                    int era = civManager.GetPlayerEra(playerIndex);
                    var legacy = civManager.GetCurrentLeader(playerIndex)?.LegacyName;
                    if (!string.IsNullOrEmpty(legacy))
                    {
                        _legacyText.text = $"Legacy du leader sortant : {legacy}";
                        _legacyText.gameObject.SetActive(true);
                    }
                    else
                    {
                        _legacyText.gameObject.SetActive(false);
                    }
                }
                else
                {
                    _legacyText.gameObject.SetActive(false);
                }
            }

            // Texte d'instruction
            if (_instructionText != null)
            {
                _instructionText.text = eventData.Type == EventType.Micro
                    ? "Un evenement survient..."
                    : "Choisissez votre destin";
            }

            // Configurer les choix (caches au debut, reveles apres animation)
            SetupChoices(eventData);

            // Cacher les choix initialement
            SetChoicesVisible(false);

            // Lancer l'animation d'entree
            if (_animator != null && !string.IsNullOrEmpty(_showTrigger))
            {
                _animator.SetTrigger(_showTrigger);
            }

            // Programmer la revelation des choix
            Invoke(nameof(RevealChoices), 1.5f);

            // Timeout de securite
            CancelInvoke(nameof(AutoDismiss));
            Invoke(nameof(AutoDismiss), _autoHideDelay);

            Debug.Log($"[NarrativeScreen] Affichage : {eventData.Title} (joueur {playerIndex})");
        }

        /// <summary>
        /// Cache l'ecran narratif et reprend le jeu.
        /// </summary>
        public void Hide()
        {
            // Lancer l'animation de sortie
            if (_animator != null && !string.IsNullOrEmpty(_hideTrigger))
            {
                _animator.SetTrigger(_hideTrigger);
                // Desactiver apres la duree de l'animation
                Invoke(nameof(Deactivate), 0.5f);
            }
            else
            {
                Deactivate();
            }

            _currentEvent = null;
            _choicesRevealed = false;
            CancelInvoke(nameof(RevealChoices));
            CancelInvoke(nameof(AutoDismiss));
        }

        // ----------------------------------------------------------------
        // Choix
        // ----------------------------------------------------------------

        /// <summary>
        /// Appele quand un joueur selectionne un choix.
        /// </summary>
        public void OnChoiceSelected(int index)
        {
            if (_currentEvent == null) return;
            if (index < 0 || index >= _currentEvent.Choices.Length) return;

            // Desactiver tous les boutons pour eviter les doubles clics
            SetButtonsInteractable(false);

            // Appliquer le choix via l'EventManager
            var eventManager = GameManager.Instance?.GetComponent<EventManager>();
            if (eventManager != null)
            {
                eventManager.OnEventChoice(_currentEvent, index, _currentPlayerIndex);
            }

            // Afficher le narrative follow-up
            string followUp = _currentEvent.Choices[index].NarrativeFollowUp;
            if (!string.IsNullOrEmpty(followUp) && _descriptionText != null)
            {
                _descriptionText.text = followUp;
                _titleText.text = _currentEvent.Title + " — Votre choix";
            }

            // Cacher les boutons
            SetChoicesVisible(false);

            // Fermer apres un delai
            Invoke(nameof(Hide), 3f);
        }

        // ----------------------------------------------------------------
        // Prive
        // ----------------------------------------------------------------

        private void SetupChoices(EventData eventData)
        {
            int choiceCount = eventData.Choices?.Length ?? 0;

            for (int i = 0; i < (_choiceButtons?.Length ?? 0); i++)
            {
                bool hasChoice = i < choiceCount;
                if (_choiceButtons != null && i < _choiceButtons.Length)
                    _choiceButtons[i].gameObject.SetActive(hasChoice);

                if (_choiceTexts != null && i < _choiceTexts.Length)
                {
                    if (hasChoice && _choiceTexts[i] != null)
                        _choiceTexts[i].text = eventData.Choices[i].ChoiceText;
                }

                if (_choiceEffectTexts != null && i < _choiceEffectTexts.Length)
                {
                    if (hasChoice && _choiceEffectTexts[i] != null)
                    {
                        string preview = ChoiceResolver.GetEffectPreview(eventData.Choices[i], _currentPlayerIndex);
                        _choiceEffectTexts[i].text = preview;
                    }
                }

                if (_choiceLegacyTexts != null && i < _choiceLegacyTexts.Length)
                {
                    if (hasChoice && _choiceLegacyTexts[i] != null)
                    {
                        string legacy = eventData.Choices[i].LegacyUnlock;
                        if (!string.IsNullOrEmpty(legacy))
                            _choiceLegacyTexts[i].text = $"+ Legacy: {legacy}";
                        else
                            _choiceLegacyTexts[i].text = "";
                    }
                }

                // Connecter le listener du bouton
                if (hasChoice && _choiceButtons != null && i < _choiceButtons.Length)
                {
                    int capturedIndex = i; // Capturer pour la closure
                    _choiceButtons[i].onClick.RemoveAllListeners();
                    _choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(capturedIndex));
                }
            }
        }

        private void RevealChoices()
        {
            if (_currentEvent == null) return;
            SetChoicesVisible(true);
            SetButtonsInteractable(true);
            _choicesRevealed = true;
        }

        private void SetChoicesVisible(bool visible)
        {
            if (_choiceButtons == null) return;

            // Ne montrer que les boutons qui ont ete configures comme valides
            // (ceux desactives par SetupChoices restent invisibles)
            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                if (_choiceButtons[i] != null && _currentEvent != null)
                {
                    bool hasChoice = i < (_currentEvent.Choices?.Length ?? 0);
                    _choiceButtons[i].gameObject.SetActive(visible && hasChoice);
                }
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_choiceButtons == null) return;
            foreach (var btn in _choiceButtons)
            {
                if (btn != null)
                    btn.interactable = interactable;
            }
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
            GameManager.Instance.CurrentState = GameState.Playing;
        }

        private void AutoDismiss()
        {
            Debug.Log("[NarrativeScreen] Auto-dismiss (timeout).");
            Hide();
        }
    }
}
