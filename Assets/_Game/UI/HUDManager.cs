using UnityEngine;
using UnityEngine.UI;

namespace CivVSCiv
{
    /// <summary>
    /// HUD minimal : tour, joueur, ressources, bouton fin de tour.
    /// Auto-créé au démarrage.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        private GameObject _hudCanvas;
        private Text _turnText;
        private Text _resourcesText;
        private Text _phaseText;
        private Button _endTurnBtn;

        private void Awake()
        {
            EventBus.Subscribe<GameEvents.TurnPhaseChanged>(OnPhaseChanged);
            EventBus.Subscribe<GameEvents.PlayerTurnStarted>(OnTurnStarted);
            CreateHUD();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.TurnPhaseChanged>(OnPhaseChanged);
            EventBus.Unsubscribe<GameEvents.PlayerTurnStarted>(OnTurnStarted);
        }

        private void CreateHUD()
        {
            // Find existing canvas or create
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var cGo = new GameObject("HUDCanvas");
                canvas = cGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cGo.AddComponent<GraphicRaycaster>();
            }

            _hudCanvas = canvas.gameObject;

            // Top bar background
            var bg = new GameObject("HUDBackground", typeof(Image));
            bg.transform.SetParent(canvas.transform, false);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.93f);
            bgRT.anchorMax = new Vector2(1, 1);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            // Turn text
            _turnText = CreateText("TurnText", bg.transform, "Tour 1 — Phénicie", 28);
            var ttRT = _turnText.GetComponent<RectTransform>();
            ttRT.anchorMin = new Vector2(0.01f, 0);
            ttRT.anchorMax = new Vector2(0.4f, 1);
            ttRT.offsetMin = ttRT.offsetMax = Vector2.zero;

            // Resources text
            _resourcesText = CreateText("ResText", bg.transform, "⭐ 100 | 🔬 3 | 🏛 1", 22);
            var rtRT = _resourcesText.GetComponent<RectTransform>();
            rtRT.anchorMin = new Vector2(0.41f, 0);
            rtRT.anchorMax = new Vector2(0.7f, 1);
            rtRT.offsetMin = rtRT.offsetMax = Vector2.zero;

            // End Turn button
            var btnGo = new GameObject("EndTurnBtn", typeof(Image), typeof(Button));
            btnGo.transform.SetParent(bg.transform, false);
            var btnRT = btnGo.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.85f, 0.15f);
            btnRT.anchorMax = new Vector2(0.98f, 0.85f);
            btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;
            btnGo.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f);

            var btnLabel = CreateText("BtnLabel", btnGo.transform, "Fin de tour", 20);
            var blRT = btnLabel.GetComponent<RectTransform>();
            blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one;
            blRT.offsetMin = blRT.offsetMax = Vector2.zero;
            btnLabel.alignment = TextAnchor.MiddleCenter;

            _endTurnBtn = btnGo.GetComponent<Button>();
            _endTurnBtn.onClick.AddListener(OnEndTurnClicked);

            // Phase text (center of screen, semi-transparent)
            _phaseText = CreateText("PhaseText", canvas.transform, "", 36);
            var ptRT = _phaseText.GetComponent<RectTransform>();
            ptRT.anchorMin = new Vector2(0.3f, 0.45f);
            ptRT.anchorMax = new Vector2(0.7f, 0.55f);
            ptRT.offsetMin = ptRT.offsetMax = Vector2.zero;
            _phaseText.alignment = TextAnchor.MiddleCenter;
            _phaseText.color = new Color(1, 1, 1, 0.8f);
            _phaseText.gameObject.SetActive(false);
        }

        private Text CreateText(string name, Transform parent, string content, int fontSize)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.raycastTarget = false;
            return txt;
        }

        private void OnPhaseChanged(GameEvents.TurnPhaseChanged evt)
        {
            string[] phaseNames = { "Événement", "Mouvement", "Ville", "Diplomatie", "Recherche", "Fin de tour" };
            string pName = (int)evt.Phase < phaseNames.Length ? phaseNames[(int)evt.Phase] : "";
            _phaseText.text = pName;
            _phaseText.gameObject.SetActive(true);
            Invoke(nameof(HidePhaseText), 1.5f);
        }

        private void HidePhaseText() { if (_phaseText != null) _phaseText.gameObject.SetActive(false); }

        private void OnTurnStarted(GameEvents.PlayerTurnStarted evt)
        {
            var civs = new[] { "Phénicie", "Grèce" };
            var gm = GameManager.Instance;
            string civName = evt.PlayerIndex < civs.Length ? civs[evt.PlayerIndex] : $"J{evt.PlayerIndex}";
            int gold = gm != null ? gm.GetPlayerGold(evt.PlayerIndex) : 0;
            int sci = gm != null ? gm.GetPlayerScience(evt.PlayerIndex) : 0;
            int cult = gm != null ? gm.GetPlayerCulture(evt.PlayerIndex) : 0;
            int turn = gm?.TurnManager?.CurrentTurn ?? 1;
            _turnText.text = $"Tour {turn} — {civName}";
            _resourcesText.text = $"⭐ {gold} | 🔬 {sci} | 🏛 {cult}";
        }

        private void OnEndTurnClicked()
        {
            var tm = GameManager.Instance?.TurnManager;
            if (tm != null) tm.EndTurn();
        }
    }
}
