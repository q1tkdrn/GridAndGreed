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
    [Tooltip("비어 있으면 기본 6개 패턴을 순서대로 사용합니다.")]
    public BattlePatternRules.Pattern[] patterns;
    [Min(1)] public int patternDamage = 5;
    [Min(0.1f)] public float patternPreviewSeconds = 1f;
}
