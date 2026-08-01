using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ItemSlot : MonoBehaviour
{
    public ItemShopManager shop;
    private int currentIndex = 0;

    public Image leftIcon;
    public Image centerIcon;
    public Image rightIcon; 

    public TMP_Text nameText;
    public TMP_Text priceText;

    public ItemData item; //empty
    public int price;
    void Start()
    {
        SetItem();
    }
    public void SetItem()
    {
        int leftIndex = (currentIndex - 1 + shop.items.Length) % shop.items.Length;
        int rightIndex = (currentIndex + 1) % shop.items.Length;
        
        leftIcon.sprite = shop.items[leftIndex].icon;
        rightIcon.sprite = shop.items[rightIndex].icon;
        
        item = shop.items[currentIndex];
        centerIcon.sprite = item.icon;
        nameText.text = item.itemName;

        price = item.price;
        priceText.text = price.ToString();
    }
    public void LeftButton()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = shop.items.Length - 1;
        SetItem();
    }
    public void RightButton()
    {
        currentIndex++;
        if (currentIndex >= shop.items.Length)
            currentIndex = 0;
        SetItem();
    }
}
