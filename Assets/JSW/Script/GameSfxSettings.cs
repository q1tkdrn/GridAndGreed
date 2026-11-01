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
    [Header("Battle")]
    public AudioClip pickup;
    public AudioClip greedPickup;
    public AudioClip greedStand;
    public AudioClip hit;
    public AudioClip down;
    public AudioClip bigJudgment;
    public AudioClip trait;
    public AudioClip denied;
    [Header("Shop")]
    public AudioClip purchase;
    [Range(0f, 1f)] public float volume = 0.7f;
}
