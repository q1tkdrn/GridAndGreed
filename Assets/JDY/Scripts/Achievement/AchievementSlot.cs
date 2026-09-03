using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AchievementSlot : MonoBehaviour
{
    private AchievementData data;
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image stamp;
    [SerializeField] private Button button;
    [Header("Stamp Image")]
    [SerializeField] private Sprite completedStamp;
    [SerializeField] private Sprite rewardedStamp;
    public void SetData(AchievementData data)
    {
        this.data = data;
        button.image.sprite = data.icon;
        nameText.text = data.title;
        descriptionText.text = data.description;
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