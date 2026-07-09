using UnityEngine;
using System.Collections;

namespace CivVSCiv
{
    public class StartupFounder : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1.5f);
            bool ok = false;
            try { ok = InitializeGame(); }
            catch (System.Exception e) { Debug.LogError($"[Startup] Initialization failed: {e}"); }
            if (ok) Debug.Log("[Startup] Pret a jouer !");
        }

        private bool InitializeGame()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Debug.LogError("[Startup] GameManager null"); return false; }
            if (gm.CurrentState != GameState.Playing) { Debug.LogWarning("[Startup] Not playing"); return false; }

            var cm = gm.CityManager;
            var um = gm.UnitManager;
            if (cm == null || um == null) { Debug.LogError("[Startup] Managers null"); return false; }

            Debug.Log($"[Startup] {um.AllUnits.Count} units on map");

            var civs = new[] { "Tyr", "Athenes" };
            for (int i = 0; i < 2 && i < civs.Length; i++)
            {
                var startPos = FindStartPosition(gm, i);
                if (startPos != null)
                {
                    var city = cm.AddCity(civs[i], i, startPos.Coordinates, true);
                    if (city != null)
                        Debug.Log($"[Startup] {civs[i]} fondee pour joueur {i}");
                }
                else
                    Debug.LogError($"[Startup] Pas de position pour joueur {i}");
            }

            MakeUnitsVisible(um);
            return true;
        }

        private HexCell FindStartPosition(GameManager gm, int playerIndex)
        {
            for (int attempt = 0; attempt < 200; attempt++)
            {
                int x = playerIndex == 0
                    ? Random.Range(3, gm.Width / 3)
                    : Random.Range(gm.Width * 2 / 3, gm.Width - 3);
                int y = Random.Range(3, gm.Height - 3);
                var cell = gm.Cells[x, y];
                if (cell.MovementCost > 0 && cell.TileType != TileType.Mountain
                    && cell.TileType != TileType.Desert && cell.TileType != TileType.Marsh
                    && cell.TileType != TileType.Ice)
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
                if (unit.transform.childCount > 0) continue;

                string name = unit.UnitName.ToLower();
                if (name.Contains("guerrier") || name.Contains("warrior") || name.Contains("phalange"))
                    UnitVisuals.CreateWarrior(unit.transform, unit.OwnerIndex);
                else if (name.Contains("cavalier") || name.Contains("cavalry") || name.Contains("char"))
                    UnitVisuals.CreateCavalry(unit.transform, unit.OwnerIndex);
                else if (name.Contains("bateau") || name.Contains("ship") || name.Contains("trière") || name.Contains("birème"))
                    UnitVisuals.CreateShip(unit.transform, unit.OwnerIndex);
                else
                    UnitVisuals.CreateScout(unit.transform, unit.OwnerIndex);

                // Label
                var label = new GameObject("Label");
                label.transform.SetParent(unit.transform, false);
                label.transform.localPosition = new Vector3(0, 1.2f, 0);
                var tm = label.AddComponent<TextMesh>();
                tm.text = unit.UnitName;
                tm.fontSize = 24;
                tm.color = unit.OwnerIndex == 0 ? new Color(0.8f, 0.5f, 1f) : new Color(0.5f, 0.7f, 1f);
                tm.alignment = TextAlignment.Center;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.characterSize = 0.2f;

                var renderer = FindAnyObjectByType<HexGridRenderer>();
                if (renderer != null)
                {
                    var pos = renderer.HexToWorld(unit.Position);
                    unit.transform.position = new Vector3(pos.x, 0, pos.z);
                }
            }
        }
    }
}
