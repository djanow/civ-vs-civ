using System.Collections.Generic;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Donnees d'une cite.
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
    /// Instance de cite en jeu. Contient les donnees et les methodes
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

        /// <summary>
        /// Construit une instance a partir des donnees de base.
        /// </summary>
        public City(CityData data)
        {
            CityName = data.CityName;
            Population = data.Population;
            CurrentProduction = null;
            CurrentProductionCost = 0;
            ProductionStored = 0;
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
    /// Gere les cites de toutes les civilisations.
    /// </summary>
    public class CityManager : MonoBehaviour
    {
        private List<CityData> _allCities = new List<CityData>();

        /// <summary>Initialise le gestionnaire.</summary>
        public void Initialize()
        {
            _allCities.Clear();
        }

        /// <summary>Toutes les cites.</summary>
        public List<CityData> GetAllCities() => _allCities;

        /// <summary>Cites d'un joueur.</summary>
        public List<CityData> GetPlayerCities(int playerIndex)
        {
            return _allCities.FindAll(c => c.OwnerIndex == playerIndex);
        }

        /// <summary>Ajoute une cite.</summary>
        public CityData AddCity(string name, int owner, HexCoordinates location, bool isCapital)
        {
            var city = new CityData
            {
                CityId = _allCities.Count,
                CityName = name,
                OwnerIndex = owner,
                Location = location,
                Population = 1,
                IsCapital = isCapital
            };
            _allCities.Add(city);
            return city;
        }

        /// <summary>Ajoute de la population a une cite par index.</summary>
        public void AddPopulation(int playerIndex, int cityIndex, int delta)
        {
            var cities = GetPlayerCities(playerIndex);
            if (cityIndex >= 0 && cityIndex < cities.Count)
            {
                cities[cityIndex].Population = Mathf.Max(1, cities[cityIndex].Population + delta);
            }
        }

        /// <summary>Ajoute de la population a une cite par nom.</summary>
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

        /// <summary>Ajoute de la population a la capitale.</summary>
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

        /// <summary>Verifie si le joueur a une ville cotiere.</summary>
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

                // Verifier les voisins pour la cote
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
    }
}
