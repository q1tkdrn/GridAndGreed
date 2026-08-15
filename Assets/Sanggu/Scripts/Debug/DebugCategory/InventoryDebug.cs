using UnityEngine;

public class InventoryDebug : MonoBehaviour
{
    [DebugButton("소울 추가")]
    public void AddSoul(int soul)
    {
        InventoryManager.Instance.AddSoul(soul);
    }
    
    [DebugButton("소울 제거")]
    public void RemoveSoul(int soul)
    {
        InventoryManager.Instance.RemoveSoul(soul);
    }

    [DebugButton("아이템 추가")]
    public void AddItem(string itemId)
    {
        InventoryManager.Instance.AddItem(itemId);
    }
    
    [DebugButton("아이템 제거")]
    public void RemoveItem(string itemId)
    {
        InventoryManager.Instance.RemoveItem(itemId);
    }
}
