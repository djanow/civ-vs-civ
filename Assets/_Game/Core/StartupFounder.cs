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

            bool ok = false;
            try { ok = InitializeGame(); }
            catch (System.Exception e) { Debug.LogError($"[Startup] Initialization failed: {e}"); }
            if (ok) Debug.Log("[Startup] Initialisation terminee. Pret a jouer !");
        }

        private bool InitializeGame()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Debug.LogError("[Startup] GameManager.Instance is null"); return false; }
            if (gm.CurrentState != GameState.Playing) { Debug.LogWarning("[Startup] Game not in Playing state"); return false; }

            var cm = gm.CityManager;
            var um = gm.UnitManager;
            if (cm == null || um == null) { Debug.LogError("[Startup] CityManager or UnitManager is null"); return false; }

            Debug.Log($"[Startup] Found {um.AllUnits.Count} units on map");

            var civs = new[] { "Tyr", "Athènes" };
            for (int i = 0; i < 2 && i < civs.Length; i++)
            {
                var startPos = FindStartPosition(gm, i);
                if (startPos != null)
                {
                    var city = cm.AddCity(civs[i], i, startPos.Coordinates, true);
                    if (city != null)
                        Debug.Log($"[Startup] Ville fondee: {civs[i]} a {startPos.Coordinates} pour joueur {i}");
                }
                else
                    Debug.LogError($"[Startup] Aucune position valide pour joueur {i}");
            }

            MakeUnitsVisible(um);



            return true;
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
                    sphere.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

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
