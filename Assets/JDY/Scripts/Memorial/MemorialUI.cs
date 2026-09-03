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

    [Header("Memorial Data")]
    [SerializeField] private MemorialData[] memorials;

    private int memorialIndex;
    private int currentImageIndex;
    private int currentTextIndex;
    private Action onComplete;
    private void Start()
    {
        memorialPanel.SetActive(false);
    }
    public void ShowMemorial(int index, Action onComplete)
    {
        this.onComplete = onComplete;

        memorialIndex = index;
        currentImageIndex = 0;
        currentTextIndex = 0;

        memorialPanel.SetActive(true);
        SetUI();
    }
    public void NextUIButton()
    {
        Story story = memorials[memorialIndex].contents[currentImageIndex];

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
        MemorialData memorial = memorials[memorialIndex];

        if (currentImageIndex < memorial.contents.Length)
        {
            Story story = memorial.contents[currentImageIndex];

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