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
            ShowSlot(i, item, item != null ? int.Parse(item.id) : 0);
        }
    }

    private const string EmptyLabel = "<color=#4A4A4A>비어있음</color>";

    /// <summary>장착 칸 하나를 그린다. 비어 있으면 설명 자리(칸 가운데)에 '비어있음'만 보여 준다. (빈 칸 판단은 itemId == 0)</summary>
    private void ShowSlot(int i, ItemData item, int id)
    {
        bool equipped = item != null;
        slots[i].itemId = equipped ? id : 0;
        slots[i].name.text = equipped ? item.itemName : "";
        slots[i].description.text = equipped ? item.description : EmptyLabel;
        slots[i].image.sprite = equipped ? item.icon : null;
        slots[i].name.gameObject.SetActive(equipped);
        slots[i].description.gameObject.SetActive(true);
        slots[i].image.gameObject.SetActive(equipped);
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
        // 여정 중 정비 제한 (JSW/Script/JourneyLink.cs)
        if (JourneyRefit.Limited)
        {
            var display = BattleDisplayManager.GetInstance();
            int slot = -1;
            for (int i = 0; i < slots.Length; i++)
                if (item.isEquip ? slots[i].itemId == id : slots[i].itemId == 0) { slot = i; break; }
            if (slot >= 0 && !JourneyRefit.CanSetItem(display.currentUnits, display.currentItems, slot, item.isEquip ? null : item.itemData)) return;
        }
        if (!item.isEquip)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].itemId != 0) continue;
                ShowSlot(i, item.itemData, id);
                BattleDisplayManager.GetInstance().currentItems[i] = item.itemData;
                items[id - 1].isEquip = true;
                break;
            }
        }
        else
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].itemId != id) continue;
                ShowSlot(i, null, 0);
                BattleDisplayManager.GetInstance().currentItems[i] = null;
                items[id - 1].isEquip = false;
                break;
            }
        }
        BattleDisplayManager.GetInstance().SaveFormation();
        JourneyRefit.Refresh(BattleDisplayManager.GetInstance().currentUnits, BattleDisplayManager.GetInstance().currentItems);
    }

    public void UnEquipItemInSlot(int i)
    {
        if (i < 0 || i >= slots.Length) return;
        if (slots[i].itemId == 0) return;
        if(!items[slots[i].itemId - 1].isEquip) return;
        var refitDisplay = BattleDisplayManager.GetInstance();
        if (!JourneyRefit.CanSetItem(refitDisplay.currentUnits, refitDisplay.currentItems, i, null)) return;
        items[slots[i].itemId - 1].isEquip = false;
        ShowSlot(i, null, 0);
        BattleDisplayManager.GetInstance().currentItems[i] = null;
        BattleDisplayManager.GetInstance().SaveFormation();
        JourneyRefit.Refresh(refitDisplay.currentUnits, refitDisplay.currentItems);
    }
}
