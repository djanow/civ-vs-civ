using UnityEngine;
using UnityEngine.UI;

namespace CivVSCiv
{
    /// <summary>
    /// Popup d'interaction diplomatique.
    /// Affiche une proposition entrante et permet au joueur d'accepter,
    /// refuser, ou faire une contre-offre.
    /// Se ferme automatiquement apres 15 secondes (defaut accepte).
    /// </summary>
    public class DiplomacyPopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _popupRoot;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Text _offerText;
        [SerializeField] private Text _timerText;
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Button _declineButton;
        [SerializeField] private Button _counterOfferButton;

        [Header("Configuration")]
        [SerializeField] private float _timeoutSeconds = 15f;
        [SerializeField] private string _aiNamePrefix = "Civilisation ";

        private DiplomacyManager _diplomacyManager;
        private DiplomaticAction _currentAction;
        private int _currentActionIndex;
        private float _timer;
        private bool _isActive;
        private bool _hasResponded;

        private void Awake()
        {
            _diplomacyManager = FindAnyObjectByType<DiplomacyManager>();

            if (_popupRoot != null)
                _popupRoot.SetActive(false);

            _isActive = false;
            _hasResponded = false;

            // Hook up les boutons
            if (_acceptButton != null)
                _acceptButton.onClick.AddListener(OnAccept);

            if (_declineButton != null)
                _declineButton.onClick.AddListener(OnDecline);

            if (_counterOfferButton != null)
                _counterOfferButton.onClick.AddListener(OnCounterOfferClick);

            // S'abonner aux actions diplomatiques entrantes
            EventBus.Subscribe<GameEvents.DiplomaticInteraction>(OnDiplomaticInteraction);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.DiplomaticInteraction>(OnDiplomaticInteraction);
        }

        private void Update()
        {
            if (!_isActive || _hasResponded) return;

            _timer -= Time.deltaTime;

            if (_timerText != null)
            {
                _timerText.text = $"Temps restant: {Mathf.Max(0, Mathf.CeilToInt(_timer))}s";
            }

            if (_timer <= 0f)
            {
                // Timeout : accepter par defaut
                OnTimeout();
            }
        }

        private void OnDiplomaticInteraction(GameEvents.DiplomaticInteraction evt)
        {
            // N'afficher le popup que si l'action vise le joueur humain (player 0)
            if (evt.Action.TargetIndex == 0 && !evt.Action.Accepted)
            {
                Show(evt.Action);
            }
        }

        /// <summary>
        /// Affiche le popup avec une action diplomatique.
        /// </summary>
        public void Show(DiplomaticAction action)
        {
            _currentAction = action;
            _currentActionIndex = FindActionIndex(action);
            _isActive = true;
            _hasResponded = false;
            _timer = _timeoutSeconds;

            if (_popupRoot != null)
                _popupRoot.SetActive(true);

            // Mettre a jour les textes
            string actionTypeName = action.Type switch
            {
                DiploActionType.TradeRoute => "Route Commerciale",
                DiploActionType.NonAggressionPact => "Pacte de Non-Agression",
                DiploActionType.Alliance => "Proposition d'Alliance",
                DiploActionType.WarDeclaration => "Declaration de Guerre",
                DiploActionType.PeaceTreaty => "Proposition de Paix",
                DiploActionType.Gift => "Cadeau",
                _ => "Action Diplomatique"
            };

            if (_titleText != null)
                _titleText.text = actionTypeName;

            if (_messageText != null)
            {
                string proposerName = $"{_aiNamePrefix}{action.ProposerIndex}";
                string message = action.Type switch
                {
                    DiploActionType.TradeRoute =>
                        $"{proposerName} propose d'etablir une route commerciale.",
                    DiploActionType.NonAggressionPact =>
                        $"{proposerName} propose un pacte de non-agression pour {action.Offer.Turns} tours.",
                    DiploActionType.Alliance =>
                        $"{proposerName} propose une alliance militaire.",
                    DiploActionType.WarDeclaration =>
                        $"{proposerName} vous declare la guerre!",
                    DiploActionType.PeaceTreaty =>
                        $"{proposerName} propose un traite de paix.",
                    DiploActionType.Gift =>
                        $"{proposerName} vous offre un cadeau.",
                    _ => $"{proposerName} souhaite interagir avec vous."
                };

                _messageText.text = message;
            }

            if (_offerText != null)
            {
                if (action.Offer.IsValid)
                {
                    _offerText.text = $"Offre: {action.Offer}";
                    _offerText.gameObject.SetActive(true);
                }
                else
                {
                    _offerText.gameObject.SetActive(false);
                }
            }

            // Configurer les boutons selon le type
            if (_acceptButton != null)
            {
                _acceptButton.gameObject.SetActive(
                    action.Type != DiploActionType.WarDeclaration);
                _acceptButton.interactable = true;
            }

            if (_declineButton != null)
            {
                _declineButton.gameObject.SetActive(
                    action.Type != DiploActionType.WarDeclaration);
                _declineButton.interactable = true;
            }

            if (_counterOfferButton != null)
            {
                _counterOfferButton.gameObject.SetActive(
                    action.Type == DiploActionType.TradeRoute);
                _counterOfferButton.interactable = true;
            }
        }

        /// <summary>
        /// Appele quand le joueur accepte l'action diplomatique.
        /// </summary>
        public void OnAccept()
        {
            if (_hasResponded || !_isActive) return;
            _hasResponded = true;

            if (_diplomacyManager != null)
            {
                _diplomacyManager.RespondToAction(_currentActionIndex, true);
            }

            ClosePopup();
        }

        /// <summary>
        /// Appele quand le joueur decline l'action diplomatique.
        /// </summary>
        public void OnDecline()
        {
            if (_hasResponded || !_isActive) return;
            _hasResponded = true;

            if (_diplomacyManager != null)
            {
                _diplomacyManager.RespondToAction(_currentActionIndex, false);
            }

            ClosePopup();
        }

        /// <summary>
        /// Appele quand le joueur veut faire une contre-offre.
        /// </summary>
        public void OnCounterOffer(TradeOffer offer)
        {
            if (_hasResponded || !_isActive) return;
            _hasResponded = true;

            if (_diplomacyManager != null)
            {
                _diplomacyManager.CounterOffer(_currentActionIndex, offer);
            }

            ClosePopup();
        }

        /// <summary>
        /// Appele par le bouton de contre-offre (ouvre un champ de saisie simplifie).
        /// </summary>
        private void OnCounterOfferClick()
        {
            // Contre-offre simplifiee : proposer la moitie de l'or
            if (_currentAction.Offer.IsValid)
            {
                var counterOffer = new TradeOffer
                {
                    GoldAmount = Mathf.Max(1, _currentAction.Offer.GoldAmount / 2),
                    ScienceAmount = _currentAction.Offer.ScienceAmount,
                    Turns = _currentAction.Offer.Turns
                };

                OnCounterOffer(counterOffer);
            }
            else
            {
                // Pas d'offre existante, contre-offre par defaut
                var defaultOffer = new TradeOffer
                {
                    GoldAmount = 5,
                    ScienceAmount = 0,
                    Turns = 5
                };

                OnCounterOffer(defaultOffer);
            }
        }

        /// <summary>
        /// Gere le timeout : accepte par defaut.
        /// </summary>
        private void OnTimeout()
        {
            if (_hasResponded || !_isActive) return;

            Debug.Log("[DiplomacyPopup] Timeout: action acceptee par defaut.");

            // Pour les declarations de guerre, pas d'acceptation par defaut
            if (_currentAction.Type == DiploActionType.WarDeclaration)
            {
                _hasResponded = true;
                ClosePopup();
                return;
            }

            OnAccept();
        }

        /// <summary>
        /// Ferme le popup.
        /// </summary>
        private void ClosePopup()
        {
            _isActive = false;
            _currentAction = default;

            if (_popupRoot != null)
                _popupRoot.SetActive(false);
        }

        /// <summary>
        /// Trouve l'index d'une action dans la liste des actions en attente.
        /// </summary>
        private int FindActionIndex(DiplomaticAction action)
        {
            if (_diplomacyManager == null) return -1;

            var pending = _diplomacyManager.PendingActions;
            for (int i = 0; i < pending.Count; i++)
            {
                if (pending[i].Type == action.Type &&
                    pending[i].ProposerIndex == action.ProposerIndex &&
                    pending[i].TargetIndex == action.TargetIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Appele depuis un bouton de fermeture manuelle.
        /// </summary>
        public void OnCloseButton()
        {
            if (_hasResponded)
            {
                ClosePopup();
                return;
            }

            // Si pas encore repondu, decliner par defaut
            OnDecline();
        }
    }
}
