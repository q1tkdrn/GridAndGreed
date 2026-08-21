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
    None,
    Soul,
    Item,
    Character,
    Memorial,
    Ending
}