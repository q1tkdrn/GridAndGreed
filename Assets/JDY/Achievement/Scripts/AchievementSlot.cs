using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AchievementSlot : MonoBehaviour
{
    public AchievementData data;

    public TMP_Text achievementName;
    public Button rewardButton;
    public Image stamp;

    public Sprite completedStamp;
    public Sprite rewardedStamp;

    void Start()
    {
        achievementName.text = data.name;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (AchievementManager.Instance.IsRewarded(data.id))
        {
            stamp.gameObject.SetActive(true);
            stamp.sprite = rewardedStamp;

            rewardButton.interactable = false;
        }
        else if (AchievementManager.Instance.IsCompleted(data.id))
        {
            stamp.gameObject.SetActive(true);
            stamp.sprite = completedStamp;

            rewardButton.interactable = true;
        }
        else
        {
            stamp.gameObject.SetActive(false);
            rewardButton.interactable = false;
        }
    }
    public void GetReward()
    {
        AchievementManager.Instance.GetReward(data.id);
        UpdateUI();
    }
}