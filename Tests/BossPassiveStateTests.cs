using System;
using System.Collections.Generic;
using Source = AfterlifePassiveRules.DamageSource;

public static class BossPassiveStateTests
{
    private static int checks;
    private static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }

    public static void Main()
    {
        var fusion = new BossPassiveState("fusion");
        Check(fusion.JudgmentWillGain == 1, "Every fusion judgment grants one will power");
        Check(fusion.AllyDeathWillGain == 0, "Fusion does not react to death");
        var subject = new BossPassiveState("subject");
        Check(subject.AllyDeathWillGain == 1, "Each ally death grants subject one will power");
        Check(subject.JudgmentWillGain == 0, "Subject does not react to judgment");

        var pope = new BossPassiveState("pope");
        var random = new Random(7341);
        string previous = null;
        for (int turn = 1; turn <= 30; turn++)
        {
            pope.StartTurn(turn, random.Next);
            Check(pope.SanctuaryCount == 40, "Exactly 40 sanctuary tiles each turn");
            var selected = new List<string>();
            for (int y = 0; y < 9; y++)
                for (int x = 0; x < 9; x++)
                    if (pope.IsSanctuary(x, y)) selected.Add(x + "," + y);
            Check(selected.Count == 40, "All sanctuary tiles lie within the 81-cell board");
            string signature = string.Join(";", selected);
            if (previous != null) Check(signature != previous, "The seeded draw refreshes the sanctuary next turn");
            previous = signature;
        }
        Check(!pope.IsSanctuary(-1, 0) && !pope.IsSanctuary(9, 0)
            && !pope.IsSanctuary(0, -1) && !pope.IsSanctuary(0, 9), "Off-board units are not in sanctuary");
        foreach (Source source in Enum.GetValues(typeof(Source)))
        {
            Check(pope.ResolveDamage(source, 12, true) == 0, "Sanctuary stops all character damage sources");
            Check(pope.ResolveDamage(source, 12, false) == 12, "Moving off sanctuary restores damage");
        }
        Check(pope.ResolveDamage(Source.Other, 3, false) == 3, "Unattributed item damage remains independent of character tiles");
        var first = new BossPassiveState("pope");
        first.StartTurn(1, (min, max) => min);
        Check(first.IsSanctuary(0, 0) && first.IsSanctuary(3, 4) && !first.IsSanctuary(4, 4),
            "Turn one also draws sanctuary before deployment");

        var door = new BossPassiveState("door");
        Check(door.RelocatesJudgmentUser(1), "Actual judgment damage triggers relocation");
        Check(!door.RelocatesJudgmentUser(0), "Zero judgment damage cannot trigger relocation");

        var secretary = new BossPassiveState("secretary");
        for (int turn = 1; turn <= 4; turn++)
        {
            secretary.StartTurn(turn, random.Next);
            foreach (Source source in Enum.GetValues(typeof(Source)))
            {
                Check(secretary.ResolveDamage(source, 19, false) == (turn % 2 == 1 ? 1 : 19),
                    "Library odd/even rule applies to every damage source");
                Check(secretary.ResolveDamage(source, 0, false) == 0, "Zero is never upgraded to one damage");
            }
        }

        var instructor = new BossPassiveState("instructor");
        Check(instructor.ResolveDamage(Source.AutomaticAttack, 20, false) == 1, "Strength attacks deal one");
        Check(instructor.ResolveDamage(Source.Judgment, 6, false) == 12, "Judgment is doubled");
        Check(instructor.ResolveDamage(Source.Other, 5, false) == 5, "Archer/move/item damage is not a strength attack");
        Check(instructor.ConsumeCounterDamage(Source.Other, 5) == 0, "Other damage does not consume the counter");
        Check(instructor.ConsumeCounterDamage(Source.Judgment, 12) == 0, "Judgment does not consume the counter");
        Check(instructor.ConsumeCounterDamage(Source.AutomaticAttack, 0) == 0, "Zero attack damage does not consume the counter");
        Check(instructor.ConsumeCounterDamage(Source.AutomaticAttack, 1) == 3, "First strength hit counters for three");
        Check(instructor.ConsumeCounterDamage(Source.AutomaticAttack, 1) == 0, "Second unit does not trigger another counter");
        instructor.StartTurn(2, random.Next);
        Check(instructor.ConsumeCounterDamage(Source.AutomaticAttack, 1) == 0, "Counter is once per battle, not once per turn");
        Check(new BossPassiveState("instructor").ConsumeCounterDamage(Source.AutomaticAttack, 1) == 3,
            "A new battle restores the counter");

        foreach (string id in new[] { "noble", "king", "death1", "death2" })
        {
            var unchanged = new BossPassiveState(id);
            unchanged.StartTurn(1, random.Next);
            Check(unchanged.SanctuaryCount == 0, "Other bosses have no sanctuary");
            Check(unchanged.JudgmentWillGain == 0 && unchanged.AllyDeathWillGain == 0,
                "New event rules do not leak into existing bosses");
            Check(!unchanged.RelocatesJudgmentUser(5), "Other bosses do not inherit door teleportation");
            foreach (Source source in Enum.GetValues(typeof(Source)))
                Check(unchanged.ResolveDamage(source, 7, true) == 7, "Existing boss damage rules remain unchanged");
        }
        Check(new BossPassiveState("death2").ResolveDamage(Source.Judgment,
            AfterlifePassiveRules.ResolveDamage("death2", AfterlifePassiveRules.PhaseOneEffect.None, Source.Judgment, 20), false) == 0,
            "Existing afterlife immunity remains effective in the combined pipeline");
        Check(new BossPassiveState("noble").AllyMoveHealing == 2, "Noble heals two per ally movement");
        foreach (string id in new[] { "king", "death1", "death2", "door", "fusion", "pope", "subject", "secretary", "instructor" })
            Check(new BossPassiveState(id).AllyMoveHealing == 0, "Movement healing is exclusive to noble");
        Console.WriteLine($"PASS: {checks} boss passive assertions");
    }
}
