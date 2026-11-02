using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class OpeningUI : MonoBehaviour
{
    [Header("Script")]
    //[SerializeField] private TypingEffect typingEffect;
    [SerializeField] private TextFadeEffect textFadeEffect;
    [Header("UI-image")]
    [SerializeField] private Image frame;
    [SerializeField] private Sprite[] Images;
    [Header("UI-text")]
    [SerializeField] private TMP_Text text;
    [TextArea]
    [SerializeField] private string[] content;
    [Header("UI")]
    [SerializeField] private GameObject nextButton;
    private int currentIndex;
    void Start()
    {
        currentIndex = 0;
        nextButton.SetActive(false);
        SetUI();
    }
    public void NextUIButton()
    {
        currentIndex++;
        textFadeEffect.FadeOutText(text, SetUI);
        nextButton.SetActive(false);
    }
    private void SetUI()
    {
        if (currentIndex < Images.Length)
        {
            frame.sprite = Images[currentIndex];
            //typingEffect.StartTyping(content[currentIndex], ()=>nextButton.SetActive(true));
            text.text = content[currentIndex];
            textFadeEffect.FadeInText(text, () => nextButton.SetActive(true));
        }
        else
        {
            // 새 게임: 오프닝이 끝나면 튜토리얼 전투로 (끝나면 마을로 간다)
            TutorialLink.Begin();
        }
    }
}
