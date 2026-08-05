using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CharacterSlot : MonoBehaviour
{
    public CharacterShopManager shop;
    private int currentIndex = 0;

    public Image leftIcon;
    public Image centerIcon;
    public Image rightIcon;

    public TMP_Text nameText;
    public TMP_Text priceText;
    public TMP_Text STRText;
    public TMP_Text INTText;

    public CharacterData character; //empty
    public int price;
    void Start()
    {
        SetCharacter();
    }
    public void SetCharacter()
    {
        int leftIndex = (currentIndex - 1 + shop.characters.Length) % shop.characters.Length;
        int rightIndex = (currentIndex + 1) % shop.characters.Length;

        leftIcon.sprite = shop.characters[leftIndex].icon;
        rightIcon.sprite = shop.characters[rightIndex].icon;

        character = shop.characters[currentIndex];
        centerIcon.sprite = character.icon;
        nameText.text = character.characterName;

        price = character.price;
        priceText.text = price.ToString();
    }
    public void LeftButton()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = shop.characters.Length - 1;
        SetCharacter();
    }
    public void RightButton()
    {
        currentIndex++;
        if (currentIndex >= shop.characters.Length)
            currentIndex = 0;
        SetCharacter();
    }
}
