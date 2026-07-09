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
                
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();

                var cam = Camera.main;
                if (cam == null) { var cgo = new GameObject("Main Camera"); cgo.tag = "MainCamera"; cam = cgo.AddComponent<Camera>(); }
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.08f, 0.15f);
                cam.orthographic = false;
                cam.fieldOfView = 60f;
                cam.nearClipPlane = 0.1f; cam.farClipPlane = 500f;
                cam.transform.position = new Vector3(30, 28.7f, 46.1f);
                cam.transform.rotation = Quaternion.identity;
                if (cam.GetComponent<CameraController>() == null)
                {
                    var cc = cam.gameObject.AddComponent<CameraController>();
                    // Reinitialiser la vue globe dans CameraController via les valeurs par defaut du component
                }

                if (Object.FindAnyObjectByType<Light>() == null)
                {
                    var lg = new GameObject("Directional Light");
                    var l = lg.AddComponent<Light>();
                    l.type = LightType.Directional; l.intensity = 1.2f;
                    l.transform.rotation = Quaternion.Euler(50, -30, 0);
                }

                new GameObject("HUDManager").AddComponent<HUDManager>();
                go.AddComponent<StartupFounder>();

                Debug.Log("[AutoBootstrap] Done.");
            }
            catch (System.Exception e) { Debug.LogError($"[AutoBootstrap] Failed: {e}"); }
        }
    }
}
