using TMPro;
using UnityEngine;
public class CharacterBuy : MonoBehaviour
{
    public CharacterSlot CharacterSlot;
    public TMP_Text soulText;
    void Update()
    {
        soulText.text = InventoryManager.Instance.GetSoul().ToString();
    }
    public void BuyButton()
    {
        try
        {
            if (InventoryManager.Instance.GetSoul() >= CharacterSlot.price)
            {
                InventoryManager.Instance.RemoveSoul(CharacterSlot.price);
                InventoryManager.Instance.UnlockCharacter(CharacterSlot.character.id);

                Debug.Log(CharacterSlot.character.name + "을 구입");
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
