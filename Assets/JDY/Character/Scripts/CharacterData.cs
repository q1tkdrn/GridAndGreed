using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public string id;
    public Sprite icon;
    public int STR;
    public int INT;
    public int price;
    [TextArea]
    public string description;
}
