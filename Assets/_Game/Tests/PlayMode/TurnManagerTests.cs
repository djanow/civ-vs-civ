using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CivVSCiv;

public class TurnManagerTests
{
    [UnityTest]
    public IEnumerator TurnManager_StartsAtTurn1_Player0_MovementPhase()
    {
        var go = new GameObject("TurnManager");
        var tm = go.AddComponent<TurnManager>();

        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual(1, tm.CurrentTurn);
        Assert.AreEqual(0, tm.CurrentPlayerIndex);
        Assert.AreEqual(TurnPhase.Movement, tm.CurrentPhase);

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator TurnManager_CyclesThroughPhases()
    {
        var go = new GameObject("TurnManager");
        var tm = go.AddComponent<TurnManager>();
        yield return new WaitForSeconds(0.2f);

        // Movement -> CityManagement
        tm.EndTurn();
        yield return new WaitForSeconds(0.2f);
        Assert.AreEqual(TurnPhase.CityManagement, tm.CurrentPhase);

        // CityManagement -> Research
        tm.EndTurn();
        yield return new WaitForSeconds(0.2f);
        Assert.AreEqual(TurnPhase.Research, tm.CurrentPhase);

        // Research -> EndOfTurn
        tm.EndTurn();
        yield return new WaitForSeconds(0.2f);
        Assert.AreEqual(TurnPhase.EndOfTurn, tm.CurrentPhase);

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator TurnManager_SwitchesPlayerAfterAllPhases()
    {
        var go = new GameObject("TurnManager");
        var tm = go.AddComponent<TurnManager>();
        yield return new WaitForSeconds(0.2f);

        // Finir toutes les phases du joueur 0 (Movement -> CityManagement -> Research -> EndOfTurn)
        for (int i = 0; i < 3; i++)
        {
            tm.EndTurn();
            yield return new WaitForSeconds(0.2f);
        }

        Assert.AreEqual(1, tm.CurrentPlayerIndex);

        Object.Destroy(go);
    }
}
