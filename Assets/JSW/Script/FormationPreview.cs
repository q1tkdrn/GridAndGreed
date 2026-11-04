using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 여정 첫 출발 확인창에 지금 편성(캐릭터 3명 + 아이템 3칸)을 보여 준다 (JSW, 2026-11-04).
/// 지도 씬(Main)에는 캐릭터·아이템 에셋 참조가 없어서 Resources/FormationPreview(이 파일의 Catalog)를 쓴다.
/// 캐릭터·아이템이 늘면 Tools > Rebuild Formation Preview.
/// 어떤 캐릭터·아이템이 실제로 들어가는지는 FormationSave.Load와 같은 규칙으로 고른다(저장된 것 → 없거나 못 쓰면 가진 캐릭터로 채움).
/// </summary>
public static class FormationPreview
{
    [Serializable]
    public class UnitEntry
    {
        public int id;
        public string name;
        public Sprite[] skins;   // 0 기본, 1~3 스킨 (UnitTemp.GetSkin 순서)
    }

    [Serializable]
    public class ItemEntry
    {
        public string id;
        public string name;
        public Sprite icon;
    }

    private const string Prefix = "Formation_v1_";

    /// <summary>확인창(ModalDialog)의 설명 자리에 편성 줄을 넣는다.</summary>
    public static void Attach(ModalDialog dialog)
    {
        if (dialog == null) return;
        var catalog = Resources.Load<FormationPreviewCatalog>("FormationPreview");
        var body = dialog.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "RestartConfirmation");
        if (catalog == null || body == null) return;
        var explanation = body.Find("Explanation");
        if (explanation != null) explanation.gameObject.SetActive(false);

        Pick(catalog, out var units, out var items);
        var font = body.GetComponentInChildren<TMP_Text>(true)?.font;

        // 두 줄이 들어가도록 질문을 조금 위로 (원래 115, 제목 밑줄은 본문 기준 193)
        if (body.Find("Question") is RectTransform question) question.anchoredPosition = new Vector2(question.anchoredPosition.x, 156f);

        var row = new GameObject("Formation Preview", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(body, false);
        row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = Vector2.zero;
        row.sizeDelta = new Vector2(530f, 170f);

        // 안내문 창 바탕이 밝은 종이 색이라, 상자 대신 그림에 어두운 외곽선을 둘러 흰 테두리가 보이게 한다 (2026-11-04: 어두운 띠는 어색해서 뺌)
        // 위: 아이템 3칸 (캐릭터와 같은 열) / 아래: 캐릭터 3명
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 175f;
            var unit = units[i];
            var item = items[i];

            var slot = NewImage("ItemSlot" + i, row, new Vector2(x, 98f), new Vector2(48f, 48f));
            slot.sprite = FxSprites.Dot;
            slot.color = new Color(0f, 0f, 0f, item != null ? 0.18f : 0.08f);   // 아이템 자리 표시 (빈칸은 더 흐리게)
            var icon = NewImage("Item" + i, slot.rectTransform, Vector2.zero, new Vector2(46f, 46f));
            icon.sprite = item?.icon;
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;
            Ink(icon);

            var portrait = NewImage("Unit" + i, row, new Vector2(x, 26f), new Vector2(96f, 96f));
            portrait.sprite = unit != null ? SkinOf(unit) : null;
            portrait.preserveAspect = true;
            portrait.enabled = portrait.sprite != null;
            Ink(portrait);

            var label = new GameObject("Name" + i, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            var lr = label.rectTransform;
            lr.SetParent(row, false);
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
            lr.anchoredPosition = new Vector2(x, -32f);   // 아래끝 -45: 첫 버튼 위끝(-47) 바로 위
            lr.sizeDelta = new Vector2(166f, 26f);
            if (font != null) label.font = font;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = new Color(0.2f, 0.2f, 0.2f, 1f);   // 종이 위 진한 글씨 (질문 글과 같은 계열)
            label.raycastTarget = false;
            label.text = unit != null ? unit.name : "";
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 22f;
        }
    }

    /// <summary>흰 선 그림을 밝은 종이 위에서도 보이게: 어두운 외곽선 + 그림자</summary>
    private static void Ink(Image image)
    {
        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);
        var shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(2.5f, -3f);
    }

    private static Sprite SkinOf(UnitEntry unit)
    {
        // 편성 저장(FormationSave.Save)이 열린 스킨만 저장하므로 저장값을 그대로 쓴다.
        int skin = PlayerPrefs.GetInt(Prefix + "Skin_" + unit.id, 0);
        if (skin == 3) skin = 0;   // 4번째 스킨 칸은 기본 그림 임시 (BoardPanel과 같게)
        if (skin < 0 || skin >= unit.skins.Length || unit.skins[skin] == null) skin = 0;
        return unit.skins.Length > 0 ? unit.skins[skin] : null;
    }

    /// <summary>FormationSave.Load와 같은 규칙.</summary>
    private static void Pick(FormationPreviewCatalog catalog, out UnitEntry[] units, out ItemEntry[] items)
    {
        units = new UnitEntry[3];
        items = new ItemEntry[3];
        var used = new HashSet<int>();
        for (int i = 0; i < 3; i++)
        {
            int id = PlayerPrefs.GetInt(Prefix + "Unit_" + i, i + 1);
            var u = catalog.units.FirstOrDefault(x => x.id == id && Owns(x.id) && !used.Contains(x.id));
            units[i] = u;
            if (u != null) used.Add(u.id);
        }
        for (int i = 0; i < 3; i++)
        {
            if (units[i] != null) continue;
            var u = catalog.units.FirstOrDefault(x => Owns(x.id) && !used.Contains(x.id));
            units[i] = u;
            if (u != null) used.Add(u.id);
        }
        var usedItems = new HashSet<string>();
        for (int i = 0; i < 3; i++)
        {
            string id = PlayerPrefs.GetString(Prefix + "Item_" + i, "");
            var it = catalog.items.FirstOrDefault(x => x.id == id && PlayerPrefs.GetInt("Item_" + id, 0) > 0 && !usedItems.Contains(id));
            items[i] = it;
            if (it != null) usedItems.Add(it.id);
        }
    }

    private static bool Owns(int id) => (id >= 1 && id <= 3) || PlayerPrefs.GetInt("Character_" + id, 0) == 1;

    private static Image NewImage(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        var r = img.rectTransform;
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        img.raycastTarget = false;
        return img;
    }
}
