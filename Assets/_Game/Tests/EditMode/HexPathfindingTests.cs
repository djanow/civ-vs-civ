using NUnit.Framework;
using System.Collections.Generic;
using CivVSCiv;

public class HexPathfindingTests
{
    private HexCell[,] CreateFlatGrid(int w, int h)
    {
        var cells = new HexCell[w, h];
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                var coords = HexCoordinates.FromOffset(x, y);
                cells[x, y] = new HexCell(coords, TileType.Plain);
            }
        }
        return cells;
    }

    [Test]
    public void FindPath_StartEqualsGoal_ReturnsSingleCell()
    {
        var cells = CreateFlatGrid(10, 10);
        var start = HexCoordinates.FromOffset(0, 0);

        var path = HexPathfinding.FindPath(
            cells, cells.GetLength(0), cells.GetLength(1), start, start);

        Assert.AreEqual(1, path.Count);
        Assert.AreEqual(start, path[0]);
    }

    [Test]
    public void FindPath_StraightHorizontalLine_ReturnsDirectPath()
    {
        var cells = CreateFlatGrid(10, 10);
        var start = HexCoordinates.FromOffset(0, 0);
        var goal = HexCoordinates.FromOffset(5, 3);

        var path = HexPathfinding.FindPath(
            cells, cells.GetLength(0), cells.GetLength(1), start, goal);

        Assert.Greater(path.Count, 0);
        Assert.AreEqual(start, path[0]);
        Assert.AreEqual(goal, path[path.Count - 1]);

        // Verifier que chaque etape est adjacente
        for (int i = 0; i < path.Count - 1; i++)
        {
            int dist = path[i].DistanceTo(path[i + 1]);
            Assert.AreEqual(1, dist,
                $"Step {i}: {path[i]} -> {path[i + 1]} should be adjacent (dist={dist})");
        }
    }

    [Test]
    public void FindPath_BlockedByOcean_ReturnsEmpty()
    {
        var cells = CreateFlatGrid(10, 10);

        // Creer un mur d'ocean entre start et goal
        var start = HexCoordinates.FromOffset(0, 5);
        var goal = HexCoordinates.FromOffset(8, 5);
        for (int x = 0; x < 10; x++)
        {
            cells[x, 5] = new HexCell(HexCoordinates.FromOffset(x, 5), TileType.Ocean);
        }

        var path = HexPathfinding.FindPath(
            cells, cells.GetLength(0), cells.GetLength(1), start, goal);
        Assert.AreEqual(0, path.Count);
    }

    [Test]
    public void FindPath_MountainOnlyCrossableAtPass()
    {
        var cells = CreateFlatGrid(10, 10);

        // Mur de montagnes avec un col au milieu
        for (int x = 0; x < 10; x++)
        {
            cells[x, 5] = new HexCell(HexCoordinates.FromOffset(x, 5), TileType.Mountain);
        }
        cells[4, 5].IsMountainPass = true;

        var start = HexCoordinates.FromOffset(4, 3);
        var goal = HexCoordinates.FromOffset(4, 7);

        var path = HexPathfinding.FindPath(
            cells, cells.GetLength(0), cells.GetLength(1), start, goal);
        Assert.Greater(path.Count, 0, "Should find path through the mountain pass");
    }

    [Test]
    public void FindPath_PrefersFasterTerrain()
    {
        var cells = CreateFlatGrid(10, 5);

        // Creer deux chemins vers le goal : un direct dans un marais, un plus long en plaine
        var start = HexCoordinates.FromOffset(1, 2);
        var goal = HexCoordinates.FromOffset(7, 2);

        // Chemin direct : marais (cout 2 par case)
        cells[2, 2] = new HexCell(HexCoordinates.FromOffset(2, 2), TileType.Marsh);
        cells[4, 2] = new HexCell(HexCoordinates.FromOffset(4, 2), TileType.Marsh);
        cells[5, 2] = new HexCell(HexCoordinates.FromOffset(5, 2), TileType.Marsh);

        var path = HexPathfinding.FindPath(
            cells, cells.GetLength(0), cells.GetLength(1), start, goal);
        Assert.Greater(path.Count, 0);

        // Le chemin ne devrait pas passer par le marais si un chemin plaine est moins cher
        foreach (var hex in path)
        {
            var (col, row) = hex.ToOffset();
            if (row == 2 && (col == 2 || col == 4 || col == 5))
            {
                Assert.Fail($"Path should avoid marsh at ({col}, {row})");
            }
        }
    }
}
