using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설명 창을 전투 캐릭터 정보창(UnitInfoView)과 같은 모양으로 바꾼다 (2026-11-05):
/// 완전히 검은 바탕 + '대사창' 분필 테두리(Resources/대사창, 9-slice) + 흰 글자.
/// 편성 캐릭터 카드(Card), 편성·전투 아이템 칸(ItemUI)의 설명 창이 쓴다.
/// 씬·프리팹은 건드리지 않고 처음 한 번 코드로 바꾼다. 창 자체 그림은 투명하게만 두어 마우스 판정은 그대로다.
/// </summary>
public static class DialogBox
{
    public const float FillInset = 8f;                 // 테두리 바깥으로 바탕이 삐져나오지 않게 안쪽으로
    public const float FramePixelsPerUnit = 1.3f;      // 테두리 굵기 (UnitInfoView와 같게)
    private const string FrameName = "Dialog Frame";

    private static Sprite _frame;

    public static Sprite Frame
    {
        get
        {
            if (_frame == null)
            {
                var sprites = Resources.LoadAll<Sprite>("대사창");
                if (sprites.Length > 0) _frame = sprites[0];
            }
            return _frame;
        }
    }

    public static void Style(GameObject panel, params TMP_Text[] texts) => Style(panel, FillInset, texts);

    /// <param name="fillInset">검은 바탕을 테두리 안쪽으로 들이는 정도. 흰 그림 위에 뜨는 창(편성 카드)은 작게 해서 흰 틈이 안 보이게 한다.</param>
    public static void Style(GameObject panel, float fillInset, params TMP_Text[] texts)
    {
        if (panel == null) return;
        foreach (var text in texts)
            if (text != null) text.color = new Color(1f, 1f, 1f, text.color.a);
        if (panel.transform.Find(FrameName) != null) return;   // 이미 바꿈

        var own = panel.GetComponent<Image>();
        if (own != null)
        {
            own.sprite = null;
            own.color = new Color(0f, 0f, 0f, 0f);
        }

        var fill = new GameObject("Dialog Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var fr = (RectTransform)fill.transform;
        fr.SetParent(panel.transform, false);
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = new Vector2(fillInset, fillInset);
        fr.offsetMax = new Vector2(-fillInset, -fillInset);
        var fillImage = fill.GetComponent<Image>();
        fillImage.color = Color.black;
        fillImage.raycastTarget = false;
        fr.SetAsFirstSibling();

        var frame = new GameObject(FrameName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var r = (RectTransform)frame.transform;
        r.SetParent(panel.transform, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var image = frame.GetComponent<Image>();
        image.raycastTarget = false;
        if (Frame != null)
        {
            image.sprite = Frame;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = FramePixelsPerUnit;
        }
        else image.enabled = false;
        r.SetSiblingIndex(1);   // 바탕 바로 위, 글자 아래
    }
}
