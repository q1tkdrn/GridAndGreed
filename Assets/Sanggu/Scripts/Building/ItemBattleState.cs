using System;
using System.Collections.Generic;

/// <summary>Equipped item rules and battle-local limits. Never modifies item/unit assets.</summary>
public sealed class ItemBattleState
{
    private readonly HashSet<string> equipped;
    private bool wineUsed;
    private bool contractUsed;

    public ItemBattleState(IEnumerable<string> ids)
    {
        equipped = new HashSet<string>(ids ?? Array.Empty<string>());
    }

    private int Bonus(string id, int amount) => equipped.Contains(id) ? amount : 0;
    public int PowerBonus(int characterId, int hp) => Bonus("1", 2) + Bonus("15", 1)
        + Bonus("19", 2) + ConditionalBonus(characterId, hp);
    public int IntelligenceBonus(int characterId, int hp) => Bonus("7", 2) + Bonus("14", 1)
        + Bonus("19", 2) + ConditionalBonus(characterId, hp);
    private int ConditionalBonus(int characterId, int hp) => (hp <= 5 ? Bonus("13", 5) : 0)
        + (characterId == 9 ? Bonus("22", 3) : 0);

    public int MoveDamage => Bonus("3", 1);
    public int DeathDamage => Bonus("5", 5);
    public int DeathHealing => Bonus("6", 3);
    public int TurnHealing => Bonus("4", 2) + Bonus("20", 1);
    public int TurnHpCost => Bonus("16", 2);
    public int ActionPointBonus => Bonus("16", 1);
    public int WillPowerReduction => Bonus("21", 1);
    public int ReviveTurns(int original) => Math.Max(0, original - Bonus("9", 1));
    public int TurnDamage(Func<int, int, int> randomRange) => Bonus("2", 3)
        + (equipped.Contains("12") ? randomRange(1, 7) : 0);

    public void StartTurn() => wineUsed = false;

    // Resolve refunds before the caller advances the turn at zero action points.
    public int SpendAction(int current, bool judgment, Func<int, int, int> randomRange)
    {
        int remaining = Math.Max(0, current - 1);
        if (judgment && equipped.Contains("8") && randomRange(0, 100) < 33) remaining++;
        if (remaining == 0 && equipped.Contains("11") && !wineUsed)
        {
            wineUsed = true;
            remaining = 1;
        }
        return remaining;
    }

    public int ResolveHp(int hp, int maxHp)
    {
        if (hp <= 0 && equipped.Contains("23") && !contractUsed)
        {
            contractUsed = true;
            hp = 10;
        }
        return Math.Max(0, Math.Min(maxHp, hp));
    }
}
