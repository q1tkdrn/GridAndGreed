using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class DialogUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogText;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject questionPanel;

    [Header("Setting")]
    [SerializeField] private float typingSpeed = 0.05f;

    private List<DialogData> currentDialogs;
    private int currentIndex;

    private Coroutine typingCoroutine;
    private string currentText;
    private Action completeCallback;

    private bool showQuestionOnComplete;

    private void Start()
    {
        questionPanel.SetActive(false);
    }
    public void StartDialog(List<DialogData> dialogs, Action onComplete = null, bool showQuestion = true)
    {
        questionPanel.SetActive(false);
        if (dialogs == null || dialogs.Count == 0)
        {
            Debug.LogWarning("¾øÀ½");
            return;
        }
        currentDialogs = dialogs;
        currentIndex = 0;

        completeCallback = onComplete;
        showQuestionOnComplete = showQuestion;

        SetDialog();
    }
    public void NextDialogButton()
    {
        currentIndex++;

        if (currentIndex >= currentDialogs.Count)
        {
            EndDialog();
            return;
        }

        SetDialog();
    }

    private void SetDialog()
    {
        DialogData current = currentDialogs[currentIndex];

        nameText.text = current.npcName;

        nextButton.SetActive(false);

        StartTyping(current.text);
    }

    private void StartTyping(string text)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        currentText = text;
        typingCoroutine = StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        dialogText.text = "";

        foreach (char c in currentText)
        {
            dialogText.text += c;

            yield return new WaitForSeconds(typingSpeed);
        }

        typingCoroutine = null;

        if (!showQuestionOnComplete)
        {
            EndDialog();
            yield break;
        }
        nextButton.SetActive(true);
    }
    private void EndDialog()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        nextButton.SetActive(false);

        currentDialogs = null;
        currentIndex = 0;
        completeCallback?.Invoke();
        completeCallback = null;

        if (showQuestionOnComplete)
        {
            questionPanel.SetActive(true);
            nameText.text = "";
            dialogText.text = "";
        }
    }
}