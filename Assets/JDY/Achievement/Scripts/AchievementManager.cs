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
    public void AddProgress(string id, int amount)
    {
        if (IsCompleted(id))
            return;

        int current = PlayerPrefs.GetInt(id + "_Current", 0);
        current += amount;
        PlayerPrefs.SetInt(id + "_Current", current);

        foreach (AchievementData data in achievements)
        {
            if (data.id == id)
            {
                if (current >= data.targetValue)
                {
                    PlayerPrefs.SetInt(id + "_Completed", 1);
                    Debug.Log("업적 달성 : " + id);
                }
                break;
            }
        }
        PlayerPrefs.Save();
    }
    public bool IsCompleted(string id)
    {
        return PlayerPrefs.GetInt(id + "_Completed", 0) == 1;
    }
    //AchievementManager.Instance.AddProgress("SpendMoney", money);
}
