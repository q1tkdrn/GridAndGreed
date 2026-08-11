using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ItemSlot : MonoBehaviour
{
    public ItemManager manager;
    private int currentIndex = 0;

    public Image leftIcon;
    public Image centerIcon;
    public Image rightIcon; 

    public TMP_Text nameText;
    public TMP_Text priceText;
    public TMP_Text describeText;

    public ItemData item; //empty
    public int price;
    void Start()
    {
        SetItem();
    }
    public void SetItem()
    {
        int leftIndex = (currentIndex - 1 + manager.items.Length) % manager.items.Length;
        int rightIndex = (currentIndex + 1) % manager.items.Length;
        
        leftIcon.sprite = manager.items[leftIndex].icon;
        rightIcon.sprite = manager.items[rightIndex].icon;
        
        item = manager.items[currentIndex];

        centerIcon.sprite = item.icon;
        nameText.text = item.itemName;
        price = item.price;
        priceText.text = price.ToString();
        describeText.text = item.description;

    }
    public void LeftButton()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = manager.items.Length - 1;
        SetItem();
    }
    public void RightButton()
    {
        currentIndex++;
        if (currentIndex >= manager.items.Length)
            currentIndex = 0;
        SetItem();
    }
}
