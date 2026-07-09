using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CivVSCiv
{
    /// <summary>
    /// Interface utilisateur pour l'arbre technologique.
    /// Affiche les techs disponibles groupees par ere,
    /// met en evidence les techs completees et les prerequis.
    /// </summary>
    public class TechTreeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Text _titleText;
        [SerializeField] private Transform _techContainer;
        [SerializeField] private GameObject _techSlotPrefab;

        [Header("Colors")]
        [SerializeField] private Color _colorAvailable = Color.white;
        [SerializeField] private Color _colorCompleted = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _colorInProgress = new Color(0.8f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _colorLocked = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color _colorEraGate = new Color(1f, 0.6f, 0f, 1f);

        [Header("Era Labels")]
        [SerializeField] private string[] _eraLabels = { "Antiquite", "Classique", "Medievale" };

        /// <summary>Callback appele quand le joueur clique sur une tech disponible.</summary>
        public System.Action<int> OnTechClicked;

        private ResearchManager _researchManager;
        private int _currentPlayerIndex;
        private bool _isVisible;
        private List<GameObject> _slotObjects;

        private void Awake()
        {
            _slotObjects = new List<GameObject>();
            _researchManager = FindAnyObjectByType<ResearchManager>();

            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            _isVisible = false;

            EventBus.Subscribe<GameEvents.TechCompleted>(OnTechCompleted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.TechCompleted>(OnTechCompleted);
        }

        private void OnTechCompleted(GameEvents.TechCompleted evt)
        {
            if (_isVisible && evt.PlayerIndex == _currentPlayerIndex)
            {
                RefreshTechDisplay();
            }
        }

        /// <summary>
        /// Affiche l'arbre technologique pour un joueur.
        /// </summary>
        public void Show(int playerIndex)
        {
            _currentPlayerIndex = playerIndex;
            _isVisible = true;

            EnsureUIRefs();

            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            if (_titleText != null)
                _titleText.text = "Choisissez une technologie a rechercher";

            RefreshTechDisplay();

            // Si aucune tech disponible, fermer et avancer immediatement
            if (_researchManager != null && _researchManager.GetAvailableTechs(playerIndex).Count == 0)
            {
                Debug.Log("[TechTreeUI] Aucune tech disponible, fermeture.");
                Hide();
                if (OnTechClicked != null)
                    OnTechClicked(-1);
            }
        }

        /// <summary>
        /// Cree les elements UI si les references sont nulles (zero-setup).
        /// </summary>
        private void EnsureUIRefs()
        {
            if (_panelRoot != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("TechCanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                go.AddComponent<GraphicRaycaster>();
            }

            // Panel root
            _panelRoot = new GameObject("TechTreePanel", typeof(Image));
            _panelRoot.transform.SetParent(canvas.transform, false);
            var rt = _panelRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.15f);
            rt.anchorMax = new Vector2(0.8f, 0.85f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var bg = _panelRoot.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // Title
            var titleGO = new GameObject("Title", typeof(Text));
            titleGO.transform.SetParent(_panelRoot.transform, false);
            _titleText = titleGO.GetComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleText.fontSize = 22;
            _titleText.color = Color.white;
            _titleText.alignment = TextAnchor.MiddleCenter;
            var titleRT = _titleText.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 0.88f);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

            // Tech container (vertical layout for scrollable list)
            var contGO = new GameObject("TechContainer", typeof(Image));
            contGO.transform.SetParent(_panelRoot.transform, false);
            _techContainer = contGO.transform;
            var contRT = contGO.GetComponent<RectTransform>();
            contRT.anchorMin = new Vector2(0.05f, 0.08f);
            contRT.anchorMax = new Vector2(0.95f, 0.83f);
            contRT.offsetMin = contRT.offsetMax = Vector2.zero;
            var contImage = contGO.GetComponent<Image>();
            contImage.color = new Color(0.12f, 0.12f, 0.18f, 0.8f);

            // Close button at bottom
            var closeBtn = new GameObject("CloseBtn", typeof(Image), typeof(Button));
            closeBtn.transform.SetParent(_panelRoot.transform, false);
            var closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(0.35f, 0.01f);
            closeRT.anchorMax = new Vector2(0.65f, 0.06f);
            closeRT.offsetMin = closeRT.offsetMax = Vector2.zero;
            var closeImage = closeBtn.GetComponent<Image>();
            closeImage.color = new Color(0.3f, 0.3f, 0.3f);
            var closeLabel = new GameObject("CloseLabel", typeof(Text));
            closeLabel.transform.SetParent(closeBtn.transform, false);
            var clText = closeLabel.GetComponent<Text>();
            clText.text = "Fermer";
            clText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            clText.fontSize = 14;
            clText.color = Color.white;
            clText.alignment = TextAnchor.MiddleCenter;
            var clRT = clText.GetComponent<RectTransform>();
            clRT.anchorMin = Vector2.zero; clRT.anchorMax = Vector2.one;
            clRT.offsetMin = clRT.offsetMax = Vector2.zero;
            closeBtn.GetComponent<Button>().onClick.AddListener(() => {
                Hide();
                if (OnTechClicked != null)
                    OnTechClicked(-1);
            });
        }

        /// <summary>
        /// Cache l'arbre technologique.
        /// </summary>
        public void Hide()
        {
            _isVisible = false;

            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        /// <summary>
        /// Retourne si l'UI est actuellement visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Appele quand le joueur selectionne une tech dans l'UI.
        /// </summary>
        public void OnTechSelected(int techId)
        {
            if (_researchManager == null)
            {
                _researchManager = FindAnyObjectByType<ResearchManager>();
                if (_researchManager == null) return;
            }

            if (_researchManager.CanResearch(_currentPlayerIndex, techId))
            {
                _researchManager.SetResearch(_currentPlayerIndex, techId);
                Hide();
                if (OnTechClicked != null)
                    OnTechClicked(techId);
            }
            else
            {
                Debug.Log($"[TechTreeUI] Tech {techId} non disponible pour J{_currentPlayerIndex}.");
            }
        }

        /// <summary>
        /// Rafraichit l'affichage de toutes les techs.
        /// </summary>
        private void RefreshTechDisplay()
        {
            ClearTechSlots();

            if (_researchManager == null || _researchManager.TechTree == null)
            {
                Debug.LogWarning("[TechTreeUI] ResearchManager ou TechTree non disponible.");
                return;
            }

            var allTechs = _researchManager.TechTree.TechNodes;
            if (allTechs == null || allTechs.Length == 0)
            {
                Debug.LogWarning("[TechTreeUI] Aucune tech definie dans TechTree.");
                return;
            }

            // Grouper par ere (ordre decroissant : Medievale -> Classique -> Antiquite)
            for (int era = _eraLabels.Length - 1; era >= 0; era--)
            {
                // Ajouter un en-tete d'ere
                AddEraHeader(era);

                // Ajouter les techs de cette ere
                for (int i = 0; i < allTechs.Length; i++)
                {
                    if (allTechs[i].Era != era) continue;

                    bool completed = _researchManager.IsTechCompleted(_currentPlayerIndex, allTechs[i].TechId);
                    bool canResearch = _researchManager.CanResearch(_currentPlayerIndex, allTechs[i].TechId);
                    bool inProgress = _researchManager.GetCurrentResearchId(_currentPlayerIndex) == allTechs[i].TechId;

                    AddTechSlot(allTechs[i], completed, canResearch, inProgress);
                }
            }
        }

        /// <summary>
        /// Ajoute un en-tete d'ere dans l'affichage.
        /// </summary>
        private void AddEraHeader(int era)
        {
            if (_techContainer == null) return;

            // Creer un en-tete simple de type texte
            var headerObj = new GameObject($"EraHeader_{era}");
            headerObj.transform.SetParent(_techContainer, false);

            var text = headerObj.AddComponent<Text>();
            string label = era >= 0 && era < _eraLabels.Length ? _eraLabels[era] : $"Ere {era}";
            text.text = $"=== {label} ===";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;

            _slotObjects.Add(headerObj);
        }

        /// <summary>
        /// Ajoute un slot de tech individuel.
        /// </summary>
        private void AddTechSlot(TechNodeData tech, bool completed, bool canResearch, bool inProgress)
        {
            if (_techContainer == null) return;

            GameObject slotObj;
            if (_techSlotPrefab != null)
            {
                slotObj = Instantiate(_techSlotPrefab, _techContainer);
            }
            else
            {
                slotObj = new GameObject($"TechSlot_{tech.TechId}");
                slotObj.transform.SetParent(_techContainer, false);

                var text = slotObj.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 14;
                text.alignment = TextAnchor.MiddleLeft;
            }

            // Configurer l'affichage
            var slotText = slotObj.GetComponent<Text>();
            if (slotText != null)
            {
                string status = "";
                if (completed) status = " [COMPLETE]";
                else if (inProgress) status = " [EN COURS]";

                string prereqInfo = "";
                if (tech.PrerequisiteIds != null && tech.PrerequisiteIds.Length > 0)
                {
                    prereqInfo = $" (requiert: {string.Join(", ", tech.PrerequisiteIds)})";
                }

                slotText.text = $"{tech.TechName}{status} - {tech.ScienceCost} science{prereqInfo}";

                if (completed)
                    slotText.color = _colorCompleted;
                else if (inProgress)
                    slotText.color = _colorInProgress;
                else if (canResearch)
                    slotText.color = tech.IsEraGate ? _colorEraGate : _colorAvailable;
                else
                    slotText.color = _colorLocked;
            }

            // Ajouter un bouton pour cliquer
            var button = slotObj.GetComponent<Button>();
            if (button == null)
            {
                button = slotObj.AddComponent<Button>();
            }

            int capturedTechId = tech.TechId;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnTechSelected(capturedTechId));

            // Desactiver le bouton si la tech n'est pas disponible
            button.interactable = canResearch && !completed && !inProgress;

            _slotObjects.Add(slotObj);
        }

        /// <summary>
        /// Nettoie les slots de tech de l'affichage precedent.
        /// </summary>
        private void ClearTechSlots()
        {
            for (int i = 0; i < _slotObjects.Count; i++)
            {
                if (_slotObjects[i] != null)
                    Destroy(_slotObjects[i]);
            }
            _slotObjects.Clear();
        }

        /// <summary>
        /// Appele quand le joueur ferme l'arbre.
        /// </summary>
        public void OnCloseButton()
        {
            Hide();
        }
    }
}
