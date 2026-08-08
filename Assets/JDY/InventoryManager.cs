using System.Collections.Generic;
using UnityEngine;
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    private int soul = 0;

    private int itemIdMax = 24;
    private Dictionary<string, int> ownedItems = new Dictionary<string, int>();
    
    private List<string> ownedCharacters = new List<string>();
    void Awake()//Obj
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            soul = PlayerPrefs.GetInt("Soul", 0);
            for(int i = 0; i < itemIdMax; i++)
            {
                int count = PlayerPrefs.GetInt("Item_" + i, 0);

                if (count > 0)
                    ownedItems.Add(i.ToString(), count);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    //Soul
    public void AddSoul(int amount) 
    {
        soul += amount;
        PlayerPrefs.SetInt("Soul", soul);
        PlayerPrefs.Save();
    }
    public void RemoveSoul(int amount)
    {
        soul -= amount;
        PlayerPrefs.SetInt("Soul", soul);
        PlayerPrefs.Save();
    }
    public int GetSoul()
    {
        return soul;
    }
    //Item
    public void AddItem(string itemId, int amount = 1)
    {
        if (ownedItems.ContainsKey(itemId))
            ownedItems[itemId] += amount;
        else
            ownedItems.Add(itemId, amount);

        PlayerPrefs.SetInt("Item_" + itemId, ownedItems[itemId]);
        //PlayerPrefs.Save();
    }
    public void AddItem(ItemData item, int amount = 1)
    {
        AddItem(item.id, amount);
    }
    public void RemoveItem(string itemId, int amount = 1)
    {
        if (!ownedItems.ContainsKey(itemId))
            return;

        ownedItems[itemId] -= amount;

        if (ownedItems[itemId] <= 0)
        {
            ownedItems.Remove(itemId);
            PlayerPrefs.DeleteKey("Item_" + itemId);
        }
        else
        {
            PlayerPrefs.SetInt("Item_" + itemId, ownedItems[itemId]);
        }

        //PlayerPrefs.Save();
    }
    public void RemoveItem(ItemData item, int amount = 1)
    {
        RemoveItem(item.id, amount);
    }
    public bool HasItem(string itemId)
    {
        return ownedItems.ContainsKey(itemId);
    }
    public bool HasItem(ItemData item)
    {
        return HasItem(item.id);
    }
    public int GetItemCount(string itemId)
    {
        return ownedItems.TryGetValue(itemId, out int count) ? count : 0;
    }
    public int GetItemCount(ItemData item)
    {
        return GetItemCount(item.id);
    }
    //Character
    /*
    public void AddCharacter(CharacterData character)
    {

    }
    */
    public void RemoveCharacter(string characterId)
    {
        ownedCharacters.Remove(characterId);
    }
    public void UnlockCharacter(string characterId)
    {
        ownedCharacters.Add(characterId);
    }
    public bool HasCharacter(string characterId)
    {
        return ownedCharacters.Contains(characterId);
    }
    /*
    public void UnlockMemory(MemoryData memory)
    {
        // 기억 해금
     }
    */
}
