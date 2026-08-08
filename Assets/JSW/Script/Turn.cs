using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Turn : MonoBehaviour
{
    private GameObject GameController;
    public static int TurnCount = 5;

    public TMP_Text TurnCountText; // 인스펙터에서 UI 텍스트 오브젝트 연결

    void Start()
    {
        UpdateTurnUI();
    }

    public void TurnCount_Subtract(int x)
    {
        TurnCount -= x;

        if (TurnCount < 0)
        {
            TurnCount = 0;
        }

        UpdateTurnUI();

        if (TurnCount == 0)
        {
            OnTurnEnd();
        }
    }

    public void TurnCount_Add(int x)
    {
        TurnCount += x;
        UpdateTurnUI();
    }

    private void UpdateTurnUI()
    {
        if (TurnCountText != null)
        {
            TurnCountText.text = "Turn : " + TurnCount;
        }
    }

    private void OnTurnEnd()
    {
        // 턴이 0이 됐을 때 처리 (예: 다음 라운드 시작, 게임 오버 등)
        Debug.Log("턴이 모두 소진되었습니다.");
    }
}