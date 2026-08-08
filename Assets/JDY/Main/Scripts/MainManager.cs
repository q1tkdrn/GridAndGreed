using UnityEngine;
using UnityEngine.SceneManagement;

public class MainManager : MonoBehaviour
{
    public GameObject doorMain;
    public GameObject backMain;
    private bool whatMain;
    void Start()
    {
        whatMain = false;//Test
        backMain.SetActive(whatMain);
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
    public void LeftButton()
    {
        whatMain = !whatMain;//Test
        backMain.SetActive(whatMain);
    }
}
