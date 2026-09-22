using UnityEngine;

[CreateAssetMenu(fileName = "Unit", menuName = "Units/Unit")]
public class UnitTemp : ScriptableObject
{
    public string unitName;
    public int id;
    public int power;
    public int intelligence;
    public int reviveCool;
    public string abilityText;
    public Sprite illustration;
    public Sprite defaultSkin;
    public Sprite skin1;
    public Sprite skin2;
    public Sprite skin3;
    public bool isSkin1Unlocked = false;
    public bool isSkin2Unlocked = false;
    public bool isSkin3Unlocked = false;
    public int currentSkin = 0;

    public Sprite GetSkin(int index) => index switch
    {
        1 => skin1,
        2 => skin2,
        3 => skin3,
        _ => defaultSkin
    };

    public bool IsSkinUnlocked(int index)
    {
        if (index == 0) return true;
        if (index < 1 || index > 3 || GetSkin(index) == null) return false;
        // Existing UnitTemp assets store insect, boss, NPC (different from the shop enum).
        var skin = index switch { 1 => Skin.InsectSkin, 2 => Skin.BossSkin, _ => Skin.NpcSkin };
        return InventoryManager.Instance != null
            && InventoryManager.Instance.HasSkin(id.ToString(), skin);
    }
}
