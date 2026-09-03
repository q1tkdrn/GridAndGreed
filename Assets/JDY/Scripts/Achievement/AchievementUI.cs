using UnityEngine;

public class AchievementUI : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject achievementSlotPrefab;
    [Header("UI")]
    [SerializeField] private Transform content;
    void Start()
    {
        CreateAchievementSlots();
    }
    public void CreateAchievementSlots()
    {
        foreach (AchievementData data in AchievementManager.Instance.achievements)
        {
            GameObject obj;

            if (data.id != "ACH-28" || data.id != "ACH-29" || data.id != "ACH-30" || data.id != "ACH-31")
            {
                obj = Instantiate(achievementSlotPrefab, content);
                AchievementSlot slot = obj.GetComponent<AchievementSlot>();
                slot.SetData(data);
            }
        }
    }
}
