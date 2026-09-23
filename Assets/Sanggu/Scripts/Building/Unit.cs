using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    public UnitTemp unitTemp;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI intelligenceText;
    [SerializeField] private Image sd;
    
    public void Init()
    {
        powerText.text = $"<color=#FFB080>힘 {unitTemp.power}</color>";
        intelligenceText.text = $"<color=#88CEFF>지능 {unitTemp.intelligence}</color>";
        if (!powerText.enableAutoSizing) powerText.fontSizeMax = powerText.fontSize;
        if (!intelligenceText.enableAutoSizing) intelligenceText.fontSizeMax = intelligenceText.fontSize;
        powerText.fontSizeMin = intelligenceText.fontSizeMin = 18;
        powerText.enableAutoSizing = intelligenceText.enableAutoSizing = true;
        powerText.textWrappingMode = intelligenceText.textWrappingMode = TextWrappingModes.NoWrap;
        if (!unitTemp.IsSkinUnlocked(unitTemp.currentSkin)) unitTemp.currentSkin = 0;
        sd.sprite = unitTemp.currentSkin switch
        {
            0 => unitTemp.defaultSkin,
            1 => unitTemp.skin1,
            2 => unitTemp.skin2,
            3 => unitTemp.skin3,
            _ => sd.sprite
        };
    }
}
