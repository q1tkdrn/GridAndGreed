using UnityEngine;

public class MainMusic : MonoBehaviour
{
    [Header("Manager Obj")]
    public MusicManager musicManager;
    private int currentIndex;
    public AudioSource audioSource;
    void Start()
    {
        currentIndex = PlayerPrefs.GetInt("MainMusic");
        audioSource.clip = musicManager.musics[currentIndex];
        audioSource.Play();
    }
}
