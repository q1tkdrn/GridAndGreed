using System;
using TMPro;
using UnityEngine;

public class DebugInputPanel : MonoBehaviour
{
    public TextMeshProUGUI fieldName;
    public TMP_InputField inputField;
    public DebugEntry DebugEntry;

    public void Init()
    {
        var str = "";
        if (DebugEntry.IsField)
        {
            str = $"{DebugEntry.Field.Name} : {DebugEntry.GetValue()}";
        } else if (DebugEntry.IsMethod)
        {
            str = $"{DebugEntry.Method.Name}";
            if(DebugEntry.Method.GetParameters().Length == 0) inputField.gameObject.SetActive(false);
        }
        if(DebugEntry.FieldName != null) str = DebugEntry.FieldName;
        fieldName.text = str;
    }
    
    public void OnButtonClick()
    {
        if(DebugEntry.IsField) DebugEntry.SetFieldValue(inputField.text);
        if (DebugEntry.IsMethod)
        {
            if(DebugEntry.Method.GetParameters().Length == 0) DebugEntry.Execute();
            else DebugEntry.Execute(inputField.text);
        }
    }
}
