using System;
using TMPro;
using UnityEngine;

public class Card : MonoBehaviour
{
    public UnitTemp unitTemp;
    public bool isSelected = false;
    
    public TextMeshProUGUI text;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private GameObject popUpPanel;

    public void Init()
    {
        popUpPanel.SetActive(false);
        popupText.text = 
            $"<align=\"center\"><size=45>{unitTemp.unitName}</size></align>\n" +
            $"<align=\"left\"><size=30>" +
            $"힘: {unitTemp.power}\n" +
            $"지능: {unitTemp.intelligence}\n" +
            $"부활 대기 시간: {unitTemp.reviveCool}턴\n" +
            $"능력: {unitTemp.effectText}" +
            $"</size>";
        text.gameObject.SetActive(isSelected);
    }

    public void OnMouseEnter()
    {
        popUpPanel.SetActive(true);
    }

    public void OnMouseExit()
    {
        popUpPanel.SetActive(false);
    }
}
