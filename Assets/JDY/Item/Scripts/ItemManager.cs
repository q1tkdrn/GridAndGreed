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
        Debug.LogWarning("아이템을 찾을 수 없습니다: " + itemId);
        return null;
    }
}
