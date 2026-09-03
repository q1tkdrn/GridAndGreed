using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class OpeningUI : MonoBehaviour
{
    [Header("UI-image")]
    [SerializeField] private Image frame;
    [SerializeField] private Sprite[] Images;
    [Header("UI-text")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private string[] content;
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
            text.text = content[currentIndex];
        }
        else
        {
            SceneManager.LoadScene("Main");
        }
    }
}
