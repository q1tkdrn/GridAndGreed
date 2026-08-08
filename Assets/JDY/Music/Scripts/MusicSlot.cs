using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class MusicSlot : MonoBehaviour
{
    public MusicManager manager;
    public AudioSource audioSource;
    private int currentIndex = 0;

    public TMP_Text[] nameText;
    void Start()
    {
        SetMusic();
    }
    private int GetIndex(int offset)
    {
        return (currentIndex + offset + manager.musics.Length) % manager.musics.Length;
    }
    public void SetMusic()
    {
        for (int i = 0; i < nameText.Length; i++)
        {
            int index = GetIndex(i - 2);
            nameText[i].text = manager.musics[index].name;
        }
    }
    void Update()
    {
        Vector2 scroll = Mouse.current.scroll.ReadValue();
        if (Keyboard.current.downArrowKey.wasPressedThisFrame || scroll.y < 0)
        {
            NextMusic();
        }
        if (Keyboard.current.upArrowKey.wasPressedThisFrame || scroll.y > 0)
        {
            PrevMusic();
        }
    }
    private void PrevMusic()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = manager.musics.Length - 1;
        SetMusic();
    }
    private void NextMusic()
    {
        currentIndex++;
        if (currentIndex >= manager.musics.Length)
            currentIndex = 0;
        SetMusic();
    }
    public void PlayButton()
    {
        audioSource.clip = manager.musics[currentIndex];
        audioSource.Play();
    }
}
