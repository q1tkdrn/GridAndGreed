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
    private const float DoorZoom = 1.17f;          // 여정_입장4의 문틀이 1~3보다 이만큼 크다 (3→4 줌으로 이어 붙인다)
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
    private RectTransform _door;       // 문 열림 연출 (여정_입장1~4)
    private Image _doorBack, _doorFront;
    private Sprite[] _doorFrames;
    private bool _skipDoor;
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
        BuildGear();
        BuildDoor();

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
        // 사신(이름표보다 먼저 만들어짐)의 자식이라 이름표 아래에 그려지고 클릭도 이름표에 막혔다 (2026-11-04).
        // 자기 캔버스로 정렬 순서를 지도 위로 올려 항상 맨 앞에 보이고 눌리게 한다. ESC 메뉴(550)·확인 팝업(600)보다는 아래.
        var front = go.AddComponent<Canvas>();
        front.overrideSorting = true;
        front.sortingOrder = SortingOrder + 10;
        go.AddComponent<GraphicRaycaster>();
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
            var clearSprite = ClearMarkSprite();
            if (clearSprite != null)
            {
                // 처치 표시(해골+뼈, JSW/Resources/여정_스테이지클리어_X표시)는 글자 바로 왼쪽에 붙인다.
                // 글자 시작점은 이름표마다 달라서(145~156px) 그림에서 직접 찾는다. 표시 그림 오른쪽 투명 여백 약 3px 포함해 보이는 틈 약 6px.
                mark.anchoredPosition = new Vector2(TextLeft(sprite) * LabelScale - 3f, 0f);
                const float markHeight = 54f;
                mark.sizeDelta = new Vector2(markHeight * clearSprite.rect.width / clearSprite.rect.height, markHeight);
                var icon = NewImage("Icon", mark, Color.white);
                icon.sprite = clearSprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                Stretch(icon.rectTransform);
                return;
            }
            // 그림이 없으면 예전처럼 코드로 그린 빨간 X
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

    private static Sprite _clearMark;
    private static readonly Dictionary<Sprite, float> _textLeft = new Dictionary<Sprite, float>();

    /// <summary>이름표 그림에서 글자(어두운 픽셀)가 시작하는 x (그림 픽셀). 못 찾으면 예전 값(투명 여백 끝).</summary>
    private static float TextLeft(Sprite label)
    {
        if (_textLeft.TryGetValue(label, out float cached)) return cached;
        float result = label.rect.width * (46f / 356f);
        var tex = label.texture;
        var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0);
        Graphics.Blit(tex, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var r = label.rect;
        var copy = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(r.x, r.y, r.width, r.height), 0, 0);
        copy.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        var px = copy.GetPixels32();
        int w = copy.width, h = copy.height;
        Destroy(copy);
        for (int x = 0; x < w; x++)
        {
            int dark = 0;
            for (int y = 0; y < h; y++)
            {
                var c = px[y * w + x];
                if (c.a > 128 && (c.r + c.g + c.b) / 3 < 90) dark++;
            }
            if (dark >= 2) { result = x; break; }
        }
        _textLeft[label] = result;
        return result;
    }

    /// <summary>처치 표시 그림. 시안 시트(1920×1080)에서 가장 큰 조각(해골+뼈)을 쓴다. 작은 조각은 거의 투명한 잡티.</summary>
    private static Sprite ClearMarkSprite()
    {
        if (_clearMark != null) return _clearMark;
        var parts = Resources.LoadAll<Sprite>("여정_스테이지클리어_X표시");
        foreach (var s in parts)
            if (_clearMark == null || s.rect.width * s.rect.height > _clearMark.rect.width * _clearMark.rect.height) _clearMark = s;
        return _clearMark;
    }

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
        if (!RunSave.HasRun)
        {
            // 여정 첫 출발: 정비 기회가 적으니 지금 편성을 보여 주고 확인받는다 (JSW/Script/FormationPreview.cs, 2026-11-04)
            // '나중에 출발': ESC를 눌러야만 닫히던 것이 불편해서 닫기 버튼을 둔다 (여정_첫_여정시작창.png, 2026-11-06)
            var dialog = ModalDialog.Show(place.Title, "이 편성으로 출발할까요?", "", null,
                new ModalDialog.Option("이대로 출발", () => Enter(place)),
                new ModalDialog.Option("편성 바꾸기", OpenFormation),
                new ModalDialog.Option("나중에 출발", null));
            FormationPreview.Attach(dialog);
            if (dialog != null) dialog.LayoutWideThenPair();
            return;
        }
        ModalDialog.Show(place.Title, $"{WithParticle(place.bossName)} 기다리고 있다.", "스테이지에 입장하시겠습니까?", null,
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
        if (instant && _door != null)
        {
            Destroy(_door.gameObject); // 전투에서 돌아올 때는 문 연출 없이
            _door = null;
        }
        if (!instant)
        {
            // 마을 → 문이 열리며 빛이 터진다 → 하얀 화면 → 지도가 드러난다
            var white = _white;
            foreach (Transform child in transform) child.gameObject.SetActive(child == white.transform);
            if (_doorFrames != null)
            {
                yield return PlayDoor();
            }
            else
            {
                // 문 그림이 없으면 예전처럼 하얗게만 밝아진다
                for (float t = 0f; t < FadeToWhite; t += Time.unscaledDeltaTime)
                {
                    SetAlpha(white, Mathf.SmoothStep(0f, 1f, t / FadeToWhite));
                    yield return null;
                }
            }
            SetAlpha(white, 1f);
            if (_door != null) Destroy(_door.gameObject);
            foreach (Transform child in transform) if (_door == null || child != _door) child.gameObject.SetActive(true);
            _door = null;
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

    // ───────────────────────────── 문 열림 연출 ─────────────────────────────
    // 긴 판(약 1.8초): 여정을 새로 나갈 때(진행 중인 여정 없음), 마을에 들어올 때마다 한 번.
    // 짧은 판(약 1.0초): 그 밖에. 3번(열린 문)에서 시작해 4번으로 바로 줌.
    // 연출 중 클릭하면 바로 하얀 화면으로 넘어간다.

    private static int _longDoorScene = -1;

    private void BuildDoor()
    {
        var frames = new Sprite[4];
        for (int i = 0; i < 4; i++) frames[i] = LoadSprite("여정_입장" + (i + 1));
        if (frames.Any(f => f == null))
        {
            Debug.LogWarning("[JourneyMap] 여정_입장1~4 스프라이트를 못 찾아 문 연출을 건너뜁니다 (Texture Type이 Sprite인지 확인).");
            return;
        }
        _doorFrames = frames;
        _door = NewRect("Door", transform);
        _doorBack = DoorImage("Frame Back");
        _doorFront = DoorImage("Frame Front");
        var hit = NewImage("Skip", _door, new Color(0f, 0f, 0f, 0f));
        Stretch(hit.rectTransform);
        hit.gameObject.AddComponent<PressRelay>().onPress = () => _skipDoor = true;
        _door.gameObject.SetActive(false);
    }

    private Image DoorImage(string name)
    {
        var image = NewImage(name, _door, new Color(1f, 1f, 1f, 0f));
        image.raycastTarget = false;
        var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = _doorFrames[0].rect.width / _doorFrames[0].rect.height;
        return image;
    }

    private IEnumerator PlayDoor()
    {
        int scene = SceneManager.GetActiveScene().handle;
        bool full = !RunSave.HasRun && _longDoorScene != scene;
        if (full) _longDoorScene = scene;

        _skipDoor = false;
        _door.gameObject.SetActive(true);
        var back = _doorBack;
        var front = _doorFront;
        var f = _doorFrames;

        if (full)
        {
            back.sprite = f[0];
            SetScale(back, 1f);
            yield return Tween(0.20f, k => SetAlpha(back, Smooth(k)));                          // 마을 → 닫힌 문
            yield return Tween(0.25f, k => SetScale(back, Mathf.Lerp(1f, 1.03f, Smooth(k))));   // 살짝 다가감
            back.sprite = f[1];
            yield return Tween(0.15f, null);
            back.sprite = f[2];
            yield return Tween(0.15f, null);
        }
        else
        {
            back.sprite = f[2];
            SetScale(back, 1.03f);
            yield return Tween(0.15f, k => SetAlpha(back, Smooth(k)));
        }

        // 3 → 4: 3번을 키우면서 4번을 겹쳐 띄운다 (문틀 크기 차이를 줌으로 숨김)
        front.sprite = f[3];
        SetScale(front, 1f);
        float from = back.rectTransform.localScale.x;
        yield return Tween(0.15f, k =>
        {
            SetScale(back, Mathf.Lerp(from, from * DoorZoom, Smooth(k)));
            SetAlpha(front, Smooth(k));
        });
        // 빛이 터지며 튕기듯 다가감
        yield return Tween(full ? 0.25f : 0.15f, k => SetScale(front, Mathf.Lerp(1f, 1.06f, 1f - (1f - k) * (1f - k))));

        // 빛이 화면을 덮는다 (건너뛰어도 여기는 짧게 보여 준다)
        float white0 = _white.color.a;
        float scale0 = front.rectTransform.localScale.x;
        float toWhite = _skipDoor ? 0.12f : (full ? 0.20f : 0.15f);
        yield return Tween(toWhite, k =>
        {
            SetAlpha(_white, Mathf.Lerp(white0, 1f, Smooth(k)));
            SetScale(front, Mathf.Lerp(scale0, 1.08f, k));
        }, skippable: false);
    }

    /// <summary>k(0→1)를 duration초 동안 넘겨 준다. skippable이면 클릭 시 바로 끝난다.</summary>
    private IEnumerator Tween(float duration, Action<float> step, bool skippable = true)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            if (skippable && _skipDoor) yield break;
            step?.Invoke(t / duration);
            yield return null;
        }
        if (!(skippable && _skipDoor)) step?.Invoke(1f);
    }

    private static float Smooth(float k) => Mathf.SmoothStep(0f, 1f, k);

    private static void SetScale(Graphic g, float s) => g.rectTransform.localScale = new Vector3(s, s, 1f);

    private sealed class PressRelay : MonoBehaviour, IPointerDownHandler
    {
        public Action onPress;
        public void OnPointerDown(PointerEventData eventData) => onPress?.Invoke();
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

    // 오른쪽 위 톱니: 마을과 같은 자리·모양. 누르면 마을 ESC 메뉴가 지도 위에 뜨고, 닫으면 지도로 돌아온다. (2026-11-05)
    // 지도 캔버스 안에 있어서 문·흰 화면 전환과 닫힘 페이드에 자연히 같이 가려진다.
    private void BuildGear()
    {
        GearHud.GearButton.Create(transform, () => { if (!_busy) EscMenu.OpenFromHud(); });
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
        if (GameFonts.Bold != null) return GameFonts.Bold;   // 2026-11-06: 글꼴은 GameFonts에서
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
