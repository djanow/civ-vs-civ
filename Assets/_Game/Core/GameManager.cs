using System.Collections.Generic;
using UnityEngine;

namespace CivVSCiv
{
    public enum GameState
    {
        MainMenu,
        Generating,
        Playing,
        Interlude,
        Paused,
        GameOver
    }

    /// <summary>
    /// Singleton principal. Point d'entrée du jeu.
    /// Orchestre l'initialisation et le flow global.
    /// Gère les ressources joueurs, les unlocks narratifs,
    /// et la transition entre états de jeu.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameSetupData _setupData;
        [SerializeField] private HexGridData _gridData;
        [SerializeField] private int _civCount = 2;

        [Header("Références aux managers (auto-résolus)")]
        [SerializeField] private TurnManager _turnManager;
        [SerializeField] private CivManager _civManager;
        [SerializeField] private DiplomacyManager _diplomacyManager;
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private CityStateManager _cityStateManager;
        [SerializeField] private UnitManager _unitManager;
        [SerializeField] private EventManager _eventManager;
        [SerializeField] private InterludeManager _interludeManager;
        [SerializeField] private ResearchManager _researchManager;
        private HexGridRenderer _gridRenderer;

        public GameState CurrentState { get; set; } = GameState.MainMenu;
        public HexCell[,] Cells { get; private set; }
        public int Width => Cells?.GetLength(0) ?? 0;
        public int Height => Cells?.GetLength(1) ?? 0;

        [Header("Ressources des joueurs")]
        public int[] PlayerGold;
        public int[] PlayerScience;
        public int[] PlayerCulture;
        public int[] PlayerEra; // 0=Antiquite, 1=Classique, 2=Medievale

        // --- Accesseurs publics pour les managers ---
        public CivManager CivManager => _civManager;
        public DiplomacyManager DiplomacyManager => _diplomacyManager;
        public CityManager CityManager => _cityManager;
        public UnitManager UnitManager => _unitManager;
        public EventManager EventManager => _eventManager;
        public InterludeManager InterludeManager => _interludeManager;
        public ResearchManager ResearchManager => _researchManager;
        public TurnManager TurnManager => _turnManager;
        public GameSetupData SetupData => _setupData;

        /// <summary>Unlocks narratifs par joueur (flags système).</summary>
        private Dictionary<int, HashSet<string>> _playerUnlocks = new Dictionary<int, HashSet<string>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Résolution automatique des managers
            ResolveManagers();

            // Auto-création des données de grille si non assignées
            if (_gridData == null)
            {
                _gridData = ScriptableObject.CreateInstance<HexGridData>();
                _gridData.Width = 40;
                _gridData.Height = 30;
                _gridData.WaterLevel = 0.3f;
                _gridData.MountainDensity = 0.1f;
                _gridData.ForestDensity = 0.2f;
                _gridData.MinDistanceBetweenCivs = 10;
                _gridData.name = "HexGridData_Auto";
            }

            // S'abonner aux événements
            EventBus.Subscribe<GameEvents.NarrativeEventTriggered>(OnNarrativeEventTriggered);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.NarrativeEventTriggered>(OnNarrativeEventTriggered);
        }

        private void ResolveManagers()
        {
            _turnManager    = FindOrCreate<TurnManager>("TurnManager");
            _civManager     = FindOrCreate<CivManager>("CivManager");
            _diplomacyManager = FindOrCreate<DiplomacyManager>("DiplomacyManager");
            _cityManager    = FindOrCreate<CityManager>("CityManager");
            _cityStateManager = FindOrCreate<CityStateManager>("CityStateManager");
            _unitManager    = FindOrCreate<UnitManager>("UnitManager");
            _eventManager   = FindOrCreate<EventManager>("EventManager");
            _interludeManager = FindOrCreate<InterludeManager>("InterludeManager");
            _researchManager = FindOrCreate<ResearchManager>("ResearchManager");

            // Grid renderer (avec FogOfWar)
            _gridRenderer = FindAnyObjectByType<HexGridRenderer>();
            if (_gridRenderer == null)
            {
                var hgGo = new GameObject("HexGridManager");
                _gridRenderer = hgGo.AddComponent<HexGridRenderer>();
            }

            // FogOfWar (pairé avec le renderer)
            if (FindAnyObjectByType<FogOfWarRenderer>() == null)
            {
                var fogGo = new GameObject("FogOfWar");
                fogGo.AddComponent<FogOfWarRenderer>();
            }

            // Créer le MinimapWidget
            if (FindAnyObjectByType<MinimapWidget>() == null)
            {
                var mmGo = new GameObject("MinimapController");
                mmGo.AddComponent<MinimapWidget>();
            }

            // Créer le Canvas UI si absent
            EnsureCanvasExists();
        }

        private T FindOrCreate<T>(string name) where T : Component
        {
            var comp = FindAnyObjectByType<T>();
            if (comp == null)
            {
                var go = new GameObject(name);
                comp = go.AddComponent<T>();
            }
            return comp;
        }

        private void EnsureCanvasExists()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null) return;

            var canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Créer le RawImage minimap
            var minimap = new GameObject("Minimap", typeof(RawImage));
            minimap.transform.SetParent(canvasGo.transform, false);
            var mmRT = minimap.GetComponent<RectTransform>();
            mmRT.anchorMin = mmRT.anchorMax = new Vector2(0, 0);
            mmRT.pivot = new Vector2(0, 0);
            mmRT.anchoredPosition = new Vector2(10, 10);
            mmRT.sizeDelta = new Vector2(180, 140);

            // ViewportRect enfant
            var vpr = new GameObject("ViewportRect", typeof(Image));
            vpr.transform.SetParent(minimap.transform, false);
            vpr.GetComponent<Image>().color = new Color(1, 1, 1, 0.3f);
            var vprRT = vpr.GetComponent<RectTransform>();
            vprRT.anchorMin = Vector2.zero; vprRT.anchorMax = Vector2.one;
            vprRT.offsetMin = vprRT.offsetMax = Vector2.zero;
        }

        private void Start()
        {
            StartNewGame();
        }

        // ----------------------------------------------------------------
        // Initialisation de la partie
        // ----------------------------------------------------------------

        public void StartNewGame()
        {
            CurrentState = GameState.Generating;

            // Charger les données de setup
            LoadSetupData();

            // Randomiser le seed
            _gridData.Seed = System.DateTime.Now.GetHashCode();

            // Générer la carte
            Cells = HexGridGenerator.Generate(_gridData, _civCount);

            // Initialiser les ressources des joueurs
            PlayerGold = new int[_civCount];
            PlayerScience = new int[_civCount];
            PlayerCulture = new int[_civCount];
            PlayerEra = new int[_civCount]; // 0 = Antiquite
            _playerUnlocks.Clear();

            for (int i = 0; i < _civCount; i++)
            {
                PlayerGold[i] = _setupData != null ? _setupData.StartingGold : 100;
                PlayerScience[i] = _setupData != null ? _setupData.StartingScience : 3;
                PlayerCulture[i] = _setupData != null ? _setupData.StartingCulture : 1;
                PlayerEra[i] = 0;
                _playerUnlocks[i] = new HashSet<string>();
            }

            // Initialiser tous les managers
            InitializeManagers();

            // Publier l'événement de carte générée
            EventBus.Publish(new GameEvents.MapGenerated
            {
                Cells = Cells,
                Width = Width,
                Height = Height
            });

            CurrentState = GameState.Playing;
        }

        /// <summary>
        /// Charge les données depuis le GameSetupData.
        /// </summary>
        private void LoadSetupData()
        {
            if (_setupData == null)
            {
                Debug.LogWarning("[GameManager] Aucun GameSetupData assigné.");
                var found = Resources.Load<GameSetupData>("GameSetup");
                if (found != null)
                    _setupData = found;
            }
        }

        /// <summary>
        /// Initialise tous les managers avec les données de setup.
        /// </summary>
        private void InitializeManagers()
        {
            // CivManager: assigner les civilisations aux joueurs
            if (_civManager != null && _setupData?.AvailableCivs != null)
            {
                int assignCount = Mathf.Min(_civCount, _setupData.AvailableCivs.Length);
                CivilizationData[] assignments = new CivilizationData[assignCount];
                for (int i = 0; i < assignCount; i++)
                    assignments[i] = _setupData.AvailableCivs[i];
                _civManager.InitializePlayers(assignCount, assignments);
            }

            // DiplomacyManager
            if (_diplomacyManager != null)
                _diplomacyManager.Initialize(_civCount);

            // CityManager
            if (_cityManager != null)
                _cityManager.Initialize();

            // CityStateManager
            if (_cityStateManager != null)
                _cityStateManager.ResetState();

            // UnitManager
            if (_unitManager != null)
                _unitManager.Initialize(_civCount);

            // ResearchManager
            if (_researchManager != null)
                _researchManager.Initialize(_civCount);

            Debug.Log("[GameManager] Tous les managers initialisés.");
        }

        // ----------------------------------------------------------------
        // Gestion des ressources
        // ----------------------------------------------------------------

        public void ModifyGold(int playerIndex, int delta)
        {
            if (playerIndex < 0 || playerIndex >= PlayerGold.Length) return;
            PlayerGold[playerIndex] = Mathf.Max(0, PlayerGold[playerIndex] + delta);
        }

        public void ModifyScience(int playerIndex, int delta)
        {
            if (playerIndex < 0 || playerIndex >= PlayerScience.Length) return;
            PlayerScience[playerIndex] = Mathf.Max(0, PlayerScience[playerIndex] + delta);
        }

        public void ModifyCulture(int playerIndex, int delta)
        {
            if (playerIndex < 0 || playerIndex >= PlayerCulture.Length) return;
            PlayerCulture[playerIndex] = Mathf.Max(0, PlayerCulture[playerIndex] + delta);
        }

        public int GetPlayerGold(int playerIndex)
        {
            return (playerIndex >= 0 && playerIndex < PlayerGold.Length) ? PlayerGold[playerIndex] : 0;
        }

        public int GetPlayerScience(int playerIndex)
        {
            return (playerIndex >= 0 && playerIndex < PlayerScience.Length) ? PlayerScience[playerIndex] : 0;
        }

        public int GetPlayerCulture(int playerIndex)
        {
            return (playerIndex >= 0 && playerIndex < PlayerCulture.Length) ? PlayerCulture[playerIndex] : 0;
        }

        // ----------------------------------------------------------------
        // Système d'unlocks (flags narratifs)
        // ----------------------------------------------------------------

        public void AddUnlock(int playerIndex, string unlockName)
        {
            if (!_playerUnlocks.ContainsKey(playerIndex))
                _playerUnlocks[playerIndex] = new HashSet<string>();
            _playerUnlocks[playerIndex].Add(unlockName);
            Debug.Log($"[GameManager] Unlock \"{unlockName}\" pour joueur {playerIndex}");
        }

        public bool HasUnlock(int playerIndex, string unlockName)
        {
            return _playerUnlocks.ContainsKey(playerIndex)
                && _playerUnlocks[playerIndex].Contains(unlockName);
        }

        public string[] GetPlayerUnlocks(int playerIndex)
        {
            if (!_playerUnlocks.ContainsKey(playerIndex))
                return System.Array.Empty<string>();
            var result = new string[_playerUnlocks[playerIndex].Count];
            _playerUnlocks[playerIndex].CopyTo(result);
            return result;
        }

        // ----------------------------------------------------------------
        // Gestion des états
        // ----------------------------------------------------------------

        public void ResumeFromInterlude()
        {
            if (CurrentState == GameState.Interlude)
            {
                CurrentState = GameState.Playing;
                if (_turnManager != null)
                    _turnManager.OnNarrativeEventDismissed();
            }
        }

        public void SetGameOver()
        {
            CurrentState = GameState.GameOver;
            Debug.Log("[GameManager] Partie terminée.");
        }

        // ----------------------------------------------------------------
        // Événements
        // ----------------------------------------------------------------

        private void OnNarrativeEventTriggered(GameEvents.NarrativeEventTriggered evt)
        {
            Debug.Log($"[GameManager] Événement narratif : {evt.Title} (joueur {evt.PlayerIndex})");
        }

        // ----------------------------------------------------------------
        // Utilitaires
        // ----------------------------------------------------------------

        public bool IsCellInBounds(HexCoordinates coords)
        {
            var (x, y) = coords.ToOffset();
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public HexCell GetCell(HexCoordinates coords)
        {
            if (!IsCellInBounds(coords)) return null;
            var (x, y) = coords.ToOffset();
            return Cells[x, y];
        }
    }
}
