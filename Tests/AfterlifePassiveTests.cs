using System;
using E = AfterlifePassiveRules.PhaseOneEffect;
using D = AfterlifePassiveRules.DamageSource;

public static class AfterlifePassiveTests
{
    static int checks;
    static void Equal(int expected, int actual)
    {
        checks++;
        if (expected != actual) throw new Exception($"Check {checks}: expected {expected}, got {actual}");
    }
    public static void Main()
    {
        Equal(0, AfterlifePassiveRules.ResolveDamage("death1", E.JudgmentWeakness, D.AutomaticAttack, 6));
        Equal(6, AfterlifePassiveRules.ResolveDamage("death1", E.JudgmentWeakness, D.Judgment, 4));
        Equal(3, AfterlifePassiveRules.ResolveDamage("death1", E.JudgmentWeakness, D.Other, 3));
        Equal(5, AfterlifePassiveRules.ResolveDamage("death1", E.JudgmentWeakness, D.Other, 5));
        // Selecting another effect next turn must remove immunity and judgment vulnerability.
        Equal(6, AfterlifePassiveRules.ResolveDamage("death1", E.Regenerate, D.AutomaticAttack, 6));
        Equal(4, AfterlifePassiveRules.ResolveDamage("death1", E.LimitActions, D.Judgment, 4));
        foreach (E effect in Enum.GetValues(typeof(E)))
            foreach (D source in Enum.GetValues(typeof(D)))
                foreach (int damage in new[] { -1, 0, 1, 2, 3, 10, 100 })
                    Equal(0, AfterlifePassiveRules.ResolveDamage("death2", effect, source, damage));
        // Phase two survives every external damage source but dies after ten completed turns.
        int hp = 10;
        for (int turn = 1; turn <= 10; turn++)
        {
            hp -= AfterlifePassiveRules.ResolveDamage("death2", E.None, D.Other, 3);
            hp = AfterlifePassiveRules.GetTurnEndHp("death2", E.None, hp, 10);
            Equal(10 - turn, hp);
        }
        Equal(0, AfterlifePassiveRules.GetTurnEndHp("death2", E.None, 0, 10));
        Equal(100, AfterlifePassiveRules.GetTurnEndHp("death1", E.Regenerate, 99, 100));
        Equal(84, AfterlifePassiveRules.GetTurnEndHp("death1", E.Regenerate, 80, 100));
        Equal(0, AfterlifePassiveRules.GetTurnEndHp("death1", E.Regenerate, 0, 100));
        Equal(80, AfterlifePassiveRules.GetTurnEndHp("death1", E.LimitActions, 80, 100));
        Equal(4, AfterlifePassiveRules.GetActionPointMaximum(E.LimitActions));
        Equal(7, AfterlifePassiveRules.GetActionPointMaximum(E.JudgmentWeakness));
        Equal(7, AfterlifePassiveRules.GetActionPointMaximum(E.Regenerate));
        Equal(7, AfterlifePassiveRules.GetActionPointMaximum(E.None));
        Equal(6, AfterlifePassiveRules.ResolveDamage("noble", E.JudgmentWeakness, D.AutomaticAttack, 6));
        Equal(60, AfterlifePassiveRules.GetTurnEndHp("noble", E.Regenerate, 60, 60));
        Console.WriteLine($"PASS: {checks} afterlife passive assertions");
    }
}
