using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Resources/FormationPreview.asset을 캐릭터(UnitTemp)·아이템(ItemData) 에셋에서 다시 만든다.</summary>
public static class FormationPreviewBuilder
{
    private const string Path = "Assets/JSW/Resources/FormationPreview.asset";

    [MenuItem("Tools/Rebuild Formation Preview")]
    public static void Rebuild()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<FormationPreviewCatalog>(Path);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<FormationPreviewCatalog>();
            AssetDatabase.CreateAsset(catalog, Path);
        }
        catalog.units = AssetDatabase.FindAssets("t:UnitTemp")
            .Select(g => AssetDatabase.LoadAssetAtPath<UnitTemp>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(u => u != null && u.id > 0)
            .OrderBy(u => u.id)
            .Select(u => new FormationPreview.UnitEntry { id = u.id, name = u.unitName, skins = new[] { u.GetSkin(0), u.GetSkin(1), u.GetSkin(2), u.GetSkin(3) } })
            .ToArray();
        catalog.items = AssetDatabase.FindAssets("t:ItemData")
            .Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(i => i != null && !string.IsNullOrEmpty(i.id))
            .Select(i => new FormationPreview.ItemEntry { id = i.id, name = i.itemName, icon = i.icon })
            .ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"Formation Preview: 캐릭터 {catalog.units.Length}, 아이템 {catalog.items.Length}");
    }
}
