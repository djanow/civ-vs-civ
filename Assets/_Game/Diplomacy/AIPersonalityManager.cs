using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Gestionnaire statique des personnalités IA.
    /// Fournit des méthodes de décision pondérées selon la personnalité
    /// et les relations actuelles.
    /// </summary>
    public static class AIPersonalityManager
    {
        /// <summary>
        /// Retourne la volonté de commerce (0..1) selon la personnalité et les relations.
        /// Plus le score est élevé, plus l'IA est susceptible d'accepter/proposer du commerce.
        /// </summary>
        public static float GetTradeWillingness(AIPersonalityType personality, int relations)
        {
            float baseWillingness = personality switch
            {
                AIPersonalityType.Aggressive => 0.15f,
                AIPersonalityType.Diplomatic => 0.65f,
                AIPersonalityType.Opportunistic => 0.50f,
                AIPersonalityType.Commercial => 0.85f,
                AIPersonalityType.Expansionist => 0.35f,
                AIPersonalityType.Isolationist => 0.10f,
                _ => 0.40f
            };

            // Ajustement selon les relations (-100 à +100 mappe sur -0.5 à +0.5)
            float relationModifier = relations / 200f;

            float result = Mathf.Clamp01(baseWillingness + relationModifier);

            return result;
        }

        /// <summary>
        /// Retourne le seuil de déclaration de guerre (0..1).
        /// Plus le score est élevé, plus l'IA est susceptible de déclarer la guerre.
        /// </summary>
        public static float GetWarThreshold(AIPersonalityType personality, int powerRatio)
        {
            float baseThreshold = personality switch
            {
                AIPersonalityType.Aggressive => 0.40f,
                AIPersonalityType.Diplomatic => 0.05f,
                AIPersonalityType.Opportunistic => 0.20f,
                AIPersonalityType.Commercial => 0.08f,
                AIPersonalityType.Expansionist => 0.30f,
                AIPersonalityType.Isolationist => 0.15f,
                _ => 0.15f
            };

            // Ajustement selon le rapport de puissance
            // Plus l'IA est puissante relativement, plus elle est agressive
            float powerModifier = powerRatio switch
            {
                > 2f => 0.25f,     // Massivement plus puissant
                > 1.5f => 0.15f,   // Plus puissant
                > 0.8f => 0.0f,    // Équilibré
                _ => -0.10f         // Plus faible
            };

            return Mathf.Clamp01(baseThreshold + powerModifier);
        }

        /// <summary>
        /// Retourne le désir d'alliance (0..1) selon la personnalité,
        /// les relations et le nombre d'ennemis communs.
        /// </summary>
        public static float GetAllianceDesire(AIPersonalityType personality, int relations, int commonEnemies)
        {
            float baseDesire = personality switch
            {
                AIPersonalityType.Aggressive => 0.20f,
                AIPersonalityType.Diplomatic => 0.70f,
                AIPersonalityType.Opportunistic => 0.40f,
                AIPersonalityType.Commercial => 0.55f,
                AIPersonalityType.Expansionist => 0.25f,
                AIPersonalityType.Isolationist => 0.05f,
                _ => 0.30f
            };

            // Bonus pour bonnes relations
            float relationBonus = Mathf.Max(0, relations / 200f);

            // Bonus pour ennemis communs
            float enemyBonus = commonEnemies * 0.10f;

            return Mathf.Clamp01(baseDesire + relationBonus + enemyBonus);
        }

        /// <summary>
        /// Retourne un texte de saveur décrivant la personnalité IA.
        /// Utilisé dans l'UI diplomatico.
        /// </summary>
        public static string GetPersonalityFlavorText(AIPersonalityType personality)
        {
            return personality switch
            {
                AIPersonalityType.Aggressive =>
                    "Ils ne connaissent que la langue du fer. Chaque traité n'est qu'une trêve en attendant la prochaine guerre.",

                AIPersonalityType.Diplomatic =>
                    "Leurs ambassadeurs parcourent le monde. Ils préfèrent un traité signé à une bataille gagnée.",

                AIPersonalityType.Opportunistic =>
                    "Ils écoutent les rumeurs de faiblesse comme d'autres comptent leur or. Leur loyauté suit le rapport de force.",

                AIPersonalityType.Commercial =>
                    "Leur drachme voyage plus loin que leurs armées. Leur véritable frontière est la dernière route commerciale établie.",

                AIPersonalityType.Expansionist =>
                    "Leurs colons sont déjà en marche avant que les traités soient secs. Chaque nouvelle frontière est une promesse.",

                AIPersonalityType.Isolationist =>
                    "Ils regardent vos envoyés depuis leurs remparts. Leur territoire est un sanctuaire, pas un marché.",

                _ => "Leurs intentions sont difficiles à déchiffrer."
            };
        }

        /// <summary>
        /// Retourne un nom lisible pour la personnalité.
        /// </summary>
        public static string GetPersonalityName(AIPersonalityType personality)
        {
            return personality switch
            {
                AIPersonalityType.Aggressive => "Agressif",
                AIPersonalityType.Diplomatic => "Diplomate",
                AIPersonalityType.Opportunistic => "Opportuniste",
                AIPersonalityType.Commercial => "Commercial",
                AIPersonalityType.Expansionist => "Expansionniste",
                AIPersonalityType.Isolationist => "Isolationniste",
                _ => "Inconnu"
            };
        }
    }
}
