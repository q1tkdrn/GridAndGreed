using UnityEngine;
using UnityEngine.UI;
public class AchievementUI : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject achievementSlotPrefab;
    [Header("Script")]
    [SerializeField] private AchievementDialog achievementDialog;
    [SerializeField] private CharacterManager characterManager;//item=instance, character, memorial, none, soul
    [SerializeField] private MemorialManager memorialManager;
    [Header("UI")]
    [SerializeField] private Transform content;
    [Header("Image")]
    [SerializeField] private Image NPC;
    [SerializeField] private Sprite[] NPCImages;
    void Start()
    {
        CreateAchievementSlots();
        NPC.sprite = achievementDialog.currentPhase == 2 ? NPCImages[1] : NPCImages[0];
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
            slot.SetData(data, FindReward(data));
        }
    }
    private bool IsShowAchievement(AchievementData data)
    {
        int ending = achievementDialog.currentPhase;

        if (ending == 2)
            return true;

        if (data.id == "ACH-28" || data.id == "ACH-29")
            return ending == 1;

        if (data.id == "ACH-30" || data.id == "ACH-31")
            return false;

        return true;
    }
    private string FindReward(AchievementData data)
    {
        string rewardText = "보상 : ";

        foreach (AchievementReward reward in data.rewards)
        {
            switch (reward.type)
            {
                case RewardType.Soul:
                    rewardText += "소울 "+reward.amount + "개\n";
                    break;

                case RewardType.Item:
                    rewardText += "아이템 - "+FindItemName(reward.rewardID) + "\n";
                    break;

                case RewardType.Memorial:
                    rewardText += "기억 - "+FindMemorialName(reward.rewardID) + "\n";
                    break;

                case RewardType.Character:
                    rewardText += "캐릭터 - "+FindCharacterName(reward.rewardID) + "\n";
                    break;

                case RewardType.None:
                    rewardText += "없음";
                    break;
            }
        }
        return rewardText;
    }

    private string FindItemName(string id)
    {
        foreach (ItemData item in ItemManager.Instance.items)
        {
            if (item.id == id) return item.itemName;
        }
        return "없음";
    }

    private string FindMemorialName(string id)
    {
        foreach (MemorialData memorial in memorialManager.memorials)
        {
            if (memorial.id == id) return memorial.memorialName;
        }
        return "없음";
    }

    private string FindCharacterName(string id)
    {
        foreach (CharacterData character in characterManager.characters)
        {
            if (character.id == id) return character.characterName;
        }
        return "없음";
    }
}