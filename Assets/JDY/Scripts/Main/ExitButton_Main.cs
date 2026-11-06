using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitButton : MonoBehaviour
{
    public void exitButton()
    {
        // 음악 화면은 들어올 때처럼 암전으로 돌아간다 (MainManager.MusicButton, 2026-11-05)
        if (SceneManager.GetActiveScene().name == "Music")
        {
            SceneFade.LoadScene("Main", Color.black, 0.2f, 0.25f);
            return;
        }
        LoadingScreen.Load("Main"); // NPC 화면에서 마을로 돌아갈 때도 로딩 화면 (JSW/Script/LoadingScreen.cs)
    }
}
