using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Donnees de l'arbre technologique. Configure les techs disponibles
    /// pour toutes les civilisations, organisees par ere.
    /// </summary>
    [CreateAssetMenu(fileName = "TechTreeData", menuName = "CivVSCiv/Tech Tree Data")]
    public class TechTreeData : ScriptableObject
    {
        [SerializeField] private TechNodeData[] _techNodes;

        public TechNodeData[] TechNodes => _techNodes;

        /// <summary>
        /// Retourne une tech par son ID.
        /// </summary>
        public TechNodeData GetTech(int techId)
        {
            if (_techNodes == null) return default;

            for (int i = 0; i < _techNodes.Length; i++)
            {
                if (_techNodes[i].TechId == techId)
                    return _techNodes[i];
            }

            Debug.LogWarning($"[TechTreeData] Tech ID {techId} not found.");
            return default;
        }

        /// <summary>
        /// Retourne toutes les techs d'une ere donnee.
        /// </summary>
        public TechNodeData[] GetTechsByEra(int era)
        {
            if (_techNodes == null) return new TechNodeData[0];

            int count = 0;
            for (int i = 0; i < _techNodes.Length; i++)
            {
                if (_techNodes[i].Era == era)
                    count++;
            }

            var result = new TechNodeData[count];
            int idx = 0;
            for (int i = 0; i < _techNodes.Length; i++)
            {
                if (_techNodes[i].Era == era)
                {
                    result[idx] = _techNodes[i];
                    idx++;
                }
            }

            return result;
        }

        /// <summary>
        /// Valide l'integrite de l'arbre tech (pas de cycles, prerequis existants).
        /// Retourne true si valide.
        /// </summary>
        public bool Validate()
        {
            if (_techNodes == null || _techNodes.Length == 0)
            {
                Debug.LogError("[TechTreeData] No tech nodes defined.");
                return false;
            }

            bool valid = true;

            // Verifier que tous les IDs sont uniques
            for (int i = 0; i < _techNodes.Length; i++)
            {
                for (int j = i + 1; j < _techNodes.Length; j++)
                {
                    if (_techNodes[i].TechId == _techNodes[j].TechId)
                    {
                        Debug.LogError($"[TechTreeData] Duplicate TechId: {_techNodes[i].TechId}.");
                        valid = false;
                    }
                }
            }

            // Verifier que les prerequis existent et ne creent pas de cycles
            for (int i = 0; i < _techNodes.Length; i++)
            {
                var node = _techNodes[i];
                if (node.PrerequisiteIds == null) continue;

                for (int p = 0; p < node.PrerequisiteIds.Length; p++)
                {
                    int prereqId = node.PrerequisiteIds[p];
                    bool found = false;
                    for (int j = 0; j < _techNodes.Length; j++)
                    {
                        if (_techNodes[j].TechId == prereqId)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Debug.LogError($"[TechTreeData] Tech '{node.TechName}' (ID {node.TechId}) " +
                            $"references missing prerequisite ID {prereqId}.");
                        valid = false;
                    }
                }
            }

            if (valid)
            {
                Debug.Log($"[TechTreeData] Validation OK: {_techNodes.Length} tech nodes.");
            }

            return valid;
        }
    }

    /// <summary>
    /// Donnees d'un noeud technologique individuel.
    /// </summary>
    [System.Serializable]
    public struct TechNodeData
    {
        [Tooltip("Identifiant unique de la tech.")]
        public int TechId;

        [Tooltip("Nom affiche de la tech.")]
        public string TechName;

        [Tooltip("Ere : 0=Antiquite, 1=Classique, 2=Medievale")]
        public int Era;

        [Tooltip("Cout en points de science.")]
        public int ScienceCost;

        [Tooltip("IDs des techs requises avant de pouvoir rechercher celle-ci.")]
        public int[] PrerequisiteIds;

        [Tooltip("Description textuelle.")]
        public string Description;

        [Tooltip("Ce que cette tech debloque (unites, batiments, ameliorations).")]
        public string[] Unlocks;

        [Tooltip("Si vrai, terminer cette tech permet de passer a l'ere suivante.")]
        public bool IsEraGate;
    }
}
