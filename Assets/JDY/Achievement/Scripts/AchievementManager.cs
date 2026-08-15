using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;
    public AchievementData[] achievements;
    void Awake()//Obj
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void AddProgress(string id, int amount)//ex)AchievementManager.Instance.AddProgress("ACH-1", 10);
    {
        if (IsCompleted(id))
            return;

        int current = PlayerPrefs.GetInt(id + "_Current", 0);
        current += amount;
        PlayerPrefs.SetInt(id + "_Current", current);//ex)ACH-1_Current = 100

        foreach (AchievementData data in achievements)
        {
            if (data.id == id)
            {
                if (current >= data.targetValue)
                {
                    PlayerPrefs.SetInt(id + "_Completed", 1);//ex)ACH-1_Completed = 1(달성)
                    Debug.Log("업적 달성 : " + id);
                }
                break;
            }
        }
        //PlayerPrefs.Save();
    }
    public bool IsCompleted(string id)
    {
        return PlayerPrefs.GetInt(id + "_Completed", 0) == 1;
    }
    public void GetReward(string id)
    {
        if (!IsCompleted(id) || IsRewarded(id))
            return;

        foreach (AchievementData data in achievements)
        {
            if (data.id != id)
                continue;

            foreach (AchievementReward reward in data.rewards)
            {
                switch (reward.type)
                {
                    case RewardType.Soul:
                        InventoryManager.Instance.AddSoul(reward.amount);
                        break;

                    case RewardType.Item:
                        InventoryManager.Instance.AddItem(reward.rewardID, reward.amount);
                        break;
                    case RewardType.Memorial:
                        InventoryManager.Instance.UnlockMemorial(reward.rewardID);
                        break;
                    case RewardType.Character:
                        InventoryManager.Instance.UnlockCharacter(reward.rewardID);
                        break;
                }
            }

            PlayerPrefs.SetInt(id + "_Rewarded", 1);

            Debug.Log("보상 지급: " + id);
            break;
        }
    }
    public bool IsRewarded(string id)
    {
        return PlayerPrefs.GetInt(id + "_Rewarded", 0) == 1;
    }
}
