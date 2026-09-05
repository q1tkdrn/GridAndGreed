using UnityEngine;

[CreateAssetMenu(fileName = "Unit", menuName = "Units/Unit")]
public class UnitTemp : ScriptableObject
{
    public string unitName;
    public int id;
    public int power;
    public int intelligence;
    public int reviveCool;
    public string abilityText;
    public Sprite illustration;
    public Sprite defaultSkin;
    public Sprite skin1;
    public Sprite skin2;
    public Sprite skin3;
    public bool isSkin1Unlocked = false;
    public bool isSkin2Unlocked = false;
    public bool isSkin3Unlocked = false;
    public int currentSkin = 0;
}
