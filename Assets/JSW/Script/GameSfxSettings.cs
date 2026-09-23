using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Game SFX Settings")]
public sealed class GameSfxSettings : ScriptableObject
{
    public AudioMixerGroup output;
    public AudioClip click;
    public AudioClip move;
    public AudioClip judgment;
    public AudioClip attack;
    [Range(0f, 1f)] public float volume = 0.7f;
}
