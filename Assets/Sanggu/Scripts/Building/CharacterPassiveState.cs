/// <summary>
/// 한 전투에서만 유지되는 캐릭터 패시브 상태. 원본 UnitTemp를 변경하지 않는다.
/// </summary>
public sealed class CharacterPassiveState
{
    private readonly int characterId;
    private bool moved;
    private bool shieldAvailable = true;

    public int PowerBonus { get; private set; }
    public int IntelligenceBonus { get; private set; }
    public int MoveDamage => characterId == 3 ? 3 : 0;
    public int MoveHealing => characterId == 5 ? 1 : 0;
    public int TurnEndHealing => characterId == 4 ? 3 : 0;
    public int TurnEndDamage => characterId == 2 && !moved ? 5 : 0;
    public bool LosesBattleOnDeath => characterId == 9;

    public CharacterPassiveState(int characterId)
    {
        this.characterId = characterId;
    }

    public void StartTurn()
    {
        moved = false;
        IntelligenceBonus = 0;
    }

    public void Move() => moved = true;

    public void AfterJudge()
    {
        // Wizard's base intelligence is 4; judgment can raise it to 6 this turn.
        if (characterId == 7 && IntelligenceBonus < 2) IntelligenceBonus++;
    }

    public bool TryBlockHit()
    {
        if (characterId != 1 || !shieldAvailable) return false;
        shieldAvailable = false;
        return true;
    }

    public void OnDeath()
    {
        shieldAvailable = true;
    }

    public void OnAllyDeath()
    {
        if (characterId == 6) PowerBonus++;
    }
}
