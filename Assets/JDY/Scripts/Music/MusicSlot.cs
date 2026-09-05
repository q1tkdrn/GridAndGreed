using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class MusicSlot : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private MusicManager manager;
    [SerializeField] private MusicDialog dialog;
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    private int currentIndex;
    [Header("UI")]
    [SerializeField] private TMP_Text[] nameText;
    void Start()
    {
        currentIndex = PlayerPrefs.GetInt("MainMusic", 0);
        SetSlot();
    }
    private int GetIndex(int offset)
    {
        return (currentIndex + offset + manager.musics.Length) % manager.musics.Length;
    }
    public void SetSlot()
    {
        for (int i = 0; i < nameText.Length; i++)
        {
            int index = GetIndex(i - 2);
            nameText[i].text = manager.musics[index].name;
        }
        PlayerPrefs.SetInt("MainMusic", currentIndex);
    }
    void Update()
    {
        Vector2 scroll = Mouse.current.scroll.ReadValue();
        if (Keyboard.current.downArrowKey.wasPressedThisFrame || scroll.y < 0)
        {
            NextMusic();
            dialog.StartMusicDialog(manager.musics[currentIndex].name);
        }
        if (Keyboard.current.upArrowKey.wasPressedThisFrame || scroll.y > 0)
        {
            PrevMusic();
            dialog.StartMusicDialog(manager.musics[currentIndex].name);
        }
    }
    private void PrevMusic()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = manager.musics.Length - 1;
        SetSlot();
    }
    private void NextMusic()
    {
        currentIndex++;
        if (currentIndex >= manager.musics.Length)
            currentIndex = 0;
        SetSlot();
    }
    public void PlayButton()
    {
        audioSource.clip = manager.musics[currentIndex];
        audioSource.Play();
    }
}
