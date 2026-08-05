using System.Collections.Generic;
using UnityEditor.U2D.Animation;
using UnityEngine;
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public int soul = 0;//InventorManager.Instance.soul
    public List<ItemData> ownedItems;
    void Awake()//Obj
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
    public void AddSoul(int amount) 
    {
        soul += amount;
    }
    public void RemoveSoul(int amount)
    {
        soul -= amount;
    }
    public void AddItem(ItemData item)
    {
        ownedItems.Add(item);
    }
    public void RemoveItem(ItemData item)
    {
        ownedItems.Remove(item);
    }
    /*
    public void AddCharacter(CharacterData character)
    {
        
    }
    public void RemoveCharacter(CharacterData character)
    {
        
    }
    public void UnlockCharacter(CharacterData character)
    {
        
    }
    */
    /*
    public void UnlockMemory(MemoryData memory)
    {
        // 기억 해금
    }
    */
}
