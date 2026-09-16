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
            SceneManager.LoadScene("Main");
        }
    }
}
