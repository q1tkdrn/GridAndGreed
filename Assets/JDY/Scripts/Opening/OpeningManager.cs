using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class OpeningManager : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // 스페이스로 오프닝을 건너뛰어도 새 게임은 튜토리얼로 간다
            TutorialLink.Begin();
        }
    }
}
