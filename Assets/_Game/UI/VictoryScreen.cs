using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CivVSCiv
{
    /// <summary>
    /// Écran de victoire plein écran avec overlay sombre, texte doré,
    /// boutons Rejouer / Quitter et animation d'apparition.
    /// </summary>
    public class VictoryScreen : MonoBehaviour
    {
        private GameObject _overlay;

        /// <summary>
        /// Affiche l'écran de victoire pour le gagnant donné.
        /// </summary>
        public void ShowVictory(int winnerIndex, string civName)
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Overlay plein écran noir 85% alpha
            _overlay = new GameObject("VictoryOverlay", typeof(Image));
            _overlay.transform.SetParent(canvas.transform, false);

            var rt = _overlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = _overlay.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0.85f);

            // Animation d'entrée : scale de 1.2 à 1.0
            _overlay.transform.localScale = Vector3.one * 1.2f;
            StartCoroutine(FadeIn(_overlay, 0.5f));

            // Titre "VICTOIRE !" en doré
            var title = CreateText("VictoryTitle", _overlay.transform, "VICTOIRE !", 72,
                new Color(1f, 0.85f, 0.3f));
            var titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 0.55f);
            titleRT.anchorMax = new Vector2(1, 0.7f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;
            title.alignment = TextAnchor.MiddleCenter;

            // Sous-titre
            var subtitle = CreateText("VictorySubtitle", _overlay.transform,
                $"La {civName} a conquis le monde !", 36, Color.white);
            var subRT = subtitle.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0, 0.38f);
            subRT.anchorMax = new Vector2(1, 0.48f);
            subRT.offsetMin = Vector2.zero;
            subRT.offsetMax = Vector2.zero;
            subtitle.alignment = TextAnchor.MiddleCenter;

            // Bouton Rejouer
            var replayBtn = CreateButton("RejouerBtn", _overlay.transform,
                "Rejouer", new Color(0.2f, 0.5f, 0.25f));
            var playRT = replayBtn.GetComponent<RectTransform>();
            playRT.anchorMin = new Vector2(0.35f, 0.20f);
            playRT.anchorMax = new Vector2(0.65f, 0.30f);
            playRT.offsetMin = Vector2.zero;
            playRT.offsetMax = Vector2.zero;
            replayBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (_overlay != null) Destroy(_overlay);
                GameManager.Instance?.StartNewGame();
            });

            // Bouton Quitter
            var quitBtn = CreateButton("QuitterBtn", _overlay.transform,
                "Quitter", new Color(0.5f, 0.2f, 0.2f));
            var quitRT = quitBtn.GetComponent<RectTransform>();
            quitRT.anchorMin = new Vector2(0.35f, 0.10f);
            quitRT.anchorMax = new Vector2(0.65f, 0.18f);
            quitRT.offsetMin = Vector2.zero;
            quitRT.offsetMax = Vector2.zero;
            quitBtn.GetComponent<Button>().onClick.AddListener(Application.Quit);
        }

        /// <summary>
        /// Animation de scale de 1.2 à 1.0 sur la durée donnée.
        /// </summary>
        private static IEnumerator FadeIn(GameObject go, float duration)
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 1.2f;
            Vector3 endScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                go.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            go.transform.localScale = endScale;
        }

        private static Text CreateText(string name, Transform parent, string content,
            int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.raycastTarget = false;
            return txt;
        }

        private static GameObject CreateButton(string name, Transform parent,
            string label, Color color)
        {
            var btnGo = new GameObject(name, typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            btnGo.GetComponent<Image>().color = color;

            var txt = new GameObject("Label", typeof(Text));
            txt.transform.SetParent(btnGo.transform, false);
            var txtRT = txt.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

            var textComp = txt.GetComponent<Text>();
            textComp.text = label;
            textComp.fontSize = 28;
            textComp.color = Color.white;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.raycastTarget = false;

            return btnGo;
        }
    }
}
