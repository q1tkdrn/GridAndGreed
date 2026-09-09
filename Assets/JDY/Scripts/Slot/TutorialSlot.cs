using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class TutorialSlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text indexText;
    [Header("Images")]
    [SerializeField] private Sprite[] sprite;
    private int currentIndex = 0;

    void Start()
    {
        SetImage();
    }
    private void SetImage()
    {
        image.sprite = sprite[currentIndex];
        indexText.text = (currentIndex + 1) + "/" + sprite.Length;
    }
    public void LeftButton()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = sprite.Length - 1;
        SetImage();
    }
    public void RightButton()
    {
        currentIndex++;
        if (currentIndex >= sprite.Length)
            currentIndex = 0;
        SetImage() ;
    }
}
