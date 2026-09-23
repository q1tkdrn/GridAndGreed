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
    private void OnEnable() => skinSlot.SelectionChanged += IsBuy;
    private void OnDisable() => skinSlot.SelectionChanged -= IsBuy;
    void Start()
    {
        SetBuy();
        IsBuy();
    }
    public void IsBuy()
    {
        button.interactable = skinSlot.HasCurrentSkin && InventoryManager.Instance != null
            && !InventoryManager.Instance.HasSkin(skinSlot.character.id, skinSlot.currentSkin);
    }
    public void BuyButton()
    {
        if (!skinSlot.HasCurrentSkin || InventoryManager.Instance == null
            || InventoryManager.Instance.HasSkin(skinSlot.character.id, skinSlot.currentSkin)) return;
        try
        {
            if (InventoryManager.Instance.GetSoul() >= SkinSlot.Price)
            {
                InventoryManager.Instance.RemoveSoul(SkinSlot.Price);
                InventoryManager.Instance.UnlockSkin(skinSlot.character.id, skinSlot.currentSkin);
                IsBuy();
                SetBuy();
                Debug.Log(skinSlot.character.name + "을 구입");
                AchievementManager.Instance.AddProgress("ACH-22", 1);
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
