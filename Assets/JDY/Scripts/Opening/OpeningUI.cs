using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class OpeningUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Sprite[] Images;
    private int currentIndex;
    void Start()
    {
        currentIndex = 0;
        SetImage();
    }
    public void NextImage()
    {
        currentIndex++;
        SetImage();
    }
    private void SetImage()
    {
        if (currentIndex < Images.Length)
        {
            background.sprite = Images[currentIndex];
        }
        else
        {
            SceneManager.LoadScene("Main");
        }
    }
}
