using System.Collections.Generic;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Un modifieur de combat individuel, avec son nom descriptif et sa valeur.
    /// Utilise pour la prevision de combat (UI) et le decompose en facteurs.
    /// </summary>
    public struct CombatModifier
    {
        public string Name;  // "Terrain (Foret)", "Veteran **", "General", "Doctrine agressive"
        public int Value;
    }

    /// <summary>
    /// Resultat complet d'un combat, incluant tous les facteurs de l'affrontement.
    /// </summary>
    public struct CombatResult
    {
        public bool AttackerWins;
        public int AttackerDamage;
        public int DefenderDamage;
        public int AttackerTotalPower;
        public int DefenderTotalPower;
        public List<CombatModifier> Modifiers; // Tous les facteurs decomposes
        public bool DefenderKilled;
    }

    /// <summary>
    /// Systeme de combat incluant les facteurs de terrain, veterance,
    /// bonus de categorie et la formule de degats avec variance.
    /// </summary>
    public static class CombatResolver
    {
        // ----------------------------------------------------------------
        // Methodes publiques
        // ----------------------------------------------------------------

        /// <summary>
        /// Calcule une prevision de combat sans appliquer les degats ni la variance aleatoire.
        /// Utilisee par l'UI pour afficher l'estimation au joueur avant qu'il n'attaque.
        /// </summary>
        public static CombatResult PreviewCombat(Unit attacker, Unit defender, HexCell defenderCell)
        {
            var modifiers = new List<CombatModifier>();

            int atkPower = CalculateAttackPower(attacker, defenderCell, true, modifiers);
            int defPower = CalculateDefensePower(defender, defenderCell, modifiers);

            // Preview : valeur mediane sans Random.Range(-1, 2)
            int defDamage = Mathf.Max(1, atkPower - defPower / 2);
            int atkDamage = Mathf.Max(1, defPower - atkPower / 2);

            bool defenderKilled = defDamage >= defender.CurrentHealth;
            bool attackerKilled = atkDamage >= attacker.CurrentHealth;

            return new CombatResult
            {
                AttackerWins = defenderKilled && !attackerKilled,
                AttackerDamage = atkDamage,
                DefenderDamage = defDamage,
                AttackerTotalPower = atkPower,
                DefenderTotalPower = defPower,
                Modifiers = modifiers,
                DefenderKilled = defenderKilled
            };
        }

        /// <summary>
        /// Execute le combat entre deux unites et applique les degats.
        /// Publie les evenements CombatEvent et UnitKilled via EventBus.
        /// </summary>
        public static CombatResult ExecuteCombat(Unit attacker, Unit defender, HexCell defenderCell)
        {
            var modifiers = new List<CombatModifier>();

            int atkPower = CalculateAttackPower(attacker, defenderCell, true, modifiers);
            int defPower = CalculateDefensePower(defender, defenderCell, modifiers);

            // Formule de degats : max(1, power - ennemiPower/2 + Random.Range(-1, 2))
            int defDamage = Mathf.Max(1, atkPower - defPower / 2 + Random.Range(-1, 2));
            int atkDamage = Mathf.Max(1, defPower - atkPower / 2 + Random.Range(-1, 2));

            // Appliquer les degats
            attacker.TakeDamage(atkDamage);
            defender.TakeDamage(defDamage);

            bool defenderKilled = defender.IsDead();
            bool attackerKilled = attacker.IsDead();

            var result = new CombatResult
            {
                AttackerWins = defenderKilled && !attackerKilled,
                AttackerDamage = atkDamage,
                DefenderDamage = defDamage,
                AttackerTotalPower = atkPower,
                DefenderTotalPower = defPower,
                Modifiers = modifiers,
                DefenderKilled = defenderKilled
            };

            // Publier l'evenement de combat
            EventBus.Publish(new GameEvents.CombatEvent
            {
                Location = defender.Position,
                AttackerName = attacker.UnitName,
                DefenderName = defender.UnitName,
                Result = result
            });

            // Publier l'evenement de destruction si le defenseur est tue
            if (defenderKilled)
            {
                EventBus.Publish(new GameEvents.UnitKilled
                {
                    Location = defender.Position,
                    UnitName = defender.UnitName,
                    OwnerIndex = defender.OwnerIndex
                });
            }

            // Si l'attaquant est tue (contre-attaque fatale)
            if (attackerKilled)
            {
                EventBus.Publish(new GameEvents.UnitKilled
                {
                    Location = attacker.Position,
                    UnitName = attacker.UnitName,
                    OwnerIndex = attacker.OwnerIndex
                });
            }

            // Promotion possible pour le vainqueur
            if (!attackerKilled && defenderKilled && attacker.VeterancyRank < 3)
            {
                // 50% de chance de promotion apres victoire
                if (Random.value < 0.5f)
                {
                    attacker.Promote();
                }
            }

            return result;
        }

        // ----------------------------------------------------------------
        // Calculs de puissance
        // ----------------------------------------------------------------

        /// <summary>
        /// Calcule la puissance d'attaque totale avec tous les bonus.
        /// </summary>
        public static int CalculateAttackPower(Unit unit, HexCell cell, bool isAttacking)
        {
            var dummy = new List<CombatModifier>();
            return CalculateAttackPower(unit, cell, isAttacking, dummy);
        }

        /// <summary>
        /// Calcule la puissance de defense totale avec tous les bonus.
        /// </summary>
        public static int CalculateDefensePower(Unit unit, HexCell cell)
        {
            var dummy = new List<CombatModifier>();
            return CalculateDefensePower(unit, cell, dummy);
        }

        // ----------------------------------------------------------------
        // Calculs internes avec collecte des modifieurs
        // ----------------------------------------------------------------

        /// <summary>
        /// Calcule la puissance d'attaque et collecte les modifieurs appliques.
        /// </summary>
        private static int CalculateAttackPower(Unit unit, HexCell cell, bool isAttacking, List<CombatModifier> modifiers)
        {
            int power = unit.BaseAttack;

            // Bonus de veterance (+10% par rang)
            if (unit.VeterancyRank > 0)
            {
                int vetBonus = unit.GetVeterancyAttackBonus();
                power += vetBonus;
                string stars = new string('★', unit.VeterancyRank);
                modifiers.Add(new CombatModifier { Name = $"Veteran {stars}", Value = vetBonus });
            }

            // Bonus d'armee (deja integre dans BaseAttack via *3)
            if (unit.IsArmy)
            {
                modifiers.Add(new CombatModifier { Name = "Armee combinee", Value = 0 });
            }

            // Bonus de terrain specifique a la categorie (basé sur la case defenseur)
            if (unit.Category == UnitCategory.Cavalry && cell.TileType == TileType.Plain)
            {
                power += 1;
                modifiers.Add(new CombatModifier { Name = "Plaine (Cavalerie)", Value = 1 });
            }

            if (unit.Category == UnitCategory.Infantry &&
                (cell.TileType == TileType.Forest || cell.TileType == TileType.Hill))
            {
                power += 1;
                modifiers.Add(new CombatModifier { Name = "Terrain accidente (Infanterie)", Value = 1 });
            }

            // Bonus de colline pour l'attaquant en position haute
            if (isAttacking && cell.TileType == TileType.Hill)
            {
                power += 1;
                modifiers.Add(new CombatModifier { Name = "Position haute (Colline)", Value = 1 });
            }

            return power;
        }

        /// <summary>
        /// Calcule la puissance de defense et collecte les modifieurs appliques.
        /// </summary>
        private static int CalculateDefensePower(Unit unit, HexCell cell, List<CombatModifier> modifiers)
        {
            int power = unit.BaseDefense;

            // Bonus de veterance (+10% par rang)
            if (unit.VeterancyRank > 0)
            {
                int vetBonus = unit.GetVeterancyDefenseBonus();
                power += vetBonus;
                string stars = new string('★', unit.VeterancyRank);
                modifiers.Add(new CombatModifier { Name = $"Veteran {stars}", Value = vetBonus });
            }

            // Bonus d'armee
            if (unit.IsArmy)
            {
                modifiers.Add(new CombatModifier { Name = "Armee combinee", Value = 0 });
            }

            // Bonus de defense du terrain
            int terrainBonus = cell.DefenseBonus;
            if (terrainBonus != 0)
            {
                power += terrainBonus;
                string terrainName = cell.TileType switch
                {
                    TileType.Forest => "Foret",
                    TileType.Hill => "Colline",
                    TileType.Marsh => "Marais",
                    _ => cell.TileType.ToString()
                };
                string sign = terrainBonus > 0 ? "+" : "";
                modifiers.Add(new CombatModifier { Name = $"Terrain ({terrainName})", Value = terrainBonus });
            }

            // Bonus defensif pour infanterie en terrain accidente
            if (unit.Category == UnitCategory.Infantry &&
                (cell.TileType == TileType.Forest || cell.TileType == TileType.Hill))
            {
                power += 1;
                modifiers.Add(new CombatModifier { Name = "Defense terrain (Infanterie)", Value = 1 });
            }

            return power;
        }
    }
}
