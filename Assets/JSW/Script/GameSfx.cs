using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Observe UI input before scene-changing button callbacks, without replacing them.
[DefaultExecutionOrder(-1000)]
public sealed class GameSfx : MonoBehaviour
{
    private static GameSfx instance;
    private GameSfxSettings settings;
    private AudioSource source;
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private readonly Dictionary<AudioClip, int> lastPlayedFrames = new Dictionary<AudioClip, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null) return;
        new GameObject("Game SFX").AddComponent<GameSfx>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        settings = Resources.Load<GameSfxSettings>("GameSfxSettings");
        if (settings == null)
        {
            Debug.LogWarning("GameSfxSettings is missing from Resources.");
            enabled = false;
            return;
        }
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = settings.output;
    }

    private void Start()
    {
        if (settings == null || settings.output == null) return;
        foreach (string parameter in new[] { "MasterVolume", "SFXVolume" })
        {
            float value = PlayerPrefs.GetFloat(parameter, 1f);
            settings.output.audioMixer.SetFloat(parameter, value <= 0.001f ? -80f : Mathf.Log10(value) * 20f);
        }
    }

    private void Update()
    {
        var events = EventSystem.current;
        if (events == null || settings == null) return;
        if (Input.GetMouseButtonDown(0))
        {
            hits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = Input.mousePosition }, hits);
            // Only the frontmost hit can receive the pointer event.
            if (hits.Count > 0 && IsClickable(hits[0].gameObject)) Play(settings.click);
        }
        if (Input.GetButtonDown("Submit") && events.currentSelectedGameObject != null)
        {
            var button = events.currentSelectedGameObject.GetComponent<Button>();
            if (button != null && button.IsActive() && button.IsInteractable()) Play(settings.click);
        }
    }

    private static bool IsClickable(GameObject target)
    {
        for (Transform node = target.transform; node != null; node = node.parent)
        {
            var selectable = node.GetComponent<Selectable>();
            if (selectable != null) return selectable.IsActive() && selectable.IsInteractable();
            var trigger = node.GetComponent<EventTrigger>();
            if (trigger != null && trigger.isActiveAndEnabled)
            {
                foreach (var entry in trigger.triggers)
                    if (entry.eventID == EventTriggerType.PointerClick || entry.eventID == EventTriggerType.PointerDown)
                        return true;
                // EventTrigger consumes events instead of bubbling to its parents.
                return false;
            }
            if (node.GetComponent<IPointerClickHandler>() != null || node.GetComponent<IPointerDownHandler>() != null)
                return true;
        }
        return false;
    }

    private void Play(AudioClip clip)
    {
        if (source == null || clip == null) return;
        // Simultaneous unit attacks share one sound, avoiding multiplied volume.
        if (lastPlayedFrames.TryGetValue(clip, out int frame) && frame == Time.frameCount) return;
        lastPlayedFrames[clip] = Time.frameCount;
        source.PlayOneShot(clip, settings.volume);
    }

    public static void PlayMove() { if (instance != null && instance.settings != null) instance.Play(instance.settings.move); }
    public static void PlayJudgment() { if (instance != null && instance.settings != null) instance.Play(instance.settings.judgment); }
    public static void PlayAttack() { if (instance != null && instance.settings != null) instance.Play(instance.settings.attack); }
}
