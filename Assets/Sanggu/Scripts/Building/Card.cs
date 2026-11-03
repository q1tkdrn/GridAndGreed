using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public UnitTemp unitTemp;
    public bool isSelected = false;
    [SerializeField] private Image image;
    
    public TextMeshProUGUI text;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private GameObject popUpPanel;
    public GameObject border;

    [Header("잠금 (JSW)")]
    [Tooltip("잠긴 캐릭터 카드 위에 덮는 그림 (JSW/UI/Formation/카드_잠금.png)")]
    [SerializeField] private GameObject lockCover;
    [NonSerialized] public bool locked;
    [Tooltip("글자 띠 바탕 (Text 앞 형제). 글자 줄 수에 맞춰 높이가 바뀐다")]
    [SerializeField] private RectTransform labelBack;

    public void Init()
    {
        popUpPanel.SetActive(false);
        // 지금 규칙(v2)에서 쓰는 건 특성뿐 (힘·지능·부활 대기 시간은 안 씀)
        popupText.text =
            $"<align=\"center\"><size=45>{unitTemp.unitName}</size></align>\n" +
            $"<align=\"left\"><size=30>{Unit.WrapWords(unitTemp.abilityText)}</size>";
        image.sprite = unitTemp.illustration;
        // 잠긴 캐릭터는 일러스트를 보여 주지 않는다 (해금하는 재미)
        image.gameObject.SetActive(!locked);
        if (lockCover != null) lockCover.SetActive(locked);
        text.gameObject.SetActive(isSelected && !locked);
        if (labelBack != null) labelBack.gameObject.SetActive(text.gameObject.activeSelf);
    }

    /// <summary>카드 아래쪽 글자 띠. 빈 문자열이면 숨긴다.</summary>
    public void SetLabel(string label)
    {
        bool show = !locked && !string.IsNullOrEmpty(label);
        text.text = show ? label : "";
        text.gameObject.SetActive(show);
        if (labelBack == null) return;
        labelBack.gameObject.SetActive(show);
        int lines = show ? label.Split('\n').Length : 1;
        float h = 18f + 32f * lines;
        labelBack.sizeDelta = new Vector2(labelBack.sizeDelta.x, h);
        // 글자 칸도 바탕과 같은 위치·높이로 (한 줄일 때 위로 뜨지 않게)
        var tr = text.rectTransform;
        tr.anchoredPosition = labelBack.anchoredPosition;
        tr.sizeDelta = new Vector2(tr.sizeDelta.x, h);
    }

    public void OnMouseEnter()
    {
        if (locked) return;
        popUpPanel.SetActive(true);
    }

    public void OnMouseExit()
    {
        popUpPanel.SetActive(false);
    }
}
