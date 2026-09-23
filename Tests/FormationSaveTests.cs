using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine
{
    public class ScriptableObject { }
    public class Sprite { }
    public class TextAreaAttribute : Attribute { }
    public class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }
    public static class PlayerPrefs
    {
        public static readonly Dictionary<string, object> Data = new Dictionary<string, object>();
        public static int GetInt(string key, int fallback = 0) => Data.TryGetValue(key, out var value) ? (int)value : fallback;
        public static string GetString(string key, string fallback = "") => Data.TryGetValue(key, out var value) ? (string)value : fallback;
        public static void SetInt(string key, int value) => Data[key] = value;
        public static void SetString(string key, string value) => Data[key] = value;
        public static void Save() { }
    }
}
public enum Skin { InsectSkin, BossSkin, NpcSkin }
public class InventoryManager
{
    public static InventoryManager Instance = new InventoryManager();
    public bool HasSkin(string id, Skin skin) => PlayerPrefs.GetInt("Skin_" + id + "_" + skin) == 1;
}
public static class FormationSaveTests
{
    public static void Main()
    {
        int assertions = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); assertions++; }
        UnitTemp[] Roster() => Enumerable.Range(1, 9).Select(id => new UnitTemp { id = id, skin1 = new Sprite() }).ToArray();
        ItemData[] Catalog() => Enumerable.Range(1, 23).Select(id => new ItemData { id = id.ToString() }).ToArray();
        var roster = Roster(); var catalog = Catalog();
        var units = roster.Take(3).ToArray(); var items = new ItemData[3];
        FormationSave.Load(units, items, roster, catalog);
        Check(units.Select(u => u.id).SequenceEqual(new[] { 1, 2, 3 }), "First use retains starting units");
        Check(items.All(i => i == null), "First use has empty items");
        PlayerPrefs.SetInt("Character_7", 1);
        PlayerPrefs.SetInt("Item_4", 1); PlayerPrefs.SetInt("Item_22", 1);
        PlayerPrefs.SetInt("Skin_7_InsectSkin", 1);
        units = new[] { roster[6], roster[2], roster[0] };
        roster[6].currentSkin = 1;
        items = new[] { catalog[3], null, catalog[21] };
        FormationSave.Save(units, items, roster);
        // Fresh asset instances simulate leaving the scene or restarting the application.
        roster = Roster(); catalog = Catalog(); units = roster.Take(3).ToArray(); items = new ItemData[3];
        FormationSave.Load(units, items, roster, catalog);
        Check(units.Select(u => u.id).SequenceEqual(new[] { 7, 3, 1 }), "Unit order survives reload");
        Check(items[0] == catalog[3] && items[1] == null && items[2] == catalog[21], "Item order and empty middle slot survive reload");
        Check(units[0].currentSkin == 1, "Skin survives reload");
        items[0] = null; roster[6].currentSkin = 0;
        FormationSave.Save(units, items, roster);
        items[0] = catalog[3]; roster[6].currentSkin = 1;
        FormationSave.Load(units, items, roster, catalog);
        Check(items[0] == null && roster[6].currentSkin == 0, "Unequip and default skin survive reload");
        PlayerPrefs.SetInt("Formation_v1_Unit_0", 999);
        FormationSave.Load(units, items, roster, catalog);
        Check(units.All(u => u != null) && units.Select(u => u.id).Distinct().Count() == 3, "Missing unit gets valid unique replacement");
        Check(units[1].id == 3 && units[2].id == 1, "Replacement preserves remaining saved slots");
        PlayerPrefs.SetInt("Formation_v1_Unit_0", 7); PlayerPrefs.SetInt("Character_7", 0);
        PlayerPrefs.SetString("Formation_v1_Item_0", "999"); PlayerPrefs.SetInt("Item_22", 0);
        PlayerPrefs.SetInt("Formation_v1_Skin_7", 1); PlayerPrefs.SetInt("Skin_7_InsectSkin", 0);
        FormationSave.Load(units, items, roster, catalog);
        Check(units.All(u => u.id != 7), "Locked unit is rejected");
        Check(items.All(i => i == null), "Missing and no longer owned items are removed");
        Check(roster[6].currentSkin == 0, "Locked skin reverts to default");
        PlayerPrefs.SetInt("Formation_v1_Unit_0", 3);
        PlayerPrefs.SetString("Formation_v1_Item_0", "4"); PlayerPrefs.SetString("Formation_v1_Item_1", "4");
        FormationSave.Load(units, items, roster, catalog);
        Check(units.Select(u => u.id).Distinct().Count() == 3, "Duplicate unit saves are repaired");
        Check(items.Count(i => i != null) == 1, "Duplicate item saves are repaired");
        Check(PlayerPrefs.GetInt("Item_4") == 1, "Loading does not consume inventory");
        Console.WriteLine($"PASS: {assertions} formation persistence assertions");
    }
}
