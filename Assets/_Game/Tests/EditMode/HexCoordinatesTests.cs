using NUnit.Framework;
using CivVSCiv;

public class HexCoordinatesTests
{
    [Test]
    public void Constructor_CalculatesS()
    {
        var hex = new HexCoordinates(3, -1);
        Assert.AreEqual(3, hex.Q);
        Assert.AreEqual(-1, hex.R);
        Assert.AreEqual(-2, hex.S, "s must equal -(q+r)");
    }

    [Test]
    public void Distance_SameHex_ReturnsZero()
    {
        var a = new HexCoordinates(0, 0);
        Assert.AreEqual(0, a.DistanceTo(a));
    }

    [Test]
    public void Distance_Adjacent_ReturnsOne()
    {
        var a = new HexCoordinates(0, 0);
        var neighbors = a.GetNeighbors();
        foreach (var n in neighbors)
        {
            Assert.AreEqual(1, a.DistanceTo(n),
                $"Neighbor {n} should be distance 1 from origin");
        }
    }

    [Test]
    public void Distance_Diagonal_ReturnsTwo()
    {
        var a = new HexCoordinates(0, 0);
        var b = new HexCoordinates(2, -1);
        Assert.AreEqual(2, a.DistanceTo(b));
    }

    [Test]
    public void Distance_Symmetric()
    {
        var a = new HexCoordinates(3, -5);
        var b = new HexCoordinates(-1, 2);
        Assert.AreEqual(a.DistanceTo(b), b.DistanceTo(a));
    }

    [Test]
    public void Distance_HexDistanceProperty()
    {
        // Test from redblobgames reference: hex_distance(a, b) = max(|dq|, |dr|, |ds|)
        var a = new HexCoordinates(1, -3);
        var b = new HexCoordinates(-2, 1);
        int expected = MathfMax(
            MathfAbs(a.Q - b.Q),
            MathfAbs(a.R - b.R),
            MathfAbs(a.S - b.S));
        Assert.AreEqual(expected, a.DistanceTo(b));
    }

    [Test]
    public void Neighbors_CountIsSix()
    {
        var hex = new HexCoordinates(5, -2);
        var neighbors = hex.GetNeighbors();
        Assert.AreEqual(6, neighbors.Length);
    }

    [Test]
    public void Neighbors_AllAreDistanceOne()
    {
        var hex = new HexCoordinates(5, -2);
        foreach (var n in hex.GetNeighbors())
        {
            Assert.AreEqual(1, hex.DistanceTo(n));
        }
    }

    [Test]
    public void Equality_SameCoordinates_AreEqual()
    {
        var a = new HexCoordinates(3, -1);
        var b = new HexCoordinates(3, -1);
        Assert.AreEqual(a, b);
        Assert.IsTrue(a == b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Test]
    public void Equality_DifferentCoordinates_AreNotEqual()
    {
        var a = new HexCoordinates(3, -1);
        var b = new HexCoordinates(3, 0);
        Assert.AreNotEqual(a, b);
        Assert.IsTrue(a != b);
    }

    [Test]
    public void FromOffset_OddRow_ConvertsCorrectly()
    {
        // Offset "odd-r" layout: odd rows are offset right by half
        // For row 1 (odd, 1-based), col 2:
        // q = col - (row - (row&1)) / 2 = 2 - (1 - 1) / 2 = 2
        // r = row = 1
        var hex = HexCoordinates.FromOffset(2, 1);
        Assert.AreEqual(2, hex.Q);
        Assert.AreEqual(1, hex.R);
    }

    [Test]
    public void FromOffset_EvenRow_ConvertsCorrectly()
    {
        // For row 2 (even), col 2:
        // q = col - (row - (row&1)) / 2 = 2 - (2 - 0) / 2 = 1
        // r = row = 2
        var hex = HexCoordinates.FromOffset(2, 2);
        Assert.AreEqual(1, hex.Q);
        Assert.AreEqual(2, hex.R);
    }

    [Test]
    public void ToOffset_RoundTrip()
    {
        var original = new HexCoordinates(4, -2);
        var (col, row) = original.ToOffset();
        var back = HexCoordinates.FromOffset(col, row);
        Assert.AreEqual(original, back);
    }

    // Helpers to avoid Unity dependencies in naming
    private static int MathfAbs(int v) => v < 0 ? -v : v;
    private static int MathfMax(int a, int b, int c)
    {
        if (a >= b && a >= c) return a;
        if (b >= a && b >= c) return b;
        return c;
    }
}
