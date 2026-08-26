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

    public void Init()
    {
        if (itemData is null)
        {
            unlock = false;
            image.gameObject.SetActive(false);
            return;
        }
        id = int.Parse(itemData.id);
        unlock = InventoryManager.Instance.HasItem(id.ToString());
        nameText.text = itemData.itemName;
        descriptionText.text = itemData.description;
        image.sprite = itemData.icon;
        gameObject.SetActive(unlock);
    }

    public void OnMouseEnter()
    {
        if(!unlock) return;
        popup.SetActive(true);
    }

    public void OnMouseExit()
    {
        if(!unlock) return;
        popup.SetActive(false);
    }

    public void OnTurnStart()
    {
        if(id is not (10 or 17 or 18)) return;
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
            "(Mr. COCKROACH 대사 1)",
            "(Mr. COCKROACH 대사 2)",
            "(Mr. COCKROACH 대사 3)"
        },
        new string[]
        {
            "(Mr. FLOWER 대사 1)",
            "(Mr. FLOWER 대사 2)",
            "(Mr. FLOWER 대사 3)",
        },
        new string[]
        {
            "(Miss. GHOST 대사 1)",
            "(Miss. GHOST 대사 2)",
            "(Miss. GHOST 대사 3)",
        }
    };
}