using System;

[Serializable]
public class DialogData
{
    public string id;
    public int groupId;
    public string npcName;
    public DialogType type;
    public string target;
    public int fromPhase;
    public int toPhase;
    public string text;
}
public enum DialogType
{
    Welcome,
    Question,
    Character,
    Skin,
    Memory,
    Music,
    Achievements,
    Item,
    Exit
}
