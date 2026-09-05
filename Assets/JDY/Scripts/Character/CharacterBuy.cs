using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CharacterBuy : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private CharacterSlot characterSlot;
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
        button.interactable = !InventoryManager.Instance.HasCharacter(characterSlot.character.id);
    }
    public void BuyButton()
    {
        try
        {
            if (InventoryManager.Instance.GetSoul() >= characterSlot.character.price)
            {
                InventoryManager.Instance.RemoveSoul(characterSlot.character.price);
                InventoryManager.Instance.UnlockCharacter(characterSlot.character.id);
                SetBuy();
                IsBuy();
                Debug.Log(characterSlot.character.name + "을 구입");
                AchievementManager.Instance.AddProgress("ACH-5", 1);
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
