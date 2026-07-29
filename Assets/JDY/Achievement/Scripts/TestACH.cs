using UnityEngine;
using UnityEngine.SceneManagement;
public class TestACH : MonoBehaviour
{
    public void TestCodes()
    {
        AchievementManager.Instance.AddProgress("ACH-1", 1);
        SceneManager.LoadScene("Main");
    }
}
