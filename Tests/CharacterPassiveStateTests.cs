using System;

// Standalone regression tests; compile with CharacterPassiveState.cs (no Unity player required).
public static class CharacterPassiveStateTests
{
    private static int assertions;
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    public static void Main()
    {
        var knight = new CharacterPassiveState(1);
        Check(knight.TryBlockHit(), "Knight must block its first hit");
        Check(!knight.TryBlockHit(), "Knight must not block a second hit");
        knight.StartTurn();
        Check(!knight.TryBlockHit(), "New turns must not refill the shield");
        knight.OnDeath();
        Check(knight.TryBlockHit(), "Death must reset the knight shield");

        var archer = new CharacterPassiveState(2);
        Check(archer.TurnEndDamage == 5, "Stationary archer gets bonus damage");
        archer.Move();
        archer.Move();
        Check(archer.TurnEndDamage == 0, "Returning to the original cell still counts as movement");
        archer.StartTurn();
        Check(archer.TurnEndDamage == 5, "Archer movement resets each turn");

        var rogue = new CharacterPassiveState(3);
        Check(rogue.MoveDamage == 3 && rogue.MoveHealing == 0, "Rogue movement deals 3 damage");
        var bishop = new CharacterPassiveState(4);
        Check(bishop.TurnEndHealing == 3, "Bishop heals 3 at turn end without needing judgment");
        bishop.AfterJudge();
        bishop.AfterJudge();
        bishop.Move();
        Check(bishop.TurnEndHealing == 3, "Judgment count and movement do not multiply turn-end healing");
        bishop.StartTurn();
        Check(bishop.TurnEndHealing == 3, "Bishop can heal at the end of each new turn");
        Check(new CharacterPassiveState(5).MoveHealing == 1, "Dancer heals 1 on movement");

        var scavenger = new CharacterPassiveState(6);
        scavenger.OnAllyDeath();
        scavenger.OnAllyDeath();
        scavenger.OnDeath();
        scavenger.StartTurn();
        Check(scavenger.PowerBonus == 2, "Scavenger stacks survive turns and own death");
        Check(new CharacterPassiveState(6).PowerBonus == 0, "New battle must reset stacks");

        var wizard = new CharacterPassiveState(7);
        int total = 0;
        for (int i = 0; i < 3; i++)
        {
            total += 4 + wizard.IntelligenceBonus;
            wizard.AfterJudge();
        }
        Check(total == 15, "Wizard judgments must deal 4, 5, 6 damage");
        for (int i = 0; i < 10; i++)
        {
            wizard.AfterJudge();
            Check(4 + wizard.IntelligenceBonus == 6, "Repeated judgments cannot raise base intelligence above 6");
        }
        wizard.StartTurn();
        Check(wizard.IntelligenceBonus == 0, "Wizard bonus expires next turn");
        wizard.AfterJudge();
        Check(4 + wizard.IntelligenceBonus == 5, "Wizard can build stacks again after resetting to 4");
        wizard.StartTurn();
        Check(4 + wizard.IntelligenceBonus == 4, "Partial stacks also reset on the next turn");

        for (int id = 1; id <= 9; id++)
        {
            var state = new CharacterPassiveState(id);
            Check(state.LosesBattleOnDeath == (id == 9), "Only reaper death forces defeat");
            if (id != 1) Check(!state.TryBlockHit(), "Only knight blocks damage");
            if (id != 3) Check(state.MoveDamage == 0, "Movement damage must not leak to other units");
            if (id != 5) Check(state.MoveHealing == 0, "Movement healing must not leak to other units");
            if (id != 4) Check(state.TurnEndHealing == 0, "Turn-end healing must not leak to other units");
            if (id != 2) Check(state.TurnEndDamage == 0, "Archer bonus must not leak to other units");
        }
        Console.WriteLine("PASS: " + assertions + " passive assertions");
    }
}
