using UnityEngine;
using UnityEngine.SceneManagement;

namespace CivVSCiv
{
    /// <summary>
    /// Démarre automatiquement le jeu sans scène pré-configurée.
    /// Crée un GameManager au chargement de n'importe quelle scène.
    /// </summary>
    public static class AutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            if (GameManager.Instance != null) return;

            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();

            // Camera setup if missing
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            cam.transform.position = new Vector3(30, 40, 26);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.gameObject.AddComponent<CameraController>();

            // Directional light if missing
            if (FindAnyObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            Debug.Log("[AutoBootstrap] Scène initialisée automatiquement.");
        }
    }
}
