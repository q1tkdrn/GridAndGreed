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

    public CharacterData character;//buy
    void Start()
    {
        UpdateCharacter();
    }

    private void UpdateCharacter()
    {
        int leftIndex = (currentIndex - 1 + manager.characters.Length) % manager.characters.Length;
        int rightIndex = (currentIndex + 1) % manager.characters.Length;

        character = manager.characters[currentIndex];
        leftIcon.sprite = manager.characters[leftIndex].icon;
        centerIcon.sprite = character.icon;
        rightIcon.sprite = manager.characters[rightIndex].icon;

        nameText.text = character.characterName;
        describeText.text = character.description;
        priceText.text = character.price.ToString();
        STRText.text = character.STR.ToString();
        INTText.text = character.INT.ToString();
    }

    public void LeftButton()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = manager.characters.Length - 1;
        UpdateCharacter();
    }
    public void RightButton()
    {
        currentIndex++;
        if (currentIndex >= manager.characters.Length)
            currentIndex = 0;
        UpdateCharacter();
    }
}