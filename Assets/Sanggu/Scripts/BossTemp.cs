using UnityEngine;

[CreateAssetMenu(fileName = "Boss", menuName = "Boss")]
public class BossTemp : ScriptableObject
{
    [Header("Boss")]
    public string bossName;
    public string bossId;
    public Sprite bossSprite;

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
    [Tooltip("보스 공격 칸이 빨갛게 보인 뒤 실제로 맞을 때까지의 시간. 공격 칸 자체는 BellkeeperTrial의 보스별 패턴을 쓴다.")]
    [Min(0.1f)] public float patternPreviewSeconds = 1f;
}

/// <summary>보스가 받는 피해의 종류. 지금 규칙에서는 심판(Judgment)만 피해를 준다.</summary>
public static class AfterlifePassiveRules
{
    public enum DamageSource { AutomaticAttack, Judgment, Other }
}
