using UnityEngine;
using UnityEngine.EventSystems;

// 각 조각의 마우스 진입/이탈을 묶음에 전달한다.
public sealed class NpcHoverPiece : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    internal NpcHoverEffect.Group group;
    private bool inside;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inside || group == null) return;
        inside = true;
        group.hoverCount++;
    }

    public void OnPointerExit(PointerEventData eventData) => Leave();
    private void OnDisable() => Leave();

    private void Leave()
    {
        if (!inside || group == null) return;
        inside = false;
        group.hoverCount = Mathf.Max(0, group.hoverCount - 1);
    }
}
