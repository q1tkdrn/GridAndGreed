using UnityEngine;

[CreateAssetMenu(fileName = "MemorialData", menuName = "Scriptable Objects/MemorialData")]
public class MemorialData : ScriptableObject
{
    public string memorialName;
    public string id;
    public Story[] contents;
}
[System.Serializable]
public class Story
{
    public Sprite images;

    [TextArea]
    public string[] descriptions;
}