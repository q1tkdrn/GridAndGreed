using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinSlot : MonoBehaviour
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
    [SerializeField] private TMP_Text describeText;

    public CharacterData character;//buy
    public string skinName;//dialog
    public Skin currentSkin;//inventory

    private List<DialogData> dialogs;
    void Start()
    {
        SetSkin(Skin.InsectSkin);
    }

    public void SetSkin(Skin skin)
    {
        currentSkin = skin;

        UpdateCharacter();
    }

    private void UpdateCharacter()
    {
        int leftIndex = (currentIndex - 1 + manager.characters.Length) % manager.characters.Length;
        int rightIndex = (currentIndex + 1) % manager.characters.Length;

        character = manager.characters[currentIndex];

        switch (currentSkin)
        {
            case Skin.NpcSkin:
                leftIcon.sprite = manager.characters[leftIndex].npcSkin;
                centerIcon.sprite = character.npcSkin;
                rightIcon.sprite = manager.characters[rightIndex].npcSkin;

                skinName = character.npcSkinName;
                nameText.text = skinName;
                dialogs = DialogManager.Instance.GetDialogueGroup("오키드", DialogType.Skin, "시리즈-사신", 2);
                describeText.text = dialogs[0].text;
                break;

            case Skin.InsectSkin:
                leftIcon.sprite = manager.characters[leftIndex].insectSkin;
                centerIcon.sprite = character.insectSkin;
                rightIcon.sprite = manager.characters[rightIndex].insectSkin;

                skinName = character.insectSkinName;
                nameText.text = skinName;
                dialogs = DialogManager.Instance.GetDialogueGroup("오키드", DialogType.Skin, "시리즈-곤충 이야기", 2);
                describeText.text = dialogs[0].text;
                break;
            case Skin.BossSkin:
                leftIcon.sprite = manager.characters[leftIndex].bossSkin;
                centerIcon.sprite = character.bossSkin;
                rightIcon.sprite = manager.characters[rightIndex].bossSkin;

                skinName = character.bossSkinName;
                nameText.text = skinName;
                dialogs = DialogManager.Instance.GetDialogueGroup("오키드", DialogType.Skin, "시리즈-왕국", 2);
                describeText.text = dialogs[0].text;
                break;
        }
        priceText.text = character.price.ToString();
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
    public void NpcSkinButton()
    {
        SetSkin(Skin.NpcSkin);
    }

    public void InsectSkinButton()
    {
        SetSkin(Skin.InsectSkin);
    }

    public void BossSkinButton()
    {
        SetSkin(Skin.BossSkin);
    }
}

public enum Skin
{
    NpcSkin,
    InsectSkin,
    BossSkin
}