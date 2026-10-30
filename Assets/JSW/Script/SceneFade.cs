using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 화면을 한 가지 색으로 서서히 덮은 뒤 다음 씬을 열고, 새 씬에서 그 색을 서서히 걷어내는 장면 전환.
// 씬이 바뀌어도 남아 있어야 하므로 DontDestroyOnLoad 오브젝트 하나를 재사용한다.
public sealed class SceneFade : MonoBehaviour
{
    private const int SortingOrder = 30000; // 모달·ESC 메뉴보다 위

    private static SceneFade instance;
    private CanvasGroup group;
    private Image image;
    private bool busy;

    /// <summary>전환 중이면 true. 전환 중 버튼이 다시 눌리는 것을 막을 때 쓴다.</summary>
    public static bool IsFading => instance != null && instance.busy;

    /// <summary>
    /// color로 fadeOut초 동안 화면을 덮고 sceneName을 연 뒤, fadeIn초 동안 걷어낸다.
    /// </summary>
    public static void LoadScene(string sceneName, Color color, float fadeOut = 1f, float fadeIn = 0.8f)
    {
        if (IsFading) return;
        var fade = Ensure();
        fade.StartCoroutine(fade.Run(sceneName, color, fadeOut, fadeIn));
    }

    private static SceneFade Ensure()
    {
        if (instance != null) return instance;
        var go = new GameObject("SceneFade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        var cover = new GameObject("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)cover.transform;
        rect.SetParent(go.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        instance = go.AddComponent<SceneFade>();
        instance.group = go.GetComponent<CanvasGroup>();
        instance.image = cover.GetComponent<Image>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;
        instance.group.interactable = false;
        return instance;
    }

    private IEnumerator Run(string sceneName, Color color, float fadeOut, float fadeIn)
    {
        busy = true;
        image.color = color;
        group.blocksRaycasts = true; // 덮이는 동안 다른 버튼이 눌리지 않게

        yield return Fade(0f, 1f, fadeOut);

        var load = SceneManager.LoadSceneAsync(sceneName);
        while (load != null && !load.isDone) yield return null;
        yield return null; // 새 씬의 Start가 한 번 돈 뒤에 걷어낸다

        yield return Fade(1f, 0f, fadeIn);
        group.blocksRaycasts = false;
        busy = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        group.alpha = to;
    }
}
