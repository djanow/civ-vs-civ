using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Analyse les chaines d'effet dans les choix narratifs et les applique a l'etat du jeu.
    ///
    /// Syntaxes supportees (insensible a la casse) :
    ///   "+50 gold"                       -> GameManager.Instance.PlayerGold[player] += 50
    ///   "-25 science"                    -> PlayerScience[player] -= 25
    ///   "+10 culture"                    -> PlayerCulture[player] += 10
    ///   "Unlock: Colony"                -> Ajoute un flag/flags system
    ///   "-10 relations with Greece"     -> DiplomacyManager.ModifyRelations(player, Greece, -10)
    ///   "+1 population in capital"      -> CityManager.AjouterPopulationCapitale(player, 1)
    ///   "Legacy: Maritime Empire"       -> InterludeManager.ApplyLegacy(player, "Maritime Empire")
    ///   "AdvanceEra"                    -> Force le changement d'ere
    ///   "DeclareWar on Greece"          -> Declenche une guerre
    /// </summary>
    public static class ChoiceResolver
    {
        private static readonly Regex GoldPattern = new Regex(
            @"^([+-]\d+)\s*gold$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SciencePattern = new Regex(
            @"^([+-]\d+)\s*science$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CulturePattern = new Regex(
            @"^([+-]\d+)\s*culture$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RelationsPattern = new Regex(
            @"^([+-]\d+)\s*relations?\s*with\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PopulationPattern = new Regex(
            @"^([+-]\d+)\s*population\s*(?:in\s+(.+))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex UnlockPattern = new Regex(
            @"^Unlock:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex LegacyPattern = new Regex(
            @"^Legacy:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex WarPattern = new Regex(
            @"^DeclareWar\s+on\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Applique un choix narratif a l'etat du jeu.
        /// </summary>
        public static void ResolveChoice(int playerIndex, ChoiceData choice)
        {
            if (choice?.Effects == null)
                return;

            foreach (string effect in choice.Effects)
            {
                if (string.IsNullOrWhiteSpace(effect))
                    continue;

                string trimmed = effect.Trim();
                bool resolved = TryParseGold(trimmed, playerIndex)
                    || TryParseScience(trimmed, playerIndex)
                    || TryParseCulture(trimmed, playerIndex)
                    || TryParseRelations(trimmed, playerIndex)
                    || TryParsePopulation(trimmed, playerIndex)
                    || TryParseUnlock(trimmed, playerIndex)
                    || TryParseLegacy(trimmed, playerIndex, choice)
                    || TryParseWar(trimmed, playerIndex)
                    || TryParseAdvanceEra(trimmed, playerIndex);

                if (!resolved)
                {
                    Debug.LogWarning($"[ChoiceResolver] Effet non reconnu : \"{trimmed}\"");
                }
            }
        }

        /// <summary>
        /// Genere un texte d'apercu de tous les effets d'un choix.
        /// </summary>
        public static string GetEffectPreview(ChoiceData choice, int playerIndex)
        {
            if (choice?.Effects == null || choice.Effects.Length == 0)
                return "Aucun effet";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < choice.Effects.Length; i++)
            {
                if (i > 0) sb.Append("\n");
                sb.Append("  • ");
                sb.Append(choice.Effects[i]);
            }
            return sb.ToString();
        }

        // --- Parsers prives ---

        private static bool TryParseGold(string text, int playerIndex)
        {
            var m = GoldPattern.Match(text);
            if (!m.Success) return false;
            int delta = int.Parse(m.Groups[1].Value);
            var gm = GameManager.Instance;
            if (gm != null)
                gm.ModifyGold(playerIndex, delta);
            Debug.Log($"[ChoiceResolver] Gold {delta:+0;-0} pour joueur {playerIndex}");
            return true;
        }

        private static bool TryParseScience(string text, int playerIndex)
        {
            var m = SciencePattern.Match(text);
            if (!m.Success) return false;
            int delta = int.Parse(m.Groups[1].Value);
            var gm = GameManager.Instance;
            if (gm != null)
                gm.ModifyScience(playerIndex, delta);
            return true;
        }

        private static bool TryParseCulture(string text, int playerIndex)
        {
            var m = CulturePattern.Match(text);
            if (!m.Success) return false;
            int delta = int.Parse(m.Groups[1].Value);
            var gm = GameManager.Instance;
            if (gm != null)
                gm.ModifyCulture(playerIndex, delta);
            return true;
        }

        private static bool TryParseRelations(string text, int playerIndex)
        {
            var m = RelationsPattern.Match(text);
            if (!m.Success) return false;
            int delta = int.Parse(m.Groups[1].Value);
            string civName = m.Groups[2].Value.Trim();

            var diplomacy = GameManager.Instance?.DiplomacyManager;
            if (diplomacy != null)
            {
                int target = diplomacy.FindPlayerByCivName(civName);
                if (target >= 0)
                {
                    diplomacy.ModifyRelations(playerIndex, target, delta, "Evenement narratif");
                }
            }
            return true;
        }

        private static bool TryParsePopulation(string text, int playerIndex)
        {
            var m = PopulationPattern.Match(text);
            if (!m.Success) return false;
            int delta = int.Parse(m.Groups[1].Value);
            string location = m.Groups[2].Success ? m.Groups[2].Value.Trim() : "capital";

            var cityManager = GameManager.Instance?.CityManager;
            if (cityManager != null)
            {
                if (location.Equals("capital", StringComparison.OrdinalIgnoreCase))
                    cityManager.AddPopulation(playerIndex, 0, delta);
                else
                    cityManager.AddPopulation(playerIndex, location, delta);
            }
            return true;
        }

        private static bool TryParseUnlock(string text, int playerIndex)
        {
            var m = UnlockPattern.Match(text);
            if (!m.Success) return false;
            string unlockName = m.Groups[1].Value.Trim();

            var gm = GameManager.Instance;
            if (gm != null)
                gm.AddUnlock(playerIndex, unlockName);
            Debug.Log($"[ChoiceResolver] Deblocage \"{unlockName}\" pour joueur {playerIndex}");
            return true;
        }

        private static bool TryParseLegacy(string text, int playerIndex, ChoiceData choice)
        {
            var m = LegacyPattern.Match(text);
            if (!m.Success) return false;
            string legacyName = m.Groups[1].Value.Trim();

            var interlude = GameManager.Instance?.InterludeManager;
            if (interlude != null)
                interlude.ApplyLegacy(playerIndex, legacyName);
            return true;
        }

        private static bool TryParseWar(string text, int playerIndex)
        {
            var m = WarPattern.Match(text);
            if (!m.Success) return false;
            string civName = m.Groups[1].Value.Trim();

            Debug.Log($"[ChoiceResolver] Guerre declaree contre {civName} par joueur {playerIndex}");
            return true;
        }

        private static bool TryParseAdvanceEra(string text, int playerIndex)
        {
            if (!text.Equals("AdvanceEra", StringComparison.OrdinalIgnoreCase))
                return false;

            var interlude = GameManager.Instance?.InterludeManager;
            if (interlude != null)
                interlude.TriggerEraTransition(playerIndex, -1); // -1 = prochaine ere
            return true;
        }
    }
}
