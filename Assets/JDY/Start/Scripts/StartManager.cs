using UnityEngine;
using UnityEngine.SceneManagement;
public class StartManager : MonoBehaviour
{
    public void NewStartButton()
    {
        SceneManager.LoadScene("Opening");
    }
}
