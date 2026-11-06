using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 입장 화면 (JSW, 2026-11-04). 예전 '사신 VS 보스' 애니메이션을 대신한다.
/// 오른쪽: 보스 일러스트가 반투명하게 아래에서 천천히 떠오른다.
/// 왼쪽: 스테이지 이름 → 보스 이름 → 입장 문구가 한 글자씩.
/// 클릭/Space: 글자 진행 중이면 바로 다 보여 주고, 다 보였으면 넘어간다. 가만히 두면 잠시 뒤 저절로 넘어간다.
/// BoardPanel.ShowCutScene이 CutScene 오브젝트에 붙여서 쓴다. 씬에는 저장하지 않는다.
/// </summary>
public class StageIntro : MonoBehaviour
{
    private const float RiseDuration = 3.4f;
    private const float IllustAlpha = 0.5f;
    private const float CharDelay = 0.08f;     // 천천히 (35자 ≈ 2.8초)
    private const float HoldAfterText = 3.0f;
    private const float FadeOut = 0.45f;
    private const float TextStart = 1.8f;   // 제목·보스 이름이 다 나타난 뒤에 문구 시작

    private RectTransform _root;
    private CanvasGroup _group;
    private Image _illust, _rule, _place;
    private const float PlaceAlpha = 0.9f;
    private TextMeshProUGUI _stage, _bossName, _intro, _hint;
    private bool _built;

    public IEnumerator Play(BossTemp boss, Action onDone)
    {
        Build();
        foreach (Transform child in transform)
            if (child != _root) child.gameObject.SetActive(false);   // 예전 연출(사신/보스/VS)은 숨김
        var animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;

        var entry = StageText.Find(boss);
        _illust.sprite = boss != null ? boss.bossSprite : null;
        _illust.enabled = _illust.sprite != null;
        _stage.text = entry != null ? entry.stage : (boss != null ? boss.bossName : "");
        _bossName.text = entry != null && boss != null ? boss.bossName : "";
        _intro.text = entry != null ? entry.intro : "";
        _intro.maxVisibleCharacters = 0;
        _place.sprite = StageText.Art(boss);
        _place.enabled = _place.sprite != null;
        if (_place.enabled) _place.rectTransform.sizeDelta = _place.sprite.rect.size * 0.9f;
        SetAlpha(_place, 0f);

        _group.alpha = 1f;
        SetAlpha(_stage, 0f); SetAlpha(_bossName, 0f); SetAlpha(_hint, 0f); SetAlpha(_illust, 0f);
        _rule.color = new Color(1f, 1f, 1f, 0f);

        float t = 0f, charClock = 0f, doneAt = -1f;
        int chars = 0, total = _intro.text.Length;
        bool skipTyping = false;
        while (true)
        {
            // 전투 화면이 켜지는 첫 몇 프레임은 로딩 때문에 dt가 크게 튄다 → 그대로 쓰면 글자가 한 번에 여러 개 찍혀
            // '중간부터 나오는' 것처럼 보인다. 한 프레임에 0.1초까지만 진행한다 (느린 PC에서 전체가 느려지지 않을 만큼만 자름).
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            t += dt;

            // 보스 일러스트: 아래에서 위로, 끝에서 느려짐
            float k = Ease(Mathf.Clamp01(t / RiseDuration));
            _illust.rectTransform.anchoredPosition = new Vector2(430f, Mathf.Lerp(-230f, -40f, k));
            SetAlpha(_illust, IllustAlpha * Mathf.Clamp01(t / (RiseDuration * 0.7f)));

            // 제목 → 보스 이름 → 줄
            SetAlpha(_stage, Mathf.Clamp01((t - 0.3f) / 0.6f));
            SetAlpha(_bossName, Mathf.Clamp01((t - 0.7f) / 0.6f));
            _rule.color = new Color(1f, 1f, 1f, 0.35f * Mathf.Clamp01((t - 0.9f) / 0.6f));

            // 장소 그림: 문구가 써지는 도중부터 천천히, 조금 떠오르며
            float pk = Mathf.Clamp01((t - TextStart - 1.0f) / 1.6f);
            SetAlpha(_place, PlaceAlpha * pk);
            _place.rectTransform.anchoredPosition = new Vector2(-760f, -345f + 14f * Ease(pk));

            // 입장 문구 한 글자씩
            bool clicked = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
            if (t > TextStart && chars < total)
            {
                if (clicked && chars > 0) { skipTyping = true; } clicked = false;
                charClock += dt;
                while (chars < total && (skipTyping || charClock >= CharDelay))
                {
                    charClock -= CharDelay;
                    chars++;
                }
                _intro.maxVisibleCharacters = chars;
                if (chars >= total) doneAt = t;
            }
            else if (chars >= total && doneAt < 0f) doneAt = t;

            if (doneAt >= 0f)
            {
                SetAlpha(_hint, 0.6f * Mathf.Clamp01((t - doneAt) / 0.5f));
                if (clicked || t - doneAt >= HoldAfterText) break;
            }
            yield return null;
        }

        for (float f = 0f; f < FadeOut; f += Time.unscaledDeltaTime)
        {
            _group.alpha = 1f - f / FadeOut;
            yield return null;
        }
        _group.alpha = 0f;
        onDone?.Invoke();
    }

    private void Build()
    {
        if (_built) return;
        _built = true;
        // 글꼴은 GameFonts에서 (2026-11-06: 부크크 명조 → 고운바탕, 장소 이름은 연성)
        TMP_FontAsset bold = GameFonts.Display, light = GameFonts.Body;

        _root = new GameObject("Stage Intro", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        _root.SetParent(transform, false);
        Stretch(_root);
        _group = _root.GetComponent<CanvasGroup>();

        var shade = NewImage("Shade", _root, new Color(0.03f, 0.03f, 0.035f, 0.96f));
        Stretch(shade.rectTransform);

        _illust = NewImage("Boss", _root, Color.white);
        _illust.preserveAspect = true;
        var ir = _illust.rectTransform;
        ir.anchorMin = ir.anchorMax = new Vector2(0.5f, 0.5f);
        ir.sizeDelta = new Vector2(1150f, 1250f);

        _stage = NewText("Stage", bold, 96f, new Vector2(-350f, 130f), new Vector2(820f, 130f));   // 왼쪽 여백 200px
        _bossName = NewText("Boss Name", light, 40f, new Vector2(-350f, 42f), new Vector2(820f, 60f));
        _bossName.color = new Color(0.75f, 0.75f, 0.75f, 1f);

        _rule = NewImage("Rule", _root, Color.white);
        _rule.sprite = FxSprites.FadeBar;
        var rr = _rule.rectTransform;
        rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
        rr.pivot = new Vector2(0f, 0.5f);
        rr.anchoredPosition = new Vector2(-760f, 0f);
        rr.sizeDelta = new Vector2(760f, 3f);

        _intro = NewText("Intro", light, 42f, new Vector2(-350f, -160f), new Vector2(820f, 260f));   // 위끝 -30: 구분선과 30px 띄움
        _intro.textWrappingMode = TextWrappingModes.Normal;
        _intro.alignment = TextAlignmentOptions.TopLeft;
        _intro.lineSpacing = 18f;

        _place = NewImage("Place", _root, Color.white);   // 왼쪽 아래, 문구 왼쪽 끝에 맞춤
        _place.preserveAspect = true;
        var pr = _place.rectTransform;
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.pivot = new Vector2(0f, 0.5f);

        _hint = NewText("Hint", light, 28f, new Vector2(700f, -470f), new Vector2(400f, 50f));
        _hint.alignment = TextAlignmentOptions.Right;
        _hint.text = "클릭해서 계속 ▶";
    }

    private TextMeshProUGUI NewText(string name, TMP_FontAsset font, float size, Vector2 pos, Vector2 box)
    {
        var t = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var r = t.rectTransform;
        r.SetParent(_root, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = box;
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Left;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.rectTransform.SetParent(parent, false);
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static float Ease(float x) => 1f - Mathf.Pow(1f - x, 3f);

    private static void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }
}
