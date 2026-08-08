using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public int id;
    public ItemData itemData;
    public GameObject popup;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image image;
    public bool unlock;
    public bool isEquip;

    public void Init()
    {
        if (itemData is null)
        {
            unlock = false;
            image.gameObject.SetActive(false);
            return;
        }
        id = int.Parse(itemData.id);
        unlock = InventoryManager.Instance.HasItem(id.ToString());
        image.gameObject.SetActive(unlock);
        nameText.text = itemData.itemName;
        descriptionText.text = itemData.description;
        image.sprite = itemData.icon;
    }

    public void OnMouseEnter()
    {
        if(!unlock) return;
        popup.SetActive(true);
    }

    public void OnMouseExit()
    {
        if(!unlock) return;
        popup.SetActive(false);
    }
}