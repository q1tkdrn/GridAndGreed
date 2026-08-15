using System.Collections.Generic;
using UnityEngine;

public class ItemDialog : MonoBehaviour
{
    [SerializeField] private DialogUI dialogUI;
    void Start()
    {
        //Test
        List<DialogData> dialogs = DialogManager.Instance.GetDialogueGroup("¸±¸®", DialogType.Welcome,"",0);
        dialogUI.StartDialog(dialogs);
    }
    void Update()
    {
        
    }
}
