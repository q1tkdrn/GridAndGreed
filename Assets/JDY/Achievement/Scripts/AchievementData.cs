using UnityEngine;

[CreateAssetMenu(fileName = "AchievementData", menuName = "Scriptable Objects/AchievementData")]
public class AchievementData : ScriptableObject
{
    public string id;
    public string title;
    public string description;

    public int targetValue;

    public RewardType rewardType;//0 = soul, 1 = item, 2 = character, 3 = memory
    public int rewardSoul;//soul
    public ItemData rewardItem;
    //public CharacterData rewardCharacter;
    //public MemoryData rewardMemory;
}
