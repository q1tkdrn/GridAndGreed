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
            PlayerPrefs.DeleteAll();//Debug
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
    private bool IsCompleted(string id)
    {
        return PlayerPrefs.GetInt(id + "_Completed", 0) == 1;
    }
    public void GetReward(string id)//ex)AchievementManager.Instance.GetReward("ACH-1");
    {//Button
        if (!IsCompleted(id) || IsRewarded(id))
            return;

        foreach (AchievementData data in achievements)
        {
            if (data.id != id)
                continue;

            switch (data.rewardType)
            {
                case RewardType.Soul:
                    InventoryManager.Instance.AddSoul(data.rewardSoul);
                    break;

                case RewardType.Item:
                    InventoryManager.Instance.AddItem(data.rewardItem);
                    break;
                    /*
                 case RewardType.Character:
                    InventoryManager.Instance.UnlockCharacter(data.rewardCharacter);
                    break;
                case RewardType.Memory:
                    InventoryManager.Instance.UnlockMemory(data.rewardMemory);
                    break;
                    */
            }
            PlayerPrefs.SetInt(id + "_Rewarded", 1);//ex)ACH-1_Rewarded = 1(받음)
            //PlayerPrefs.Save(); //Debug
            Debug.Log("업적 보상 받음 : " + id);
            break;
        }
    }
    private bool IsRewarded(string id)
    {
        return PlayerPrefs.GetInt(id + "_Rewarded", 0) == 1;
    }
}
