using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CharacterSlot : MonoBehaviour
{
    public CharacterManager manager;
    private int currentIndex = 0;

    public Image leftIcon;
    public Image centerIcon;
    public Image rightIcon;

    public TMP_Text nameText;
    public TMP_Text priceText;
    public TMP_Text STRText;
    public TMP_Text INTText;
    public TMP_Text describeText;

    public CharacterData character; //empty
    public int price;
    void Start()
    {
        SetCharacter();
    }
    public void SetCharacter()
    {
        int leftIndex = (currentIndex - 1 + manager.characters.Length) % manager.characters.Length;
        int rightIndex = (currentIndex + 1) % manager.characters.Length;

        leftIcon.sprite = manager.characters[leftIndex].icon;
        rightIcon.sprite = manager.characters[rightIndex].icon;

        character = manager.characters[currentIndex];

        centerIcon.sprite = character.icon;
        nameText.text = character.characterName;
        price = character.price;
        priceText.text = price.ToString();
        STRText.text = character.STR.ToString();
        INTText.text = character.INT.ToString();
        describeText.text = character.description;
    }
    public void LeftButton()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = manager.characters.Length - 1;
        SetCharacter();
    }
    public void RightButton()
    {
        currentIndex++;
        if (currentIndex >= manager.characters.Length)
            currentIndex = 0;
        SetCharacter();
    }
}
