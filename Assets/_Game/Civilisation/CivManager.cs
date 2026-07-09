using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Gère les données des civilisations et des leaders pour la session en cours.
    /// </summary>
    public class CivManager : MonoBehaviour
    {
        [Header("Références aux données")]
        [SerializeField] private CivilizationData[] _availableCivs;

        [Header("Données des civilisations (auto-créées si null)")]
        [SerializeField] private CivilizationData _phoeniciaData;
        [SerializeField] private CivilizationData _greeceData;

        /// <summary>CivData indexée par playerIndex.</summary>
        private CivilizationData[] _playerCivs;

        /// <summary>Ère actuelle par joueur.</summary>
        private int[] _playerEras;

        /// <summary>Legs accumulés par joueur (noms).</summary>
        private List<string>[] _playerLegacies;

        /// <summary>Leader actuel par joueur.</summary>
        private int[] _playerLeaderIndices;

        private void Awake()
        {
            _playerCivs = new CivilizationData[0];
            _playerEras = new int[0];
            _playerLegacies = new List<string>[0];
            _playerLeaderIndices = new int[0];

            // Auto-création des données de civilisation si non assignées
            if (_phoeniciaData == null)
                _phoeniciaData = CreatePhoeniciaData();
            if (_greeceData == null)
                _greeceData = CreateGreeceData();

            if (_availableCivs == null || _availableCivs.Length == 0)
                _availableCivs = new[] { _phoeniciaData, _greeceData };
        }

        private static CivilizationData CreatePhoeniciaData()
        {
            var civ = ScriptableObject.CreateInstance<CivilizationData>();
            civ.CivId = 0;
            civ.CivName = "Phénicie";
            civ.CivDescription = "Enfants de la Mer — Maîtres de la Méditerranée, les Phéniciens règnent par le commerce et la navigation.";
            civ.SignatureBonusName = "Routes Maritimes";
            civ.SignatureBonusDescription = "Les routes commerciales maritimes rapportent +50% d'or et les ports sont construits deux fois plus vite.";
            civ.name = "PhoeniciaData_Auto";

            civ.Leaders = new LeaderData[3];
            civ.Leaders[0] = CreateLeader("Hiram Ier", 0, "Commerce Phénicien",
                "Les routes commerciales rapportent +1 or supplémentaire par case côtière.",
                "Héritage de Tyr", "Les ports phéniciens deviennent des centres culturels majeurs.");
            civ.Leaders[1] = CreateLeader("Didon", 1, "Fondatrice Légendaire",
                "Les nouvelles colonies commencent avec un bâtiment culturel gratuit.",
                "Héritage de Carthage", "Carthage devient une puissance maritime inégalée.");
            civ.Leaders[2] = CreateLeader("Hannon", 2, "Explorateur Infatigable",
                "Les navires de guerre gagnent +2 de portée de vision et peuvent explorer sans pénalité en eaux profondes.",
                "Héritage d'Hannon", "Les routes commerciales maritimes s'étendent à travers le monde connu.");
            return civ;
        }

        private static CivilizationData CreateGreeceData()
        {
            var civ = ScriptableObject.CreateInstance<CivilizationData>();
            civ.CivId = 1;
            civ.CivName = "Grèce";
            civ.CivDescription = "Polis — Berceau de la démocratie, la Grèce rayonne par sa culture, sa philosophie et ses cités-états indépendantes.";
            civ.SignatureBonusName = "Héritage des Cités-États";
            civ.SignatureBonusDescription = "Les cités-états alliées fournissent des bonus culturels et scientifiques accrus. +15% culture dans toutes les villes.";
            civ.name = "GreeceData_Auto";

            civ.Leaders = new LeaderData[3];
            civ.Leaders[0] = CreateLeader("Leonidas", 0, "Guerrier Spartiate",
                "Les unités militaires reçoivent +1 de défense et +15% de bonus en combat défensif.",
                "Héritage des Thermopyles", "La bravoure spartiate inspire les générations futures, améliorant la défense de toutes les villes.");
            civ.Leaders[1] = CreateLeader("Périclès", 1, "Siècle de Périclès",
                "Les bâtiments culturels et les merveilles sont construits 15% plus vite. +1 culture par tour et par cité.",
                "Héritage Athénien", "Athènes devient le phare culturel du monde antique, attirant artistes et philosophes.");
            civ.Leaders[2] = CreateLeader("Alexandre le Grand", 2, "Conquérant du Monde",
                "Les unités militaires gagnent +2 de mouvement et +25% en combat offensif. Conquérir une ville ennemie octroie de la culture.",
                "Héritage d'Alexandre", "L'empire hellénistique s'étend aux confins du monde, diffusant la culture grecque sur tous les continents.");
            return civ;
        }

        private static LeaderData CreateLeader(string name, int era, string bonusName, string bonusDesc, string legacyName, string legacyDesc)
        {
            var leader = ScriptableObject.CreateInstance<LeaderData>();
            leader.LeaderName = name;
            leader.Era = era;
            leader.EraBonusName = bonusName;
            leader.EraBonusDescription = bonusDesc;
            leader.LegacyName = legacyName;
            leader.LegacyDescription = legacyDesc;
            leader.name = $"Leader_{name}_Auto";
            return leader;
        }

        /// <summary>Configure les civilisations initiales pour chaque joueur.</summary>
        public void InitializePlayers(int playerCount, CivilizationData[] civAssignments)
        {
            _playerCivs = new CivilizationData[playerCount];
            _playerEras = new int[playerCount];
            _playerLegacies = new List<string>[playerCount];
            _playerLeaderIndices = new int[playerCount];

            for (int i = 0; i < playerCount; i++)
            {
                _playerCivs[i] = civAssignments[i];
                _playerEras[i] = 0; // Démarrer à l'Antiquité
                _playerLegacies[i] = new List<string>();
                _playerLeaderIndices[i] = 0; // Premier leader
            }
        }

        /// <summary>Données de la civilisation d'un joueur.</summary>
        public CivilizationData GetCivData(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerCivs.Length)
                return null;
            return _playerCivs[playerIndex];
        }

        /// <summary>Leader actuel d'un joueur.</summary>
        public LeaderData GetCurrentLeader(int playerIndex)
        {
            var civ = GetCivData(playerIndex);
            if (civ == null) return null;

            int idx = _playerLeaderIndices[playerIndex];
            if (idx < 0 || idx >= civ.Leaders.Length)
                return null;
            return civ.Leaders[idx];
        }

        /// <summary>Ère actuelle d'un joueur.</summary>
        public int GetPlayerEra(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerEras.Length)
                return 0;
            return _playerEras[playerIndex];
        }

        /// <summary>
        /// Passe à l'ère suivante pour un joueur.
        /// Retourne le legacy du leader sortant, ou null.
        /// </summary>
        public string AdvanceEra(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerEras.Length)
                return null;

            var previousLeader = GetCurrentLeader(playerIndex);
            _playerEras[playerIndex]++;

            // Avancer l'index du leader
            if (_playerLeaderIndices[playerIndex] < _playerCivs[playerIndex].Leaders.Length - 1)
            {
                _playerLeaderIndices[playerIndex]++;
            }

            // Enregistrer le legacy
            if (previousLeader != null && !string.IsNullOrEmpty(previousLeader.LegacyName))
            {
                _playerLegacies[playerIndex].Add(previousLeader.LegacyName);
                return previousLeader.LegacyName;
            }

            return null;
        }

        /// <summary>Legs accumulés pour un joueur.</summary>
        public string[] GetPlayerLegacies(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerLegacies.Length)
                return System.Array.Empty<string>();
            return _playerLegacies[playerIndex].ToArray();
        }

        /// <summary>Ajoute un legacy à un joueur (pour les choix narratifs).</summary>
        public void AddLegacy(int playerIndex, string legacyName)
        {
            if (playerIndex < 0 || playerIndex >= _playerLegacies.Length)
                return;
            if (!_playerLegacies[playerIndex].Contains(legacyName))
            {
                _playerLegacies[playerIndex].Add(legacyName);
            }
        }

        /// <summary>Toutes les civs disponibles.</summary>
        public CivilizationData[] GetAllCivs() => _availableCivs;
    }
}
