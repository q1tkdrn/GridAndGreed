using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Turn : MonoBehaviour
{
    private GameObject GameController;
    private Plate pt;
    public static int TurnCount = 5;
    private GameManager gm;
    public TMP_Text TurnCountText;
    public TMP_Text BossHP;
    public TMP_Text PlayerHP;

    void Start()
    {
        UpdateTurnUI();
        gm = gameObject.AddComponent<GameManager>();
        pt = gameObject.AddComponent<Plate>();

    }
    private void Call_Boss_PlateCreate()
    {
        pt.Boss_PlateCreate6();
    }
    public void TurnCount_Subtract(int x)
    {
        TurnCount -= x;

        UpdateTurnUI();

        if (TurnCount == 0)
        {
            gm.Player_All_Attck();
            Debug.Log("턴이 모두 소진되었습니다.");
            Debug.Log("보스의 HP : " + GameManager.BossHP);
            Invoke(nameof(Call_Boss_PlateCreate), 0.5f);
            UpdateTurnUI();
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
            BossHP.text = "BossHP : " + GameManager.BossHP;
        }
    }

    public void Turn_On()
    {
        Debug.Log("턴이 시작되었습니다!");
        TurnCount = 5;
        UpdateTurnUI();
    }
}