namespace CivVSCiv
{
    /// <summary>
    /// Personnalite IA qui guide le comportement diplomatique et strategique
    /// d'une civilisation.
    /// </summary>
    public enum AIPersonalityType
    {
        /// <summary>Construit du commerce, evite les guerres, defend ses routes (ex: Phenicie)</summary>
        Commercial,

        /// <summary>Teste toutes les approches, curieux, adaptatif (ex: Grece)</summary>
        Competitive,

        /// <summary>Expansion militaire prioritaire, declare guerre facilement</summary>
        Aggressive,

        /// <summary>Se concentre sur son territoire, repousse les alliances</summary>
        Isolationist,

        /// <summary>Cherche des allies, propose des pactes, influence par la culture</summary>
        Diplomatic
    }
}
