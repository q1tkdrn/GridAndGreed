using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class SkinBuy : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private SkinSlot skinSlot;
    [Header("UI")]
    [SerializeField] private TMP_Text soulText;
    [SerializeField] private Button button;
    void Start()
    {
        SetBuy();
        IsBuy();
    }
    public void IsBuy()
    {
        button.interactable = !InventoryManager.Instance.HasSkin(skinSlot.character.id, skinSlot.currentSkin);
    }
    public void BuyButton()
    {
        try
        {
            if (InventoryManager.Instance.GetSoul() >= skinSlot.character.price)
            {
                InventoryManager.Instance.RemoveSoul(skinSlot.character.price);
                InventoryManager.Instance.UnlockSkin(skinSlot.character.id, skinSlot.currentSkin);
                IsBuy();
                SetBuy();
                Debug.Log(skinSlot.character.name + "을 구입");
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
