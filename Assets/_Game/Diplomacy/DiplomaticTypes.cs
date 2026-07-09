namespace CivVSCiv
{
    /// <summary>
    /// Types de personnalite IA qui influencent les decisions diplomatiques.
    /// </summary>
    public enum AIPersonalityType
    {
        /// <summary>Agressif, favorise la guerre, faible volonte commerciale</summary>
        Aggressive,
        /// <summary>Prefere la diplomatie, chercher les alliances</summary>
        Diplomatic,
        /// <summary>Base ses decisions sur le rapport de force</summary>
        Opportunistic,
        /// <summary>Commercial, donne priorite aux routes et a l'economie</summary>
        Commercial,
        /// <summary>Expansionniste, veut etendre son territoire</summary>
        Expansionist,
        /// <summary>Isolationniste, se mefie des etrangers</summary>
        Isolationist
    }

    /// <summary>
    /// Types d'actions diplomatiques possibles.
    /// </summary>
    public enum DiploActionType
    {
        TradeRoute,
        NonAggressionPact,
        Alliance,
        WarDeclaration,
        PeaceTreaty,
        Gift
    }

    /// <summary>
    /// Structure decrivant une offre d'echange diplomatique.
    /// </summary>
    public struct TradeOffer
    {
        public int GoldAmount;
        public int ScienceAmount;
        public int Turns;

        public bool IsValid => GoldAmount > 0 || ScienceAmount > 0;

        public override string ToString()
        {
            string result = "";
            if (GoldAmount > 0) result += $"{GoldAmount} or";
            if (ScienceAmount > 0)
            {
                if (result.Length > 0) result += " + ";
                result += $"{ScienceAmount} science";
            }
            if (Turns > 0)
            {
                if (result.Length > 0) result += " ";
                result += $"sur {Turns} tours";
            }
            return string.IsNullOrEmpty(result) ? "Offre vide" : result;
        }
    }

    /// <summary>
    /// Structure encapsulant une action diplomatique complete.
    /// </summary>
    public struct DiplomaticAction
    {
        public DiploActionType Type;
        public int ProposerIndex;
        public int TargetIndex;
        public TradeOffer Offer;
        public bool Accepted;

        /// <summary>
        /// Cree une action diplomatique declaree.
        /// </summary>
        public static DiplomaticAction Create(DiploActionType type, int proposer, int target)
        {
            return new DiplomaticAction
            {
                Type = type,
                ProposerIndex = proposer,
                TargetIndex = target,
                Offer = default,
                Accepted = false
            };
        }

        /// <summary>
        /// Cree une action diplomatique avec une offre d'echange.
        /// </summary>
        public static DiplomaticAction CreateWithOffer(DiploActionType type, int proposer, int target, TradeOffer offer)
        {
            return new DiplomaticAction
            {
                Type = type,
                ProposerIndex = proposer,
                TargetIndex = target,
                Offer = offer,
                Accepted = false
            };
        }

        public override string ToString()
        {
            string typeName = Type switch
            {
                DiploActionType.TradeRoute => "Route commerciale",
                DiploActionType.NonAggressionPact => "Pacte de non-agression",
                DiploActionType.Alliance => "Alliance",
                DiploActionType.WarDeclaration => "Declaration de guerre",
                DiploActionType.PeaceTreaty => "Traite de paix",
                DiploActionType.Gift => "Cadeau",
                _ => Type.ToString()
            };

            string result = $"[J{ProposerIndex} -> J{TargetIndex}] {typeName}";
            if (Offer.IsValid) result += $" ({Offer})";
            if (Accepted) result += " [Accepte]";
            return result;
        }
    }
}
