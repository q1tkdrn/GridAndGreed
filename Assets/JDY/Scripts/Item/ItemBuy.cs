using UnityEngine;
using TMPro;
public class ItemBuy : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private ItemSlot itemSlot;
    [SerializeField] private TMP_Text soulText;
    void Update()
    {
        soulText.text = InventoryManager.Instance.GetSoul().ToString();
    }
    public void BuyButton()
    {
        try 
        {
            if (InventoryManager.Instance.GetSoul() >= itemSlot.item.price)
            {
                InventoryManager.Instance.RemoveSoul(itemSlot.item.price);
                InventoryManager.Instance.AddItem(itemSlot.item);

                Debug.Log(itemSlot.item.name+"을 구입");
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
}
