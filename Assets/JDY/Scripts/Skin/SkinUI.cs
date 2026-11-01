using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button[] SkinButton;
    [SerializeField] private SkinDialog skinDialog;

    private SkinSlot skinSlot;
    private Image[] shades;
    private TMP_Text[] labels;
    private SkinLockedMark[] marks;

    private void Awake()
    {
        skinSlot = GetComponentInChildren<SkinSlot>(true);
        shades = new Image[SkinButton.Length];
        labels = new TMP_Text[SkinButton.Length];
        marks = new SkinLockedMark[SkinButton.Length];
        for (int i = 0; i < SkinButton.Length; i++)
        {
            var tab = SkinButton[i];
            var overlay = new GameObject("Inactive Shade", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)overlay.transform;
            rect.SetParent(tab.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
            shades[i] = overlay.GetComponent<Image>();
            shades[i].raycastTarget = false;
            var cross = new GameObject("Locked X", typeof(RectTransform), typeof(CanvasRenderer), typeof(SkinLockedMark));
            var crossRect = (RectTransform)cross.transform;
            crossRect.SetParent(tab.transform, false);
            crossRect.anchorMin = crossRect.anchorMax = new Vector2(1f, 0.5f);
            crossRect.pivot = new Vector2(0.5f, 0.5f);
            crossRect.anchoredPosition = new Vector2(-37f, 18f);
            crossRect.sizeDelta = new Vector2(60f, 60f);
            marks[i] = cross.GetComponent<SkinLockedMark>();
            marks[i].color = new Color(0.82f, 0.12f, 0.12f);
            marks[i].raycastTarget = false;
            labels[i] = tab.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void OnEnable()
    {
        if (skinSlot != null) skinSlot.SelectionChanged += RefreshShades;
    }

    private void OnDisable()
    {
        if (skinSlot != null) skinSlot.SelectionChanged -= RefreshShades;
    }

    private void Start()
    {
        for (int i = 0; i < SkinButton.Length; i++)
            SkinButton[i].interactable = skinDialog.currentPhase >= i;
        RefreshShades();
    }

    private void RefreshShades()
    {
        if (skinSlot == null) return;
        Skin[] themes = { Skin.InsectSkin, Skin.BossSkin, Skin.NpcSkin };
        for (int i = 0; i < SkinButton.Length; i++)
        {
            bool selected = skinSlot.currentSkin == themes[i];
            bool locked = !SkinButton[i].interactable;
            shades[i].color = new Color(0f, 0f, 0f, locked ? 0.72f : selected ? 0f : 0.48f);
            marks[i].gameObject.SetActive(locked);
            if (labels[i] != null)
                labels[i].color = selected && !locked ? new Color(0.196f, 0.196f, 0.196f)
                    : locked ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
        }
    }
}
