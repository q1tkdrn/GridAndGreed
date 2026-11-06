using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 전투 손맛 연출 (2026-11-05). 그림 없이 기존 그래픽을 움직이기만 한다.
/// - 보스 피격: HP 바가 실제로 줄어드는 순간(BossHpBarFx.Damaged) 보스가 피해만큼 흔들리고 피해 숫자가 튀어 오른다.
/// - 공격 예고: 보스 공격 칸이 가운데부터 번져 채워지고, 맞기 직전에 두 번 깜빡인다.
/// - 아군 피격: 맞은 캐릭터가 붉게 번쩍이며 흔들리고 판 전체가 살짝 흔들린다. 3번째 피격은 쓰러지며 사라진다.
/// - 힘 칸·욕심 칸: 밟으면 캐릭터 발밑에서 빛 고리가 퍼진다. 욕심 칸에서 버티면 금빛 고리 + "버티기!"
/// - 의지력: 오르면 숫자가 붉게 튀고, 한 번에 2 이상 오르면 화면 가장자리가 붉게 맥동한다.
///
/// 마음에 안 들면 Enabled = false 한 줄로 전부 끈다 (호출하는 쪽은 그대로 둬도 된다).
/// </summary>
public static class BattleJuice
{
    public static bool Enabled = true;

    private const int SortingOrder = 310;   // 전투 UI·심판 컷인(300) 위, 튜토리얼(520)·ESC(550) 아래
    private static readonly Color PowerColor = new Color(0.7f, 1f, 0.7f, 1f);
    private static readonly Color GreedColor = new Color(1f, 0.82f, 0.3f, 1f);
    private static readonly Color BigDamageColor = new Color(1f, 0.82f, 0.3f, 1f);

    private static Runner _runner;
    private static Sprite _ring;
    private static Sprite _vignette;
    private static string _pendingDetail;
    private static float _pendingTime;

    // ───────────────────────────── 보스 피격 ─────────────────────────────

    /// <summary>심판 직전: 피해 숫자 아래에 붙일 설명 (특성·아이템 추가 피해, 한도로 줄어든 피해).</summary>
    public static void SetJudgeDetail(int raw, int damage, List<string> used)
    {
        if (!Enabled) return;
        var parts = new List<string>();
        if (used != null) parts.AddRange(used);
        if (damage < raw) parts.Add($"{raw} → {damage}");
        _pendingDetail = parts.Count > 0 ? string.Join("  ·  ", parts) : null;
        _pendingTime = Time.unscaledTime;
    }

    /// <summary>보스 HP 바가 실제로 줄어든 순간 (BossHpBarFx). anchor: HP 바.</summary>
    public static void BossDamaged(int amount, RectTransform anchor)
    {
        if (!Enabled || amount <= 0) return;
        var runner = Get();
        if (runner == null) return;
        var board = Object.FindFirstObjectByType<BoardPanel>();
        if (board != null && board.BossImage != null)
            HitShake.For(board.BossImage).Play(8f + Mathf.Min(amount, 60) * 0.35f, 0.25f + Mathf.Min(amount, 60) * 0.005f);
        string detail = _pendingDetail != null && Time.unscaledTime - _pendingTime < 6f ? _pendingDetail : null;
        _pendingDetail = null;
        // 보스 초상화 가운데쯤 (HP 바 아래)
        // HP 바의 가운데 아래(보스 칸 가운데)에 띄운다. 피벗이 왼쪽이라 position 대신 네 모서리로 가운데를 구한다.
        Vector3 pos = new Vector3(Screen.width * 0.86f, Screen.height * 0.55f);
        if (anchor != null)
        {
            var c = new Vector3[4];
            anchor.GetWorldCorners(c);
            pos = new Vector3((c[0].x + c[2].x) * 0.5f, c[0].y - Screen.height * 0.22f, 0f);
        }
        runner.StartCoroutine(DamageNumber(runner, pos, amount, detail, board != null ? board.UiFont : null));
    }

    private static IEnumerator DamageNumber(Runner runner, Vector3 pos, int amount, string detail, TMP_FontAsset font)
    {
        float size = 56f + Mathf.Min(amount, 60) * 0.75f;
        // 묶음: 어두운 둥근 바탕 + 숫자 + 설명. 밝은 보스 그림 위에서도 읽히게 바탕을 깐다.
        var group = new GameObject("Damage Number", typeof(RectTransform), typeof(CanvasGroup));
        var rect = (RectTransform)group.transform;
        rect.SetParent(runner.transform, false);
        rect.sizeDelta = new Vector2(10f, 10f);
        rect.position = pos;
        var fade = group.GetComponent<CanvasGroup>();
        fade.blocksRaycasts = false;

        var back = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        back.rectTransform.SetParent(rect, false);
        back.rectTransform.sizeDelta = new Vector2(size * 2.4f, size * 1.5f);
        back.sprite = SoftDotSprite();
        back.color = new Color(0f, 0f, 0f, 0.6f);
        back.raycastTarget = false;

        var text = NewText(rect, $"-{amount}", font, size, amount >= 30 ? BigDamageColor : Color.white);
        if (!string.IsNullOrEmpty(detail))
        {
            var sub = NewText(rect, detail, font, 26f, new Color(1f, 0.9f, 0.75f));
            sub.rectTransform.anchoredPosition = new Vector2(0f, -size * 0.75f);
            back.rectTransform.sizeDelta = new Vector2(Mathf.Max(size * 2.4f, sub.preferredWidth + 60f), size * 2.1f);
            back.rectTransform.anchoredPosition = new Vector2(0f, -size * 0.25f);
        }

        float x = Random.Range(-10f, 10f);
        const float total = 1.2f;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            if (rect == null) yield break;
            float k = t / total;
            float pop = t < 0.12f ? Mathf.Lerp(0.2f, 1.4f, t / 0.12f) : t < 0.24f ? Mathf.Lerp(1.4f, 1f, (t - 0.12f) / 0.12f) : 1f;
            text.rectTransform.localScale = Vector3.one * pop;   // 숫자만 튀고, 바탕과 설명은 그대로
            // 화면 좌표로 매 프레임 다시 잡는다 (캔버스 크기가 바뀌어도 보스 칸에 붙어 있게)
            rect.position = pos + (Vector3)(new Vector2(x * k, 70f * Mathf.Sqrt(k)) * rect.lossyScale.x);
            fade.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            yield return null;
        }
        if (group != null) Object.Destroy(group);
    }

    private static Sprite _softDot;
    // 가운데가 진하고 가장자리로 갈수록 투명해지는 둥근 점 (숫자 뒤 바탕)
    private static Sprite SoftDotSprite()
    {
        if (_softDot != null) return _softDot;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Juice Soft Dot" };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
            float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            a = a * a * (3f - 2f * a);
            px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        _softDot = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return _softDot;
    }

    // ───────────────────────────── 보스 공격 예고 ─────────────────────────────

    /// <summary>
    /// 공격 칸 연출. 원래의 "칸을 켜고 seconds초 기다리기"를 대신한다 (같은 시간이 걸린다).
    /// 꺼져 있으면 그냥 기다리기만 한다.
    /// </summary>
    public static IEnumerator Telegraph(List<(Image plate, Vector2Int cell)> plates, float seconds)
    {
        if (!Enabled || plates == null || plates.Count == 0)
        {
            yield return new WaitForSeconds(seconds);
            yield break;
        }
        Vector2 center = Vector2.zero;
        foreach (var p in plates) center += (Vector2)p.cell;
        center /= plates.Count;
        float maxDist = 0.01f;
        foreach (var p in plates) maxDist = Mathf.Max(maxDist, Vector2.Distance(p.cell, center));
        var baseColor = new Color[plates.Count];
        for (int i = 0; i < plates.Count; i++) baseColor[i] = plates[i].plate != null ? plates[i].plate.color : Color.red;

        float fill = Mathf.Min(0.3f, seconds * 0.35f);
        float blink = Mathf.Min(0.36f, seconds * 0.4f);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            for (int i = 0; i < plates.Count; i++)
            {
                var img = plates[i].plate;
                if (img == null) continue;
                float a = 1f;
                if (t < fill)
                {
                    // 가운데부터 바깥으로 번진다
                    float d = Vector2.Distance(plates[i].cell, center) / maxDist;
                    a = Mathf.Clamp01((t / fill) * 1.6f - d);
                }
                else if (t > seconds - blink)
                {
                    // 맞기 직전 두 번 깜빡임
                    float b = (t - (seconds - blink)) / blink;
                    a = 0.45f + 0.55f * Mathf.Abs(Mathf.Cos(b * Mathf.PI * 2f));
                }
                var c = baseColor[i];
                c.a *= a;
                img.color = c;
            }
            yield return null;
        }
        for (int i = 0; i < plates.Count; i++) if (plates[i].plate != null) plates[i].plate.color = baseColor[i];
    }

    // ───────────────────────────── 아군 피격·쓰러짐 ─────────────────────────────

    /// <summary>보스 공격이 맞은 직후. 맞은 캐릭터 수만큼 판 흔들림이 커진다.</summary>
    public static void UnitsHit(BoardPanel board, IList<Image> units)
    {
        if (!Enabled || units == null || units.Count == 0) return;
        foreach (var u in units) if (u != null) HitShake.For(u).Play(14f, 0.45f);
        if (board != null) ShakeBoard((RectTransform)board.transform, 7f + 4f * Mathf.Min(units.Count, 3), 0.32f);
    }

    /// <summary>3번째 피격으로 쓰러질 때: 원래 그림은 바로 숨기고, 같은 모양의 잔상이 옆으로 쓰러지며 사라진다.</summary>
    public static void UnitDown(Image unit)
    {
        if (!Enabled || unit == null || unit.sprite == null) return;
        var runner = Get();
        if (runner == null) return;
        var src = unit.rectTransform;
        var go = new GameObject("Down Ghost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(src.parent, false);
        rect.anchorMin = src.anchorMin; rect.anchorMax = src.anchorMax;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = src.sizeDelta;
        // 발 기준으로 쓰러지게 피벗을 아래로 옮긴 만큼 위치를 보정한다
        rect.anchoredPosition = src.anchoredPosition + new Vector2((0.5f - src.pivot.x) * src.rect.width, -src.pivot.y * src.rect.height);
        var img = go.GetComponent<Image>();
        img.sprite = unit.sprite;
        img.preserveAspect = unit.preserveAspect;
        img.color = new Color(1f, 0.45f, 0.45f, 1f);
        img.raycastTarget = false;
        runner.StartCoroutine(FallAndFade(rect, img));
    }

    private static IEnumerator FallAndFade(RectTransform rect, Image img)
    {
        const float total = 0.75f;
        float dir = Random.value < 0.5f ? 1f : -1f;
        Vector2 start = rect.anchoredPosition;
        for (float t = 0f; t < total; t += Time.deltaTime)
        {
            if (rect == null) yield break;
            float k = t / total;
            float ease = k * k;
            rect.localRotation = Quaternion.Euler(0f, 0f, -85f * dir * ease);
            rect.anchoredPosition = start + new Vector2(10f * dir * ease, -8f * ease);
            var c = img.color; c.a = 1f - Mathf.Clamp01((k - 0.35f) / 0.65f); img.color = c;
            yield return null;
        }
        if (rect != null) Object.Destroy(rect.gameObject);
    }

    // ───────────────────────────── 힘 칸·욕심 칸 ─────────────────────────────

    public static void PowerPickup(Image unit, bool greed)
    {
        if (!Enabled || unit == null) return;
        var runner = Get();
        if (runner == null) return;
        var pos = Feet(unit);
        runner.StartCoroutine(Ring(runner, pos, greed ? GreedColor : PowerColor, 0f, 0.45f, 150f));
        if (greed) runner.StartCoroutine(Ring(runner, pos, GreedColor, 0.1f, 0.45f, 110f));
    }

    /// <summary>욕심 칸에서 맞고 버텼을 때. 위험을 감수한 보상이라 크게.</summary>
    public static void GreedStand(Image unit)
    {
        if (!Enabled || unit == null) return;
        var runner = Get();
        if (runner == null) return;
        var pos = Feet(unit);
        runner.StartCoroutine(Ring(runner, pos, GreedColor, 0f, 0.6f, 260f));
        runner.StartCoroutine(Ring(runner, pos, GreedColor, 0.12f, 0.6f, 200f));
        runner.StartCoroutine(Ring(runner, pos, Color.white, 0.24f, 0.5f, 140f));
        var board = Object.FindFirstObjectByType<BoardPanel>();
        var head = unit.rectTransform.position + new Vector3(0f, unit.rectTransform.rect.height * unit.rectTransform.lossyScale.y * 0.9f, 0f);
        runner.StartCoroutine(PopWord(runner, head, "버티기!", board != null ? board.UiFont : null, GreedColor));
    }

    private static IEnumerator PopWord(Runner runner, Vector3 pos, string word, TMP_FontAsset font, Color color)
    {
        var text = NewText(runner.transform, word, font, 46f, color);
        var rect = text.rectTransform;
        rect.position = pos;
        Vector2 start = rect.anchoredPosition;
        const float total = 1.0f;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            if (rect == null) yield break;
            float k = t / total;
            float pop = t < 0.1f ? Mathf.Lerp(0.3f, 1.35f, t / 0.1f) : t < 0.22f ? Mathf.Lerp(1.35f, 1f, (t - 0.1f) / 0.12f) : 1f;
            rect.localScale = Vector3.one * pop;
            rect.anchoredPosition = start + new Vector2(0f, 40f * k);
            text.alpha = k < 0.65f ? 1f : 1f - (k - 0.65f) / 0.35f;
            yield return null;
        }
        if (text != null) Object.Destroy(text.gameObject);
    }

    // ───────────────────────────── 의지력 ─────────────────────────────

    public static void WillPowerChanged(TMP_Text text, int before, int after)
    {
        if (!Enabled || text == null || after <= before || before < 0) return;
        var runner = Get();
        if (runner == null) return;
        runner.StartCoroutine(Punch(text, after - before >= 2 ? 1.8f : 1.45f));
        if (after - before >= 2) runner.StartCoroutine(Vignette(runner));
    }

    private static IEnumerator Punch(TMP_Text text, float peak)
    {
        var rect = text.rectTransform;
        var color = text.color;
        var red = new Color(1f, 0.35f, 0.3f, color.a);
        const float total = 0.7f;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            if (text == null) yield break;
            float k = t / total;
            float s = k < 0.15f ? Mathf.Lerp(1f, peak, k / 0.15f) : Mathf.Lerp(peak, 1f, (k - 0.15f) / 0.85f);
            rect.localScale = Vector3.one * s;
            text.color = Color.Lerp(red, color, Mathf.Clamp01((k - 0.3f) / 0.7f));
            yield return null;
        }
        if (text != null) { rect.localScale = Vector3.one; text.color = color; }
    }

    private static IEnumerator Vignette(Runner runner)
    {
        var go = new GameObject("Will Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(runner.transform, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = VignetteSprite();
        img.raycastTarget = false;
        const float total = 1.3f;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            float k = t / total;
            // 두 번 맥동하며 사라진다
            float a = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)) * (1f - k * 0.6f) * 0.55f;
            img.color = new Color(0.75f, 0.05f, 0.05f, a);
            yield return null;
        }
        Object.Destroy(go);
    }

    // ───────────────────────────── 공통 ─────────────────────────────

    private static readonly Dictionary<RectTransform, Coroutine> _shakes = new Dictionary<RectTransform, Coroutine>();
    private static readonly Dictionary<RectTransform, Vector3> _shakeBase = new Dictionary<RectTransform, Vector3>();

    private static void ShakeBoard(RectTransform target, float amplitude, float duration)
    {
        var runner = Get();
        if (runner == null || target == null) return;
        if (_shakes.TryGetValue(target, out var running) && running != null) runner.StopCoroutine(running);
        else _shakeBase[target] = target.localPosition;
        _shakes[target] = runner.StartCoroutine(Shake(target, amplitude, duration));
    }

    private static IEnumerator Shake(RectTransform target, float amplitude, float duration)
    {
        var basePos = _shakeBase[target];
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (target == null) yield break;
            float fall = 1f - t / duration;
            target.localPosition = basePos + new Vector3(Mathf.Sin(t * 70f) * amplitude * fall, Mathf.Cos(t * 55f) * amplitude * 0.5f * fall, 0f);
            yield return null;
        }
        if (target != null) target.localPosition = basePos;
        _shakes.Remove(target);
        _shakeBase.Remove(target);
    }

    private static IEnumerator Ring(Runner runner, Vector3 pos, Color color, float delay, float duration, float size)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        var go = new GameObject("Ring", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(runner.transform, false);
        rect.position = pos;
        rect.sizeDelta = new Vector2(size, size * 0.55f);   // 바닥에 깔린 느낌으로 납작하게
        var img = go.GetComponent<Image>();
        img.sprite = RingSprite();
        img.raycastTarget = false;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            rect.localScale = Vector3.one * Mathf.Lerp(0.25f, 1f, 1f - (1f - k) * (1f - k));
            var c = color; c.a = (1f - k) * 0.95f;
            img.color = c;
            yield return null;
        }
        Object.Destroy(go);
    }

    private static Vector3 Feet(Image unit)
    {
        var r = unit.rectTransform;
        var corners = new Vector3[4];
        r.GetWorldCorners(corners);
        return new Vector3((corners[0].x + corners[3].x) * 0.5f, corners[0].y + (corners[1].y - corners[0].y) * 0.12f, 0f);
    }

    // 외곽선(outline)은 이 글꼴(작은 아틀라스)에서 글자가 뭉개진 덩어리로 보여서 쓰지 않는다 (2026-11-05).
    // 대신 굵은 글꼴 + 살짝 어긋난 검은 그림자 글자를 뒤에 깐다.
    private static TMP_FontAsset _bold;

    private static TextMeshProUGUI NewText(Transform parent, string value, TMP_FontAsset font, float size, Color color)
    {
        if (_bold == null) _bold = GameFonts.Bold;
        if (_bold != null) font = _bold;
        var go = new GameObject("Juice Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        var text = go.GetComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(parent, false);
        text.rectTransform.sizeDelta = new Vector2(700f, size * 1.3f);
        // 그림자: 같은 글자를 검게, 오른쪽 아래로 조금. 본 글자 바로 앞 형제로 두어 뒤에 그려지고, 매 프레임 위치·크기·투명도를 따라간다.
        var shadow = new GameObject("Shadow", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        shadow.rectTransform.SetParent(parent, false);
        shadow.rectTransform.sizeDelta = new Vector2(700f, size * 1.3f);
        shadow.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
        if (font != null) shadow.font = font;
        shadow.text = value; shadow.fontSize = size; shadow.alignment = TextAlignmentOptions.Center;
        shadow.textWrappingMode = TextWrappingModes.NoWrap; shadow.color = new Color(0f, 0f, 0f, 0.75f); shadow.raycastTarget = false;
        var follow = go.AddComponent<ShadowFollow>(); follow.shadow = shadow; follow.offset = new Vector2(size * 0.05f, -size * 0.05f);
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Runner Get()
    {
        if (_runner != null && _runner.gameObject.scene == SceneManager.GetActiveScene()) return _runner;
        var go = new GameObject("Battle Juice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _runner = go.AddComponent<Runner>();
        // 새로 만든 캔버스는 프레임 끝에야 화면 크기에 맞춰지므로, 바로 위치를 잡을 수 있게 지금 맞춘다.
        // (안 하면 첫 피해 숫자·고리가 엉뚱한 곳에 뜬다)
        Canvas.ForceUpdateCanvases();
        return _runner;
    }

    private static Sprite RingSprite()
    {
        if (_ring != null) return _ring;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Juice Ring" };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.14f);
            a = a * a * (3f - 2f * a);
            px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        _ring = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return _ring;
    }

    private static Sprite VignetteSprite()
    {
        if (_vignette != null) return _vignette;
        const int w = 160, h = 90;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Juice Vignette" };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
            float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.3f, r));
            px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        _vignette = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return _vignette;
    }

    private sealed class Runner : MonoBehaviour { }

    // 그림자 글자의 투명도를 본 글자와 맞춘다.
    private sealed class ShadowFollow : MonoBehaviour
    {
        public TextMeshProUGUI shadow;
        public Vector2 offset;
        private TextMeshProUGUI _text;
        private void Awake() => _text = GetComponent<TextMeshProUGUI>();
        private void LateUpdate()
        {
            if (shadow == null || _text == null) return;
            var r = _text.rectTransform;
            shadow.rectTransform.anchoredPosition = r.anchoredPosition + offset * r.localScale.x;
            shadow.rectTransform.localScale = r.localScale;
            shadow.alpha = _text.alpha * 0.75f;
        }
        private void OnDestroy() { if (shadow != null) Destroy(shadow.gameObject); }
    }
}
