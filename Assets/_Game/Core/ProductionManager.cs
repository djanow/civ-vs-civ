using System.Collections.Generic;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Système de production statique pour les cités.
    /// Gère la liste des bâtiments/unités constructibles,
    /// le lancement de production et le traitement tour-par-tour.
    ///
    /// Les données de construction proviennent de GameSetupData
    /// (BuildingDefinitions et UnitDefinitions).
    /// </summary>
    public static class ProductionManager
    {
        /// <summary>
        /// Retourne la liste des bâtiments disponibles pour une cité.
        /// Filtre par technologie requise et unicité.
        /// </summary>
        public static List<BuildingData> GetAvailableBuildings(City city, int playerIndex)
        {
            var result = new List<BuildingData>();
            var setup = GameManager.Instance?.SetupData;
            if (setup?.BuildingDefinitions == null) return result;

            var research = GameManager.Instance?.ResearchManager;
            var civ = GameManager.Instance?.CivManager;

            foreach (var building in setup.BuildingDefinitions)
            {
                if (building == null) continue;

                // Vérifier la technologie requise
                if (building.RequiredTechId >= 0)
                {
                    if (research == null) continue;
                    var completedTechs = research.GetCompletedTechs(playerIndex);
                    if (!completedTechs.Contains(building.RequiredTechId))
                        continue;
                }

                // Vérifier l'unicité (bâtiment exclusif à une civilisation)
                if (building.IsUnique)
                {
                    // Ne proposer que si c'est le bâtiment unique de cette civilisation
                    // (les bâtiments uniques sont filtrés par nom de civilisation)
                    continue;
                }

                // Ne pas proposer si déjà en construction
                if (city.CurrentProduction == building.BuildingName)
                    continue;

                result.Add(building);
            }

            return result;
        }

        /// <summary>
        /// Retourne la liste des unités disponibles pour une cité.
        /// Filtre par technologie requise et unicité.
        /// </summary>
        public static List<UnitData> GetAvailableUnits(City city, int playerIndex)
        {
            var result = new List<UnitData>();
            var setup = GameManager.Instance?.SetupData;
            if (setup?.UnitDefinitions == null) return result;

            var research = GameManager.Instance?.ResearchManager;
            var civ = GameManager.Instance?.CivManager;

            foreach (var unit in setup.UnitDefinitions)
            {
                if (unit == null) continue;

                // Vérifier la technologie requise
                if (unit.RequiredTechId >= 0)
                {
                    if (unit.RequiredTechId >= 0)
                {
                    if (research == null) continue;
                    var completedTechs = research.GetCompletedTechs(playerIndex);
                    if (!completedTechs.Contains(unit.RequiredTechId))
                        continue;
                }
                }

                // Vérifier l'unicité
                if (unit.IsUnique)
                {
                    if (civ == null) continue;
                    continue;
                }

                // Ne pas proposer si déjà en construction
                if (city.CurrentProduction == unit.UnitName)
                    continue;

                result.Add(unit);
            }

            return result;
        }

        /// <summary>
        /// Lance la production d'un objet dans une cité.
        /// </summary>
        /// <param name="city">La cité qui produit.</param>
        /// <param name="itemName">Nom de l'unité ou du bâtiment à produire.</param>
        public static void StartProduction(City city, string itemName)
        {
            if (city == null || string.IsNullOrEmpty(itemName)) return;

            // Chercher le coût dans les définitions
            int cost = FindProductionCost(itemName);
            city.StartProduction(itemName, cost);

            Debug.Log($"[Production] {city.CityName} commence la production de {itemName} (coût: {cost})");
        }

        /// <summary>
        /// Traite la production d'une cité pour un tour.
        /// Ajoute la production de base (population * 2) au stock.
        /// Si le stock atteint le coût, la production est terminée.
        /// </summary>
        /// <param name="city">La cité à traiter.</param>
        /// <param name="cells">La grille de jeu (pour les calculs de yields).</param>
        /// <returns>True si la production est terminée ce tour.</returns>
        public static bool ProcessCityProduction(City city, HexCell[,] cells)
        {
            if (city == null) return false;
            if (string.IsNullOrEmpty(city.CurrentProduction)) return false;
            if (city.CurrentProductionCost <= 0) return false;

            // Production de base par tour : 1 + population
            int productionPerTurn = 1 + city.Population;

            city.ProductionStored += productionPerTurn;

            Debug.Log($"[Production] {city.CityName} progresse: {city.ProductionStored}/{city.CurrentProductionCost} (+{productionPerTurn}/tour)");

            // Vérifier si la production est terminée
            if (city.ProductionStored >= city.CurrentProductionCost)
            {
                CompleteProduction(city);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finalise la production d'une cité :
        /// - Crée l'unité sur la carte ou ajoute le bâtiment
        /// - Publie l'événement CityProductionCompleted
        /// - Réinitialise la file de production
        /// </summary>
        private static void CompleteProduction(City city)
        {
            string itemName = city.CurrentProduction;
            Debug.Log($"[Production] {city.CityName} a terminé la construction de {itemName}");

            // Déterminer si c'est une unité ou un bâtiment
            var unitDef = FindUnitDefinition(itemName);
            if (unitDef != null)
            {
                // Créer l'unité sur la carte
                var unitManager = GameManager.Instance?.UnitManager;
                if (unitManager != null)
                {
                    // Trouver la position de la cité
                    var cm = GameManager.Instance?.CityManager;
                    HexCoordinates? cityLocation = FindCityLocation(city.CityName);
                    if (cityLocation.HasValue)
                    {
                        // Trouver une case adjacente libre ou placer sur la cité
                        HexCoordinates spawnPos = FindSpawnPosition(cityLocation.Value);
                        var spawned = unitManager.SpawnUnit(unitDef, spawnPos, GetCityOwner(city.CityName));
                        if (spawned != null)
                        {
                            // Mettre à jour la visibilité
                            unitManager.UpdatePlayerVisibility(spawned.OwnerIndex);
                        }
                    }
                }
            }

            // Publier l'événement
            EventBus.Publish(new GameEvents.CityProductionCompleted
            {
                CityLocation = FindCityLocation(city.CityName) ?? new HexCoordinates(0, 0),
                ItemName = itemName
            });

            // Réinitialiser la production
            city.CurrentProduction = null;
            city.CurrentProductionCost = 0;
            city.ProductionStored = 0;
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        /// <summary>
        /// Trouve le coût de production d'un objet par son nom.
        /// </summary>
        private static int FindProductionCost(string itemName)
        {
            var setup = GameManager.Instance?.SetupData;
            if (setup == null) return 30; // Coût par défaut

            // Chercher dans les unités
            if (setup.UnitDefinitions != null)
            {
                foreach (var u in setup.UnitDefinitions)
                {
                    if (u != null && u.UnitName == itemName)
                        return u.ProductionCost;
                }
            }

            // Chercher dans les bâtiments
            if (setup.BuildingDefinitions != null)
            {
                foreach (var b in setup.BuildingDefinitions)
                {
                    if (b != null && b.BuildingName == itemName)
                        return b.ProductionCost;
                }
            }

            Debug.LogWarning($"[Production] Coût introuvable pour \"{itemName}\", utilisation du coût par défaut (30)");
            return 30;
        }

        /// <summary>
        /// Trouve la définition d'une unité par son nom.
        /// </summary>
        private static UnitData FindUnitDefinition(string itemName)
        {
            var setup = GameManager.Instance?.SetupData;
            if (setup?.UnitDefinitions == null) return null;

            foreach (var u in setup.UnitDefinitions)
            {
                if (u != null && u.UnitName == itemName)
                    return u;
            }
            return null;
        }

        /// <summary>
        /// Trouve la position d'une cité par son nom.
        /// </summary>
        private static HexCoordinates? FindCityLocation(string cityName)
        {
            var cm = GameManager.Instance?.CityManager;
            if (cm == null) return null;

            var cities = cm.GetAllCities();
            foreach (var c in cities)
            {
                if (c.CityName == cityName)
                    return c.Location;
            }
            return null;
        }

        /// <summary>
        /// Trouve le propriétaire d'une cité par son nom.
        /// </summary>
        private static int GetCityOwner(string cityName)
        {
            var cm = GameManager.Instance?.CityManager;
            if (cm == null) return -1;

            var cities = cm.GetAllCities();
            foreach (var c in cities)
            {
                if (c.CityName == cityName)
                    return c.OwnerIndex;
            }
            return -1;
        }

        /// <summary>
        /// Trouve une position de spawn libre autour de la cité.
        /// </summary>
        private static HexCoordinates FindSpawnPosition(HexCoordinates cityPos)
        {
            var cells = GameManager.Instance?.Cells;
            if (cells == null) return cityPos;

            // Vérifier d'abord la case de la cité elle-même
            var unitManager = GameManager.Instance?.UnitManager;
            if (unitManager != null && unitManager.GetUnitAt(cityPos) == null)
                return cityPos;

            // Chercher un voisin accessible
            var neighbors = cityPos.GetNeighbors();
            foreach (var n in neighbors)
            {
                var (nx, ny) = n.ToOffset();
                if (nx < 0 || nx >= cells.GetLength(0) || ny < 0 || ny >= cells.GetLength(1))
                    continue;

                if (cells[nx, ny].MovementCost >= 0 && unitManager != null && unitManager.GetUnitAt(n) == null)
                    return n;
            }

            // Fallback : retourner la position de la cité (stack)
            return cityPos;
        }
    }
}
