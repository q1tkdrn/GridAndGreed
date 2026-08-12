using UnityEngine;

[CreateAssetMenu(fileName = "Boss", menuName = "Boss")]
public class BossTemp : ScriptableObject
{
    [Header("Boss")]
    public string bossName;
    public Sprite bossSprite;
    public Vector2Int size;
    public Vector2Int sizeOnCutScene;
    
    [Header("Stage")]
    public string stageName;
    public Sprite stageSprite;
}