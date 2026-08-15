using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitButton : MonoBehaviour
{
    public void exitButton()
    {
        SceneManager.LoadScene("Main");
    }
}
