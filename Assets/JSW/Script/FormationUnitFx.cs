using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 편성창 캐릭터 3명 화면의 연출 (JSW, 2026-11-03).
/// 스포트라이트·바닥 빛·그림자, 숨쉬기, 마우스를 올리면 판 위 심판 동작 1번 재생 + "눌러서 교체" 안내.
/// Unit.Init에서 붙인다. 씬에는 아무것도 저장하지 않는다.
/// </summary>
public class FormationUnitFx : MonoBehaviour
{
    // SD 칸(300×300) 가운데 기준 발 높이. 숨쉬기·바닥 빛이 여기를 기준으로 한다.
    private const float FootY = -128f;
    private const float BreathAmount = 0.012f;
    private const float BreathPeriod = 2.6f;
    private const float CutCanvas = 426f;   // 컷인 700px 그림을 그릴 크기 (캐릭터 키 ≈ SD 260)
    private const float CutFoot = 0.82f;    // 컷인 그림에서 발 높이 (위에서부터 비율, 새 도구 기준)

    // 도구 이전에 만든 컷인은 발 높이가 다르다 (편성창 캡처로 잰 값)
    private static float CutFootOf(string folder) => folder switch
    {
        "Knight" => 0.72f,
        "Rogue" => 0.76f,
        _ => CutFoot
    };

    private Image _sd;
    private RectTransform _sdRect;
    private Vector2 _sdBaseSize;
    private UnitTemp _unit;
    private Image _cone, _pool, _shadow;
    private TextMeshProUGUI _hint;
    private bool _hover;
    private float _glow;          // 0 = 평소, 1 = 마우스 올림
    private Coroutine _act;
    private Sprite _restSprite;
    private float _phase;
    private TextMeshProUGUI _label;   // 이름·특성 글자 (안내는 그 바로 아래)

    public void Setup(Image sd, UnitTemp unit, TextMeshProUGUI label)
    {
        _unit = unit;
        if (_sd == null)
        {
            _sd = sd;
            _sdRect = sd.rectTransform;
            _sdBaseSize = _sdRect.sizeDelta;
            _phase = transform.GetSiblingIndex() * 0.9f;
            BuildLights();
            _label = label;
            BuildHint(label != null ? label.font : null);
            HookPointer();
            FormationDust.Ensure(transform.parent as RectTransform);
        }
        var current = sd.sprite;   // Init이 방금 넣은 그림 (재생 중이었다면 되돌리기로 덮이지 않게)
        _restSprite = null;
        StopAct();
        _sd.sprite = current;
        _restSprite = current;
        RefreshHint();
    }

    private void OnDisable()
    {
        _hover = false;
        _glow = 0f;
        StopAct();
        if (_sdRect != null) { _sdRect.localScale = Vector3.one; _sdRect.anchoredPosition = Vector2.zero; }
    }

    private void Update()
    {
        if (_sd == null) return;
        float dt = Time.unscaledDeltaTime;
        _glow = Mathf.MoveTowards(_glow, _hover ? 1f : 0f, dt * 5f);

        SetAlpha(_cone, Mathf.Lerp(0.05f, 0.085f, _glow));
        SetAlpha(_pool, Mathf.Lerp(0.10f, 0.20f, _glow));
        SetAlpha(_hint, _glow);

        // 숨쉬기: 발 위치는 그대로 두고 위아래로만 아주 조금 (동작 재생 중엔 멈춤)
        if (_act == null)
        {
            float s = 1f + BreathAmount * Mathf.Sin((Time.unscaledTime + _phase) * Mathf.PI * 2f / BreathPeriod);
            _sdRect.localScale = new Vector3(1f, s, 1f);
            _sdRect.anchoredPosition = new Vector2(0f, FootY * (1f - s));
        }
    }

    // ── 마우스 ───────────────────────────────────────────────

    private void HookPointer()
    {
        var trigger = _sd.GetComponent<EventTrigger>();
        if (trigger == null) trigger = _sd.gameObject.AddComponent<EventTrigger>();
        Add(trigger, EventTriggerType.PointerEnter, _ => OnEnter());
        Add(trigger, EventTriggerType.PointerExit, _ => OnExit());
    }

    private static void Add(EventTrigger t, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var e = new EventTrigger.Entry { eventID = type };
        e.callback.AddListener(action);
        t.triggers.Add(e);
    }

    private void OnEnter()
    {
        _hover = true;
        StopAct();
        var profile = _unit == null ? null : JudgeShot.Find(_unit.id, _unit.currentSkin == 3 ? 0 : _unit.currentSkin);
        // 컷인 그림이 있으면 컷인 동작, 없으면 판 위 동작
        var cut = JudgeShot.CutFrames(profile);
        if (cut != null)
        {
            // 컷인 그림(700px 캔버스, 캐릭터 높이 61%, 발 82%)을 SD 캐릭터와 같은 키·같은 발 위치로
            float size = CutCanvas * cut[0].rect.width / 700f;
            float y = FootY - (0.5f - CutFootOf(profile.folder)) * size;
            _act = StartCoroutine(PlayFrames(cut, profile.cutTimes, new Vector2(size, size), y));
            return;
        }
        var frames = JudgeShot.ActFrames(profile);
        if (frames != null) _act = StartCoroutine(PlayFrames(frames, profile.frameTimes, _sdBaseSize * profile.canvasScale, 0f));
    }

    private void OnExit()
    {
        _hover = false;
        StopAct();   // 마우스를 떼면 바로 끝낸다
    }

    /// <summary>심판 동작을 한 번만 재생한다 (발사체·번쩍임 없음).</summary>
    private IEnumerator PlayFrames(Sprite[] frames, float[] times, Vector2 size, float y)
    {
        _sdRect.localScale = Vector3.one;
        _sdRect.anchoredPosition = new Vector2(0f, y);
        _sdRect.sizeDelta = size;
        for (int f = 0; f < frames.Length; f++)
        {
            _sd.sprite = frames[f];
            yield return new WaitForSecondsRealtime(times[Mathf.Min(f, times.Length - 1)]);
        }
        RestoreSprite();
        _act = null;
    }

    private void StopAct()
    {
        if (_act != null) StopCoroutine(_act);
        _act = null;
        RestoreSprite();
    }

    private void RestoreSprite()
    {
        if (_sd == null) return;
        if (_restSprite != null) _sd.sprite = _restSprite;
        _sdRect.sizeDelta = _sdBaseSize;
        _sdRect.anchoredPosition = Vector2.zero;
    }

    // ── 꾸밈 ─────────────────────────────────────────────────

    private void BuildLights()
    {
        // 위에서 떨어지는 빛 기둥 → 바닥 빛 웅덩이 → 발밑 그림자 → 캐릭터 순서로 쌓는다
        var footWorldY = ((RectTransform)transform).anchoredPosition.y + FootY;
        float coneHeight = 540f - footWorldY + 40f;   // 화면 위 끝까지
        _cone = MakeImage("FX Cone", FxSprites.Cone, new Vector2(420f, coneHeight), new Vector2(0f, FootY + coneHeight * 0.5f - 20f));
        _pool = MakeImage("FX Pool", FxSprites.Dot, new Vector2(400f, 70f), new Vector2(0f, FootY));
        _shadow = MakeImage("FX Shadow", FxSprites.Dot, new Vector2(190f, 30f), new Vector2(0f, FootY - 2f));
        _shadow.color = new Color(0f, 0f, 0f, 0.75f);
        _pool.color = new Color(0.92f, 0.91f, 0.87f, 0.10f);
        _cone.color = new Color(0.92f, 0.91f, 0.87f, 0.05f);
        _shadow.transform.SetSiblingIndex(0);
        _pool.transform.SetSiblingIndex(0);
        _cone.transform.SetSiblingIndex(0);
    }

    private Image MakeImage(string name, Sprite sprite, Vector2 size, Vector2 pos)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        var r = img.rectTransform;
        r.SetParent(transform, false);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private void BuildHint(TMP_FontAsset font)
    {
        _hint = new GameObject("FX Hint", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var r = _hint.rectTransform;
        r.SetParent(transform, false);
        r.sizeDelta = new Vector2(400f, 40f);
        if (font != null) _hint.font = font;
        _hint.fontSize = 26f;
        _hint.alignment = TextAlignmentOptions.Center;
        _hint.raycastTarget = false;
        _hint.color = new Color(0.92f, 0.91f, 0.87f, 0f);
    }

    private void RefreshHint()
    {
        if (_hint == null) return;
        _hint.text = JourneyRefit.IsUnitLocked(_unit) ? "여정 중에는 바꿀 수 없어요" : "▶ 눌러서 교체";
        // 특성이 두 줄이어도 겹치지 않게 글자 바로 아래에 둔다
        float y = -285f;
        if (_label != null)
        {
            var lr = _label.rectTransform;
            float h = _label.GetPreferredValues(_label.text, lr.rect.width, 0f).y;
            y = lr.anchoredPosition.y - h - 26f;
        }
        _hint.rectTransform.anchoredPosition = new Vector2(0f, y);
    }

    private static void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color; c.a = a; g.color = c;
    }
}

/// <summary>코드로 만드는 부드러운 그림 (그림 파일 없이).</summary>
public static class FxSprites
{
    private static Sprite _dot, _cone, _fadeBar;

    /// <summary>가운데가 진하고 가장자리로 사라지는 원 (늘려서 타원으로 씀)</summary>
    public static Sprite Dot => _dot != null ? _dot : _dot = Make(64, 64, (x, y) =>
    {
        float d = Mathf.Sqrt(x * x + y * y);
        return Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d);
    });

    /// <summary>아래가 넓은 빛 기둥. 위로 갈수록 좁아지고, 위·아래 끝은 흐려진다</summary>
    public static Sprite Cone => _cone != null ? _cone : _cone = Make(128, 256, (x, y) =>
    {
        float v = (y + 1f) * 0.5f;                 // 0 아래 ~ 1 위
        float half = Mathf.Lerp(1f, 0.38f, v);     // 아래 폭 100%, 위 폭 38%
        float edge = 1f - Ss(half * 0.15f, half, Mathf.Abs(x));
        float vert = Ss(0f, 0.12f, v) * Mathf.Lerp(1f, 0.35f, v);
        return edge * vert;
    });

    /// <summary>좌우 끝이 사라지는 띠 (안내 띠 바탕)</summary>
    public static Sprite FadeBar => _fadeBar != null ? _fadeBar : _fadeBar = Make(256, 8, (x, y) =>
        Ss(0f, 1f, Mathf.Clamp01((1f - Mathf.Abs(x)) / 0.35f)));

    // GLSL smoothstep (Mathf.SmoothStep은 from→to 보간이라 다름)
    private static float Ss(float e0, float e1, float x)
    {
        float k = Mathf.Clamp01((x - e0) / (e1 - e0));
        return k * k * (3f - 2f * k);
    }

    private static Sprite Make(int w, int h, System.Func<float, float, float> alpha)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var px = new Color32[w * h];
        for (int j = 0; j < h; j++)
        for (int i = 0; i < w; i++)
        {
            float x = (i + 0.5f) / w * 2f - 1f, y = (j + 0.5f) / h * 2f - 1f;
            px[j * w + i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(x, y)) * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply();
        var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        s.hideFlags = HideFlags.DontSave;
        return s;
    }
}
