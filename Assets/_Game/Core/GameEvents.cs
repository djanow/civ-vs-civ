namespace CivVSCiv
{
    /// <summary>
    /// Phases de jeu d'un tour. Les phases NarrativeEvent et Diplomacy
    /// sont conditionnelles (déclenchées uniquement si un événement survient).
    /// </summary>
    public enum TurnPhase
    {
        NarrativeEvent,     // Événement narratif si déclenché
        Movement,           // Exploration et mouvement des unités
        CityManagement,     // Gestion de cité
        Diplomacy,          // Interaction diplomatique si déclenchée
        Research,           // Choix tech et progression
        EndOfTurn           // Résolution IA, événements système
    }

    /// <summary>
    /// Conteneur pour tous les types d'événements du jeu.
    /// Chaque struct est un événement qui peut être publié via EventBus.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>
        /// Publié quand la carte a fini d'être générée.
        /// </summary>
        public struct MapGenerated
        {
            public HexCell[,] Cells;
            public int Width;
            public int Height;
        }

        /// <summary>
        /// Publié avec les positions de départ de chaque civilisation.
        /// </summary>
        public struct CivStartPositions
        {
            public HexCoordinates[] StartPositions;
        }

        /// <summary>
        /// Publié quand la phase de tour change.
        /// </summary>
        public struct TurnPhaseChanged
        {
            public TurnPhase Phase;
            public int TurnNumber;
            public int PlayerIndex;
        }

        /// <summary>
        /// Publié quand le tour d'un joueur se termine complètement.
        /// </summary>
        public struct TurnEnded
        {
            public int TurnNumber;
            public int PlayerIndex;
        }

        /// <summary>
        /// Publié quand le tour d'un joueur commence.
        /// </summary>
        public struct PlayerTurnStarted
        {
            public int PlayerIndex;
        }

        /// <summary>
        /// Publié quand une nouvelle cité est fondée.
        /// </summary>
        public struct CityFounded
        {
            public HexCoordinates Location;
            public int OwnerIndex;
            public string CityName;
        }

        /// <summary>
        /// Publié quand la production d'une cité est terminée.
        /// </summary>
        public struct CityProductionCompleted
        {
            public HexCoordinates CityLocation;
            public string ItemName;
        }

        /// <summary>
        /// Publié quand un joueur change d'ère (nouveau leader).
        /// </summary>
        public struct EraAdvanced
        {
            public int PlayerIndex;
            public int OldEra;
            public int NewEra;
            public string NewLeaderName;
        }

        /// <summary>
        /// Publié quand un legs de leader est débloqué.
        /// </summary>
        public struct LegacyUnlocked
        {
            public int PlayerIndex;
            public string LegacyName;
            public string Description;
        }

        /// <summary>
        /// Publié à chaque résolution de combat entre deux unités.
        /// </summary>
        public struct CombatEvent
        {
            public HexCoordinates Location;
            public string AttackerName;
            public string DefenderName;
            public CombatResult Result;
        }

        /// <summary>
        /// Publié quand une unité est tuée au combat.
        /// </summary>
        public struct UnitKilled
        {
            public HexCoordinates Location;
            public string UnitName;
            public int OwnerIndex;
        }

        /// <summary>
        /// Publié quand une Armée est formée par fusion de 3 unités.
        /// </summary>
        public struct ArmyFormed
        {
            public HexCoordinates Location;
            public string ArmyName;
        }

        /// <summary>
        /// Publié quand un événement narratif est déclenché.
        /// </summary>
        public struct NarrativeEventTriggered
        {
            public int PlayerIndex;
            public int EventId;
            public string Title;
        }

        /// <summary>
        /// Publié quand un joueur fait un choix dans un événement narratif.
        /// </summary>
        public struct NarrativeChoiceMade
        {
            public int PlayerIndex;
            public int EventId;
            public int ChoiceIndex;
            public string EffectsDescription;
        }

        // ============================================================
        // Phase 4 — Événements de Recherche
        // ============================================================

        /// <summary>
        /// Publié quand une technologie est complétée.
        /// </summary>
        public struct TechCompleted
        {
            public int PlayerIndex;
            public int TechId;
            public string TechName;
        }

        /// <summary>
        /// Publié quand une recherche est lancée.
        /// </summary>
        public struct ResearchStarted
        {
            public int PlayerIndex;
            public int TechId;
            public string TechName;
        }

        // ============================================================
        // Phase 4 — Événements Diplomatiques
        // ============================================================

        /// <summary>
        /// Publié quand une action diplomatique a lieu (acceptée/refusée).
        /// </summary>
        public struct DiplomaticInteraction
        {
            public DiplomaticAction Action;
        }

        /// <summary>
        /// Publié quand une guerre est déclarée.
        /// </summary>
        public struct WarDeclared
        {
            public int AttackerIndex;
            public int DefenderIndex;
        }

        /// <summary>
        /// Publié quand un traité de paix est signé.
        /// </summary>
        public struct PeaceSigned
        {
            public int PartyA;
            public int PartyB;
        }
    }
}
