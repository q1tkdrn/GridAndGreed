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
    [Tooltip("스킨 글자 바탕 (JSW)")] public GameObject[] skinLabelBacks = new GameObject[3];

    private int _currentUnitIndex = 0;
    private int _currentCardIndex = 0;
    private int _cardIndex = 0;

    private void OnEnable()
    {
        BattleDisplayManager.GetInstance().EnsureFormationLoaded();
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
            card.locked = !InventoryManager.Instance.HasCharacter(id.ToString());
            card.isSelected = currentUnits.Contains(card.unitTemp);
            card.Init();
            card.border.SetActive(false);
            card.SetLabel(card.isSelected ? LabelEquipped : "");

            if (card.unitTemp == currentUnits[_currentUnitIndex])
            {
                _currentCardIndex = cards.ToList().IndexOf(card);
                _cardIndex = _currentCardIndex;
                card.border.SetActive(true);
                card.SetLabel(LabelCurrent);
            }
        }

        InitSkin();
    }

    // 카드 아래쪽 글자 띠 문구 (JSW)
    private const string LabelEquipped = "장착됨";
    private const string LabelCurrent = "현재 캐릭터\n눌러서 완료";
    private const string LabelSwap = "한 번 더 눌러\n교체";

    /// <summary>교체 화면의 '돌아가기': 바꾸지 않고 캐릭터 3명 화면으로 (JSW)</summary>
    public void CloseChangePanel()
    {
        popup1.SetActive(false);
        popup2.SetActive(false);
        changePanel.SetActive(false);
        formPanel.SetActive(true);
    }

    public void OnCardClick(int i)
    {
        if (cards[i].locked) return;
        if (_currentCardIndex == i)
        {
            popup1.SetActive(false);
            popup2.SetActive(false);

            formPanel.SetActive(true);
            changePanel.SetActive(false);

            // 여정 중 정비 제한 (JSW/Script/JourneyLink.cs)
            if (!JourneyRefit.CanSetUnit(currentUnits, BattleDisplayManager.GetInstance().currentItems, _currentUnitIndex, cards[i].unitTemp)) return;
            currentUnits[_currentUnitIndex] = cards[i].unitTemp;

            Init();
            BattleDisplayManager.GetInstance().SaveFormation();
            JourneyRefit.Refresh(currentUnits, BattleDisplayManager.GetInstance().currentItems);
            return;
        }

        if (cards[i].isSelected && i != _cardIndex) return;

        var prev = cards[_currentCardIndex];
        prev.border.SetActive(false);
        prev.SetLabel(prev.isSelected ? LabelEquipped : "");

        _currentCardIndex = i;
        cards[i].border.SetActive(true);
        cards[i].SetLabel(i == _cardIndex ? LabelCurrent : LabelSwap);

        InitSkin();
    }

    private void InitSkin()
    {
        var unit = cards[_currentCardIndex].unitTemp;
        if (!unit.IsSkinUnlocked(unit.currentSkin)) unit.currentSkin = 0;
        for (int i = 0; i < skins.Length; i++)
        {
            var sprite = unit.GetSkin(i + 1);
            bool unlocked = unit.IsSkinUnlocked(i + 1);
            skins[i].sprite = sprite;
            skins[i].gameObject.SetActive(sprite != null);
            // 잠긴 스킨은 어둡게 (JSW)
            skins[i].color = unlocked ? Color.white : new Color(0.22f, 0.22f, 0.22f, 1f);
            skinTexts[i].gameObject.SetActive(sprite != null);
            if (i < skinLabelBacks.Length && skinLabelBacks[i] != null) skinLabelBacks[i].SetActive(sprite != null);
            string label = unit.currentSkin == i + 1 ? "장착됨" : unlocked ? "해금됨" : "잠김";
            skinTexts[i].text = label;
        }
    }

    public void OnSkinClick(int i)
    {
        if (i < 1 || i > 3 || !cards[_currentCardIndex].unitTemp.IsSkinUnlocked(i)) return;
        if(cards[_currentCardIndex].unitTemp.currentSkin == i) cards[_currentCardIndex].unitTemp.currentSkin = 0;
        else cards[_currentCardIndex].unitTemp.currentSkin = i;
        BattleDisplayManager.GetInstance().SaveFormation();
        InitSkin();
    }
}
