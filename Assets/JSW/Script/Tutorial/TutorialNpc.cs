using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마을 가운데 화면(Main Panel) 왼쪽의 튜토리얼 NPC. 클릭하면 튜토리얼을 다시 할지 묻는다. (보상 없음)
/// 그림: Resources/마을_튜토리얼NPC_흰 (다른 NPC처럼 흰 실루엣. 마을_튜토리얼NPC.png 시안에서 잘라 냄)
/// 색 그림이 생기면 NpcHoverEffect.ColorSheets의 "마을_튜토리얼NPC_흰" 항목에 이름을 넣으면 된다.
/// </summary>
public static class TutorialNpc
{
    // 시안(1920×1080)에서 실루엣이 차지하는 영역 (Unity 기준: 아래가 0)
    private static readonly Vector2 AnchorMin = new Vector2(228f / 1920f, 1f - 672f / 1080f);
    private static readonly Vector2 AnchorMax = new Vector2(483f / 1920f, 1f - 264f / 1080f);

    /// <summary>MainManager.Start에서 NpcHoverEffect.Setup 전에 부른다.</summary>
    public static void Create(GameObject village)
    {
        Transform parent = null;
        foreach (var t in village.GetComponentsInChildren<Transform>(true))
            if (t.name == "Main Panel") { parent = t.Find("Button Panel1") ?? t; break; }
        var sprite = Resources.Load<Sprite>("마을_튜토리얼NPC_흰");
        if (parent == null || sprite == null)
        {
            Debug.LogWarning("[TutorialNpc] 마을 가운데 화면 또는 NPC 그림을 찾지 못했습니다.");
            return;
        }

        var go = new GameObject("Tutorial NPC Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = AnchorMin;
        rect.anchorMax = AnchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        go.GetComponent<Button>().onClick.AddListener(Ask);
    }

    private static void Ask()
    {
        ModalDialog.Show("튜토리얼",
            "튜토리얼을 다시 해 볼까요?",
            "기사·궁수·도적으로 연습해요. 보상은 없어요.",
            null,
            new ModalDialog.Option("시작", TutorialLink.Begin),
            new ModalDialog.Option("돌아가기", null));
    }
}
