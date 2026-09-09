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
        powerText.text = unitTemp.power.ToString();
        intelligenceText.text = unitTemp.intelligence.ToString();
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
