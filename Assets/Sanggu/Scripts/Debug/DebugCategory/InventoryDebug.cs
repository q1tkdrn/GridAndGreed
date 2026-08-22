using UnityEngine;

public class InventoryDebug : MonoBehaviour
{
    [DebugButton("소울 추가")]
    public void AddSoul(int soul)
    {
        InventoryManager.Instance.AddSoul(soul);
        DebugLog.GetInstance().Log($"AddSoul({soul}), CurrentSoul({InventoryManager.Instance.GetSoul()})");
    }
    
    [DebugButton("소울 제거")]
    public void RemoveSoul(int soul)
    {
        InventoryManager.Instance.RemoveSoul(soul);
        DebugLog.GetInstance().Log($"RemoveSoul({soul}), CurrentSoul({InventoryManager.Instance.GetSoul()})");
    }

    [DebugButton("아이템 추가")]
    public void AddItem(string itemId)
    {
        InventoryManager.Instance.AddItem(itemId);
        DebugLog.GetInstance().Log($"AddItem({itemId})");
    }
    
    [DebugButton("아이템 제거")]
    public void RemoveItem(string itemId)
    {
        InventoryManager.Instance.RemoveItem(itemId);
        DebugLog.GetInstance().Log($"RemoveItem({itemId})");
    }

    [DebugButton("캐릭터 해금")]
    public void AddCharacter(string characterId)
    {
        InventoryManager.Instance.UnlockCharacter(characterId);
        DebugLog.GetInstance().Log($"AddCharacter({characterId})");
    }
}
