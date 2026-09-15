using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class OpeningUI : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private TypingEffect typingEffect;
    [Header("UI-image")]
    [SerializeField] private Image frame;
    [SerializeField] private Sprite[] Images;
    [Header("UI-text")]
    [TextArea]
    [SerializeField] private string[] content;
    [Header("UI")]
    [SerializeField] private GameObject nextButton;
    private int currentIndex;
    void Start()
    {
        currentIndex = 0;
        SetUI();
    }
    public void NextUIButton()
    {
        currentIndex++;
        SetUI();
    }
    private void SetUI()
    {
        if (currentIndex < Images.Length)
        {
            frame.sprite = Images[currentIndex];
            nextButton.SetActive(false);
            typingEffect.StartTyping(content[currentIndex], ()=>nextButton.SetActive(true));
        }
        else
        {
            SceneManager.LoadScene("Main");
        }
    }
}
