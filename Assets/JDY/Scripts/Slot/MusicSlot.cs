using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
    [Tooltip("재생 버튼 이미지. 고른 곡이 재생 중이면 일시정지 그림으로 바뀐다.")]
    [SerializeField] private Image playButtonImage;
    [Tooltip("재생 중에 보여줄 일시정지 그림. 비워 두면 Resources/음악_재생 을 쓴다.")]
    [SerializeField] private Sprite pauseSprite;
    [Header("재생 중인 곡 강조")]
    [Tooltip("재생 중인 곡이 가운데에 있을 때, 가운데 바로 옆 곡의 밝기(알파).")]
    [SerializeField, Range(0f, 1f)] private float nearAlpha = 0.6f;
    [Tooltip("재생 중인 곡이 가운데에 있을 때, 맨 끝 곡의 밝기(알파).")]
    [SerializeField, Range(0f, 1f)] private float farAlpha = 0.35f;
    private Sprite playSprite;
    private bool isOwnPlayback; // 이 화면의 재생 버튼으로 튼 곡인지 (들어올 때 나오는 배경음은 아님)
    private bool isPaused;
    void Start()
    {
        currentIndex = PlayerPrefs.GetInt("MainMusic", 0);
        SetSlot();
        if (playButtonImage != null) playSprite = playButtonImage.sprite;
        if (pauseSprite == null)
        {
            var sprites = Resources.LoadAll<Sprite>("음악_재생");
            if (sprites.Length > 0) pauseSprite = sprites[0];
        }
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
            nameText[i].text = MusicTitle.Of(manager.musics[index]);   // 분류어 뗀 제목 (JSW/Script/MusicTitle.cs)
        }
        PlayerPrefs.SetInt("MainMusic", currentIndex);
        RefreshHighlight();
    }
    void Update()
    {
        Vector2 scroll = Mouse.current.scroll.ReadValue();
        if (Keyboard.current.downArrowKey.wasPressedThisFrame || scroll.y < 0)
        {
            NextMusic();
            dialog.StartMusicDialog(MusicTitle.Of(manager.musics[currentIndex]));
        }
        if (Keyboard.current.upArrowKey.wasPressedThisFrame || scroll.y > 0)
        {
            PrevMusic();
            dialog.StartMusicDialog(MusicTitle.Of(manager.musics[currentIndex]));
        }
        RefreshPlayIcon();
        RefreshHighlight();
    }

    // 지금 고른 곡이 재생 중이면 일시정지(‖), 아니면 재생(▶) 그림을 보여준다.
    private void RefreshPlayIcon()
    {
        if (playButtonImage == null || playSprite == null || pauseSprite == null) return;
        bool playingSelected = isOwnPlayback && audioSource.isPlaying && audioSource.clip == manager.musics[currentIndex];
        var sprite = playingSelected ? pauseSprite : playSprite;
        if (playButtonImage.sprite != sprite) playButtonImage.sprite = sprite;
    }

    // 재생 중인 곡(들어올 때 나오는 곡 포함, 일시정지는 제외)이 가운데에 있으면
    // 가운데는 그대로 두고 나머지는 멀수록 어둡게. 가운데에서 벗어나면 모두 같은 밝기로 되돌린다.
    private void RefreshHighlight()
    {
        if (nameText == null || audioSource == null || manager == null || manager.musics.Length == 0) return;
        var playing = audioSource.isPlaying ? audioSource.resource as AudioClip : null;
        bool highlight = playing != null && playing == manager.musics[currentIndex];
        int center = nameText.Length / 2;
        for (int i = 0; i < nameText.Length; i++)
        {
            if (nameText[i] == null) continue;
            int distance = Mathf.Abs(i - center);
            float alpha = !highlight || distance == 0 ? 1f : distance == 1 ? nearAlpha : farAlpha;
            if (!Mathf.Approximately(nameText[i].alpha, alpha)) nameText[i].alpha = alpha;
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
        var selected = manager.musics[currentIndex];
        if (isOwnPlayback && audioSource.clip == selected)
        {
            // 같은 곡: 재생 중이면 일시정지, 멈춰 있으면 멈춘 곳부터 이어서 재생
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
                isPaused = true;
            }
            else
            {
                if (isPaused) audioSource.UnPause(); else audioSource.Play();
                isPaused = false;
            }
        }
        else
        {
            // 다른 곡을 골랐거나 처음 누름: 고른 곡을 처음부터
            audioSource.clip = selected;
            audioSource.Play();
            isOwnPlayback = true;
            isPaused = false;
        }
        RefreshPlayIcon();
        RefreshHighlight();
    }
}
