using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitButton : MonoBehaviour
{
    public void exitButton()
    {
        LoadingScreen.Load("Main"); // NPC 화면에서 마을로 돌아갈 때도 로딩 화면 (JSW/Script/LoadingScreen.cs)
    }
}
