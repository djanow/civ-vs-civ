using UnityEngine;
using UnityEngine.UI;

namespace CivVSCiv
{
    /// <summary>
    /// Panneau UI detaille d'une cite.
    /// S'affiche quand le joueur selectionne une cite.
    /// Montre : nom, population, yields, file de production.
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
        [SerializeField] private GameObject _productionItemPrefab; // prefab avec un Text + Button

        [Header("Boutons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _openProductionPanelButton;

        private City _currentCity;
        private HexCell[,] _currentCells;

        // ============================================================
        // API publique
        // ============================================================

        /// <summary>
        /// Affiche le panneau avec les details de la cite donnee.
        /// </summary>
        public void Show(City city)
        {
            if (city == null) return;

            _currentCity = city;
            _currentCells = GameManager.Instance?.Cells;

            gameObject.SetActive(true);
            RefreshDisplay();
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
        /// Met a jour la liste des options de production disponibles.
        /// </summary>
        public void UpdateProductionOptions(City city)
        {
            if (city == null) return;
            _currentCity = city;

            // Nettoyer la liste existante
            foreach (Transform child in _productionListParent)
            {
                Destroy(child.gameObject);
            }

            // Pour le MVP, on affiche des options de production generiques
            string[] basicOptions = { "Guerrier", "Eclaireur", "Temple", "Atelier" };
            int[] basicCosts = { 30, 20, 40, 35 };

            for (int i = 0; i < basicOptions.Length; i++)
            {
                var optionGO = Instantiate(_productionItemPrefab, _productionListParent);
                var button = optionGO.GetComponentInChildren<Button>();
                var text = optionGO.GetComponentInChildren<Text>();

                if (text != null)
                {
                    text.text = $"{basicOptions[i]} ({basicCosts[i]} tours)";
                }

                if (button != null)
                {
                    string itemName = basicOptions[i];
                    int cost = basicCosts[i];
                    button.onClick.AddListener(() => OnProductionSelected(itemName, cost));
                }
            }
        }

        /// <summary>
        /// Appele quand le joueur selectionne un item de production.
        /// </summary>
        public void OnProductionSelected(string itemName)
        {
            if (_currentCity == null) return;

            // Utiliser un cout par defaut si on connait pas l'item
            int cost = 30;
            _currentCity.StartProduction(itemName, cost);
            RefreshDisplay();
            UpdateProductionOptions(_currentCity);
        }

        // ============================================================
        // Prive
        // ============================================================

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Hide);

            if (_openProductionPanelButton != null)
                _openProductionPanelButton.onClick.AddListener(() =>
                {
                    if (_currentCity != null)
                        UpdateProductionOptions(_currentCity);
                });

            // Cache par defaut
            gameObject.SetActive(false);
        }

        private void RefreshDisplay()
        {
            if (_currentCity == null) return;

            if (_cityNameText != null)
                _cityNameText.text = _currentCity.CityName;

            if (_populationText != null)
                _populationText.text = $"Population : {_currentCity.Population}";

            string prodStatus = "Rien en construction";
            if (!string.IsNullOrEmpty(_currentCity.CurrentProduction))
            {
                float progress = _currentCity.CurrentProductionCost > 0
                    ? (float)_currentCity.ProductionStored / _currentCity.CurrentProductionCost * 100f
                    : 0f;
                prodStatus = $"{_currentCity.CurrentProduction} ({progress:F0}%)";
            }
            if (_productionText != null)
                _productionText.text = prodStatus;

            // Yields (uniquement si on a acces a la grille)
            if (_currentCells != null)
            {
                if (_foodYieldText != null)
                    _foodYieldText.text = $"Nourriture : {_currentCity.CalculateFoodYield(_currentCells)}/tour";

                if (_goldYieldText != null)
                    _goldYieldText.text = $"Or : {_currentCity.CalculateGoldYield(_currentCells)}/tour";
            }
        }

        // Surcharge avec cout explicite pour l'appel depuis le bouton
        private void OnProductionSelected(string itemName, int cost)
        {
            if (_currentCity == null) return;
            _currentCity.StartProduction(itemName, cost);
            RefreshDisplay();
            UpdateProductionOptions(_currentCity);
        }
    }
}
