using System;

public static class ItemBattleStateTests
{
    private static int assertions;
    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception(message);
    }
    private static ItemBattleState Items(params string[] ids) => new ItemBattleState(ids);
    private static int Low(int min, int max) => min;
    private static int High(int min, int max) => max - 1;

    public static void Main()
    {
        var empty = Items();
        Check(empty.PowerBonus(9, 1) == 0 && empty.IntelligenceBonus(9, 1) == 0, "No unequipped stats");
        Check(empty.ResolveHp(0, 20) == 0 && empty.SpendAction(1, true, Low) == 0, "No unequipped rescue/refund");
        Check(Items("1").PowerBonus(1, 20) == 2, "Rusty sword");
        Check(Items("7").IntelligenceBonus(1, 20) == 2, "Spellbook");
        Check(Items("14").IntelligenceBonus(1, 20) == 1, "Glasses");
        Check(Items("15").PowerBonus(1, 20) == 1, "Knuckles");
        Check(Items("19").PowerBonus(1, 20) == 2 && Items("19").IntelligenceBonus(1, 20) == 2, "Sewing machine");
        var stack = Items("1", "15", "19", "1");
        Check(stack.PowerBonus(1, 20) == 5, "Different bonuses stack; duplicate IDs do not");
        var leaf = Items("13");
        Check(leaf.PowerBonus(1, 5) == 5 && leaf.IntelligenceBonus(1, 5) == 5, "Leaf includes five HP");
        Check(leaf.PowerBonus(1, 6) == 0, "Leaf stops after healing above five HP");
        var scythe = Items("22");
        Check(scythe.PowerBonus(9, 20) == 3 && scythe.IntelligenceBonus(9, 20) == 3, "Reaper scythe");
        Check(scythe.PowerBonus(8, 20) == 0, "Scythe excludes other characters");
        Check(Items("2").TurnDamage(Low) == 3, "Arrows");
        Check(Items("12").TurnDamage(Low) == 1 && Items("12").TurnDamage(High) == 6, "Dice inclusive endpoints");
        Check(Items("2", "12").TurnDamage(High) == 9, "Turn damage stacks");
        Check(Items("3").MoveDamage == 1, "Third dagger");
        Check(Items("4", "20").TurnHealing == 3, "Salt water and cotton stack");
        Check(Items("5").DeathDamage == 5 && Items("6").DeathHealing == 3, "Death effects");
        Check(Items("9").ReviveTurns(3) == 2 && Items("9").ReviveTurns(1) == 0, "Watch allows immediate revival");
        Check(Items("9").ReviveTurns(0) == 0, "Revival delay cannot be negative");
        var tobacco = Items("16");
        tobacco.StartTurn(); tobacco.StartTurn();
        Check(tobacco.TurnHpCost == 2 && tobacco.ActionPointBonus == 1, "Tobacco does not accumulate");
        Check(Items("21").WillPowerReduction == 1, "Guitar");
        var wine = Items("11");
        Check(wine.SpendAction(2, false, Low) == 1, "Wine waits until zero");
        Check(wine.SpendAction(1, false, Low) == 1, "Wine first refund");
        Check(wine.SpendAction(1, false, Low) == 0, "Wine only once per turn");
        wine.StartTurn();
        Check(wine.SpendAction(1, false, Low) == 1, "Wine resets next turn");
        var madness = Items("8");
        Check(madness.SpendAction(1, true, (a,b) => 32) == 1, "Madness succeeds below 33");
        Check(madness.SpendAction(1, true, (a,b) => 33) == 0, "Madness fails at 33");
        Check(madness.SpendAction(1, false, Low) == 0, "Madness excludes movement");
        var refunds = Items("8", "11");
        Check(refunds.SpendAction(1, true, Low) == 1, "Madness resolves before zero-AP check");
        Check(refunds.SpendAction(1, false, Low) == 1, "Successful madness does not consume wine");
        var contract = Items("23");
        Check(contract.ResolveHp(1, 20) == 1, "Contract waits for lethal damage");
        Check(contract.ResolveHp(-8, 20) == 10, "Contract rescues overkill");
        contract.StartTurn();
        Check(contract.ResolveHp(0, 20) == 0, "Contract does not reset each turn");
        Check(Items("23").ResolveHp(0, 20) == 10, "New battle resets contract");
        Check(Items("23").ResolveHp(0, 8) == 8, "Rescue respects maximum HP");
        Check(empty.ResolveHp(30, 20) == 20, "Healing caps at max HP");
        foreach (var id in new[] { "10", "17", "18" })
        {
            var cosmetic = Items(id);
            Check(cosmetic.PowerBonus(9, 1) == 0 && cosmetic.TurnDamage(Low) == 0
                && cosmetic.TurnHealing == 0 && cosmetic.ActionPointBonus == 0, "Cosmetic items have no combat bonuses");
        }
        Console.WriteLine("PASS: " + assertions + " item assertions");
    }
}
