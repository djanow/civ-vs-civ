using System.Collections.Generic;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Données d'une cité.
    /// </summary>
    [System.Serializable]
    public class CityData
    {
        public int CityId;
        public string CityName;
        public int OwnerIndex;
        public HexCoordinates Location;
        public int Population;
        public bool IsCapital;
    }

    /// <summary>
    /// Instance de cité en jeu. Contient les données et les méthodes
    /// de production et calcul des yields.
    /// </summary>
    [System.Serializable]
    public class City
    {
        public string CityName;
        public int Population;
        public string CurrentProduction;
        public int CurrentProductionCost;
        public int ProductionStored;
        public int FoodStored;

        /// <summary>
        /// Seuil de croissance démographique : 10 + Population * 5.
        /// </summary>
        public int FoodThreshold => 10 + Population * 5;

        /// <summary>
        /// Construit une instance à partir des données de base.
        /// </summary>
        public City(CityData data)
        {
            CityName = data.CityName;
            Population = data.Population;
            CurrentProduction = null;
            CurrentProductionCost = 0;
            ProductionStored = 0;
            FoodStored = 0;
        }

        public int CalculateFoodYield(HexCell[,] cells) => Population * 2;

        public int CalculateGoldYield(HexCell[,] cells) => Population;

        public void StartProduction(string itemName, int cost)
        {
            CurrentProduction = itemName;
            CurrentProductionCost = cost;
            ProductionStored = 0;
        }
    }

    /// <summary>
    /// Gère les cités de toutes les civilisations.
    /// Crée les GameObjects visibles sur la carte.
    /// </summary>
    public class CityManager : MonoBehaviour
    {
        [Header("Visuel des cités sur la carte")]
        [SerializeField] private float _cityMarkerHeight = 1.5f;
        [SerializeField] private float _cityMarkerRadius = 0.3f;

        private List<CityData> _allCities = new List<CityData>();
        private readonly List<City> _runtimeCities = new List<City>();
        private readonly Dictionary<HexCoordinates, GameObject> _cityGameObjects = new Dictionary<HexCoordinates, GameObject>();

        private HexGridRenderer _gridRenderer;

        // Couleurs par propriétaire
        private static readonly Color[] OwnerColors =
        {
            new Color(0.6f, 0.2f, 0.8f),  // 0: Violet (Phénicie)
            new Color(0.2f, 0.5f, 0.9f),  // 1: Bleu (Grèce)
            new Color(0.8f, 0.3f, 0.2f),  // 2: Rouge
            new Color(0.2f, 0.8f, 0.3f),  // 3: Vert
        };

        private void Awake()
        {
            _gridRenderer = FindAnyObjectByType<HexGridRenderer>();
        }

        /// <summary>Initialise le gestionnaire.</summary>
        public void Initialize()
        {
            _allCities.Clear();
            _runtimeCities.Clear();

            // Nettoyer les GameObjects de cités
            foreach (var kvp in _cityGameObjects)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            _cityGameObjects.Clear();
        }

        /// <summary>Toutes les cités (données persistantes).</summary>
        public List<CityData> GetAllCities() => _allCities;

        /// <summary>Toutes les cités (instances runtime avec production).</summary>
        public List<City> GetRuntimeCities() => _runtimeCities;

        /// <summary>
        /// Trouve l'instance runtime d'une cité à une position donnée.
        /// </summary>
        public City GetRuntimeCityAt(HexCoordinates location)
        {
            foreach (var city in _runtimeCities)
            {
                var cityData = FindCityData(city.CityName);
                if (cityData != null && cityData.Location == location)
                    return city;
            }
            return null;
        }

        /// <summary>
        /// Trouve l'instance runtime d'une cité par son nom.
        /// </summary>
        public City GetRuntimeCity(string cityName)
        {
            foreach (var city in _runtimeCities)
            {
                if (city.CityName == cityName)
                    return city;
            }
            return null;
        }

        /// <summary>Cites d'un joueur.</summary>
        public List<CityData> GetPlayerCities(int playerIndex)
        {
            return _allCities.FindAll(c => c.OwnerIndex == playerIndex);
        }

        /// <summary>
        /// Ajoute une cité.
        /// Crée également un GameObject visible sur la carte.
        /// </summary>
        public CityData AddCity(string name, int owner, HexCoordinates location, bool isCapital)
        {
            var cityData = new CityData
            {
                CityId = _allCities.Count,
                CityName = name,
                OwnerIndex = owner,
                Location = location,
                Population = 1,
                IsCapital = isCapital
            };
            _allCities.Add(cityData);

            // Créer l'instance runtime
            var runtimeCity = new City(cityData);
            _runtimeCities.Add(runtimeCity);

            // Créer le GameObject visible sur la carte
            CreateCityGameObject(cityData);

            Debug.Log($"[CityManager] Cité fondée : {name} à {location} (joueur {owner})");
            return cityData;
        }

        /// <summary>
        /// Crée un marqueur 3D visible pour une cité sur la carte hexagonale.
        /// </summary>
        private void CreateCityGameObject(CityData city)
        {
            if (_gridRenderer == null)
                _gridRenderer = FindAnyObjectByType<HexGridRenderer>();

            Vector3 worldPos = _gridRenderer != null
                ? _gridRenderer.HexToWorld(city.Location)
                : Vector3.zero;

            // Créer un marqueur cylindrique (colonne) pour la cité
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"City_{city.CityName}";

            // Taille : plus haut pour les capitales
            float height = city.IsCapital ? _cityMarkerHeight * 1.5f : _cityMarkerHeight;
            marker.transform.localScale = new Vector3(_cityMarkerRadius, height, _cityMarkerRadius);
            marker.transform.position = new Vector3(worldPos.x, height, worldPos.z);

            // Couleur par propriétaire
            var mr = marker.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Standard"));
            Color cityColor = city.OwnerIndex >= 0 && city.OwnerIndex < OwnerColors.Length
                ? OwnerColors[city.OwnerIndex]
                : new Color(0.5f, 0.5f, 0.5f);

            // Capitales en couleur pleine, autres légèrement transparentes
            if (city.IsCapital)
            {
                mat.color = cityColor;
            }
            else
            {
                mat.color = new Color(cityColor.r * 0.8f, cityColor.g * 0.8f, cityColor.b * 0.8f);
            }
            mr.sharedMaterial = mat;

            // Ajouter un anneau à la base pour les capitales
            if (city.IsCapital)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = $"CityRing_{city.CityName}";
                ring.transform.SetParent(marker.transform);
                ring.transform.localPosition = Vector3.zero;
                ring.transform.localScale = new Vector3(1.8f, 0.1f, 1.8f);

                var ringMr = ring.GetComponent<MeshRenderer>();
                var ringMat = new Material(Shader.Find("Standard"));
                ringMat.color = new Color(cityColor.r, cityColor.g, cityColor.b, 0.3f);
                ringMat.SetFloat("_Mode", 3);
                ringMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ringMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ringMat.SetInt("_ZWrite", 0);
                ringMat.renderQueue = 3000;
                ringMr.sharedMaterial = ringMat;

                Destroy(ring.GetComponent<Collider>());
            }

            // Supprimer le collider pour ne pas bloquer les clics sur le sol
            // (les clics sont gérés par raycast sur le plan y=0)
            Destroy(marker.GetComponent<Collider>());

            _cityGameObjects[city.Location] = marker;

            // Publier l'événement
            EventBus.Publish(new GameEvents.CityFounded
            {
                Location = city.Location,
                OwnerIndex = city.OwnerIndex,
                CityName = city.CityName
            });
        }

        /// <summary>
        /// Supprime le GameObject visible d'une cité.
        /// </summary>
        public void RemoveCityGameObject(HexCoordinates location)
        {
            if (_cityGameObjects.TryGetValue(location, out var go))
            {
                Destroy(go);
                _cityGameObjects.Remove(location);
            }
        }

        /// <summary>
        /// Met à jour la couleur du marqueur de cité (ex: après un changement de propriétaire).
        /// </summary>
        public void UpdateCityColor(HexCoordinates location, int newOwner)
        {
            if (_cityGameObjects.TryGetValue(location, out var go))
            {
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && newOwner >= 0 && newOwner < OwnerColors.Length)
                {
                    mr.sharedMaterial.color = OwnerColors[newOwner];
                }
            }
        }

        /// <summary>Ajoute de la population à une cité par index.</summary>
        public void AddPopulation(int playerIndex, int cityIndex, int delta)
        {
            var cities = GetPlayerCities(playerIndex);
            if (cityIndex >= 0 && cityIndex < cities.Count)
            {
                cities[cityIndex].Population = Mathf.Max(1, cities[cityIndex].Population + delta);
            }
        }

        /// <summary>Ajoute de la population à une cité par nom.</summary>
        public void AddPopulation(int playerIndex, string cityName, int delta)
        {
            var cities = GetPlayerCities(playerIndex);
            foreach (var city in cities)
            {
                if (city.CityName == cityName)
                {
                    city.Population = Mathf.Max(1, city.Population + delta);
                    return;
                }
            }
        }

        /// <summary>Ajoute de la population à la capitale.</summary>
        public void AddPopulationToCapital(int playerIndex, int delta)
        {
            var cities = GetPlayerCities(playerIndex);
            foreach (var city in cities)
            {
                if (city.IsCapital)
                {
                    city.Population = Mathf.Max(1, city.Population + delta);
                    return;
                }
            }
        }

        /// <summary>Vérifie si le joueur a une ville côtière.</summary>
        public bool HasCoastalCity(int playerIndex)
        {
            var cities = GetPlayerCities(playerIndex);
            if (cities.Count == 0) return false;

            var cells = GameManager.Instance?.Cells;
            if (cells == null) return false;

            foreach (var city in cities)
            {
                var (x, y) = city.Location.ToOffset();
                if (x < 0 || x >= cells.GetLength(0) || y < 0 || y >= cells.GetLength(1))
                    continue;

                if (cells[x, y] != null && cells[x, y].TileType == TileType.Sea)
                    return true;

                // Vérifier les voisins pour la côte
                var neighbors = city.Location.GetNeighbors();
                foreach (var n in neighbors)
                {
                    var (nx, ny) = n.ToOffset();
                    if (nx >= 0 && nx < cells.GetLength(0) && ny >= 0 && ny < cells.GetLength(1))
                    {
                        if (cells[nx, ny] != null && cells[nx, ny].TileType == TileType.Sea)
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Trouve les données persistantes d'une cité par son nom.
        /// </summary>
        private CityData FindCityData(string cityName)
        {
            foreach (var c in _allCities)
            {
                if (c.CityName == cityName)
                    return c;
            }
            return null;
        }
    }
}
