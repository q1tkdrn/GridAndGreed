using UnityEngine;

/// <summary>여정 출발 확인창용 캐릭터·아이템 그림 목록 (Resources/FormationPreview). Tools > Rebuild Formation Preview로 다시 만든다.</summary>
public class FormationPreviewCatalog : ScriptableObject
{
    public FormationPreview.UnitEntry[] units;
    public FormationPreview.ItemEntry[] items;
}
