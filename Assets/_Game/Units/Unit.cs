using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Une unite placee sur la carte hexagonale.
    /// Porte ses stats de combat, sa position, et son etat de veterance.
    /// </summary>
    public class Unit : MonoBehaviour
    {
        [Header("Identite")]
        public string UnitName;
        public HexCoordinates Position;
        public int OwnerIndex;

        [Header("Stats de combat")]
        public int CurrentHealth;
        public int MaxHealth;
        public int MovementRange;
        public int MovementRemaining;
        public int BaseAttack;
        public int BaseDefense;
        public UnitCategory Category;

        [Header("Veterance")]
        public int VeterancyRank;       // 0 a 3
        public string VeterancyName;    // "La Garde de Tyr"

        [Header("Armee")]
        public bool IsArmy;             // true si formee par fusion 3->1

        /// <summary>
        /// Initialise l'unite a partir d'un UnitData.
        /// </summary>
        public void Initialize(UnitData data, HexCoordinates position, int ownerIndex)
        {
            UnitName = data.UnitName;
            Position = position;
            OwnerIndex = ownerIndex;

            MaxHealth = data.MaxHealth;
            CurrentHealth = MaxHealth;
            MovementRange = data.MovementRange;
            MovementRemaining = MovementRange;
            BaseAttack = data.BaseAttack;
            BaseDefense = data.BaseDefense;
            Category = data.Category;

            VeterancyRank = 0;
            VeterancyName = "";
            IsArmy = false;
        }

        /// <summary>
        /// Initialise l'unite en tant qu'armee fusionnee (stats combinees).
        /// </summary>
        public void InitializeAsArmy(string armyName, Unit template, int combinedHealth, int healthSum)
        {
            UnitName = template.UnitName;
            Position = template.Position;
            OwnerIndex = template.OwnerIndex;

            MaxHealth = combinedHealth;
            CurrentHealth = healthSum;
            MovementRange = template.MovementRange;
            MovementRemaining = MovementRange;
            BaseAttack = template.BaseAttack * 3;
            BaseDefense = template.BaseDefense * 3;
            Category = template.Category;

            VeterancyRank = template.VeterancyRank;
            VeterancyName = armyName;
            IsArmy = true;
        }

        /// <summary>
        /// Verifie si l'unite peut atteindre la case cible avec ses points de mouvement restants.
        /// Utilise l'A* de HexPathfinding pour calculer le chemin et son cout total.
        /// </summary>
        public bool CanMoveTo(HexCoordinates target, HexCell[,] cells)
        {
            if (MovementRemaining <= 0) return false;
            if (target == Position) return true;

            int width = cells.GetLength(0);
            int height = cells.GetLength(1);

            var path = HexPathfinding.FindPath(cells, width, height, Position, target);
            if (path == null || path.Count == 0) return false;

            // Calculer le cout total du chemin (sauf la case de depart)
            int totalCost = 0;
            for (int i = 1; i < path.Count; i++)
            {
                var (x, y) = path[i].ToOffset();
                if (x < 0 || x >= width || y < 0 || y >= height) return false;
                int cost = cells[x, y].MovementCost;
                if (cost < 0) return false;
                totalCost += cost;
            }

            return totalCost <= MovementRemaining;
        }

        /// <summary>
        /// Deplace l'unite vers une nouvelle case (met a jour la position logique).
        /// La mise a jour visuelle est geree par UnitManager.
        /// </summary>
        public void MoveTo(HexCoordinates target)
        {
            Position = target;
        }

        /// <summary>
        /// Inflige des degats a l'unite. Le montant est plancher a 0.
        /// </summary>
        public void TakeDamage(int amount)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        }

        /// <summary>
        /// Retourne true si l'unite est morte (CurrentHealth <= 0).
        /// </summary>
        public bool IsDead()
        {
            return CurrentHealth <= 0;
        }

        /// <summary>
        /// Pro promeut l'unite au rang de veterance suivant (max 3).
        /// Chaque rang donne un nom distinctif.
        /// </summary>
        public void Promote()
        {
            if (VeterancyRank >= 3) return;

            VeterancyRank++;

            switch (VeterancyRank)
            {
                case 1:
                    VeterancyName = $"{UnitName} d'elite";
                    break;
                case 2:
                    VeterancyName = $"Veteran {UnitName}";
                    break;
                case 3:
                    VeterancyName = $"{UnitName} legendaire";
                    break;
            }
        }

        /// <summary>
        /// Reinitialise les points de mouvement au max (debut de tour).
        /// </summary>
        public void RefreshMovement()
        {
            MovementRemaining = MovementRange;
        }

        /// <summary>
        /// Consomme des points de mouvement.
        /// </summary>
        public void SpendMovement(int amount)
        {
            MovementRemaining = Mathf.Max(0, MovementRemaining - amount);
        }

        /// <summary>
        /// Retourne le bonus d'attaque lie a la veterance (+10% par rang).
        /// </summary>
        public int GetVeterancyAttackBonus()
        {
            return Mathf.FloorToInt(BaseAttack * VeterancyRank * 0.1f);
        }

        /// <summary>
        /// Retourne le bonus de defense lie a la veterance (+10% par rang).
        /// </summary>
        public int GetVeterancyDefenseBonus()
        {
            return Mathf.FloorToInt(BaseDefense * VeterancyRank * 0.1f);
        }
    }
}
