using System;

public static class BossPatternHpGateTests
{
    private static int checks;
    private static void Check(bool value)
    {
        checks++;
        if (!value) throw new Exception("HP gate check failed: " + checks);
    }

    public static void Main()
    {
        var gate = new BossPatternHpGate(100);
        Check(!gate.CompleteGroup()); // Earlier groups cannot unlock future thresholds.
        Check(gate.ResolveHp(100, 60) == 60);
        Check(gate.ResolveHp(60, 40) == 50);
        Check(gate.IsWaiting);
        Check(gate.ResolveHp(50, -500) == 50);
        Check(gate.ResolveHp(50, 70) == 70); // Healing remains possible while waiting.
        Check(gate.ResolveHp(70, 20) == 50);
        Check(gate.CompleteGroup());
        Check(!gate.CompleteGroup()); // Completion does not release both gates.
        Check(gate.ResolveHp(50, -500) == 30); // No deferred overkill.
        Check(gate.ResolveHp(30, 0) == 30);
        Check(gate.CompleteGroup());
        Check(gate.ResolveHp(30, 0) == 0);
        Check(gate.ResolveHp(20, 90) == 90);
        Check(gate.ResolveHp(90, 0) == 0); // Healing does not re-arm gates.

        gate = new BossPatternHpGate(101);
        Check(gate.ResolveHp(101, 0) == 51); // Round up to stay above fractional thresholds.
        Check(gate.CompleteGroup());
        Check(gate.ResolveHp(51, 0) == 31);

        gate = new BossPatternHpGate(10, BossPatternHpGate.IsEnabled("death2", true));
        int hp = 10;
        for (int turn = 1; turn <= 10; turn++)
        {
            hp = gate.ResolveHp(hp, hp - 1); // Afterlife phase two turn-end drain.
            if (turn % 4 == 0) gate.CompleteGroup();
            Check(hp == 10 - turn);
        }
        Check(hp == 0);
        foreach (string id in new[] { "death1", "noble", "king", "door", "fusion", "pope", "subject", "secretary", "instructor" })
            Check(BossPatternHpGate.IsEnabled(id, true));
        Check(!BossPatternHpGate.IsEnabled("noble", false));
        Check(gate.ResolveHp(hp, 0) == 0);

        gate = new BossPatternHpGate(1);
        Check(gate.ResolveHp(1, 0) == 1);
        Check(gate.CompleteGroup());
        Check(gate.ResolveHp(1, 0) == 1);
        Check(gate.CompleteGroup());
        Check(gate.ResolveHp(1, 0) == 0);
        Check(new BossPatternHpGate(100, false).ResolveHp(100, 0) == 0);
        Check(new BossPatternHpGate(100).Floor == 50); // Fresh battle / phase resets gates.
        Console.WriteLine($"PASS: {checks} boss pattern HP gate assertions");
    }
}
