using UnityEngine;
using UnityEngine.UI;
public class AchievementUI : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject achievementSlotPrefab;
    [Header("Script")]
    [SerializeField] private AchievementDialog achievementDialog;
    [Header("UI")]
    [SerializeField] private Transform content;
    [Header("Image")]
    [SerializeField] private Image NPC;
    [SerializeField] private Sprite[] NPCImages;
    void Start()
    {
        CreateAchievementSlots();
        NPC.sprite = achievementDialog.isEnding == 2 ? NPCImages[1] : NPCImages[0];
    }
    public void CreateAchievementSlots()
    {
        foreach (AchievementData data in AchievementManager.Instance.achievements)
        {
            GameObject obj;

            if (!IsShowAchievement(data))
                continue;

            obj = Instantiate(achievementSlotPrefab, content);
            AchievementSlot slot = obj.GetComponent<AchievementSlot>();
            slot.SetData(data);
        }
    }
    private bool IsShowAchievement(AchievementData data)
    {
        int ending = achievementDialog.isEnding;

        if (ending == 2)
            return true;

        if (data.id == "ACH-28" || data.id == "ACH-29")
            return ending == 1;

        if (data.id == "ACH-30" || data.id == "ACH-31")
            return false;

        return true;
    }
}
