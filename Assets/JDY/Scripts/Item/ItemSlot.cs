using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ItemSlot : MonoBehaviour
{
    [Header("Manager Obj")]
    [SerializeField] private ItemManager itemManager;
    [Header("Slot Image")]
    [SerializeField] private Image leftIcon;
    [SerializeField] private Image centerIcon;
    [SerializeField] private Image rightIcon;
    [Header("Item Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text describeText;

    private int currentIndex = 0;
    public ItemData item; //empty(ItemBuy)
    public int price;
    void Start()
    {
        SetItem();
    }
    public void SetItem()
    {
        int leftIndex = (currentIndex - 1 + itemManager.items.Length) % itemManager.items.Length;
        int rightIndex = (currentIndex + 1) % itemManager.items.Length;
        
        leftIcon.sprite = itemManager.items[leftIndex].icon;
        rightIcon.sprite = itemManager.items[rightIndex].icon;
        
        item = itemManager.items[currentIndex];

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
            currentIndex = itemManager.items.Length - 1;
        SetItem();
    }
    public void RightButton()
    {
        currentIndex++;
        if (currentIndex >= itemManager.items.Length)
            currentIndex = 0;
        SetItem();
    }
}
