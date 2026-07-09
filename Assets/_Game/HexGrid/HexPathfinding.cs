using System.Collections.Generic;
using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Algorithme A* sur grille hexagonale avec couts de terrain.
    /// Utilise une List<Node> comme file de priorite pour eviter
    /// le probleme de doublons de SortedSet quand deux Nodes ont le meme FCost.
    /// </summary>
    public static class HexPathfinding
    {
        private class Node
        {
            public HexCoordinates Coords;
            public int GCost; // Cout depuis le depart
            public int HCost; // Heuristique vers l'arrivee
            public int FCost => GCost + HCost;
            public Node Parent;
        }

        public static List<HexCoordinates> FindPath(
            HexCell[,] cells, int width, int height,
            HexCoordinates start, HexCoordinates goal)
        {
            if (start == goal)
                return new List<HexCoordinates> { start };

            var (sx, sy) = start.ToOffset();
            var (gx, gy) = goal.ToOffset();

            // Verifier que start et goal sont dans les limites
            if (sx < 0 || sx >= width || sy < 0 || sy >= height) return new List<HexCoordinates>();
            if (gx < 0 || gx >= width || gy < 0 || gy >= height) return new List<HexCoordinates>();

            // Verifier que le goal est franchissable
            if (cells[gx, gy].MovementCost < 0) return new List<HexCoordinates>();

            // Liste utilisee comme file de priorite : on extrait le min
            // a chaque iteration par parcours lineaire. Evite le bug
            // SortedSet qui ecrase les entrees ayant le meme FCost.
            var openSet = new List<Node>();
            var closedSet = new HashSet<HexCoordinates>();
            var nodeMap = new Dictionary<HexCoordinates, Node>();

            var startNode = new Node { Coords = start, GCost = 0, HCost = start.WrappedDistanceTo(goal, width) };
            openSet.Add(startNode);
            nodeMap[start] = startNode;

            while (openSet.Count > 0)
            {
                // Extraire le noeud avec le FCost le plus bas
                Node current = openSet[0];
                int currentIdx = 0;
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < current.FCost ||
                        (openSet[i].FCost == current.FCost &&
                         openSet[i].HCost < current.HCost))
                    {
                        current = openSet[i];
                        currentIdx = i;
                    }
                }

                openSet.RemoveAt(currentIdx);

                if (current.Coords == goal)
                    return ReconstructPath(current);

                closedSet.Add(current.Coords);

                foreach (var neighbor in current.Coords.GetNeighborsWrapped(width, height))
                {
                    var (nx, ny) = neighbor.ToOffset();
                    if (ny < 0 || ny >= height) continue;
                    if (closedSet.Contains(neighbor)) continue;

                    int moveCost = cells[nx, ny].MovementCost;
                    if (moveCost < 0) continue; // Infranchissable

                    int tentativeG = current.GCost + moveCost;

                    if (nodeMap.TryGetValue(neighbor, out var neighborNode))
                    {
                        // Noeud deja connu : mettre a jour si on trouve un meilleur chemin
                        if (tentativeG < neighborNode.GCost)
                        {
                            neighborNode.Parent = current;
                            neighborNode.GCost = tentativeG;
                            neighborNode.HCost = neighbor.WrappedDistanceTo(goal, width);
                        }
                    }
                    else
                    {
                        // Nouveau noeud : creer et ajouter a openSet
                        neighborNode = new Node
                        {
                            Coords = neighbor,
                            Parent = current,
                            GCost = tentativeG,
                            HCost = neighbor.WrappedDistanceTo(goal, width)
                        };
                        nodeMap[neighbor] = neighborNode;
                        openSet.Add(neighborNode);
                    }
                }
            }

            return new List<HexCoordinates>(); // Aucun chemin trouve
        }

        private static List<HexCoordinates> ReconstructPath(Node endNode)
        {
            var path = new List<HexCoordinates>();
            var current = endNode;
            while (current != null)
            {
                path.Add(current.Coords);
                current = current.Parent;
            }
            path.Reverse();
            return path;
        }
    }
}
