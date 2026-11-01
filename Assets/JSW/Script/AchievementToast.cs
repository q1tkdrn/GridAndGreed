using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 새로 달성한 업적만 순서대로 표시한다. 씬 이동과 일시정지 중에도 알림을 유지한다.
public sealed class AchievementToast : MonoBehaviour
{
    private static AchievementToast instance;
    private readonly Queue<AchievementData> pending = new Queue<AchievementData>();
    private RectTransform safeArea;
    private RectTransform panel;
    private CanvasGroup group;
    private UnityEngine.UI.Image icon;
    private TMP_Text title;
    private float elapsed;
    private bool showing;
    private const float FadeIn = 0.2f;
    private const float Hold = 3.5f;
    private const float FadeOut = 0.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    public static void Enqueue(AchievementData data)
    {
        if (data == null) return;
        if (instance == null)
        {
            var root = new GameObject("AchievementToast", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler));
            instance = root.AddComponent<AchievementToast>();
            DontDestroyOnLoad(root);
            instance.Build();
        }
        instance.pending.Enqueue(data);
    }

    public static void ClearPending()
    {
        if (instance == null) return;
        instance.pending.Clear();
        instance.showing = false;
        instance.panel.gameObject.SetActive(false);
    }

    private void Build()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        safeArea = Rect("SafeArea", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panel = Rect("Notification", safeArea, new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-24, 24), new Vector2(440, 104));
        panel.pivot = new Vector2(1, 0);
        Picture(panel, new Color(0.75f, 0.73f, 0.68f, 1));
        var background = Rect("Background", panel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4, -4));
        Picture(background, new Color(0.065f, 0.065f, 0.065f, 0.98f));
        group = panel.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        var iconRect = Rect("Icon", panel, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(54, 0), new Vector2(76, 76));
        icon = Picture(iconRect, Color.white);
        icon.preserveAspect = true;
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/BookkMyungjo_Bold SDF");
        Label("Heading", "업적 달성", font, 19, new Color(0.75f, 0.73f, 0.68f), new Vector2(264, 28), 30);
        title = Label("Title", "", font, 26, Color.white, new Vector2(264, -12), 52);
        title.enableAutoSizing = true;
        title.fontSizeMin = 18;
        title.fontSizeMax = 26;
        title.overflowMode = TextOverflowModes.Ellipsis;
        panel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Screen.width > 0 && Screen.height > 0)
        {
            var area = Screen.safeArea;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }
        if (!showing)
        {
            if (pending.Count == 0) return;
            var data = pending.Dequeue();
            icon.sprite = data.icon;
            icon.enabled = data.icon != null;
            title.text = data.title;
            elapsed = 0;
            showing = true;
            panel.gameObject.SetActive(true);
        }
        elapsed += Time.unscaledDeltaTime;
        float alpha = elapsed < FadeIn ? elapsed / FadeIn
            : elapsed < FadeIn + Hold ? 1 : 1 - (elapsed - FadeIn - Hold) / FadeOut;
        group.alpha = Mathf.Clamp01(alpha);
        panel.anchoredPosition = new Vector2(-24, 24 - 14 * (1 - group.alpha));
        if (elapsed >= FadeIn + Hold + FadeOut)
        {
            showing = false;
            panel.gameObject.SetActive(false);
        }
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static UnityEngine.UI.Image Picture(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text Label(string name, string value, TMP_FontAsset font, float size, Color color, Vector2 position, float height)
    {
        var rect = Rect(name, panel, new Vector2(0, 0.5f), new Vector2(0, 0.5f), position, new Vector2(306, height));
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = value;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        label.richText = false;
        return label;
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
