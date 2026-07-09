using System.Collections.Generic;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Gère l'état de visibilité de la carte pour chaque joueur.
    /// Trois états par cellule : caché (jamais vu), exploré (déjà vu, dans le brouillard), visible (actuellement vu).
    /// </summary>
    public class FogOfWarManager : MonoBehaviour
    {
        // Par joueur : ensemble des cellules actuellement visibles
        private Dictionary<int, HashSet<HexCoordinates>> _visibleCells;
        // Par joueur : ensemble des cellules déjà explorées
        private Dictionary<int, HashSet<HexCoordinates>> _exploredCells;

        private int _width, _height;

        private void Awake()
        {
            _visibleCells = new Dictionary<int, HashSet<HexCoordinates>>();
            _exploredCells = new Dictionary<int, HashSet<HexCoordinates>>();

            EventBus.Subscribe<GameEvents.MapGenerated>(OnMapGenerated);
            EventBus.Subscribe<GameEvents.PlayerTurnStarted>(OnPlayerTurnStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.MapGenerated>(OnMapGenerated);
            EventBus.Unsubscribe<GameEvents.PlayerTurnStarted>(OnPlayerTurnStarted);
        }

        private void OnMapGenerated(GameEvents.MapGenerated evt)
        {
            _width = evt.Width;
            _height = evt.Height;
            _visibleCells.Clear();
            _exploredCells.Clear();

            for (int i = 0; i < 2; i++)
            {
                _visibleCells[i] = new HashSet<HexCoordinates>();
                _exploredCells[i] = new HashSet<HexCoordinates>();
            }
        }

        private void OnPlayerTurnStarted(GameEvents.PlayerTurnStarted evt)
        {
            // Pour le MVP, on révèle automatiquement un rayon de 2 autour de la position de départ
            // En phase 3 (unités), ce sera remplacé par la vision des unités
            // Placeholder : on ne fait rien ici, la visibilité sera gérée par les unités
        }

        /// <summary>
        /// Met à jour les cellules visibles pour un joueur.
        /// Appelé à chaque tour après le mouvement des unités.
        /// </summary>
        public void UpdateVisibility(HexCoordinates origin, int visionRange, int playerIndex)
        {
            if (!_visibleCells.ContainsKey(playerIndex))
                return;

            var visible = GetCellsInRange(origin, visionRange);
            foreach (var cell in visible)
            {
                if (!IsCellInBounds(cell)) continue;
                _visibleCells[playerIndex].Add(cell);
                _exploredCells[playerIndex].Add(cell);
            }
        }

        public void ClearVisibility(int playerIndex)
        {
            if (_visibleCells.ContainsKey(playerIndex))
                _visibleCells[playerIndex].Clear();
        }

        public bool IsVisible(HexCoordinates coords, int playerIndex)
        {
            return _visibleCells.ContainsKey(playerIndex) &&
                   _visibleCells[playerIndex].Contains(coords);
        }

        public bool HasBeenExplored(HexCoordinates coords, int playerIndex)
        {
            return _exploredCells.ContainsKey(playerIndex) &&
                   _exploredCells[playerIndex].Contains(coords);
        }

        private List<HexCoordinates> GetCellsInRange(HexCoordinates center, int range)
        {
            var result = new List<HexCoordinates>();
            for (int dq = -range; dq <= range; dq++)
            {
                for (int dr = Mathf.Max(-range, -dq - range);
                     dr <= Mathf.Min(range, -dq + range);
                     dr++)
                {
                    result.Add(new HexCoordinates(
                        center.Q + dq,
                        center.R + dr));
                }
            }
            return result;
        }

        private bool IsCellInBounds(HexCoordinates coords)
        {
            var (x, y) = coords.ToOffset();
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }
    }
}
