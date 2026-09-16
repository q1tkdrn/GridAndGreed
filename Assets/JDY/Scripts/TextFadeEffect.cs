using TMPro;
using UnityEngine;
using System;
using System.Collections;
public class TextFadeEffect : MonoBehaviour
{
    [SerializeField] private float fadeInTime = 2f;
    [SerializeField] private float fadeOutTime = 0.5f;

    private Coroutine fadeCoroutine;

    public void FadeInText(TMP_Text text, Action onComplete = null)
    {
        StopCurrentFade();
        fadeCoroutine = StartCoroutine(FadeIn(text, onComplete));
    }
    public void FadeOutText(TMP_Text text, Action onComplete = null)
    {
        StopCurrentFade();
        fadeCoroutine = StartCoroutine(FadeOut(text, onComplete));
    }
    private void StopCurrentFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }
    private IEnumerator FadeIn(TMP_Text text, Action onComplete)
    {
        float currentTime = 0f;

        Color color = text.color;
        color.a = 0f;
        text.color = color;

        while (currentTime < fadeInTime)
        {
            currentTime += Time.deltaTime;

            float progress = currentTime / fadeInTime;
            float fastProgress = progress * progress;

            color.a = Mathf.Lerp(0f, 1f, fastProgress);
            text.color = color;

            yield return null;
        }

        color.a = 1f;
        text.color = color;

        fadeCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator FadeOut(TMP_Text text, Action onComplete)
    {
        float currentTime = 0f;

        Color color = text.color;

        while (currentTime < fadeOutTime)
        {
            currentTime += Time.deltaTime;

            color.a = Mathf.Lerp(1f, 0f, currentTime / fadeOutTime);
            text.color = color;

            yield return null;
        }

        color.a = 0f;
        text.color = color;

        fadeCoroutine = null;
        onComplete?.Invoke();
    }
}