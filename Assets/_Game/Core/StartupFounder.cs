using UnityEngine;
using System.Collections;

namespace CivVSCiv
{
    /// <summary>
    /// Fonde automatiquement les villes de depart et rend les unites visibles.
    /// Revele aussi le brouillard de guerre autour des positions de depart.
    /// </summary>
    public class StartupFounder : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Attendre que tout soit initialise (GameManager, generateur, spawn des unites)
            yield return new WaitForSeconds(1.5f);

            try
            {
                var gm = GameManager.Instance;
                if (gm == null)
                {
                    Debug.LogError("[Startup] GameManager.Instance is null");
                    yield break;
                }

                // Attendre que la carte soit generee
                int safetyCounter = 0;
                while (gm.CurrentState != GameState.Playing && safetyCounter < 100)
                {
                    yield return null;
                    safetyCounter++;
                }
                if (gm.CurrentState != GameState.Playing)
                {
                    Debug.LogError("[Startup] Timeout waiting for GameState.Playing");
                    yield break;
                }

                yield return null;

                var cm = gm.CityManager;
                var um = gm.UnitManager;
                if (cm == null || um == null)
                {
                    Debug.LogError("[Startup] CityManager or UnitManager is null");
                    yield break;
                }

                Debug.Log($"[Startup] Found {um.AllUnits.Count} units on map");

                // Recuperer les positions de depart depuis le generateur
                var civs = new[] { "Tyr", "Athènes" };
                for (int i = 0; i < 2 && i < civs.Length; i++)
                {
                    // Chercher un emplacement valide pres du centre gauche/droit
                    var startPos = FindStartPosition(gm, i);
                    if (startPos != null)
                    {
                        var city = cm.AddCity(civs[i], i, startPos.Coordinates, true);
                        if (city != null)
                            Debug.Log($"[Startup] Ville fondee: {civs[i]} a {startPos.Coordinates} pour joueur {i}");
                        else
                            Debug.LogWarning($"[Startup] Impossible de fonder {civs[i]} a {startPos.Coordinates}");
                    }
                    else
                    {
                        Debug.LogError($"[Startup] Aucune position valide trouvee pour le joueur {i}");
                    }
                }

                // Rendre les unites visibles (spheres de couleur)
                MakeUnitsVisible(um);

                // Reveler le brouillard de guerre autour des unites de chaque joueur
                var fogRenderer = FindAnyObjectByType<FogOfWarRenderer>();
                if (fogRenderer != null && um != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        um.UpdatePlayerVisibility(i);
                    }
                    fogRenderer.UpdateAllFogQuads();
                    Debug.Log("[Startup] Brouillard de guerre mis a jour pour les 2 joueurs");
                }

                Debug.Log("[Startup] Initialisation terminee. Pret a jouer !");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Startup] Initialization failed: {e}");
            }
        }

        private HexCell FindStartPosition(GameManager gm, int playerIndex)
        {
            // Cherche une position valide : plaine/colline, pas d'eau
            for (int attempt = 0; attempt < 200; attempt++)
            {
                int x = playerIndex == 0
                    ? Random.Range(3, gm.Width / 3)
                    : Random.Range(gm.Width * 2 / 3, gm.Width - 3);
                int y = Random.Range(3, gm.Height - 3);

                var cell = gm.Cells[x, y];
                if (cell.MovementCost > 0 && cell.TileType != TileType.Mountain
                    && cell.TileType != TileType.Desert && cell.TileType != TileType.Marsh)
                {
                    cell.OwnerIndex = playerIndex;
                    return cell;
                }
            }
            return null;
        }

        private void MakeUnitsVisible(UnitManager um)
        {
            foreach (var unit in um.AllUnits)
            {
                if (unit == null) continue;
                // Remplacer le prefab manquant par un GameObject visible
                if (unit.GetComponent<MeshRenderer>() == null)
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = unit.UnitName;
                    sphere.transform.SetParent(unit.transform);
                    sphere.transform.localPosition = Vector3.zero;
                    sphere.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

                    // Couleur par joueur
                    var mr = sphere.GetComponent<MeshRenderer>();
                    var mat = new Material(Shader.Find("Standard"));
                    mat.color = unit.OwnerIndex == 0
                        ? new Color(0.6f, 0.2f, 0.8f)  // Violet (Phenicie)
                        : new Color(0.2f, 0.5f, 0.9f);  // Bleu (Grece)
                    mr.sharedMaterial = mat;

                    // Positionner l'unite sur la carte
                    var renderer = FindAnyObjectByType<HexGridRenderer>();
                    if (renderer != null)
                    {
                        var pos = renderer.HexToWorld(unit.Position);
                        unit.transform.position = new Vector3(pos.x, 0.5f, pos.z);
                    }
                }
            }
        }
    }
}
