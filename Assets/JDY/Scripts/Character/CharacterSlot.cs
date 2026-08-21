using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CharacterSlot : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private CharacterManager manager;
    private int currentIndex = 0;
    [Header("Slot Image")]
    [SerializeField] private Image leftIcon;
    [SerializeField] private Image centerIcon;
    [SerializeField] private Image rightIcon;
    [Header("Character Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text STRText;
    [SerializeField] private TMP_Text INTText;
    [SerializeField] private TMP_Text describeText;

    public CharacterData character; //empty
    private int price;
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
