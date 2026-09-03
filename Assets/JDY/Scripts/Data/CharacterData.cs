using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public string id;
    public Sprite icon;
    public Sprite npcSkin;
    public Sprite insectSkin;
    public Sprite bossSkin;
    public int STR;
    public int INT;
    public int price;
    public string npcSkinName;
    public string insectSkinName;
    public string bossSkinName;
    [TextArea]
    public string description;
}
