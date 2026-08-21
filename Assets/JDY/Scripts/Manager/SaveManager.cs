using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public void SaveGame()
    {
        // 돈 저장
        // 아이템 저장
        // 스테이지 저장
        // 업적 저장
        // 캐릭터 해금 저장
        // 메모리얼 저장

        PlayerPrefs.Save();
    }
}
