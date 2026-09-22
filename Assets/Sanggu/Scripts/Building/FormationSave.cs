using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Save IDs rather than scene references. Empty item slots are saved explicitly.
public static class FormationSave
{
    private const string Prefix = "Formation_v1_";

    public static void Save(UnitTemp[] units, ItemData[] items, IEnumerable<UnitTemp> roster)
    {
        for (int i = 0; i < units.Length; i++)
            PlayerPrefs.SetInt(Prefix + "Unit_" + i, units[i] != null ? units[i].id : 0);
        for (int i = 0; i < items.Length; i++)
            PlayerPrefs.SetString(Prefix + "Item_" + i, items[i] != null ? items[i].id : "");
        foreach (var unit in roster.Where(u => u != null).Distinct())
            PlayerPrefs.SetInt(Prefix + "Skin_" + unit.id, unit.IsSkinUnlocked(unit.currentSkin) ? unit.currentSkin : 0);
        PlayerPrefs.Save();
    }

    public static void Load(UnitTemp[] units, ItemData[] items, IEnumerable<UnitTemp> roster, IEnumerable<ItemData> catalog)
    {
        var available = roster.Where(u => u != null).Distinct().ToArray();
        var usedUnits = new HashSet<int>();
        // Resolve all saved slots first so a missing character cannot displace another valid slot.
        for (int i = 0; i < units.Length; i++)
        {
            int id = PlayerPrefs.GetInt(Prefix + "Unit_" + i, units[i] != null ? units[i].id : 0);
            units[i] = available.FirstOrDefault(u => u.id == id && OwnsUnit(u.id) && !usedUnits.Contains(u.id));
            if (units[i] != null) usedUnits.Add(units[i].id);
        }
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] != null) continue;
            units[i] = available.FirstOrDefault(u => OwnsUnit(u.id) && !usedUnits.Contains(u.id));
            if (units[i] != null) usedUnits.Add(units[i].id);
        }
        foreach (var unit in available)
        {
            int skin = PlayerPrefs.GetInt(Prefix + "Skin_" + unit.id, 0);
            unit.currentSkin = unit.IsSkinUnlocked(skin) ? skin : 0;
        }
        var usedItems = new HashSet<string>();
        for (int i = 0; i < items.Length; i++)
        {
            string id = PlayerPrefs.GetString(Prefix + "Item_" + i, items[i] != null ? items[i].id : "");
            items[i] = catalog.FirstOrDefault(item => item != null && item.id == id
                && PlayerPrefs.GetInt("Item_" + id, 0) > 0 && !usedItems.Contains(id));
            if (items[i] != null) usedItems.Add(items[i].id);
        }
    }

    private static bool OwnsUnit(int id) => (id >= 1 && id <= 3) || PlayerPrefs.GetInt("Character_" + id, 0) == 1;
}
