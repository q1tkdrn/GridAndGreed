using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ItemSlot : MonoBehaviour
{
    [Header("Slot Image")]
    [SerializeField] private Image leftIcon;
    [SerializeField] private Image centerIcon;
    [SerializeField] private Image rightIcon;

    [Header("Item Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text describeText;

    private List<ItemData> shopItems = new List<ItemData>();
    private int currentIndex = 0;

    public ItemData item;//empty(ItemBuy)
    void Start()
    {
        foreach (ItemData itemData in ItemManager.Instance.items)
        {
            if (itemData == null) continue;
            if (itemData.price >= 0)
            {
                shopItems.Add(itemData);
            }
        }
        SetItem();
    }

    public void SetItem()
    {
        int leftIndex = (currentIndex - 1 + shopItems.Count) % shopItems.Count;
        int rightIndex = (currentIndex + 1) % shopItems.Count;

        leftIcon.sprite = shopItems[leftIndex].icon;
        rightIcon.sprite = shopItems[rightIndex].icon;

        item = shopItems[currentIndex];

        centerIcon.sprite = item.icon;
        nameText.text = item.itemName;

        priceText.text = item.price.ToString();

        describeText.text = item.description;
    }

    public void LeftButton()
    {
        currentIndex--;

        if (currentIndex < 0)
            currentIndex = shopItems.Count - 1;

        SetItem();
    }
    public void RightButton()
    {
        currentIndex++;

        if (currentIndex >= shopItems.Count)
            currentIndex = 0;

        SetItem();
    }
}