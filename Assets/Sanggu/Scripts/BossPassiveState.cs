using System;
using System.Collections.Generic;
using Source = AfterlifePassiveRules.DamageSource;

/// <summary>Boss-local rules. A new instance starts a new battle; assets are never mutated.</summary>
public sealed class BossPassiveState
{
    private readonly string bossId;
    private readonly HashSet<int> sanctuary = new HashSet<int>();
    private bool counterUsed;
    private int turn = 1;

    public BossPassiveState(string bossId) => this.bossId = bossId;
    public int JudgmentWillGain => bossId == "fusion" ? 1 : 0;
    public int AllyDeathWillGain => bossId == "subject" ? 1 : 0;
    public int SanctuaryCount => sanctuary.Count;

    public void StartTurn(int turnNumber, Func<int, int, int> randomRange)
    {
        turn = turnNumber;
        sanctuary.Clear();
        if (bossId != "pope") return;

        // Partial Fisher-Yates: exactly 40 distinct cells out of the full 9x9 board.
        var cells = new int[81];
        for (int i = 0; i < cells.Length; i++) cells[i] = i;
        for (int i = 0; i < 40; i++)
        {
            int pick = randomRange(i, cells.Length);
            int selected = cells[pick];
            cells[pick] = cells[i];
            cells[i] = selected;
            sanctuary.Add(selected);
        }
    }

    public bool IsSanctuary(int x, int y) => x >= 0 && x < 9 && y >= 0 && y < 9
        && sanctuary.Contains(y * 9 + x);

    public int ResolveDamage(Source source, int damage, bool sourceOnSanctuary)
    {
        if (damage <= 0) return 0;
        if (bossId == "pope" && sourceOnSanctuary) return 0;
        if (bossId == "secretary" && turn % 2 == 1) return 1;
        if (bossId == "instructor")
        {
            if (source == Source.AutomaticAttack) return 1;
            if (source == Source.Judgment) return damage * 2;
        }
        return damage;
    }

    public bool RelocatesJudgmentUser(int damage) => bossId == "door" && damage > 0;

    public int ConsumeCounterDamage(Source source, int damage)
    {
        if (bossId != "instructor" || counterUsed || damage <= 0 || source != Source.AutomaticAttack)
            return 0;
        counterUsed = true;
        return 3;
    }
}
