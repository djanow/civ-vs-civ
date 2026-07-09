using UnityEngine;
using UnityEngine.UI;

namespace CivVSCiv
{
    /// <summary>
    /// Popup rapide pour les micro-evenements narratifs.
    /// S'affiche en superposition (pas plein ecran).
    /// Se ferme automatiquement apres un choix ou un delai.
    /// </summary>
    public class NarrativePopup : MonoBehaviour
    {
        [Header("Textes")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _bodyText;

        [Header("Boutons de choix")]
        [SerializeField] private Button[] _choiceButtons;
        [SerializeField] private Text[] _choiceTexts;

        [Header("Animation")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _showTrigger = "Show";
        [SerializeField] private string _hideTrigger = "Hide";
        [SerializeField] private float _autoCloseDelay = 20f;

        [Header("Fermeture")]
        [SerializeField] private Button _closeButton; // Bouton X ou ignorap (optionnel)

        /// <summary>Evenement en cours d'affichage.</summary>
        private EventData _currentEvent;

        /// <summary>Joueur concerne.</summary>
        private int _currentPlayerIndex;

        /// <summary>Timer de fermeture automatique.</summary>
        private float _displayTimer;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            // Fermeture auto si pas de choix fait
            if (gameObject.activeInHierarchy && _currentEvent != null)
            {
                _displayTimer += Time.deltaTime;
                if (_displayTimer >= _autoCloseDelay)
                {
                    Debug.Log("[NarrativePopup] Auto-close.");
                    Hide();
                }
            }
        }

        // ----------------------------------------------------------------
        // Affichage
        // ----------------------------------------------------------------

        /// <summary>
        /// Affiche un micro-evenement dans le popup.
        /// </summary>
        public void Show(EventData microEvent, int playerIndex)
        {
            if (microEvent == null)
            {
                Debug.LogWarning("[NarrativePopup] microEvent null.");
                return;
            }

            _currentEvent = microEvent;
            _currentPlayerIndex = playerIndex;
            _displayTimer = 0f;

            // Activer
            gameObject.SetActive(true);

            // Remplir les textes
            if (_titleText != null)
                _titleText.text = microEvent.Title;

            if (_bodyText != null)
                _bodyText.text = microEvent.Description;

            // Configurer les boutons de choix
            SetupChoices(microEvent);

            // Animation d'entree
            if (_animator != null && !string.IsNullOrEmpty(_showTrigger))
            {
                _animator.SetTrigger(_showTrigger);
            }

            // Bouton de fermeture
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Hide);
            }

            Debug.Log($"[NarrativePopup] Micro-evenement : {microEvent.Title}");
        }

        /// <summary>
        /// Cache le popup.
        /// </summary>
        public void Hide()
        {
            if (_animator != null && !string.IsNullOrEmpty(_hideTrigger))
            {
                _animator.SetTrigger(_hideTrigger);
                Invoke(nameof(Deactivate), 0.3f);
            }
            else
            {
                Deactivate();
            }

            // Notifier le TurnManager que l'evenement narratif est termine
            if (_currentEvent != null)
            {
                var tm = FindAnyObjectByType<TurnManager>();
                if (tm != null)
                    tm.OnNarrativeEventDismissed();
            }

            _currentEvent = null;
            _displayTimer = 0f;
        }

        // ----------------------------------------------------------------
        // Choix
        // ----------------------------------------------------------------

        /// <summary>
        /// Appele quand le joueur selectionne un choix.
        /// </summary>
        public void OnChoiceSelected(int index)
        {
            if (_currentEvent == null) return;
            if (index < 0 || index >= (_currentEvent.Choices?.Length ?? 0)) return;

            // Desactiver les boutons
            SetButtonsInteractable(false);

            // Appliquer le choix via l'EventManager
            var eventManager = GameManager.Instance?.GetComponent<EventManager>();
            if (eventManager != null)
            {
                eventManager.OnEventChoice(_currentEvent, index, _currentPlayerIndex);
            }

            // Afficher le follow-up dans le body
            string followUp = _currentEvent.Choices[index].NarrativeFollowUp;
            if (!string.IsNullOrEmpty(followUp) && _bodyText != null)
            {
                _bodyText.text = followUp;
            }

            // Cacher les boutons de choix
            if (_choiceButtons != null)
            {
                foreach (var btn in _choiceButtons)
                {
                    if (btn != null)
                        btn.gameObject.SetActive(false);
                }
            }

            // Fermeture automatique rapide apres le choix
            _displayTimer = _autoCloseDelay - 5f; // Forcer la fermeture dans 5s
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
                {
                    _choiceButtons[i].gameObject.SetActive(hasChoice);
                    if (hasChoice)
                    {
                        int capturedIndex = i;
                        _choiceButtons[i].onClick.RemoveAllListeners();
                        _choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(capturedIndex));
                    }
                }

                if (_choiceTexts != null && i < _choiceTexts.Length && hasChoice)
                {
                    if (_choiceTexts[i] != null)
                        _choiceTexts[i].text = eventData.Choices[i].ChoiceText;
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
        }
    }
}
