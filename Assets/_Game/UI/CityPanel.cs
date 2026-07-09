using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CivVSCiv
{
    /// <summary>
    /// Panneau de gestion d'une cité.
    /// Affiche les détails de la cité (nom, population, yields, production)
    /// et permet de lancer la production d'unités et de bâtiments.
    ///
    /// Crée automatiquement son UI si les références serialized sont nulles
    /// (mode runtime sans prefab).
    /// </summary>
    public class CityPanel : MonoBehaviour
    {
        [Header("Affichage principal")]
        [SerializeField] private Text _cityNameText;
        [SerializeField] private Text _populationText;
        [SerializeField] private Text _productionText;
        [SerializeField] private Text _foodYieldText;
        [SerializeField] private Text _goldYieldText;

        [Header("File de production")]
        [SerializeField] private Transform _productionListParent;
        [SerializeField] private GameObject _productionItemPrefab;

        [Header("Boutons")]
        [SerializeField] private Button _closeButton;

        [Header("Panneau de production")]
        [SerializeField] private GameObject _productionPanel; // Sous-panneau avec la liste des choix

        private City _currentCity;
        private HexCell[,] _currentCells;
        private bool _uiCreated;

        // Couleurs pour le fond
        private static readonly Color PanelBgColor = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        // ----------------------------------------------------------------
        // API publique
        // ----------------------------------------------------------------

        /// <summary>
        /// Affiche le panneau avec les détails de la cité donnée.
        /// </summary>
        public void Show(City city)
        {
            if (city == null) return;

            EnsureUI();

            _currentCity = city;
            _currentCells = GameManager.Instance?.Cells;

            gameObject.SetActive(true);
            RefreshDisplay();
            UpdateProductionOptions(city);
        }

        /// <summary>
        /// Masque le panneau.
        /// </summary>
        public void Hide()
        {
            _currentCity = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Met à jour la liste des options de production disponibles.
        /// Utilise ProductionManager pour les données.
        /// </summary>
        public void UpdateProductionOptions(City city)
        {
            if (city == null) return;

            EnsureUI();
            _currentCity = city;

            // Nettoyer les entrées existantes
            ClearProductionList();

            int currentPlayer = GameManager.Instance.TurnManager?.CurrentPlayerIndex ?? -1;

            // Unités disponibles
            var units = ProductionManager.GetAvailableUnits(city, currentPlayer);
            foreach (var unit in units)
            {
                AddProductionItem(unit.UnitName, unit.ProductionCost, "Unité");
            }

            // Bâtiments disponibles
            var buildings = ProductionManager.GetAvailableBuildings(city, currentPlayer);
            foreach (var building in buildings)
            {
                AddProductionItem(building.BuildingName, building.ProductionCost, "Bâtiment");
            }

            // Options par défaut si aucune donnée n'est chargée
            if (units.Count == 0 && buildings.Count == 0)
            {
                AddProductionItem("Guerrier", 40, "Unité");
                AddProductionItem("Éclaireur", 30, "Unité");
            }
        }

        /// <summary>
        /// Appelé quand le joueur sélectionne un item de production.
        /// </summary>
        public void OnProductionSelected(string itemName)
        {
            if (_currentCity == null) return;

            ProductionManager.StartProduction(_currentCity, itemName);
            RefreshDisplay();
            UpdateProductionOptions(_currentCity);
        }

        // ----------------------------------------------------------------
        // Initialisation UI
        // ----------------------------------------------------------------

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Hide);

            // Caché par défaut
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Crée les éléments UI manquants si la CityPanel a été créée
        /// à l'exécution (sans prefab).
        /// </summary>
        private void EnsureUI()
        {
            if (_uiCreated) return;

            // Vérifier si les références sont nulles (mode runtime)
            bool needsFullBuild = _cityNameText == null;

            if (needsFullBuild)
            {
                BuildUI();
            }

            _uiCreated = true;
        }

        /// <summary>
        /// Construit l'intégralité du panneau UI en code.
        /// </summary>
        private void BuildUI()
        {
            // Nettoyer tout enfant existant
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            RectTransform panelRT = GetComponent<RectTransform>();
            if (panelRT == null)
                panelRT = gameObject.AddComponent<RectTransform>();

            // Configurer le panel lui-même comme une large fenêtre centrée
            panelRT.anchorMin = new Vector2(0.15f, 0.1f);
            panelRT.anchorMax = new Vector2(0.85f, 0.9f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            // Fond du panneau
            var bg = gameObject.AddComponent<Image>();
            bg.color = PanelBgColor;

            // ---- Titre / Nom de la cité ----
            _cityNameText = CreateLabel("CityName", transform, "Cité", 30, TextAnchor.MiddleCenter,
                new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.95f));

            // ---- Population ----
            _populationText = CreateLabel("Population", transform, "Population: 1", 20, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.70f), new Vector2(0.48f, 0.80f));

            // ---- Nourriture ----
            _foodYieldText = CreateLabel("FoodYield", transform, "Nourriture: 2/tour", 18, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.61f), new Vector2(0.48f, 0.70f));

            // ---- Or ----
            _goldYieldText = CreateLabel("GoldYield", transform, "Or: 1/tour", 18, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.52f), new Vector2(0.48f, 0.61f));

            // ---- Production en cours ----
            _productionText = CreateLabel("ProductionStatus", transform, "Rien en construction", 18, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.43f), new Vector2(0.97f, 0.52f));

            // ---- Bouton fermer ----
            var closeBtnGo = new GameObject("CloseButton", typeof(Image), typeof(Button));
            closeBtnGo.transform.SetParent(transform, false);
            var closeBtnRT = closeBtnGo.GetComponent<RectTransform>();
            closeBtnRT.anchorMin = new Vector2(0.90f, 0.87f);
            closeBtnRT.anchorMax = new Vector2(0.98f, 0.93f);
            closeBtnRT.offsetMin = closeBtnRT.offsetMax = Vector2.zero;
            closeBtnGo.GetComponent<Image>().color = new Color(0.7f, 0.15f, 0.15f);

            var closeLabel = CreateLabel("X", closeBtnGo.transform, "Fermer", 16, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one);
            closeLabel.raycastTarget = false;

            _closeButton = closeBtnGo.GetComponent<Button>();
            _closeButton.onClick.AddListener(Hide);

            // ---- Liste de production (Vertical Layout) ----
            var listGo = new GameObject("ProductionList", typeof(RectTransform));
            listGo.transform.SetParent(transform, false);
            var listRT = listGo.GetComponent<RectTransform>();
            listRT.anchorMin = new Vector2(0.02f, 0.05f);
            listRT.anchorMax = new Vector2(0.97f, 0.38f);
            listRT.offsetMin = listRT.offsetMax = Vector2.zero;

            var vlg = listGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = listGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.MinSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Ajouter un mask pour le scroll
            var scrollMask = listGo.AddComponent<Mask>();
            var scrollRect = listGo.AddComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.viewport = listRT;
            scrollRect.content = listRT;

            // Scrollbar
            var sbGo = new GameObject("Scrollbar", typeof(Image), typeof(Scrollbar));
            sbGo.transform.SetParent(transform, false);
            var sbRT = sbGo.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(0.97f, 0.05f);
            sbRT.anchorMax = new Vector2(0.99f, 0.38f);
            sbRT.offsetMin = sbRT.offsetMax = Vector2.zero;
            sbGo.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);

            var scrollbar = sbGo.GetComponent<Scrollbar>();
            scrollRect.verticalScrollbar = scrollbar;

            // Handle de la scrollbar
            var handleGo = new GameObject("Handle", typeof(Image));
            handleGo.transform.SetParent(sbGo.transform, false);
            var handleRT = handleGo.GetComponent<RectTransform>();
            handleRT.anchorMin = handleRT.anchorMax = new Vector2(0, 1);
            handleRT.sizeDelta = new Vector2(20, 50);
            handleGo.GetComponent<Image>().color = new Color(0.6f, 0.6f, 0.6f);
            scrollbar.handleRect = handleRT;
            scrollbar.targetGraphic = handleGo.GetComponent<Image>();

            _productionListParent = listRT;

            // Créer le prefab d'item de production
            _productionItemPrefab = CreateProductionItemPrefab();

            // Label pour la section production
            var sectionLabel = CreateLabel("SectionLabel", transform, "--- Production ---", 18, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.38f), new Vector2(0.90f, 0.43f));
            sectionLabel.color = new Color(1f, 0.85f, 0.4f);
        }

        /// <summary>
        /// Crée un Text UI avec les ancres spécifiées.
        /// </summary>
        private static Text CreateLabel(string name, Transform parent, string content,
            int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var txt = go.GetComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = alignment;
            txt.raycastTarget = false;
            return txt;
        }

        /// <summary>
        /// Crée le prefab d'item de production (utilisé pour chaque entrée).
        /// </summary>
        private GameObject CreateProductionItemPrefab()
        {
            var prefab = new GameObject("ProdItemPrefab", typeof(RectTransform));
            prefab.SetActive(false);
            prefab.transform.SetParent(transform, false);

            var prefabRT = prefab.GetComponent<RectTransform>();
            prefabRT.sizeDelta = new Vector2(0, 30f);
            prefabRT.anchorMin = new Vector2(0, 1);
            prefabRT.anchorMax = new Vector2(1, 1);
            prefabRT.pivot = new Vector2(0.5f, 1);

            var bg = prefab.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

            var btn = prefab.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.45f);
            colors.normalColor = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            btn.colors = colors;

            var textGo = new GameObject("ItemLabel", typeof(Text));
            textGo.transform.SetParent(prefab.transform, false);
            var textRT = textGo.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(8, 0);
            textRT.offsetMax = new Vector2(-8, 0);

            var txt = textGo.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 15;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            // Marquer le prefab
            prefab.name = "ProductionItemPrefab";
            prefab.SetActive(false);

            return prefab;
        }

        /// <summary>
        /// Ajoute un item à la liste de production.
        /// </summary>
        private void AddProductionItem(string itemName, int cost, string category)
        {
            if (_productionItemPrefab == null || _productionListParent == null) return;

            var itemGo = Instantiate(_productionItemPrefab, _productionListParent);
            itemGo.name = $"ProdItem_{itemName}";

            var txt = itemGo.GetComponentInChildren<Text>();
            if (txt != null)
            {
                string categoryIcon = category == "Unité" ? "⚔" : "⌂"; // ⚔ ou ⌂
                txt.text = $"{categoryIcon} {itemName}  ({cost} tours)";
            }

            var btn = itemGo.GetComponent<Button>();
            if (btn != null)
            {
                string capturedName = itemName;
                btn.onClick.AddListener(() => OnProductionSelected(capturedName));
            }

            itemGo.SetActive(true);
        }

        /// <summary>
        /// Vide la liste des options de production.
        /// </summary>
        private void ClearProductionList()
        {
            if (_productionListParent == null) return;

            foreach (Transform child in _productionListParent)
            {
                if (child != null && child.name != "ProdItemPrefab")
                    Destroy(child.gameObject);
            }
        }

        // ----------------------------------------------------------------
        // Affichage
        // ----------------------------------------------------------------

        private void RefreshDisplay()
        {
            if (_currentCity == null) return;

            if (_cityNameText != null)
                _cityNameText.text = _currentCity.CityName;

            if (_populationText != null)
                _populationText.text = $"Population : {_currentCity.Population}";

            // Statut de production
            string prodStatus = "Rien en construction";
            if (!string.IsNullOrEmpty(_currentCity.CurrentProduction))
            {
                float progress = _currentCity.CurrentProductionCost > 0
                    ? (float)_currentCity.ProductionStored / _currentCity.CurrentProductionCost * 100f
                    : 0f;
                prodStatus = $"{_currentCity.CurrentProduction}  ({progress:F0}%)";
            }
            if (_productionText != null)
                _productionText.text = prodStatus;

            // Yields (si on a accès à la grille)
            if (_currentCells != null)
            {
                if (_foodYieldText != null)
                    _foodYieldText.text = $"Nourriture : {_currentCity.CalculateFoodYield(_currentCells)}/tour";

                if (_goldYieldText != null)
                    _goldYieldText.text = $"Or : {_currentCity.CalculateGoldYield(_currentCells)}/tour";
            }
        }
    }
}
