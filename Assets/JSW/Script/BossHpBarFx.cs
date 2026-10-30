using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 보스 HP 바 연출.
// - 잔상: HP가 줄면 바는 바로 줄고, 줄기 전 자리에 붉은 잔상이 잠깐 머물렀다가 서서히 따라 줄어든다.
//         HP 숫자도 잔상과 함께 내려간다. 회복은 바로 반영한다.
// - 미리보기: SetPreview(피해량)를 주면 지금 깎일 구간을 바 위에서 붉게 깜빡이고,
//            HP 숫자 옆에 "-6"처럼 예상 피해를 보여준다.
[DisallowMultipleComponent]
public sealed class BossHpBarFx : MonoBehaviour
{
    private const float HoldTime = 0.3f;        // 잔상이 멈춰 있는 시간
    private const float DrainTime = 0.6f;       // 잔상이 따라 줄어드는 시간
    private const float PulsePeriod = 0.9f;     // 미리보기 깜빡임 한 번의 시간
    private const float PreviewFadeTime = 0.12f;
    private static readonly Color TrailColor = new Color(0.72f, 0.2f, 0.17f, 1f);
    private static readonly Color PreviewColor = new Color(0.9f, 0.22f, 0.18f, 1f);
    private static readonly Color PreviewTextColor = new Color(1f, 0.45f, 0.4f, 1f);

    private Slider slider;
    private RectTransform fill;
    private Image trail;
    private Image preview;
    private TMP_Text hpText;
    private TextMeshProUGUI previewText;
    private Func<int> currentHp;
    private Func<int> maxHp;

    private float trailValue = -1f;   // 잔상의 오른쪽 끝 (0~1)
    private float lastValue = -1f;
    private float hold;
    private float drainSpeed;
    private bool wasDraining;
    private int previewDamage;        // 지금 요청된 미리보기 피해 (0이면 숨김)
    private int shownDamage;          // 사라지는 동안에도 보여줄 마지막 피해
    private float previewAlpha;
    private float pulseTime;
    private float displayHold;        // >0 이면 HP가 줄어도 바를 그대로 보여 준다 (화살이 닿을 때까지)
    private Color fillColor = Color.white;

    public static BossHpBarFx Attach(Slider slider, TMP_Text hpText, Func<int> currentHp, Func<int> maxHp)
    {
        if (slider == null || slider.fillRect == null) return null;
        var fx = slider.GetComponent<BossHpBarFx>();
        if (fx == null)
        {
            fx = slider.gameObject.AddComponent<BossHpBarFx>();
            fx.Build(slider, hpText);
        }
        fx.currentHp = currentHp;
        fx.maxHp = maxHp;
        return fx;
    }

    /// <summary>미리보기로 보여줄 피해. 0 이하면 숨긴다. 매 프레임 불러도 된다.</summary>
    public void SetPreview(int damage)
    {
        previewDamage = Mathf.Max(0, damage);
        if (previewDamage > 0) shownDamage = previewDamage;
    }

    /// <summary>
    /// 피해가 "닿는" 순간까지 바와 숫자를 줄기 전 모습으로 붙잡아 둔다. ReleaseDisplay를 부르면 그때부터 평소처럼 줄어든다.
    /// 혹시 Release가 오지 않아도 maxSeconds 뒤에는 저절로 풀린다.
    /// </summary>
    public void HoldDisplay(float maxSeconds = 1.5f) => displayHold = Mathf.Max(displayHold, maxSeconds);
    public void ReleaseDisplay() => displayHold = 0f;

    private void Build(Slider s, TMP_Text text)
    {
        slider = s;
        fill = s.fillRect;
        hpText = text;

        var parent = fill.parent;
        var fillImg = fill.GetComponent<Image>();
        if (fillImg != null) fillColor = fillImg.color;
        // 잔상은 fill 뒤에 그려서, fill이 줄어든 만큼만 보인다.
        trail = CreateBar("HP Trail", parent, TrailColor);
        trail.transform.SetSiblingIndex(fill.GetSiblingIndex());
        trail.enabled = false;
        // 미리보기는 fill 위에 겹쳐 그린다.
        preview = CreateBar("HP Preview", parent, PreviewColor);
        preview.transform.SetSiblingIndex(fill.GetSiblingIndex() + 1);
        preview.enabled = false;

        if (hpText != null) previewText = CreatePreviewText(hpText);
    }

    private Image CreateBar(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        var fillImage = fill.GetComponent<Image>();
        if (fillImage != null)
        {
            image.sprite = fillImage.sprite;
            image.type = fillImage.type;
        }
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // HP 숫자(오른쪽 정렬) 바로 오른쪽에 붙는 "-6" 글자.
    private static TextMeshProUGUI CreatePreviewText(TMP_Text source)
    {
        var go = new GameObject("HP Preview Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = source.gameObject.layer;
        var rect = (RectTransform)go.transform;
        var sourceRect = source.rectTransform;
        rect.SetParent(sourceRect.parent, false);
        rect.anchorMin = rect.anchorMax = sourceRect.anchorMin;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(160f, sourceRect.rect.height);
        rect.localPosition = sourceRect.localPosition
            + new Vector3(sourceRect.rect.xMax + 12f, sourceRect.rect.center.y, 0f);

        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = source.font;
        label.fontSharedMaterial = source.fontSharedMaterial;
        label.fontSize = source.fontSize * 0.8f;
        label.alignment = TextAlignmentOptions.Left;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = PreviewTextColor;
        label.raycastTarget = false;
        label.text = "";
        return label;
    }

    private void OnDisable()
    {
        // 다음에 켜질 때 지금 HP에서 새로 시작한다.
        trailValue = lastValue = -1f;
        previewAlpha = 0f;
        displayHold = 0f;
        if (trail != null) trail.enabled = false;
        if (preview != null) preview.enabled = false;
        if (previewText != null) previewText.text = "";
    }

    private void LateUpdate()
    {
        if (slider == null || fill == null) return;
        if (slider.direction != Slider.Direction.LeftToRight) return; // 지금 바는 왼쪽→오른쪽만 지원

        float dt = Time.unscaledDeltaTime;
        float value = Mathf.Clamp01(slider.normalizedValue);
        if (trailValue < 0f) trailValue = lastValue = value;
        int maxHold = maxHp != null ? maxHp() : 0;

        // ---------- 피해가 닿기 전: 줄기 전 모습 유지 ----------
        if (displayHold > 0f)
        {
            displayHold -= Time.deltaTime; // 사격 동작과 같은 시간으로 센다
            if (value >= trailValue) trailValue = value;
            lastValue = trailValue; // 풀리는 순간 "새로 깎였다"로 처리되어 잔상이 시작된다
            bool covered = trailValue > value + 0.0005f;
            trail.enabled = covered;
            trail.color = fillColor;
            if (covered) SetSegment(trail.rectTransform, 0f, trailValue);
            if (hpText != null && maxHold > 0)
                hpText.text = $"{Mathf.RoundToInt(trailValue * maxHold)} / {maxHold}";
            wasDraining = true;
            preview.enabled = false;
            previewAlpha = 0f;
            if (previewText != null && previewText.text.Length > 0) previewText.text = "";
            return;
        }
        trail.color = TrailColor;

        // ---------- 잔상 ----------
        if (value >= trailValue)
        {
            // 회복·전투 시작: 잔상 없이 바로 맞춘다.
            trailValue = value;
            hold = 0f;
            drainSpeed = 0f;
        }
        else if (value < lastValue - 0.0001f)
        {
            // 새로 깎였다: 잔상을 잠깐 멈춰 둔다.
            hold = HoldTime;
            drainSpeed = 0f;
        }
        else if (hold > 0f)
        {
            hold -= dt;
        }
        else
        {
            if (drainSpeed <= 0f) drainSpeed = Mathf.Max((trailValue - value) / DrainTime, 0.01f);
            trailValue = Mathf.MoveTowards(trailValue, value, drainSpeed * dt);
        }
        lastValue = value;

        bool draining = trailValue > value + 0.0005f;
        trail.enabled = draining;
        if (draining) SetSegment(trail.rectTransform, 0f, trailValue);

        // ---------- HP 숫자 ----------
        int max = maxHp != null ? maxHp() : 0;
        int cur = currentHp != null ? currentHp() : 0;
        if (hpText != null && max > 0)
        {
            if (draining)
            {
                int shown = Mathf.Clamp(Mathf.RoundToInt(trailValue * max), cur, max);
                hpText.text = $"{shown} / {max}";
            }
            else if (wasDraining)
            {
                hpText.text = $"{cur} / {max}";
            }
        }
        wasDraining = draining;

        // ---------- 미리보기 ----------
        bool showPreview = previewDamage > 0 && !draining && max > 0;
        previewAlpha = Mathf.MoveTowards(previewAlpha, showPreview ? 1f : 0f, dt / PreviewFadeTime);
        if (previewAlpha > 0f && shownDamage > 0 && max > 0)
        {
            pulseTime += dt;
            float pulse = 0.5f + 0.5f * Mathf.Cos(pulseTime * Mathf.PI * 2f / PulsePeriod); // 1에서 시작
            float lo = Mathf.Max(0f, value - (float)shownDamage / max);
            SetSegment(preview.rectTransform, lo, value);
            var c = PreviewColor;
            c.a = Mathf.Lerp(0.35f, 0.95f, pulse) * previewAlpha;
            preview.color = c;
            preview.enabled = true;
            if (previewText != null)
            {
                previewText.text = $"-{shownDamage}";
                previewText.alpha = Mathf.Lerp(0.65f, 1f, pulse) * previewAlpha;
            }
        }
        else
        {
            pulseTime = 0f;
            preview.enabled = false;
            if (previewText != null && previewText.text.Length > 0) previewText.text = "";
        }
    }

    // fill과 같은 방식으로 [lo, hi] 구간을 그린다.
    // 슬라이더는 fill의 anchorMax.x만 값으로 바꾸고 좌우 여백(offset)은 그대로 두므로,
    // 값 v일 때 fill 오른쪽 끝 = v * 폭 + offsetMax.x, 왼쪽 끝 = offsetMin.x 이다.
    private void SetSegment(RectTransform rect, float lo, float hi)
    {
        var parent = (RectTransform)rect.parent;
        float width = parent.rect.width;
        float left = fill.offsetMin.x;
        float right = hi * width + fill.offsetMax.x;
        if (lo > 0f) left = Mathf.Max(left, lo * width + fill.offsetMax.x);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(left, fill.offsetMin.y);
        rect.offsetMax = new Vector2(Mathf.Max(left, right), fill.offsetMax.y);
    }
}
