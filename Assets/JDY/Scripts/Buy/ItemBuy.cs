using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class ItemBuy : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private ItemSlot itemSlot;
    [SerializeField] private TMP_Text soulText;
    [SerializeField] private Button button;
    void Start()
    {
        SetBuy();
        IsBuy();
    }
    public void IsBuy()
    {
        button.interactable = !InventoryManager.Instance.HasItem(itemSlot.item.id);
    }
    public void BuyButton()
    {
        try 
        {
            if (InventoryManager.Instance.GetSoul() >= itemSlot.item.price)
            {
                InventoryManager.Instance.RemoveSoul(itemSlot.item.price);
                InventoryManager.Instance.AddItem(itemSlot.item);
                SetBuy();
                IsBuy();
                Debug.Log(itemSlot.item.name+"을 구입");
                AchievementManager.Instance.AddProgress("ACH-6", 1);
            }
            else
            {
                Debug.Log("잔액부족");
            }
        }
        catch
        {
            Debug.Log("Inventory Manager를 찾을 수 없는 오류");
        }
    }
    public void SetBuy()
    {
        soulText.text = InventoryManager.Instance.GetSoul().ToString();
    }
}
