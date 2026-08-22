using UnityEngine;

public class AchievementUI : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private AchievementManager manager;
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
        foreach (AchievementData data in manager.achievements)
        {
            GameObject obj = Instantiate(achievementSlotPrefab, content);

            AchievementSlot slot = obj.GetComponent<AchievementSlot>();
            slot.SetData(data);
        }
    }
}
