using UnityEngine;
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;
    public ItemData[] items;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public ItemData GetItemData(string itemId)
    {
        foreach (ItemData item in items)
        {
            if (item.id == itemId)
            {
                return item;
            }
        }
        Debug.LogWarning("아이템 없음: " + itemId);
        return null;
    }
}
