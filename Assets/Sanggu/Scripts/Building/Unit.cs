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
        // 지금 규칙(v2)은 캐릭터 고유 힘·지능을 쓰지 않는다 → 이름과 특성을 보여 준다
        powerText.text = $"{unitTemp.unitName}\n<size=78%><color=#C8C8C8>{WrapWords(unitTemp.abilityText)}</color></size>";
        powerText.enableAutoSizing = false;
        powerText.textWrappingMode = TextWrappingModes.Normal;
        if (intelligenceText != null) intelligenceText.gameObject.SetActive(false);
        if (!unitTemp.IsSkinUnlocked(unitTemp.currentSkin)) unitTemp.currentSkin = 0;
        sd.sprite = unitTemp.currentSkin switch
        {
            0 => unitTemp.defaultSkin,
            1 => unitTemp.skin1,
            2 => unitTemp.skin2,
            3 => unitTemp.skin3,
            _ => sd.sprite
        };

        // 편성창 연출: 빛·숨쉬기·마우스 올리면 심판 동작 (JSW/Script/FormationUnitFx.cs)
        var fx = GetComponent<FormationUnitFx>();
        if (fx == null) fx = gameObject.AddComponent<FormationUnitFx>();
        fx.Setup(sd, unitTemp, powerText);
    }

    /// <summary>한글이 단어 중간에서 꺾이지 않게 띄어쓰기 단위로 묶는다.</summary>
    public static string WrapWords(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", Array.ConvertAll(words, w => $"<nobr>{w}</nobr>"));
    }
}
