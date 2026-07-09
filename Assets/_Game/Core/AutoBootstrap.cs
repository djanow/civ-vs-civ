using UnityEngine;

namespace CivVSCiv
{
    public static class AutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            if (GameManager.Instance != null) return;

            try
            {
                Debug.Log("[AutoBootstrap] Starting...");

                // GameManager
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();

                // Camera: reuse or create
                var cam = Camera.main;
                if (cam == null)
                {
                    var cgo = new GameObject("Main Camera");
                    cgo.tag = "MainCamera";
                    cam = cgo.AddComponent<Camera>();
                    Debug.Log("[AutoBootstrap] Created camera");
                }
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.1f, 0.15f, 0.25f); // night sky
                cam.orthographic = true;
                cam.orthographicSize = 20f; // see ~40 world units vertically (grid is 30 tall)
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 500f;
                cam.transform.position = new Vector3(30, 30, 26); // elevated isometric
                cam.transform.rotation = Quaternion.Euler(90, 0, 0);
                if (cam.GetComponent<CameraController>() == null)
                    cam.gameObject.AddComponent<CameraController>();
                Debug.Log("[AutoBootstrap] Camera set: ortho size=20, pos=" + cam.transform.position);

                // Light
                if (Object.FindAnyObjectByType<Light>() == null)
                {
                    var lg = new GameObject("Directional Light");
                    var l = lg.AddComponent<Light>();
                    l.type = LightType.Directional;
                    l.intensity = 1.2f;
                    l.transform.rotation = Quaternion.Euler(50, -30, 0);
                    Debug.Log("[AutoBootstrap] Created light");
                }

                // Debug cube at grid center (visible proof of life)
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Debug_Center";
                cube.transform.position = new Vector3(30, 0, 26);
                cube.transform.localScale = new Vector3(3, 0.5f, 3);
                var mr = cube.GetComponent<MeshRenderer>();
                if (mr != null) mr.material.color = Color.red;

                // HUD
                new GameObject("HUDManager").AddComponent<HUDManager>();

                // Fonde les villes de depart pour chaque joueur
                go.AddComponent<StartupFounder>();

                Debug.Log("[AutoBootstrap] Done. Carte + HUD prets.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AutoBootstrap] OnSceneLoaded failed: {e}");
            }
        }
    }
}
