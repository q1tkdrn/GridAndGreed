using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UnitBuildingPanel : MonoBehaviour
{
    [Header("Units")] public UnitTemp[] currentUnits = new UnitTemp[3];
    [SerializeField] private Unit[] unitObject = new Unit[3];

    [Header("UI")] [SerializeField] private GameObject formPanel;

    [SerializeField] private GameObject popup1;

    [SerializeField] private GameObject popup2;
    [SerializeField] private Unit popupUnit;
    [SerializeField] private GameObject arrow;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Space] [SerializeField] private GameObject changePanel;
    public Card[] cards = new Card[9];

    public Image[] skins = new Image[3];
    public TextMeshProUGUI[] skinTexts = new TextMeshProUGUI[3];

    private int _currentUnitIndex = 0;
    private int _currentCardIndex = 0;
    private int _cardIndex = 0;

    private void OnEnable()
    {
        currentUnits = BattleDisplayManager.GetInstance().currentUnits;
        Init();
    }

    private void Init()
    {
        unitObject[0].unitTemp = currentUnits[0];
        unitObject[1].unitTemp = currentUnits[1];
        unitObject[2].unitTemp = currentUnits[2];

        unitObject[0].Init();
        unitObject[1].Init();
        unitObject[2].Init();
        if(!InventoryManager.Instance.HasCharacter("1")) InventoryManager.Instance.UnlockCharacter("1");
        if(!InventoryManager.Instance.HasCharacter("2")) InventoryManager.Instance.UnlockCharacter("2");
        if(!InventoryManager.Instance.HasCharacter("3")) InventoryManager.Instance.UnlockCharacter("3");
        BattleDisplayManager.GetInstance().currentUnits = currentUnits;
    }

    public void OnMouseClick(int i)
    {
        popup2.SetActive(false);
        if (i == -1)
        {
            popup1.SetActive(false);
            return;
        }

        popup1.SetActive(true);
        var vector3 = popup1.transform.position;
        vector3.x = unitObject[i].transform.position.x;
        popup1.transform.position = vector3;
        _currentUnitIndex = i;
    }

    public void ShowInfo()
    {
        popup1.SetActive(false);
        popup2.SetActive(true);
        var vector3 = arrow.transform.position;
        vector3.x = unitObject[_currentUnitIndex].transform.position.x;
        arrow.transform.position = vector3;
        popupUnit.unitTemp = currentUnits[_currentUnitIndex];
        popupUnit.Init();

        nameText.text = popupUnit.unitTemp.unitName;
        descriptionText.text = popupUnit.unitTemp.abilityText;
    }

    public void OpenChangePanel()
    {
        formPanel.SetActive(false);
        changePanel.SetActive(true);

        var id = 0;
        foreach (var card in cards)
        {
            id++;
            card.isSelected = currentUnits.Contains(card.unitTemp);
            if (card.isSelected)
            {
                card.text.text = "<size=45>장착됨</size>";
            }
            
            card.Init();
            
            if (!InventoryManager.Instance.HasCharacter(id.ToString()))
            {
                card.text.text = "잠김";
                card.text.gameObject.SetActive(true);
            }
            
            if (card.unitTemp == currentUnits[_currentUnitIndex])
            {
                _currentCardIndex = cards.ToList().IndexOf(card);
                _cardIndex = _currentCardIndex;
                card.border.SetActive(true);
                card.text.text = "장착됨\n눌러서 완료";
            }
        }

        InitSkin();
    }

    public void OnCardClick(int i)
    {
        if (!InventoryManager.Instance.HasCharacter((i + 1).ToString())) return; 
        if (_currentCardIndex == i)
        {
            popup1.SetActive(false);
            popup2.SetActive(false);

            formPanel.SetActive(true);
            changePanel.SetActive(false);

            currentUnits[_currentUnitIndex] = cards[i].unitTemp;

            Init();
            return;
        }

        if (cards[i].isSelected && i != _cardIndex) return;

        cards[_currentCardIndex].Init();
        cards[_currentCardIndex].border.SetActive(false);
        if (_currentCardIndex == _cardIndex)
        {
            cards[_currentCardIndex].text.text = "장착됨";
        }

        _currentCardIndex = i;
        cards[_currentCardIndex].border.SetActive(true);
        cards[i].text.text = "한 번 더\n클릭하여\n교체";
        if (_currentCardIndex == _cardIndex)
        {
            cards[_currentCardIndex].text.text = "장착됨\n눌러서 완료";
        }

        cards[i].text.gameObject.SetActive(true);

        InitSkin();
    }

    private void InitSkin()
    {
        skins[0].sprite = cards[_currentCardIndex].unitTemp.skin1;
        skins[1].sprite = cards[_currentCardIndex].unitTemp.skin2;
        skins[2].sprite = cards[_currentCardIndex].unitTemp.skin3;

        skinTexts[0].text = cards[_currentCardIndex].unitTemp.isSkin1Unlocked ? "해금됨" : "잠금됨";
        skinTexts[1].text = cards[_currentCardIndex].unitTemp.isSkin2Unlocked ? "해금됨" : "잠금됨";
        skinTexts[2].text = cards[_currentCardIndex].unitTemp.isSkin3Unlocked ? "해금됨" : "잠금됨";
        
        if(cards[_currentCardIndex].unitTemp.currentSkin == 0) return;
        skinTexts[cards[_currentCardIndex].unitTemp.currentSkin - 1].text = "장착됨";
    }

    public void OnSkinClick(int i)
    {
        if (i == 1 && !cards[_currentCardIndex].unitTemp.isSkin1Unlocked) return;
        if (i == 2 && !cards[_currentCardIndex].unitTemp.isSkin2Unlocked) return;
        if (i == 3 && !cards[_currentCardIndex].unitTemp.isSkin3Unlocked) return;
        if(cards[_currentCardIndex].unitTemp.currentSkin == i) cards[_currentCardIndex].unitTemp.currentSkin = 0;
        else cards[_currentCardIndex].unitTemp.currentSkin = i;
        InitSkin();
    }
}