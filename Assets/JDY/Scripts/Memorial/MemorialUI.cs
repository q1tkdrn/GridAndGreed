using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemorialUI : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private MemorialDialog memorialDialog;

    [Header("UI")]
    [SerializeField] private GameObject memorialPanel;
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text text;

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

        memorialPanel.SetActive(true);
        SetUI();
    }
    public void NextUIButton()
    {
        Story story = currentMemorial.contents[currentImageIndex];

        if (currentTextIndex < story.descriptions.Length - 1)
        {
            currentTextIndex++;
        }
        else
        {
            currentImageIndex++;
            currentTextIndex = 0;
        }

        SetUI();
    }
    private void SetUI()
    {
        if (currentImageIndex < currentMemorial.contents.Length)
        {
            Story story = currentMemorial.contents[currentImageIndex];

            frame.sprite = story.images;
            text.text = story.descriptions[currentTextIndex];
        }
        else
        {
            memorialPanel.SetActive(false);

            onComplete?.Invoke();
            onComplete = null;
        }
    }
}