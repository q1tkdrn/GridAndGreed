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
    [SerializeField] private Image NPCNameplate;
    [SerializeField] private Sprite[] NPCNameplates;
    void Start()
    {
        CreateAchievementSlots();
        NPC.sprite = achievementDialog.currentPhase == 2 ? NPCImages[1] : NPCImages[0];
        NPCNameplate.sprite = achievementDialog.currentPhase == 2 ? NPCNameplates[1] : NPCNameplates[0];
    }
    public void CreateAchievementSlots()
    {
        int shown = 0, done = 0;   // 달성도 (보이는 업적만 센다: 숨은 업적 수가 드러나지 않게)
        foreach (AchievementData data in AchievementManager.Instance.achievements)
        {
            GameObject obj;

            if (!IsShowAchievement(data))
                continue;
            shown++;
            if (AchievementManager.Instance.IsCompleted(data.id)) done++;

            obj = Instantiate(achievementSlotPrefab, content);
            AchievementSlot slot = obj.GetComponent<AchievementSlot>();
            slot.SetData(data, FindReward(data));
        }
        ShowProgress(done, shown);
    }

    // ── 달성도 (JSW, 2026-11-03): 목록 아래 "달성 5 / 31" + 오른쪽에 퍼센트 ──
    private TMPro.TextMeshProUGUI _progressCount, _progressPercent;

    private void ShowProgress(int done, int total)
    {
        if (_progressCount == null)
        {
            var scroll = content != null ? content.GetComponentInParent<ScrollRect>() : null;
            if (scroll == null) return;
            var scrollRect = (RectTransform)scroll.transform;
            var font = scroll.transform.parent.GetComponentInChildren<TMPro.TMP_Text>(true);   // 창 제목 글꼴
            float y = scrollRect.anchoredPosition.y - scrollRect.sizeDelta.y * 0.5f - 42f;   // 목록 바로 아래
            _progressCount = NewProgressText("Achievement Progress", scrollRect.parent, font, new Vector2(-55f, y), TMPro.TextAlignmentOptions.Right, 34f);
            _progressPercent = NewProgressText("Achievement Percent", scrollRect.parent, font, new Vector2(95f, y), TMPro.TextAlignmentOptions.Left, 30f);
        }
        int percent = total > 0 ? Mathf.FloorToInt(done * 100f / total) : 0;   // 내림: 다 끝나기 전에 100%가 뜨지 않게
        _progressCount.text = $"달성 {done} / {total}";
        _progressPercent.text = $"{percent}%";
    }

    private static TMPro.TextMeshProUGUI NewProgressText(string name, Transform parent, TMPro.TMP_Text like, Vector2 pos, TMPro.TextAlignmentOptions align, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(align == TMPro.TextAlignmentOptions.Right ? 1f : 0f, 0.5f);
        r.sizeDelta = new Vector2(320f, 50f);
        r.anchoredPosition = pos;
        var t = go.GetComponent<TMPro.TextMeshProUGUI>();
        if (like != null) { t.font = like.font; t.color = like.color; }
        t.fontSize = size;
        t.alignment = align;
        t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
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