using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemBuildingPanel : MonoBehaviour
{
    [Serializable]
    private struct ItemObject
    {
        public string itemName;
        public int id;
        public GameObject gameObject;
        public GameObject popup;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public Image image;
        public bool unlock;
    }

    [Serializable]
    private struct ItemSlot
    {
        public GameObject gameObject;
        public TextMeshProUGUI name;
        public TextMeshProUGUI description;
    }

    [SerializeField] private ItemObject[] items;
    [SerializeField] private ItemSlot[] slots = new ItemSlot[3];
    [SerializeField] private GameObject itemBoard;

    private void OnEnable()
    {
        Init();
    }

    //[ContextMenu("Init()")]
    public void Init()
    {
        for (int i = 0; i < items.Length; i++)
        {
            /*if (items[i].id == 0)
            {
                items[i].id = i+1;
                items[i].gameObject = itemBoard.transform.GetChild(i).gameObject;
                items[i].popup = items[i].gameObject.transform.GetChild(0).gameObject;
                items[i].nameText = items[i].popup.GetComponentsInChildren<TextMeshProUGUI>()[0];
                items[i].descriptionText = items[i].popup.GetComponentsInChildren<TextMeshProUGUI>()[1];
            }*/
            items[i].unlock = InventoryManager.Instance.HasItem(items[i].id.ToString());
            items[i].gameObject.SetActive(items[i].unlock);
        }
    }

    public void OnItemClick(int id)
    {
        var item = items[id];
        //item이 장착할 칸이 없을 경우 리턴
    }

    public void UnEquipItem(int i)
    {
        
    }
}