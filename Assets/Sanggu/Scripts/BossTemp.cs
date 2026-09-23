using UnityEngine;

[CreateAssetMenu(fileName = "Boss", menuName = "Boss")]
public class BossTemp : ScriptableObject
{
    [Header("Boss")]
    public string bossName;
    public string bossId;
    public Sprite bossSprite;

    [Header("Battle Stats")]
    [Min(1)] public int maxHp = 60;
    [Min(0)] public int initialWillPower = 0;

    [Header("Dialogue")] 
    [TextArea]
    public string battleStart;
    public string[] turnStart;
    public string[] willDecline;
    public string[] willZero;
    public string[] phaseTwo;
    public string[] phaseThree;
    public string[] attackedAP;
    public string[] attackedAD;
    public string[] attack;
    [TextArea]
    public string win;
    [TextArea]
    public string lose;
    public string[] distinctText;
    
    [Header("Stage")]
    public string stageName;
    public Sprite stageSprite;
    public AudioClip bgmStart;
    public AudioClip bgmLoop;

    [Header("Battle Pattern")]
    [Tooltip("대저택, 광장, 성당, 문, 지하실, 서고, 훈련장은 전용 A/B/C 묶음을 사용합니다. 저승은 death1/death2에 따라 1/2페이즈 묶음을 사용합니다. 다른 보스는 이 배열을 순환하며, 비어 있으면 패턴 공격을 하지 않습니다.")]
    public BattlePatternRules.Pattern[] patterns;
    [Min(1)] public int patternDamage = 5;
    [Min(0.1f)] public float patternPreviewSeconds = 1f;
}

// One-use HP gates. Reaching a gate arms it; only completion of the current
// pattern group releases it. Healing never restores an already released gate.
public sealed class BossPatternHpGate
{
    public static bool IsEnabled(string bossId, bool hasPattern) => hasPattern && bossId != "death2";
    private readonly int maximum;
    private int released;
    public bool IsWaiting { get; private set; }
    public int Floor => released == 0 ? (int)((maximum * 50L + 99) / 100)
        : released == 1 ? (int)((maximum * 30L + 99) / 100) : 0;

    public BossPatternHpGate(int maximum, bool enabled = true)
    {
        this.maximum = System.Math.Max(1, maximum);
        released = enabled ? 0 : 2;
    }

    public int ResolveHp(int current, int requested)
    {
        if (requested >= current || released >= 2) return requested;
        int floor = Floor;
        if (requested <= floor)
        {
            IsWaiting = true;
            return System.Math.Min(current, floor);
        }
        return requested;
    }

    public bool CompleteGroup()
    {
        if (!IsWaiting) return false;
        released++;
        IsWaiting = false;
        return true;
    }
}

public static class AfterlifePassiveRules
{
    public enum PhaseOneEffect { None, LimitActions, JudgmentWeakness, Regenerate }
    public enum DamageSource { AutomaticAttack, Judgment, Other }

    public static bool PreventsAllyDeathFromBossAttack(string bossId)
        => bossId == "death1" || bossId == "death2";

    public static int GetBossAttackDamage(string bossId, int damage)
        => PreventsAllyDeathFromBossAttack(bossId) ? 3 : damage;

    public static int ResolveDamage(string bossId, PhaseOneEffect effect, DamageSource source, int damage)
    {
        if (damage <= 0 || bossId == "death2") return 0;
        if (bossId == "death1" && effect == PhaseOneEffect.JudgmentWeakness)
        {
            if (source == DamageSource.AutomaticAttack) return 0;
            if (source == DamageSource.Judgment) return damage + 2;
        }
        return damage;
    }

    public static int GetTurnEndHp(string bossId, PhaseOneEffect effect, int hp, int maximum)
    {
        if (hp <= 0) return 0;
        if (bossId == "death2") return hp - 1;
        if (bossId == "death1" && effect == PhaseOneEffect.Regenerate)
            return System.Math.Min(maximum, hp + 4);
        return hp;
    }

    public static int GetActionPointMaximum(PhaseOneEffect effect)
        => effect == PhaseOneEffect.LimitActions ? 4 : 7;
}
