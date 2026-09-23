using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SettingManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer mixer;
    [Header("Slider")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    private GameObject creditPanel;
    private Button creditButton;
    private Button creditBackButton;
    private readonly Dictionary<Selectable, bool> creditBlockedControls = new();

    void Start()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float bgm = PlayerPrefs.GetFloat("BGMVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);

        masterSlider.value = master;
        bgmSlider.value = bgm;
        sfxSlider.value = sfx;

        SetMasterVolume(master);
        SetBGMVolume(bgm);
        SetSFXVolume(sfx);
        CreateCreditUI();
    }

    private void CreateCreditUI()
    {
        if (creditButton != null || sfxSlider == null) return;
        var settings = sfxSlider.transform.parent;
        var backTemplate = settings.Find("Exit Button")?.GetComponent<Button>();
        var ending = Resources.Load<Texture2D>("Ending");
        if (backTemplate == null || ending == null)
        {
            Debug.LogWarning("Credit UI requires the settings Exit Button and Resources/Ending texture.", this);
            return;
        }

        var buttonObject = new GameObject("Credit Button", typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)buttonObject.transform;
        rect.SetParent(settings, false);
        buttonObject.layer = settings.gameObject.layer;
        var sliderRect = (RectTransform)sfxSlider.transform;
        rect.anchorMin = sliderRect.anchorMin;
        rect.anchorMax = sliderRect.anchorMax;
        rect.pivot = sliderRect.pivot;
        rect.anchoredPosition = sliderRect.anchoredPosition + new Vector2(0f, -125f);
        rect.sizeDelta = new Vector2(300f, 72f);
        var background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.12f, 0.12f, 0.12f, 0.96f);
        var outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.72f, 0.70f, 0.85f);
        outline.effectDistance = new Vector2(1f, -1f);
        creditButton = buttonObject.GetComponent<Button>();
        creditButton.targetGraphic = background;
        var colors = creditButton.colors;
        colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        creditButton.colors = colors;

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.layer = settings.gameObject.layer;
        Stretch((RectTransform)labelObject.transform, rect);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        var existingText = sfxSlider.GetComponentInChildren<TMP_Text>(true);
        if (existingText != null) label.font = existingText.font;
        label.text = "Credit";
        label.fontSize = 34f;
        label.characterSpacing = 6f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.92f, 0.91f, 0.87f);
        label.raycastTarget = false;

        creditPanel = new GameObject("Credit Panel", typeof(RectTransform), typeof(Image));
        creditPanel.layer = settings.gameObject.layer;
        Stretch((RectTransform)creditPanel.transform, settings);
        creditPanel.GetComponent<Image>().color = Color.black;
        var artwork = new GameObject("Ending", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        artwork.layer = settings.gameObject.layer;
        Stretch((RectTransform)artwork.transform, creditPanel.transform);
        var image = artwork.GetComponent<RawImage>();
        image.texture = ending;
        image.raycastTarget = false;
        var aspect = artwork.GetComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspect.aspectRatio = (float)ending.width / ending.height;

        // Reuse the existing arrow artwork, caption, size and position. Replace the
        // entire event so the copied persistent listener cannot close settings.
        creditBackButton = Instantiate(backTemplate, creditPanel.transform, false);
        creditBackButton.name = "Credit Back Button";
        creditBackButton.onClick = new Button.ButtonClickedEvent();
        creditBackButton.onClick.AddListener(HideCredit);
        creditBackButton.navigation = new Navigation { mode = Navigation.Mode.None };
        creditBackButton.gameObject.SetActive(true);
        creditPanel.SetActive(false);
        creditButton.onClick.AddListener(ShowCredit);
        UIHoverScale.Attach(creditButton.gameObject);
        UIHoverScale.Attach(creditBackButton.gameObject);
    }

    private static void Stretch(RectTransform rect, Transform parent)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public void ShowCredit()
    {
        if (creditPanel == null || creditPanel.activeSelf) return;
        // Disable underlying controls for keyboard/controller navigation too.
        creditBlockedControls.Clear();
        foreach (var control in sfxSlider.transform.parent.GetComponentsInChildren<Selectable>(true))
        {
            if (control.transform.IsChildOf(creditPanel.transform)) continue;
            creditBlockedControls[control] = control.interactable;
            control.interactable = false;
        }
        creditPanel.transform.SetAsLastSibling();
        creditPanel.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(creditBackButton.gameObject);
        // ACH-10: opening the actual artwork completes the existing credit achievement.
        if (creditPanel.activeInHierarchy && AchievementManager.Instance != null)
        {
            AchievementManager.Instance.AddProgress("ACH-10", 1);
            PlayerPrefs.Save();
        }
    }

    public void HideCredit()
    {
        if (creditPanel == null) return;
        creditPanel.SetActive(false);
        foreach (var entry in creditBlockedControls)
            if (entry.Key != null) entry.Key.interactable = entry.Value;
        creditBlockedControls.Clear();
        EventSystem.current?.SetSelectedGameObject(creditButton.gameObject);
    }

    public void SetMasterVolume(float value)
    {
        SetMixerVolume("MasterVolume", value);
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
        if (value <= 0.001f) 
        {
            AchievementManager.Instance.AddProgress("ACH-27", 1);
        }
    }

    public void SetBGMVolume(float value)
    {
        SetMixerVolume("BGMVolume", value);
        PlayerPrefs.SetFloat("BGMVolume", value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        SetMixerVolume("SFXVolume", value);
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    private void SetMixerVolume(string parameterName, float value)
    {
        if (value <= 0.001f)
        {
            mixer.SetFloat(parameterName, -80f);
        }
        else
        {
            mixer.SetFloat(parameterName, Mathf.Log10(value) * 20f);
        }
    }
}
