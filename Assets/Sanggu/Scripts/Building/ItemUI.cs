using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ItemUI : MonoBehaviour
{
    public int id;
    public ItemData itemData;
    public GameObject popup;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image image;
    public bool unlock;
    public bool isEquip;

    [SerializeField] private TextMeshProUGUI talkText;

    [Header("잠금 (JSW)")]
    [Tooltip("얻지 않은 아이템 위에 덮는 반투명 자물쇠 (JSW/UI/Formation/아이템_잠금.png)")]
    [SerializeField] private GameObject lockCover;

    public void Init()
    {
        StopAllCoroutines();
        if (talkText != null) talkText.text = "";
        if (popup != null) popup.SetActive(false);
        if (itemData is null)
        {
            id = 0;
            unlock = false;
            image.gameObject.SetActive(false);
            return;
        }
        id = int.Parse(itemData.id);
        unlock = InventoryManager.Instance.HasItem(id.ToString());
        nameText.text = itemData.itemName;
        descriptionText.text = itemData.description;
        image.sprite = itemData.icon;
        // 얻지 않은 아이템도 판에 보여 주고, 자물쇠를 덮는다 (자물쇠 뒤로 아이템이 비침)
        if (!unlock) descriptionText.text += "\n<color=#9A9A9A><size=85%>아직 얻지 못했어요</size></color>";
        image.gameObject.SetActive(true);
        gameObject.SetActive(true);
        if (lockCover != null) lockCover.SetActive(!unlock);
    }

    public void OnMouseEnter()
    {
        if (itemData == null) return;
        popup.SetActive(true);
    }

    public void OnMouseExit()
    {
        if (itemData == null) return;
        popup.SetActive(false);
    }

    public void OnTurnStart()
    {
        if (itemData == null || !unlock || !isActiveAndEnabled) return;
        if(id is not (10 or 17 or 18)) return;
        if (talkText == null)
        {
            var bubble = new GameObject("ItemDialogue", typeof(RectTransform), typeof(TextMeshProUGUI));
            bubble.transform.SetParent(transform, false);
            talkText = bubble.GetComponent<TextMeshProUGUI>();
            talkText.font = nameText.font;
            talkText.fontSize = 20;
            talkText.alignment = TextAlignmentOptions.Center;
            talkText.raycastTarget = false;
            var rect = talkText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(240f, 70f);
        }
        List<string> dialogue = new List<string>();
        switch (id)
        {
            case 10:
                dialogue = _dialogue[0].ToList();
                break;
            case 17:
                dialogue = _dialogue[1].ToList();
                break;
            case 18:
                dialogue = _dialogue[2].ToList();
                break;
        }

        dialogue.Shuffle();
        StopAllCoroutines();
        StartCoroutine(PrintText(dialogue[0]));
    }

    IEnumerator PrintText(string text)
    {
        talkText.text = "";
        var sb = new StringBuilder();
        
        foreach (var t in text)
        {
            sb.Append(t);
            talkText.text = sb.ToString();
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(3f);
        talkText.text = "";
    }

    private string[][] _dialogue =
    {
        new string[]
        {
            "아직 살아 있지? 나도 그래!",
            "이번엔 어디로 갈 거야?",
            "내 얘기 좀 들어 봐!"
        },
        new string[]
        {
            "살랑, 살랑.",
            "바람이 불어오네.",
            "오늘도 꽃잎을 흔들어.",
        },
        new string[]
        {
            "흐음~ 흠흠~♪",
            "라라라~♪",
            "이 노래, 기억하니?",
        }
    };
}
