using System;

[Serializable]
public class AchievementReward
{
    public RewardType type;

    public string rewardID;
    public int amount;
}

public enum RewardType
{
    Soul,
    Item,
    Character,
    Memorial
}