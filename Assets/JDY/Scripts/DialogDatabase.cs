using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "DialogDatabase", menuName = "Scriptable Objects/DialogDatabase")]
public class DialogDatabase : ScriptableObject
{
    public List<DialogData> dialogues = new List<DialogData>();
}
