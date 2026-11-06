using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 화면 위쪽 이름 카드 (2026-11-06). 그림: Resources/전투_{이름}_네임카드.png
/// - 그림은 1920×1080 전체 크기에 카드만 그려져 있다. 카드 부분만 잘라(uvRect) 같은 자리에 놓는다.
///   꼬마 사신은 왼쪽 위, 보스는 오른쪽 위 끝에 붙여서 (각 HP 숫자 바로 위).
/// - 보스 그림 이름은 보스 에셋의 bossName과 같다 (교관, 교황, 문, …). 그림이 없는 보스(종지기 등)는 카드 없이 둔다.
/// - HP 막대 바로 위 순서에 넣어서, 입장 화면·승패 화면 같은 덮개보다 아래에 그려진다.
/// </summary>
public static class BattleNameCards
{
    private const float RefW = 1920f, RefH = 1080f;
    // 그림 속 카드 자리 (왼쪽, 위, 오른쪽, 아래 — 1920×1080 기준 픽셀)
    private static readonly Rect ReaperCard = Rect.MinMaxRect(12f, 16f, 178f, 67f);
    private static readonly Rect BossCard = Rect.MinMaxRect(1387f, 16f, 1620f, 67f);

    private const string ReaperName = "Reaper Name Card";
    private const string BossName = "Boss Name Card";

    public static void Apply(BoardPanel board, BossTemp boss)
    {
        if (board == null || board.BossImage == null) return;
        var parent = board.BossImage.transform.parent;   // GameBoard
        var anchor = parent.Find("BossHP");
        int index = anchor != null ? anchor.GetSiblingIndex() + 1 : parent.childCount;

        Place(parent, ReaperName, "전투_사신_네임카드", ReaperCard, false, index);
        Place(parent, BossName, boss != null ? $"전투_{boss.bossName}_네임카드" : null, BossCard, true, index);
    }

    private static void Place(Transform parent, string objectName, string resource, Rect card, bool fromRight, int index)
    {
        var existing = parent.Find(objectName);
        var texture = string.IsNullOrEmpty(resource) ? null : Resources.Load<Texture2D>(resource);
        if (texture == null)
        {
            if (existing != null) existing.gameObject.SetActive(false);
            return;
        }

        RawImage image;
        if (existing != null) image = existing.GetComponent<RawImage>();
        else
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            image = go.GetComponent<RawImage>();
            image.raycastTarget = false;
        }
        image.gameObject.SetActive(true);
        image.texture = texture;
        image.uvRect = new Rect(card.xMin / RefW, 1f - card.yMax / RefH, card.width / RefW, card.height / RefH);

        var rect = image.rectTransform;
        var corner = fromRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.anchorMin = rect.anchorMax = corner;
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = card.size;
        // 보스 카드는 오른쪽 끝에 붙인다 (사신 카드가 왼쪽 끝에서 떨어진 만큼, 2026-11-06)
        rect.anchoredPosition = new Vector2(fromRight ? -(ReaperCard.xMin + card.width) : card.xMin, -card.yMin);
        rect.SetSiblingIndex(Mathf.Min(index, parent.childCount - 1));
    }
}
