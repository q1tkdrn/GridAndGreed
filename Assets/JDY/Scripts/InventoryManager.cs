using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Inventory")]
    [SerializeField] private int soul = 0;
    [SerializeField] private Dictionary<string, int> ownedItems = new Dictionary<string, int>();
    [SerializeField] private List<string> ownedCharacters = new List<string>();
    [SerializeField] private List<string> ownedMemorials = new List<string>();

    private int memorialCount = 8;
    private int characterCount = 9;
    // Init
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
    private void Start()
    {
        LoadSoul();
        LoadItems();
        LoadCharacters();
        LoadMemorials();
    }
    // Soul
    private void LoadSoul()
    {
        soul = PlayerPrefs.GetInt("Soul", 0);
    }
    public void AddSoul(int amount)
    {
        soul += amount;

        PlayerPrefs.SetInt("Soul", soul);
        PlayerPrefs.Save();
    }
    public void RemoveSoul(int amount)
    {
        soul -= amount;

        AchievementManager.Instance.AddProgress("ACH-3", amount);
        AchievementManager.Instance.AddProgress("ACH-4", amount);

        PlayerPrefs.SetInt("Soul", soul);
        PlayerPrefs.Save();
    }
    public int GetSoul()
    {
        return soul;
    }
    // Item
    private void LoadItems()
    {
        for (int i = 1; i <= ItemManager.Instance.items.Length; i++)
        {
            int count = PlayerPrefs.GetInt("Item_" + i, 0);

            if (count > 0)
                ownedItems.Add(i.ToString(), count);
        }
    }
    public void AddItem(string itemId, int amount = 1)
    {
        if (HasItem(itemId))
            return;

        if (ownedItems.ContainsKey(itemId))
            ownedItems[itemId] += amount;
        else
            ownedItems.Add(itemId, amount);

        PlayerPrefs.SetInt("Item_" + itemId, ownedItems[itemId]);

        if (HasAllItems())
        {
            AchievementManager.Instance.AddProgress("ACH-31", 1);
        }

        PlayerPrefs.Save();
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

        PlayerPrefs.Save();
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
    private bool HasAllItems()
    {
        for (int i = 1; i <= ItemManager.Instance.items.Length; i++)
        {
            if (!HasItem(i.ToString()))
                return false;
        }

        return true;
    }
    // Character
    private void LoadCharacters()
    {
        for (int i = 1; i <= characterCount; i++)
        {
            string id = i.ToString();

            if (PlayerPrefs.GetInt("Character_" + id, 0) == 1)
                ownedCharacters.Add(id);
        }
    }
    public void UnlockCharacter(string characterId)
    {
        if (HasCharacter(characterId))
            return;

        ownedCharacters.Add(characterId);

        PlayerPrefs.SetInt("Character_" + characterId, 1);

        if (HasAllCharacters())
        {
            AchievementManager.Instance.AddProgress("ACH-31", 1);
        }

        PlayerPrefs.Save();
    }
    public bool HasCharacter(string characterId)
    {
        return ownedCharacters.Contains(characterId);
    }
    private bool HasAllCharacters()
    {
        for (int i = 1; i <= characterCount; i++)
        {
            if (!HasCharacter(i.ToString()))
                return false;
        }

        return true;
    }
    // Skin
    public void UnlockSkin(string characterId, Skin skin)
    {
        PlayerPrefs.SetInt($"Skin_{characterId}_{skin}", 1);

        if (HasAllSkins())
        {
            AchievementManager.Instance.AddProgress("ACH-31", 1);
        }

        PlayerPrefs.Save();
    }
    public bool HasSkin(string characterId, Skin skin)
    {
        return PlayerPrefs.GetInt($"Skin_{characterId}_{skin}", 0) == 1;
    }
    private bool HasAllSkins()
    {
        // 1~8
        for (int i = 1; i <= characterCount - 1; i++)
        {
            if (!HasSkin(i.ToString(), Skin.BossSkin) || !HasSkin(i.ToString(), Skin.InsectSkin) || !HasSkin(i.ToString(), Skin.NpcSkin))
            {
                return false;
            }
        }
        return true;
    }
    // Memorial
    private void LoadMemorials()
    {
        for (int i = 1; i <= memorialCount; i++)
        {
            string id = i.ToString();

            if (PlayerPrefs.GetInt("Memorial_" + id, 0) == 1)
                ownedMemorials.Add(id);
        }
    }
    public void UnlockMemorial(string memorialId)
    {
        if (HasMemorial(memorialId))
            return;

        ownedMemorials.Add(memorialId);

        PlayerPrefs.SetInt("Memorial_" + memorialId, 1);

        if (HasMemorial("1") && HasMemorial("2") && HasMemorial("3") && HasMemorial("6"))
        {
            AchievementManager.Instance.AddProgress("ACH-24", 1);

            // All
            if (HasMemorial("4") && HasMemorial("5") && HasMemorial("7") && HasMemorial("8"))
            {
                AchievementManager.Instance.AddProgress("ACH-31", 1);
            }
        }

        PlayerPrefs.Save();
    }
    public bool HasMemorial(string memorialId)
    {
        return ownedMemorials.Contains(memorialId);
    }
}
