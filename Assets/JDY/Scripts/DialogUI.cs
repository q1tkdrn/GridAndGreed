using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogText;
    [SerializeField] private GameObject nextButton;
    [Header("Setting")]
    [SerializeField] private float typingSpeed = 0.05f;

    private List<DialogData> currentDialogs;
    private int currentIndex;

    private Coroutine typingCoroutine;
    //private bool isTyping;
    private string currentText;
    public void StartDialog(List<DialogData> dialogs)
    {
        if (dialogs == null || dialogs.Count == 0)
        {
            Debug.LogWarning("¾øÀ½");
            return;
        }

        currentDialogs = dialogs;
        currentIndex = 0;

        dialogPanel.SetActive(true);
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

        dialogPanel.SetActive(false);

        currentDialogs = null;
        currentIndex = 0;
        //isTyping = false;
    }
}