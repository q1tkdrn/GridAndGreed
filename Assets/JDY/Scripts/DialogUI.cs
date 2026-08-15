using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class DialogUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogText;

    private List<DialogData> currentDialogs;
    private int currentIndex;

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
        dialogText.text = current.text;
    }
    private void EndDialog()
    {
        dialogPanel.SetActive(false);

        currentDialogs = null;
        currentIndex = 0;
    }
}