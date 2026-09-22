using System;
using System.Collections.Generic;

// Minimal engine/inventory substitutes; compile with the real UnitTemp.cs in a separate process.
namespace UnityEngine
{
    public class ScriptableObject { }
    public class Sprite { }
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName;
        public string menuName;
    }
}

public enum Skin { NpcSkin, InsectSkin, BossSkin }
public class InventoryManager
{
    public static InventoryManager Instance;
    public readonly HashSet<string> Purchased = new HashSet<string>();
    public bool HasSkin(string id, Skin skin) => Purchased.Contains($"{id}:{skin}");
}

public static class UnitSkinTests
{
    public static void Main()
    {
        int assertions = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            assertions++;
        }
        var unit = new UnitTemp
        {
            id = 1, skin1 = new UnityEngine.Sprite(), skin2 = new UnityEngine.Sprite(),
            skin3 = new UnityEngine.Sprite(), isSkin1Unlocked = true,
            isSkin2Unlocked = true, isSkin3Unlocked = true
        };
        Check(unit.IsSkinUnlocked(0), "Default skin needs no inventory");
        Check(!unit.IsSkinUnlocked(1), "No inventory must not crash or grant paid skins");
        InventoryManager.Instance = new InventoryManager();
        var expected = new[] { Skin.InsectSkin, Skin.BossSkin, Skin.NpcSkin };
        for (int i = 0; i < expected.Length; i++)
        {
            Check(!unit.IsSkinUnlocked(i + 1), "Old debug unlock flags do not grant ownership");
            InventoryManager.Instance.Purchased.Add($"1:{expected[i]}");
            for (int slot = 1; slot <= 3; slot++)
                Check(unit.IsSkinUnlocked(slot) == (slot == i + 1), "Purchase unlocks only the corresponding skin");
            InventoryManager.Instance.Purchased.Clear();
        }
        InventoryManager.Instance.Purchased.Add("2:InsectSkin");
        Check(!unit.IsSkinUnlocked(1), "Purchasing another character's skin does not unlock this one");
        InventoryManager.Instance.Purchased.Add("1:InsectSkin");
        unit.skin1 = null;
        Check(!unit.IsSkinUnlocked(1), "Missing sprite cannot be equipped");
        Check(!unit.IsSkinUnlocked(-1) && !unit.IsSkinUnlocked(4), "Invalid skin slots are rejected");
        Console.WriteLine($"PASS: {assertions} skin ownership and mapping assertions");
    }
}
