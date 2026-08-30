using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AchievementSlot : MonoBehaviour
{
    private AchievementData data;
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image stamp;
    [Header("Stamp Image")]
    [SerializeField] private Sprite completedStamp;
    [SerializeField] private Sprite rewardedStamp;
    public void SetData(AchievementData data)
    {
        this.data = data;

        nameText.text = data.title;
        UpdateUI();
    }
    public void UpdateUI()
    {
        if (AchievementManager.Instance.IsRewarded(data.id))
        {
            stamp.gameObject.SetActive(true);
            stamp.sprite = rewardedStamp;
        }
        else if (AchievementManager.Instance.IsCompleted(data.id))
        {
            stamp.gameObject.SetActive(true);
            stamp.sprite = completedStamp;
        }
        else
        {
            stamp.gameObject.SetActive(false);
        }
    }
    public void AchievementButton()
    {
        AchievementDialog.Instance.StartAchievementDialog(data);
        AchievementManager.Instance.GetReward(data.id);
        UpdateUI();
    }
}