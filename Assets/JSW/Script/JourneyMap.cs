using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 여정 지도: 마을 문을 누르면 뜬다.
// - 일반 보스 7곳 중 3곳만 열려 있고(안개 없음), 나머지는 안개에 덮여 있다. 이기고 돌아오면 다른 3곳의 안개가 걷힌다.
// - 열린 장소를 누르면 "○○이 기다리고 있다." → 예: 꼬마 사신이 걸어가고 보스전으로.
// - 처치한 장소: 이름표가 어두워지고 왼쪽에 X, 누를 수 없다.
// - 알현실은 항상 보이고, 4명을 처치해야 들어갈 수 있다. 저승 조건을 채운 판이면 저승도 함께 열린다.
// - 꼬마 사신에 마우스를 올리면 편성 버튼 (여정 중에는 정비 제한, JourneyLink.cs)
// 기획: 프로젝트 문서 '여정 지도 기획 v1'
public sealed class JourneyMap : MonoBehaviour
{
    private const int SortingOrder = 90;          // 마을 UI 위, ESC 메뉴(550)·확인 팝업(600) 아래
    private const float FadeToWhite = 0.35f;
    private const float FadeFromWhite = 0.45f;
    private const float CloseTime = 0.25f;
    private const int OpenCount = 3;
    private const int ClearsForFinal = 4;

    // 지도 위 장소: 이름표 그림, 보스, 이름표 위치, 사신이 설 자리, 안개 크기 (지도 그림 기준 0~1, 왼쪽 아래가 0,0)
    private sealed class Place
    {
        public string label, bossId, bossName;
        public Vector2 tag, stand, fog;
        public float fogScale;
        public bool final, afterlifeOnly;
        public string Title => label.Replace("여정_", "");
    }

    private static readonly Place[] Places =
    {
        new Place { label = "여정_대저택", bossId = "noble",      bossName = "영생귀족", tag = new Vector2(0.477f, 0.935f), stand = new Vector2(0.465f, 0.585f), fog = new Vector2(0.470f, 0.790f), fogScale = 1.0f },
        new Place { label = "여정_성당",   bossId = "pope",       bossName = "교황",     tag = new Vector2(0.830f, 0.915f), stand = new Vector2(0.790f, 0.650f), fog = new Vector2(0.800f, 0.800f), fogScale = 1.0f },
        new Place { label = "여정_서고",   bossId = "secretary",  bossName = "서기관",   tag = new Vector2(0.258f, 0.700f), stand = new Vector2(0.330f, 0.515f), fog = new Vector2(0.240f, 0.620f), fogScale = 1.05f },
        new Place { label = "여정_광장",   bossId = "fusion",     bossName = "융합체",   tag = new Vector2(0.461f, 0.553f), stand = new Vector2(0.465f, 0.415f), fog = new Vector2(0.460f, 0.470f), fogScale = 0.8f },
        new Place { label = "여정_연구소", bossId = "door",       bossName = "문",       tag = new Vector2(0.906f, 0.625f), stand = new Vector2(0.860f, 0.305f), fog = new Vector2(0.880f, 0.450f), fogScale = 1.15f },
        new Place { label = "여정_훈련장", bossId = "instructor", bossName = "교관",     tag = new Vector2(0.656f, 0.278f), stand = new Vector2(0.640f, 0.110f), fog = new Vector2(0.660f, 0.180f), fogScale = 1.05f },
        new Place { label = "여정_지하실", bossId = "subject",    bossName = "실험체",   tag = new Vector2(0.383f, 0.243f), stand = new Vector2(0.365f, 0.095f), fog = new Vector2(0.370f, 0.160f), fogScale = 1.0f },
        new Place { label = "여정_알현실", bossId = "king",       bossName = "왕",       tag = new Vector2(0.637f, 0.720f), stand = new Vector2(0.615f, 0.445f), final = true },
        new Place { label = "여정_저승",   bossId = "death1",     bossName = "죽음",     tag = new Vector2(0.090f, 0.345f), stand = new Vector2(0.110f, 0.165f), final = true, afterlifeOnly = true },
    };
    private static readonly Vector2 ReaperStart = new Vector2(0.422f, 0.278f); // 광장 아래 아치 입구
    private const float ReaperHeight = 150f;     // 캐릭터 키 (캔버스 px)
    private const float ReaperSpeed = 260f;      // 캔버스 px/초
    private const float LabelScale = 1.25f;
    private const float FogAlpha = 0.82f;
    private static readonly Color ClearedLabel = new Color(0.22f, 0.22f, 0.22f, 1f);
    private static readonly Color ClearedMark = new Color(0.82f, 0.22f, 0.18f, 1f);

    private static JourneyMap _instance;
    private CanvasGroup _group;
    private Image _white;
    private RectTransform _mapRect;
    private RectTransform _reaper;
    private GameObject _formationButton;
    private Coroutine _walk;
    private Coroutine _hideFormation;
    private bool _busy;
    private bool _afterlife;
    private TMP_FontAsset _font;
    private readonly List<(Image fog, Vector2 drift)> _revealFogs = new List<(Image, Vector2)>();

    public static bool IsOpen => _instance != null;

    /// <summary>instant: 장면 전환(흰 화면)이 이미 덮고 있으면 지도 자체의 흰 전환을 건너뛴다.</summary>
    public static void Open(bool instant = false)
    {
        if (_instance != null || (!instant && SceneFade.IsFading)) return;
        var go = new GameObject("Journey Map", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
        _instance = go.AddComponent<JourneyMap>();
        _instance.Build();
        _instance.StartCoroutine(_instance.OpenRoutine(instant));
    }

    public static void Close()
    {
        if (_instance == null || _instance._busy) return;
        _instance.StartCoroutine(_instance.CloseRoutine());
    }

    /// <summary>저승 조건: 편성(여정 중이면 여정 편성)에 꼬마 사신 + 사신의 낫 + 계약서.</summary>
    public static bool AfterlifeUnlocked
    {
        get
        {
            if (RunSave.HasRun)
            {
                var items = RunSave.ItemIds;
                return RunSave.UnitIds.Contains(JourneyRefit.ReaperId) && items.Contains("22") && items.Contains("23");
            }
            bool reaper = false, scythe = false, contract = false;
            for (int i = 0; i < 3; i++)
            {
                if (PlayerPrefs.GetInt("Formation_v1_Unit_" + i, 0) == JourneyRefit.ReaperId) reaper = true;
                string item = PlayerPrefs.GetString("Formation_v1_Item_" + i, "");
                if (item == "22") scythe = true;
                if (item == "23") contract = true;
            }
            return reaper && PlayerPrefs.GetInt("Character_9", 0) == 1
                && scythe && PlayerPrefs.GetInt("Item_22", 0) > 0
                && contract && PlayerPrefs.GetInt("Item_23", 0) > 0;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // ───────────────────────────── 상태 ─────────────────────────────

    private static List<string> Cleared() => RunSave.HasRun ? RunSave.ClearedBossIds.ToList() : new List<string>();

    // 지금 들어갈 수 있는 일반 보스 (안개가 걷힌 곳)
    private static HashSet<string> OpenBosses(List<string> cleared)
    {
        if (RunSave.HasCurrentBattle) return new HashSet<string> { RunSave.CurrentBossId }; // 싸우다 나간 보스
        var normal = Places.Where(p => !p.final).Select(p => p.bossId).ToList();
        var remaining = normal.Where(id => !cleared.Contains(id)).ToList();
        if (cleared.Count >= ClearsForFinal || remaining.Count == 0) return new HashSet<string>();

        var saved = RunSave.ChoiceIds.Where(remaining.Contains).ToList();
        if (saved.Count == Mathf.Min(OpenCount, remaining.Count)) return new HashSet<string>(saved);

        var picks = remaining.OrderBy(_ => UnityEngine.Random.value).Take(OpenCount).ToList();
        RunSave.SaveChoiceIds(picks); // 여정 중이면 저장 (껐다 켜도 같은 3곳)
        return new HashSet<string>(picks);
    }

    // ───────────────────────────── 화면 ─────────────────────────────

    private void Build()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _group = GetComponent<CanvasGroup>();
        _font = FindVillageFont();

        var back = NewImage("Back", transform, Color.black);
        Stretch(back.rectTransform);

        _afterlife = AfterlifeUnlocked;
        var map = NewImage("Map", transform, Color.white);
        map.sprite = LoadSprite(_afterlife ? "여정맵" : "여정맵_저승없음");
        map.raycastTarget = true; // 뒤의 마을이 눌리지 않게
        var fitter = map.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = map.sprite != null ? map.sprite.rect.width / map.sprite.rect.height : 5f / 3f;
        _mapRect = map.rectTransform;

        var cleared = Cleared();
        var open = OpenBosses(cleared);
        bool justCleared = JourneyLink.JustCleared;
        JourneyLink.JustCleared = false;

        // 안개 → 사신 → 이름표 순서로 쌓는다
        var fogLayer = NewRect("Fog", _mapRect);
        foreach (var place in Places.Where(p => !p.final))
        {
            bool isCleared = cleared.Contains(place.bossId);
            bool isOpen = open.Contains(place.bossId);
            if (isCleared) continue;
            if (!isOpen) BuildFog(fogLayer, place, 1f);
            else if (justCleared) _revealFogs.Add((BuildFog(fogLayer, place, 1f), (place.fog - new Vector2(0.5f, 0.5f)).normalized));
        }

        BuildReaper(cleared);
        foreach (var place in Places)
        {
            if (place.afterlifeOnly && !_afterlife) continue;
            bool isCleared = cleared.Contains(place.bossId);
            bool visible = place.final || isCleared || open.Contains(place.bossId);
            if (visible) BuildLabel(place, isCleared, cleared.Count);
        }

        BuildBackButton();

        _white = NewImage("White", transform, new Color(1f, 1f, 1f, 0f));
        Stretch(_white.rectTransform);
        _white.raycastTarget = false;
    }

    private Image BuildFog(RectTransform layer, Place place, float alpha)
    {
        // 화면 전체 안개 그림에서 잘라 만든 조각 2개를 번갈아 쓴다
        int index = Array.IndexOf(Places, place);
        var sprite = LoadSprite(index % 2 == 0 ? "여정창_안개_조각1" : "여정창_안개_조각2");
        var fog = NewImage(place.label + " Fog", layer, new Color(0.86f, 0.86f, 0.88f, alpha * FogAlpha));
        fog.sprite = sprite;
        fog.raycastTarget = false;
        var rect = fog.rectTransform;
        rect.anchorMin = rect.anchorMax = place.fog;
        float w = 640f * 1.0667f * place.fogScale; // 지도 그림(1800) → 화면(1920) 비율
        rect.sizeDelta = new Vector2(w, w * 420f / 640f);
        if (index % 3 == 1) rect.localScale = new Vector3(-1f, 1f, 1f);
        fog.gameObject.AddComponent<FogDrift>().Init(index);
        return fog;
    }

    private void BuildReaper(List<string> cleared)
    {
        var sprite = LoadSprite("여정_꼬마사신");
        if (sprite == null) return;
        var image = NewImage("Reaper", _mapRect, Color.white);
        image.sprite = sprite;
        _reaper = image.rectTransform;
        // 그림 안에서 캐릭터가 차지하는 부분 (원본 3600x2160 기준 x 1266~2030, y 489~1666)
        const float contentHeight = 1177f / 2160f;
        float h = ReaperHeight / contentHeight;
        _reaper.sizeDelta = new Vector2(h * sprite.rect.width / sprite.rect.height, h);
        _reaper.pivot = new Vector2(1648f / 3600f, (2160f - 1666f) / 2160f);
        // 마지막으로 이긴 곳 앞에 서 있는다 (처음이면 아치 입구)
        var last = cleared.Count > 0 ? Places.FirstOrDefault(p => p.bossId == cleared[^1]) : null;
        _reaper.anchorMin = _reaper.anchorMax = last != null ? last.stand : ReaperStart;
        _reaper.anchoredPosition = Vector2.zero;

        // 마우스 판정은 캐릭터 몸 부분만 (그림 칸 전체가 넓어서)
        image.raycastTarget = false;
        var hit = NewImage("Reaper Hit", _reaper, new Color(0f, 0f, 0f, 0f));
        var hr = hit.rectTransform;
        hr.anchorMin = hr.anchorMax = _reaper.pivot;
        hr.pivot = new Vector2(0.5f, 0f);
        hr.sizeDelta = new Vector2(ReaperHeight * 0.65f, ReaperHeight);
        var hover = hit.gameObject.AddComponent<HoverRelay>();
        hover.onEnter = ShowFormationButton;
        hover.onExit = HideFormationButtonSoon;

        BuildFormationButton(hr);
    }

    private void BuildFormationButton(RectTransform hit)
    {
        var go = new GameObject("Formation Button", typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)go.transform;
        rect.SetParent(hit, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 8f);
        bool limited = RunSave.HasRun;
        rect.sizeDelta = new Vector2(limited ? 210f : 150f, limited ? 76f : 56f);
        var image = go.GetComponent<Image>();
        image.color = new Color(0.07f, 0.07f, 0.07f, 0.92f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.85f, 0.82f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        go.GetComponent<Button>().onClick.AddListener(OpenFormation);
        var label = NewText("Label", rect, limited ? $"편성\n<size=65%>정비 {RunSave.RefitTokens}회</size>" : "편성", 32f);
        Stretch(label.rectTransform);
        var hover = go.AddComponent<HoverRelay>();
        hover.onEnter = ShowFormationButton;
        hover.onExit = HideFormationButtonSoon;
        _formationButton = go;
        go.SetActive(false);
    }

    private void ShowFormationButton()
    {
        if (_hideFormation != null) StopCoroutine(_hideFormation);
        _hideFormation = null;
        if (_formationButton != null && _walk == null && !_busy) _formationButton.SetActive(true);
    }

    private void HideFormationButtonSoon()
    {
        if (_hideFormation != null) StopCoroutine(_hideFormation);
        _hideFormation = StartCoroutine(HideAfter());
    }

    private IEnumerator HideAfter()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        if (_formationButton != null) _formationButton.SetActive(false);
        _hideFormation = null;
    }

    private void BuildLabel(Place place, bool cleared, int clearedCount)
    {
        var sprite = LoadSprite(place.label);
        if (sprite == null) return;
        var size = new Vector2(sprite.rect.width, sprite.rect.height) * LabelScale;

        var root = new GameObject(place.label, typeof(RectTransform), typeof(Button));
        var rect = (RectTransform)root.transform;
        rect.SetParent(_mapRect, false);
        rect.anchorMin = rect.anchorMax = place.tag;
        rect.sizeDelta = size;

        // 밝은 바닥에서도 보이게 뒤에 어두운 그림자
        var shadow = NewImage("Shadow", rect, new Color(0f, 0f, 0f, 0.6f));
        shadow.sprite = sprite;
        shadow.raycastTarget = false;
        Stretch(shadow.rectTransform);
        shadow.rectTransform.offsetMin = new Vector2(-14f, -10f);
        shadow.rectTransform.offsetMax = new Vector2(14f, 6f);

        var tag = NewImage("Tag", rect, cleared ? ClearedLabel : Color.white);
        tag.sprite = sprite;
        Stretch(tag.rectTransform);
        var button = root.GetComponent<Button>();
        button.targetGraphic = tag;

        if (cleared)
        {
            // 처치: 이름표 바로 왼쪽에 X, 누를 수 없음
            button.interactable = false;
            var colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
            var mark = NewRect("Cleared", rect);
            mark.anchorMin = mark.anchorMax = new Vector2(0f, 0.5f);
            mark.pivot = new Vector2(1f, 0.5f);
            // 이름표 그림 왼쪽에 투명 여백(356px 중 46px)이 있어서, 보이는 이름표 바로 왼쪽에 붙인다.
            mark.anchoredPosition = new Vector2(size.x * (46f / 356f) - 4f, 0f);
            mark.sizeDelta = new Vector2(44f, 44f);
            foreach (float angle in new[] { 45f, -45f })
            {
                var stroke = NewImage("Stroke", mark, ClearedMark);
                stroke.raycastTarget = false;
                var sr = stroke.rectTransform;
                sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
                sr.sizeDelta = new Vector2(52f, 9f);
                sr.localRotation = Quaternion.Euler(0f, 0f, angle);
                var edge = stroke.gameObject.AddComponent<Outline>();
                edge.effectColor = new Color(0f, 0f, 0f, 0.7f);
                edge.effectDistance = new Vector2(2f, -2f);
            }
            return;
        }

        button.onClick.AddListener(() => OnPlaceClicked(place, clearedCount));
        UIHoverScale.Attach(root, 1.06f);
    }

    // ───────────────────────────── 입장 ─────────────────────────────

    private void OnPlaceClicked(Place place, int clearedCount)
    {
        if (_busy) return;
        if (place.final && clearedCount < ClearsForFinal)
        {
            string question = place.afterlifeOnly ? "저승의 문이 아직 열리지 않았다." : "아직 문이 굳게 닫혀 있다.";
            ModalDialog.Show(place.Title, question, $"처치 {clearedCount} / {ClearsForFinal}", null,
                new ModalDialog.Option("확인", null));
            return;
        }
        ModalDialog.Show(place.Title, $"{WithParticle(place.bossName)} 기다리고 있다.", "", null,
            new ModalDialog.Option("예", () => Enter(place)),
            new ModalDialog.Option("아니요", null));
    }

    private void Enter(Place place)
    {
        if (_busy) return;
        _busy = true;
        if (_formationButton != null) _formationButton.SetActive(false);
        StartCoroutine(EnterRoutine(place));
    }

    private IEnumerator EnterRoutine(Place place)
    {
        if (_walk != null) StopCoroutine(_walk);
        yield return Walk(place.stand);
        yield return new WaitForSecondsRealtime(0.15f);
        JourneyLink.PendingBossId = place.bossId;
        SceneFade.LoadScene("BattleTemp", Color.white, 1f, 0.8f);
    }

    private void OpenFormation()
    {
        if (_busy) return;
        _busy = true;
        JourneyLink.FormationRequested = true;
        SceneFade.LoadScene("BattleTemp", Color.white, 0.5f, 0.5f);
    }

    // 천천히 미끄러지며 걸음처럼 살짝 통통 튄다. 가는 쪽을 바라본다.
    private IEnumerator Walk(Vector2 stand)
    {
        if (_reaper == null) yield break;
        Vector2 size = _mapRect.rect.size;
        Vector2 from = Vector2.Scale(_reaper.anchorMin, size) + _reaper.anchoredPosition;
        Vector2 to = Vector2.Scale(stand, size);
        float distance = Vector2.Distance(from, to);
        float duration = Mathf.Clamp(distance / ReaperSpeed, 0.3f, 2.2f);
        var scale = _reaper.localScale;
        if (Mathf.Abs(to.x - from.x) > 1f) scale.x = Mathf.Abs(scale.x) * (to.x < from.x ? -1f : 1f);
        _reaper.localScale = scale;
        // 사신이 뒤집혀도 편성 버튼 글자는 뒤집히지 않게
        if (_formationButton != null) _formationButton.transform.localScale = new Vector3(Mathf.Sign(scale.x), 1f, 1f);
        _reaper.anchorMin = _reaper.anchorMax = Vector2.zero;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            float ease = k * k * (3f - 2f * k);
            float hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3.2f)) * 9f * Mathf.Min(1f, (1f - k) * 6f);
            _reaper.anchoredPosition = Vector2.Lerp(from, to, ease) + Vector2.up * hop;
            yield return null;
        }
        _reaper.anchorMin = _reaper.anchorMax = stand;
        _reaper.anchoredPosition = Vector2.zero;
    }

    // ───────────────────────────── 열기/닫기 ─────────────────────────────

    private IEnumerator OpenRoutine(bool instant)
    {
        _busy = true;
        if (!instant)
        {
            // 마을이 하얗게 밝아진 뒤 지도가 드러난다
            var white = _white;
            foreach (Transform child in transform) if (child != white.transform) child.gameObject.SetActive(false);
            for (float t = 0f; t < FadeToWhite; t += Time.unscaledDeltaTime)
            {
                SetAlpha(white, Mathf.SmoothStep(0f, 1f, t / FadeToWhite));
                yield return null;
            }
            SetAlpha(white, 1f);
            foreach (Transform child in transform) child.gameObject.SetActive(true);
            for (float t = 0f; t < FadeFromWhite; t += Time.unscaledDeltaTime)
            {
                SetAlpha(white, 1f - Mathf.SmoothStep(0f, 1f, t / FadeFromWhite));
                yield return null;
            }
            SetAlpha(white, 0f);
        }
        _busy = false;

        // 이기고 돌아왔으면: 잠시 뒤 새로 열린 곳의 안개가 흩어지며 걷힌다
        if (_revealFogs.Count > 0)
        {
            yield return new WaitForSecondsRealtime(instant ? 1.0f : 0.4f);
            const float reveal = 1.4f;
            foreach (var (fog, _) in _revealFogs) if (fog != null) Destroy(fog.GetComponent<FogDrift>());
            var starts = _revealFogs.Select(f => f.fog.rectTransform.anchoredPosition).ToList();
            for (float t = 0f; t < reveal; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / reveal);
                for (int i = 0; i < _revealFogs.Count; i++)
                {
                    var (fog, drift) = _revealFogs[i];
                    if (fog == null) continue;
                    SetAlpha(fog, FogAlpha * (1f - k));
                    fog.rectTransform.anchoredPosition = starts[i] + drift * 120f * k;
                }
                yield return null;
            }
            foreach (var (fog, _) in _revealFogs) if (fog != null) Destroy(fog.gameObject);
            _revealFogs.Clear();
        }
    }

    private IEnumerator CloseRoutine()
    {
        _busy = true;
        for (float t = 0f; t < CloseTime; t += Time.unscaledDeltaTime)
        {
            _group.alpha = 1f - t / CloseTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    // 전투 장면 '돌아가기'(화살표 + 글자)와 같은 모양
    private void BuildBackButton()
    {
        var go = new GameObject("Back Button", typeof(RectTransform), typeof(Button));
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-40f, 30f);
        rect.sizeDelta = new Vector2(190f, 56f);

        var hit = NewImage("Hit", rect, new Color(0f, 0f, 0f, 0f));
        Stretch(hit.rectTransform);
        var button = go.GetComponent<Button>();
        button.targetGraphic = hit;
        button.onClick.AddListener(Close);

        var arrow = NewImage("Arrow", rect, Color.white);
        var arrows = Resources.LoadAll<Sprite>("Sprites/UI/Victory/55f41fd6ee183814");
        if (arrows.Length > 0) arrow.sprite = arrows[0];
        arrow.raycastTarget = false;
        var ar = arrow.rectTransform;
        ar.anchorMin = ar.anchorMax = new Vector2(0f, 0.5f);
        ar.pivot = new Vector2(0f, 0.5f);
        ar.sizeDelta = new Vector2(47f, 56f);
        ar.anchoredPosition = Vector2.zero;

        var label = NewText("Label", rect, "돌아가기", 36f);
        var lr = label.rectTransform;
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(60f, 0f); lr.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        UIHoverScale.Attach(go);
    }

    // ───────────────────────────── 도우미 ─────────────────────────────

    // 받침 유무에 따라 이/가
    private static string WithParticle(string word)
    {
        if (string.IsNullOrEmpty(word)) return "보스가";
        char last = word[word.Length - 1];
        bool batchim = last >= '가' && last <= '힣' && (last - 0xAC00) % 28 != 0;
        return word + (batchim ? "이" : "가");
    }

    private static Sprite LoadSprite(string name)
    {
        var sprites = Resources.LoadAll<Sprite>(name);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    private static TMP_FontAsset FindVillageFont()
    {
        foreach (var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            if (text.font != null && text.gameObject.scene == SceneManager.GetActiveScene()) return text.font;
        return null;
    }

    private TextMeshProUGUI NewText(string name, Transform parent, string value, float size)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        if (_font != null) text.font = _font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        Stretch(rect);
        return rect;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void SetAlpha(Graphic g, float a)
    {
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    // 마우스가 들어오고 나갈 때 알려 준다 (꼬마 사신 ↔ 편성 버튼)
    private sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action onEnter, onExit;
        public void OnPointerEnter(PointerEventData eventData) => onEnter?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => onExit?.Invoke();
    }

    // 안개가 천천히 일렁인다
    private sealed class FogDrift : MonoBehaviour
    {
        private RectTransform _rect;
        private Vector2 _base;
        private float _phase;
        private bool _ready;

        public void Init(int seed)
        {
            _rect = (RectTransform)transform;
            _phase = seed * 1.7f;
        }

        private void Update()
        {
            if (_rect == null) return;
            if (!_ready) { _base = _rect.anchoredPosition; _ready = true; }
            float t = Time.unscaledTime * 0.25f + _phase;
            var offset = new Vector2(Mathf.Sin(t) * 14f, Mathf.Sin(t * 0.7f) * 6f);
            _rect.anchoredPosition = _base + offset;
        }
    }
}
