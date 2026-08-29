using UnityEngine;

[CreateAssetMenu(fileName = "Boss", menuName = "Boss")]
public class BossTemp : ScriptableObject
{
    [Header("Boss")]
    public string bossName;
    public Sprite bossSprite;
    public Vector2Int size;
    public Vector2Int sizeOnCutScene;

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
}