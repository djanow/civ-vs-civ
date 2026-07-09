using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CivVSCiv
{
    // ──────────────────────────────────────────────────────────────
    // Structures sérialisables pour la sauvegarde
    // ──────────────────────────────────────────────────────────────

    [System.Serializable]
    public class SaveData
    {
        public int CurrentTurn;
        public int CurrentPlayerIndex;
        public int[] PlayerGold;
        public int[] PlayerScience;
        public int[] PlayerCulture;
        public int[] PlayerEra;
        public List<CitySaveData> Cities;
        public List<UnitSaveData> Units;
    }

    [System.Serializable]
    public class CitySaveData
    {
        public int CityId;
        public string CityName;
        public int OwnerIndex;
        public int Q;
        public int R;
        public int Population;
        public bool IsCapital;
    }

    [System.Serializable]
    public class UnitSaveData
    {
        public int OwnerIndex;
        public int Q;
        public int R;
        public string UnitName;
        public int HitPoints;
        public int MaxHitPoints;
        public int MovementRange;
        public int MovementRemaining;
        public int BaseAttack;
        public int BaseDefense;
        public UnitCategory Category;
        public int VeterancyRank;
        public string VeterancyName;
        public bool IsArmy;
    }

    // ──────────────────────────────────────────────────────────────
    // SaveManager
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sauvegarde et charge l'état complet d'une partie en JSON.
    /// Stockage : Application.persistentDataPath/save.json
    /// </summary>
    public static class SaveManager
    {
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "save.json");

        /// <summary>
        /// Sauvegarde la partie courante dans un fichier JSON.
        /// </summary>
        public static void SaveGame()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            var data = new SaveData
            {
                CurrentTurn = gm.TurnManager?.CurrentTurn ?? 1,
                CurrentPlayerIndex = gm.TurnManager?.CurrentPlayerIndex ?? 0,
                PlayerGold = (int[])gm.PlayerGold?.Clone(),
                PlayerScience = (int[])gm.PlayerScience?.Clone(),
                PlayerCulture = (int[])gm.PlayerCulture?.Clone(),
                PlayerEra = (int[])gm.PlayerEra?.Clone(),
                Cities = new List<CitySaveData>(),
                Units = new List<UnitSaveData>()
            };

            // Sauvegarder les cités
            var allCities = gm.CityManager?.GetAllCities();
            if (allCities != null)
            {
                foreach (var city in allCities)
                {
                    data.Cities.Add(new CitySaveData
                    {
                        CityId = city.CityId,
                        CityName = city.CityName,
                        OwnerIndex = city.OwnerIndex,
                        Q = city.Location.Q,
                        R = city.Location.R,
                        Population = city.Population,
                        IsCapital = city.IsCapital
                    });
                }
            }

            // Sauvegarder les unités
            if (gm.UnitManager != null)
            {
                foreach (var unit in gm.UnitManager.AllUnits)
                {
                    if (unit == null) continue;
                    data.Units.Add(new UnitSaveData
                    {
                        OwnerIndex = unit.OwnerIndex,
                        Q = unit.Position.Q,
                        R = unit.Position.R,
                        UnitName = unit.UnitName,
                        HitPoints = unit.CurrentHealth,
                        MaxHitPoints = unit.MaxHealth,
                        MovementRange = unit.MovementRange,
                        MovementRemaining = unit.MovementRemaining,
                        BaseAttack = unit.BaseAttack,
                        BaseDefense = unit.BaseDefense,
                        Category = unit.Category,
                        VeterancyRank = unit.VeterancyRank,
                        VeterancyName = unit.VeterancyName,
                        IsArmy = unit.IsArmy
                    });
                }
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] Partie sauvegardée ({data.Cities.Count} villes, {data.Units.Count} unités)");
        }

        /// <summary>
        /// Charge une partie sauvegardée.
        /// Nettoie l'état existant et restaure cités, unités et ressources.
        /// Retourne false si aucun fichier de sauvegarde n'existe.
        /// </summary>
        public static bool LoadGame()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("[SaveManager] Aucune sauvegarde trouvée.");
                return false;
            }

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return false;

            var gm = GameManager.Instance;
            if (gm == null) return false;

            // ── Nettoyer l'état existant ──

            // Détruire les GameObjects des cités
            if (gm.CityManager != null)
            {
                gm.CityManager.Initialize();
            }

            // Détruire toutes les unités
            if (gm.UnitManager != null)
            {
                for (int i = gm.UnitManager.AllUnits.Count - 1; i >= 0; i--)
                {
                    var u = gm.UnitManager.AllUnits[i];
                    if (u != null) Object.Destroy(u.gameObject);
                }
                gm.UnitManager.AllUnits.Clear();
                gm.UnitManager.Initialize(data.PlayerGold?.Length ?? 2);
            }

            // ── Restaurer les ressources ──
            if (data.PlayerGold != null)
                gm.PlayerGold = data.PlayerGold;
            if (data.PlayerScience != null)
                gm.PlayerScience = data.PlayerScience;
            if (data.PlayerCulture != null)
                gm.PlayerCulture = data.PlayerCulture;
            if (data.PlayerEra != null)
                gm.PlayerEra = data.PlayerEra;

            // ── Restaurer les cités ──
            if (data.Cities != null && gm.CityManager != null)
            {
                foreach (var cs in data.Cities)
                {
                    var location = new HexCoordinates(cs.Q, cs.R);
                    gm.CityManager.AddCity(cs.CityName, cs.OwnerIndex, location, cs.IsCapital);
                }
            }

            // ── Restaurer les unités ──
            if (data.Units != null && gm.UnitManager != null)
            {
                var um = gm.UnitManager;
                foreach (var us in data.Units)
                {
                    var unitData = ScriptableObject.CreateInstance<UnitData>();
                    unitData.UnitName = us.UnitName;
                    unitData.MaxHealth = us.MaxHitPoints;
                    unitData.MovementRange = us.MovementRange;
                    unitData.BaseAttack = us.BaseAttack;
                    unitData.BaseDefense = us.BaseDefense;
                    unitData.Category = us.Category;
                    unitData.ProductionCost = 0;
                    unitData.RequiredTechId = -1;
                    unitData.IsUnique = false;
                    unitData.CivilizationId = -1;

                    var pos = new HexCoordinates(us.Q, us.R);
                    var unit = um.SpawnUnit(unitData, pos, us.OwnerIndex);
                    unit.CurrentHealth = us.HitPoints;
                    unit.MovementRemaining = us.MovementRemaining;
                    unit.VeterancyRank = us.VeterancyRank;
                    unit.VeterancyName = us.VeterancyName;
                    unit.IsArmy = us.IsArmy;
                }
            }

            Debug.Log($"[SaveManager] Partie chargée (tour {data.CurrentTurn}, "
                      + $"{data.Cities?.Count ?? 0} villes, {data.Units?.Count ?? 0} unités)");
            return true;
        }

        /// <summary>
        /// Vérifie si un fichier de sauvegarde existe.
        /// </summary>
        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        /// <summary>
        /// Supprime le fichier de sauvegarde.
        /// </summary>
        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[SaveManager] Sauvegarde supprimée.");
            }
        }
    }
}
