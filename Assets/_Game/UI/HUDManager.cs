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
        private Button _produireBtn;
        private Button _rechercherBtn;

        // Start screen
        private GameObject _startScreen;
        private bool _gameStarted;

        private void Awake()
        {
            EventBus.Subscribe<GameEvents.TurnPhaseChanged>(OnPhaseChanged);
            EventBus.Subscribe<GameEvents.PlayerTurnStarted>(OnTurnStarted);
            CreateHUD();
            CreateStartScreen();
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

            // "Rechercher" button (leftmost on right side)
            _rechercherBtn = CreateTopBarButton("RechercherBtn", bg.transform, 0.60f, 0.72f,
                "Rechercher", 16, new Color(0.3f, 0.35f, 0.7f));
            _rechercherBtn.onClick.AddListener(OnRechercherClicked);

            // "Produire" button (middle on right side)
            _produireBtn = CreateTopBarButton("ProduireBtn", bg.transform, 0.73f, 0.84f,
                "Produire", 16, new Color(0.6f, 0.5f, 0.2f));
            _produireBtn.onClick.AddListener(OnProduireClicked);

            // End Turn button (rightmost)
            var btnGo = new GameObject("EndTurnBtn", typeof(Image), typeof(Button));
            btnGo.transform.SetParent(bg.transform, false);
            var btnRT = btnGo.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.86f, 0.15f);
            btnRT.anchorMax = new Vector2(0.98f, 0.85f);
            btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;
            btnGo.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f);

            var btnLabel = CreateText("BtnLabel", btnGo.transform, "Fin de tour", 18);
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

        /// <summary>
        /// Crée un bouton dans la barre du haut avec les ancres spécifiées.
        /// </summary>
        private static Button CreateTopBarButton(string name, Transform parent,
            float anchorMinX, float anchorMaxX, string label, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, 0.15f);
            rt.anchorMax = new Vector2(anchorMaxX, 0.85f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;

            var txt = new GameObject("Label", typeof(Text));
            txt.transform.SetParent(go.transform, false);
            var txtRT = txt.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;

            var textComp = txt.GetComponent<Text>();
            textComp.text = label;
            textComp.fontSize = fontSize;
            textComp.color = Color.white;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        /// <summary>
        /// Ouvre l'arbre technologique pour le joueur humain.
        /// </summary>
        private void OnRechercherClicked()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            int currentPlayer = gm.TurnManager?.CurrentPlayerIndex ?? -1;
            if (currentPlayer < 0) return;

            // Find or create the TechTreeUI
            var techTree = FindAnyObjectByType<TechTreeUI>();
            if (techTree == null)
            {
                var ttGo = new GameObject("TechTreeUI");
                techTree = ttGo.AddComponent<TechTreeUI>();
            }
            techTree.Show(currentPlayer);
        }

        /// <summary>
        /// Ouvre le panneau de production pour la première cité du joueur.
        /// </summary>
        private void OnProduireClicked()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            int currentPlayer = gm.TurnManager?.CurrentPlayerIndex ?? -1;
            if (currentPlayer < 0) return;

            var cities = gm.CityManager?.GetPlayerCities(currentPlayer);
            if (cities == null || cities.Count == 0) return;

            var firstCity = cities[0];
            var runtimeCities = gm.CityManager?.GetRuntimeCities();
            City city = null;
            if (runtimeCities != null)
            {
                foreach (var rc in runtimeCities)
                {
                    if (rc.CityName == firstCity.CityName)
                    {
                        city = rc;
                        break;
                    }
                }
            }
            if (city == null) city = new City(firstCity);

            var inputHandler = FindAnyObjectByType<InputHandler>();
            if (inputHandler != null)
                inputHandler.EnsureCityPanel();

            var cityPanel = FindAnyObjectByType<CityPanel>(true);
            if (cityPanel != null)
                cityPanel.Show(city);
        }

        // ----------------------------------------------------------------
        // Start screen
        // ----------------------------------------------------------------

        /// <summary>
        /// Crée l'écran titre "CIV VS CIV" avec bouton Jouer.
        /// </summary>
        private void CreateStartScreen()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            _startScreen = new GameObject("StartScreen", typeof(Image));
            _startScreen.transform.SetParent(canvas.transform, false);

            var rt = _startScreen.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = _startScreen.GetComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 1f);

            // Title
            var title = CreateText("Title", _startScreen.transform, "CIV VS CIV", 64);
            var titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 0.5f);
            titleRT.anchorMax = new Vector2(1, 0.7f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.85f, 0.3f);

            // Subtitle
            var sub = CreateText("Subtitle", _startScreen.transform, "Phoenicia vs Greece", 28);
            var subRT = sub.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0, 0.38f);
            subRT.anchorMax = new Vector2(1, 0.48f);
            subRT.offsetMin = Vector2.zero;
            subRT.offsetMax = Vector2.zero;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.color = new Color(0.7f, 0.7f, 0.9f);

            // Jouer button
            var playBtn = new GameObject("JouerBtn", typeof(Image), typeof(Button));
            playBtn.transform.SetParent(_startScreen.transform, false);
            var playRT = playBtn.GetComponent<RectTransform>();
            playRT.anchorMin = new Vector2(0.38f, 0.20f);
            playRT.anchorMax = new Vector2(0.62f, 0.30f);
            playRT.offsetMin = Vector2.zero;
            playRT.offsetMax = Vector2.zero;
            playBtn.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.25f);

            var playLabel = CreateText("PlayLabel", playBtn.transform, "Jouer", 32);
            var plRT = playLabel.GetComponent<RectTransform>();
            plRT.anchorMin = Vector2.zero; plRT.anchorMax = Vector2.one;
            plRT.offsetMin = plRT.offsetMax = Vector2.zero;
            playLabel.alignment = TextAnchor.MiddleCenter;
            playLabel.color = Color.white;

            var playBtnComp = playBtn.GetComponent<Button>();
            playBtnComp.onClick.AddListener(OnJouerClicked);

            // Make sure start screen is on top
            _startScreen.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Cache l'écran titre et démarre la partie.
        /// </summary>
        private void OnJouerClicked()
        {
            if (_startScreen != null)
            {
                Destroy(_startScreen);
                _startScreen = null;
            }
            _gameStarted = true;
        }
    }
}
