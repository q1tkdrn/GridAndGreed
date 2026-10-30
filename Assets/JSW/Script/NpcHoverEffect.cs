using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 마을 화면 NPC 호버 연출.
// 1) 한 NPC가 여러 조각(예: 업적 NPC의 테이블 위 상반신 + 테이블 아래 다리)으로 나뉘어 있어도
//    어느 조각에 마우스를 올리든 NPC 전체가 하나의 중심점에서 함께 커진다.
// 2) 흰 실루엣 위에 같은 위치로 그려진 색 그림(JSW/Resources/캐릭터_색*.png)을 겹쳐
//    마우스를 올리면 서서히 색이 채워지고, 떼면 서서히 다시 하얘진다.
// 조각은 "같은 부모 + 같은 OnClick 함수"인 버튼끼리 한 NPC로 묶는다.
public sealed class NpcHoverEffect : MonoBehaviour
{
    // 흰 실루엣 스프라이트 시트 이름 → 같은 배치로 그려진 색 그림(Resources 경로). null이면 색 그림 없음(크기 효과만).
    private static readonly Dictionary<string, string> ColorSheets = new Dictionary<string, string>
    {
        { "button1_1", "캐릭터_색3" }, // 업적 NPC
        { "button1_2", null },          // 엔딩 후 업적 NPC (색 그림 없음)
        { "button2", "캐릭터_색2" },   // 캐릭터·아이템·스킨 상점 NPC
        { "button3", "캐릭터_색" },    // 추모·음악 NPC
    };

    private const float HoverScale = 1.04f;
    private const float ColorFadeDuration = 0.5f;

    private readonly List<Group> groups = new List<Group>();
    private readonly Dictionary<string, Texture2D> colorTextures = new Dictionary<string, Texture2D>();

    public static void Setup(GameObject root)
    {
        if (root == null) return;
        var effect = root.GetComponent<NpcHoverEffect>();
        if (effect == null) effect = root.AddComponent<NpcHoverEffect>();
        effect.Build();
    }

    private void Build()
    {
        var byKey = new Dictionary<(Transform, string), List<Button>>();
        var order = new List<(Transform, string)>();
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            var image = button.targetGraphic as Image;
            if (image == null || image.sprite == null || image.sprite.texture == null) continue;
            if (!ColorSheets.ContainsKey(image.sprite.texture.name)) continue;
            if (button.GetComponentInParent<NpcGroupRoot>(true) != null) continue; // 이미 묶인 버튼

            var key = (button.transform.parent, ClickKey(button));
            if (!byKey.TryGetValue(key, out var list))
            {
                list = new List<Button>();
                byKey.Add(key, list);
                order.Add(key);
            }
            list.Add(button);
        }

        foreach (var key in order)
            groups.Add(CreateGroup(byKey[key]));
    }

    private static string ClickKey(Button button)
    {
        var onClick = button.onClick;
        if (onClick.GetPersistentEventCount() == 0) return button.name; // 함수가 없으면 단독 그룹
        return onClick.GetPersistentMethodName(0);
    }

    private Group CreateGroup(List<Button> buttons)
    {
        var parent = (RectTransform)buttons[0].transform.parent;

        // 부모와 같은 크기의 컨테이너에 조각들을 옮기고, 컨테이너의 pivot을 NPC 전체 중심에 둔다.
        // 앵커가 부모 기준 그대로 유지되므로 화면 크기가 바뀌어도 배치가 어긋나지 않는다.
        var containerObject = new GameObject(buttons[0].name + " (NPC)", typeof(RectTransform), typeof(NpcGroupRoot));
        containerObject.layer = parent.gameObject.layer;
        var container = (RectTransform)containerObject.transform;
        container.SetParent(parent, false);
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = container.offsetMax = Vector2.zero;

        int siblingIndex = int.MaxValue;
        foreach (var button in buttons)
            siblingIndex = Mathf.Min(siblingIndex, button.transform.GetSiblingIndex());
        container.SetSiblingIndex(siblingIndex);

        var group = new Group { container = container };
        foreach (var button in buttons)
        {
            // 묶음이 크기를 담당하므로 개별 호버 확대는 끈다.
            var single = button.GetComponent<UIHoverScale>();
            if (single != null) Destroy(single);

            button.transform.SetParent(container, false);
            var piece = button.gameObject.AddComponent<NpcHoverPiece>();
            piece.group = group;
            group.buttons.Add(button);

            var overlay = CreateColorOverlay((Image)button.targetGraphic);
            if (overlay != null) group.overlays.Add(overlay);
        }

        UpdatePivot(group);
        return group;
    }

    private Image CreateColorOverlay(Image white)
    {
        var whiteSprite = white.sprite;
        var colorPath = ColorSheets[whiteSprite.texture.name];
        if (string.IsNullOrEmpty(colorPath)) return null;

        if (!colorTextures.TryGetValue(colorPath, out var colorTexture))
        {
            colorTexture = Resources.Load<Texture2D>(colorPath);
            colorTextures.Add(colorPath, colorTexture);
            if (colorTexture == null) Debug.LogWarning($"[NpcHoverEffect] 색 그림을 찾지 못했습니다: Resources/{colorPath}");
        }
        if (colorTexture == null) return null;

        // 흰 실루엣과 색 그림은 같은 크기·배치이므로 같은 영역을 잘라 쓴다.
        // (가져오기 설정 때문에 크기가 달라져도 비율로 맞춘다.)
        var whiteTexture = whiteSprite.texture;
        float sx = (float)colorTexture.width / whiteTexture.width;
        float sy = (float)colorTexture.height / whiteTexture.height;
        var r = whiteSprite.rect;
        var rect = new Rect(r.x * sx, r.y * sy, r.width * sx, r.height * sy);
        var pivot = new Vector2(whiteSprite.pivot.x / r.width, whiteSprite.pivot.y / r.height);
        var colorSprite = Sprite.Create(colorTexture, rect, pivot, whiteSprite.pixelsPerUnit * sx, 0, SpriteMeshType.FullRect);
        colorSprite.name = whiteSprite.name + " (color)";

        var overlayObject = new GameObject("Color", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.layer = white.gameObject.layer;
        var overlayRect = (RectTransform)overlayObject.transform;
        overlayRect.SetParent(white.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;

        var overlay = overlayObject.GetComponent<Image>();
        overlay.sprite = colorSprite;
        overlay.type = white.type;
        overlay.preserveAspect = white.preserveAspect;
        overlay.raycastTarget = false;
        overlay.color = new Color(1f, 1f, 1f, 0f);
        return overlay;
    }

    private static void UpdatePivot(Group group)
    {
        // 조각들을 모두 감싸는 영역의 중심을 컨테이너 pivot으로 쓴다.
        var container = group.container;
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        var corners = new Vector3[4];
        foreach (var button in group.buttons)
        {
            ((RectTransform)button.transform).GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Vector2 local = container.InverseTransformPoint(corner);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
        }
        var size = container.rect.size;
        if (size.x <= 0f || size.y <= 0f || min.x > max.x) return;

        var center = (min + max) * 0.5f;
        var newPivot = new Vector2((center.x - container.rect.xMin) / size.x, (center.y - container.rect.yMin) / size.y);
        // 앵커가 부모 전체로 늘어나 있고 오프셋이 0이라 pivot을 바꿔도 컨테이너 위치는 그대로다.
        container.pivot = newPivot;
        container.offsetMin = container.offsetMax = Vector2.zero;
        group.pivotSize = size;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        foreach (var group in groups)
        {
            if (group.container == null) continue;

            if (!group.container.gameObject.activeInHierarchy)
            {
                // 화면이 바뀌어 꺼진 경우: 다음에 켜질 때 흰색·원래 크기에서 시작한다.
                group.hoverCount = 0;
                group.Apply(0f, 1f);
                continue;
            }

            if (group.container.rect.size != group.pivotSize) UpdatePivot(group);

            bool hovered = group.hoverCount > 0 && group.Interactable();
            group.colorT = Mathf.MoveTowards(group.colorT, hovered ? 1f : 0f, dt / ColorFadeDuration);

            float targetScale = hovered ? HoverScale : 1f;
            group.scale = Mathf.Lerp(group.scale, targetScale, 1f - Mathf.Exp(-20f * dt));
            if (Mathf.Abs(group.scale - targetScale) < 0.0001f) group.scale = targetScale;

            group.Apply(Mathf.SmoothStep(0f, 1f, group.colorT), group.scale);
        }
    }

    internal sealed class Group
    {
        public RectTransform container;
        public readonly List<Button> buttons = new List<Button>();
        public readonly List<Image> overlays = new List<Image>();
        public int hoverCount;
        public float colorT;
        public float scale = 1f;
        public Vector2 pivotSize;

        public bool Interactable()
        {
            foreach (var button in buttons)
                if (button != null && button.IsActive() && button.IsInteractable()) return true;
            return false;
        }

        public void Apply(float color, float s)
        {
            if (color <= 0f) colorT = 0f;
            if (s == 1f) scale = 1f;
            container.localScale = new Vector3(s, s, 1f);
            foreach (var overlay in overlays)
            {
                if (overlay == null) continue;
                var c = overlay.color;
                if (c.a == color) continue;
                c.a = color;
                overlay.color = c;
            }
        }
    }
}
