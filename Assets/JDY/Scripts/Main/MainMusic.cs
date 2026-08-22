using UnityEngine;

public class MainMusic : MonoBehaviour
{
    [Header("Script")]
    public MusicManager musicManager;
    private int currentIndex;
    [Header("Audio")]
    public AudioSource audioSource;
    void Start()
    {
        currentIndex = PlayerPrefs.GetInt("MainMusic", 0);
        audioSource.clip = musicManager.musics[currentIndex];
        audioSource.Play();
    }
}
