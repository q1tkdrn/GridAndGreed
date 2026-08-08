using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string id;
    public Sprite icon;
    public int price;
    [TextArea]
    public string description;

}
