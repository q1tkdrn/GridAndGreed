using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemorialUI : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private MemorialDialog memorialDialog;
    [SerializeField] private TextFadeEffect textFadeEffect;
    [Header("UI")]
    [SerializeField] private GameObject memorialPanel;
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text text;
    [SerializeField] private GameObject nextButton;
    private MemorialData currentMemorial;
    private int currentImageIndex;
    private int currentTextIndex;
    private Action onComplete;
    private void Start()
    {
        memorialPanel.SetActive(false);
    }
    public void ShowMemorial(MemorialData memorial, Action onComplete)
    {
        this.onComplete = onComplete;
        currentMemorial = memorial;

        currentImageIndex = 0;
        currentTextIndex = 0;

        if (memorial.id == "7")
        {
            AchievementManager.Instance.AddProgress("ACH-30", 1);
        }
        memorialPanel.SetActive(true);
        nextButton.SetActive(false);
        
        SetUI();
    }
    public void NextUIButton()
    {
        Story story = currentMemorial.contents[currentImageIndex];

        if (currentTextIndex < story.descriptions.Length - 1)
        {
            currentTextIndex++;

            textFadeEffect.FadeOutText(text, SetUI);
            nextButton.SetActive(false);
        }
        else
        {
            currentImageIndex++;
            currentTextIndex = 0;

            textFadeEffect.FadeOutText(text, SetUI);
            nextButton.SetActive(false);
        }
    }
    private void SetUI()
    {
        if (currentImageIndex < currentMemorial.contents.Length)
        {
            Story story = currentMemorial.contents[currentImageIndex];
            frame.sprite = story.images;
            text.text = story.descriptions[currentTextIndex];
            textFadeEffect.FadeInText(text, () => nextButton.SetActive(true));
        }
        else
        {
            memorialPanel.SetActive(false);

            onComplete?.Invoke();
            onComplete = null;
        }
    }
    public void SkipButton()
    {
        memorialPanel.SetActive(false);

        onComplete?.Invoke();
        onComplete = null;
    }
}