using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemBuildingPanel : MonoBehaviour
{

    [Serializable]
    private struct ItemSlot
    {
        public GameObject gameObject;
        public TextMeshProUGUI name;
        public TextMeshProUGUI description;
        public Image image;
        public int itemId;
    }

    [SerializeField] private ItemUI[] items;
    [SerializeField] private ItemSlot[] slots = new ItemSlot[3];
    [SerializeField] private GameObject itemBoard;

    private void OnEnable()
    {
        Init();
    }

    public void Init()
    {
        var display = BattleDisplayManager.GetInstance();
        display.EnsureFormationLoaded();
        for (int i = 0; i < items.Length; i++)
        {
            items[i].itemData = ItemManager.Instance.GetItemData(items[i].id.ToString());
            items[i].isEquip = Array.Exists(display.currentItems, item => item != null && item == items[i].itemData);
            items[i].Init();
        }
        for (int i = 0; i < slots.Length; i++)
        {
            var item = display.currentItems[i];
            bool equipped = item != null;
            slots[i].itemId = equipped ? int.Parse(item.id) : 0;
            slots[i].name.text = equipped ? item.itemName : "";
            slots[i].description.text = equipped ? item.description : "";
            slots[i].image.sprite = equipped ? item.icon : null;
            slots[i].name.gameObject.SetActive(equipped);
            slots[i].description.gameObject.SetActive(equipped);
            slots[i].image.gameObject.SetActive(equipped);
        }
    }

    [ContextMenu("Setting")]
    public void Setting()
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i].id = i+1;
            items[i].popup = items[i].gameObject.transform.GetChild(0).gameObject;
            items[i].nameText = items[i].popup.GetComponentsInChildren<TextMeshProUGUI>()[0];
            items[i].descriptionText = items[i].popup.GetComponentsInChildren<TextMeshProUGUI>()[1];
            items[i].image = items[i].gameObject.GetComponent<Image>();
        }
    }

    public void OnItemClick(int id)
    {
        if (id < 1 || id > items.Length || !items[id - 1].unlock) return;
        var item = items[id - 1];
        if (!item.isEquip)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if(slot.name.text != "") continue;
                slot.name.text = item.itemData.itemName;
                slot.description.text = item.itemData.description;
                slot.image.sprite = item.itemData.icon;
                slots[i].itemId = id;
                slot.name.gameObject.SetActive(true);
                slot.image.gameObject.SetActive(true);
                slot.description.gameObject.SetActive(true);
                BattleDisplayManager.GetInstance().currentItems[i] = item.itemData;
                items[id - 1].isEquip = true;
                break;
            }
        }
        else
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot.itemId != id) continue;
                slots[i].itemId = 0;
                slot.name.text = "";
                slot.name.gameObject.SetActive(false);
                slot.description.gameObject.SetActive(false);
                slot.image.gameObject.SetActive(false);
                BattleDisplayManager.GetInstance().currentItems[i] = null;
                items[id - 1].isEquip = false;
                break;
            }
        }
        BattleDisplayManager.GetInstance().SaveFormation();
    }

    public void UnEquipItemInSlot(int i)
    {
        if (i < 0 || i >= slots.Length) return;
        if(!slots[i].name.gameObject.activeSelf) return;
        if(!items[slots[i].itemId - 1].isEquip) return;
        items[slots[i].itemId - 1].isEquip = false;
        slots[i].name.text = "";
        slots[i].name.gameObject.SetActive(false);
        slots[i].description.gameObject.SetActive(false);
        slots[i].image.gameObject.SetActive(false);
        BattleDisplayManager.GetInstance().currentItems[i] = null;
        slots[i].itemId = 0;
        BattleDisplayManager.GetInstance().SaveFormation();
    }
}
