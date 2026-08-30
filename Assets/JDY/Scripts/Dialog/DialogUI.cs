using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
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
    //private bool isTyping;
    private string currentText;
    private Action completeCallback;
    void Start()
    {
        questionPanel.SetActive(false);
    }
    public void StartDialog(List<DialogData> dialogs, Action onComplete = null)
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

        SetDialog();
    }
    public void NextDialogButton()
    {
        /*
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);

            dialogText.text = currentText;

            isTyping = false;
            typingCoroutine = null;
            
            return;
        }
        */
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
        //isTyping = true;
        dialogText.text = "";

        foreach (char c in currentText)
        {
            dialogText.text += c;

            yield return new WaitForSeconds(typingSpeed);
        }

        //isTyping = false;
        typingCoroutine = null;
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
        nameText.text = "";
        dialogText.text = "";
        completeCallback?.Invoke();
        completeCallback = null;
        questionPanel.SetActive(true);
        //isTyping = false;
    }
}