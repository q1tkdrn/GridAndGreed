using UnityEngine;
using UnityEngine.SceneManagement;

public class MainManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject[] main;

    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;
    private int currentIndex = 1;
    void Start()
    {
        currentIndex = 1;
        SetMain();
    }
    private void SetMain()
    {
        for (int i = 0; i < main.Length; i++)
        {
            main[i].SetActive(false);
        }

        main[currentIndex].SetActive(true);

        leftButton.SetActive(currentIndex > 0);
        rightButton.SetActive(currentIndex < main.Length - 1);
    }
    public void LeftButton()
    {
        if (currentIndex <= 0) return;

        currentIndex--;
        SetMain();
    }
    public void RightButton()
    {
        if (currentIndex >= main.Length - 1) return;

        currentIndex++;
        SetMain();
    }
    public void ItemShopButton()
    {
        SceneManager.LoadScene("Item");
    }
    public void CharacterShopButton()
    {
        SceneManager.LoadScene("Character");
    }
    public void AchievementButton()
    {
        SceneManager.LoadScene("Achievement");
    }
    public void MemorialButton()
    {
        SceneManager.LoadScene("Memorial");
    }
    public void MusicButton()
    {
        SceneManager.LoadScene("Music");
    }
}
