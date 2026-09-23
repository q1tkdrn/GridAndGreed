using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Adds pointer feedback without replacing Button or EventTrigger callbacks.
[DisallowMultipleComponent]
public sealed class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Range(1f, 1.1f)] private float hoverScale = 1.04f;
    private Vector3 restingScale;
    private Selectable selectable;
    private bool initialized;
    private bool hovered;
    private float scale = 1f;

    public static void Attach(GameObject target, float amount = 1.04f)
    {
        if (target == null) return;
        var effect = target.GetComponent<UIHoverScale>();
        if (effect == null) effect = target.AddComponent<UIHoverScale>();
        effect.hoverScale = amount;
    }

    private void Awake()
    {
        restingScale = transform.localScale;
        selectable = GetComponent<Selectable>();
        initialized = true;
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void Update()
    {
        if (selectable != null && (!selectable.IsActive() || !selectable.IsInteractable()))
        {
            ResetScale();
            return;
        }
        float target = hovered ? hoverScale : 1f;
        if (scale == target) return;
        scale = Mathf.Lerp(scale, target, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
        if (Mathf.Abs(scale - target) < 0.0001f) scale = target;
        transform.localScale = new Vector3(restingScale.x * scale, restingScale.y * scale, restingScale.z);
    }

    private void OnDisable() => ResetScale();

    private void ResetScale()
    {
        hovered = false;
        scale = 1f;
        if (initialized) transform.localScale = restingScale;
    }
}
