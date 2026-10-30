using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 심판 컷인: 게임을 잠시 멈추고(Time.timeScale = 0) 화면을 어둡게 한 뒤,
// 캐릭터를 왼쪽에서 크게 미끄러뜨려 들여와 긴 동작을 재생한다. 놓는 순간 하얗게 번쩍이며 화면이 흔들린다.
// 모든 시간은 멈춘 게임 시간이 아니라 실제 시간으로 센다. ESC 메뉴가 열려 있는 동안에는 컷인도 멈춘다.
public static class JudgeCutIn
{
    // 화면 배치 (1920x1080 기준)
    private const float CharacterSize = 1000f;            // 프레임 그림(700px)을 그릴 크기
    private static readonly Vector2 CharacterPos = new Vector2(-470f, -100f);
    private const float SlideFromX = -1500f;
    private const float SlideInTime = 0.18f;
    private const float FadeOutTime = 0.2f;
    private const float DimAlpha = 0.8f;   // 리니어 색 공간이라 수치보다 옅게 보여서 높게 잡음
    private const float FlashAlpha = 0.75f;
    private const float FlashTime = 0.2f;
    private const float ShakeAmplitude = 22f;
    private const float ShakeTime = 0.28f;

    private static GameObject _root;
    private static float _savedTimeScale = 1f;
    private static bool _pausedGame;

    public static bool IsPlaying => _root != null;

    /// <summary>재생 중인 컷인을 지우고 게임 시간을 되돌린다. (전투 화면이 꺼질 때)</summary>
    public static void Abort()
    {
        if (_root != null) UnityEngine.Object.Destroy(_root);
        _root = null;
        ResumeGame();
    }

    public static IEnumerator Play(Sprite[] frames, float[] times, int releaseFrame, Action onRelease)
    {
        Abort();
        // 게임 정지
        _savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        _pausedGame = true;

        _root = new GameObject("Judge CutIn", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var root = _root;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0f;

        var dim = CreateImage("Dim", root.transform, Color.black, stretch: true);
        dim.raycastTarget = true; // 연출 중 클릭을 막는다
        var stage = new GameObject("Stage", typeof(RectTransform)).GetComponent<RectTransform>();
        stage.SetParent(root.transform, false);
        stage.anchorMin = Vector2.zero; stage.anchorMax = Vector2.one;
        stage.offsetMin = stage.offsetMax = Vector2.zero;
        var character = CreateImage("Character", stage, Color.white, stretch: false);
        character.sprite = frames[0];
        // 그림 크기가 캐릭터마다 달라도(궁수 700px, 기사 900px) 캐릭터 자체는 같은 크기·같은 발 위치로 보이게 그림 폭에 비례해 키운다.
        float size = CharacterSize * frames[0].rect.width / 700f;
        character.rectTransform.sizeDelta = new Vector2(size, size);
        var flash = CreateImage("Flash", root.transform, new Color(1f, 1f, 1f, 0f), stretch: true);

        var charRect = character.rectTransform;
        SetAlpha(dim, 0f);

        // 1) 어두워지며 미끄러져 들어온다
        for (float t = 0f; t < SlideInTime; t += Step())
        {
            if (root == null) yield break;
            float k = Mathf.Clamp01(t / SlideInTime);
            float ease = 1f - Mathf.Pow(1f - k, 3f);
            SetAlpha(dim, DimAlpha * k);
            charRect.anchoredPosition = new Vector2(Mathf.Lerp(SlideFromX, CharacterPos.x, ease), CharacterPos.y);
            yield return null;
        }
        if (root == null) yield break;
        SetAlpha(dim, DimAlpha);
        charRect.anchoredPosition = CharacterPos;

        // 2) 동작 재생
        float flashLeft = 0f, shakeLeft = 0f;
        for (int f = 0; f < frames.Length; f++)
        {
            if (root == null) yield break;
            character.sprite = frames[f];
            if (f == releaseFrame)
            {
                flashLeft = FlashTime;
                shakeLeft = ShakeTime;
                onRelease?.Invoke();
            }
            for (float t = 0f; t < times[f]; )
            {
                if (root == null) yield break;
                float dt = Step();
                t += dt;
                flashLeft = Mathf.Max(0f, flashLeft - dt);
                shakeLeft = Mathf.Max(0f, shakeLeft - dt);
                SetAlpha(flash, FlashAlpha * (flashLeft / FlashTime));
                float s = ShakeAmplitude * (shakeLeft / ShakeTime);
                stage.anchoredPosition = s > 0f ? new Vector2(UnityEngine.Random.Range(-s, s), UnityEngine.Random.Range(-s, s) * 0.5f) : Vector2.zero;
                yield return null;
            }
        }
        if (root == null) yield break;
        SetAlpha(flash, 0f);
        stage.anchoredPosition = Vector2.zero;

        // 3) 사라지며 게임으로 돌아간다
        for (float t = 0f; t < FadeOutTime; t += Step())
        {
            if (root == null) yield break;
            float k = Mathf.Clamp01(t / FadeOutTime);
            SetAlpha(dim, DimAlpha * (1f - k));
            SetAlpha(character, 1f - k);
            charRect.anchoredPosition = CharacterPos + new Vector2(-60f * k, 0f);
            yield return null;
        }
        Abort();
    }

    // 실제 시간으로 한 프레임. ESC 메뉴가 열려 있으면 0 (컷인도 멈춤)
    private static float Step()
    {
        if (EscMenu.IsOpen) return 0f;
        return Mathf.Min(Time.unscaledDeltaTime, 0.05f);
    }

    private static void ResumeGame()
    {
        if (!_pausedGame) return;
        _pausedGame = false;
        // ESC 메뉴가 열려 있으면 메뉴가 닫힐 때 되돌릴 값만 맞춰 둔다
        if (!EscMenu.IsOpen && Time.timeScale == 0f) Time.timeScale = _savedTimeScale;
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool stretch)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        if (stretch)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void SetAlpha(Graphic g, float a)
    {
        var c = g.color;
        c.a = a;
        g.color = c;
    }
}
