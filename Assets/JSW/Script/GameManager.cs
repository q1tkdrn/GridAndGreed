using NUnit.Framework;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    private enum PlayerType {Knight, Archer, Thief, Bishop, Dancer, Scavenger, Wizard, Fanatic, Little_Reaper}
    private GameObject PlayerPrefeb;
    private GameObject BossPrefeb;
    public static List<GameObject> Player = new List<GameObject>();
    public static Vector2[,] position = new Vector2[9, 9];
    private GameObject MainBoard;
    private float MainX;
    private float MainY;

    // 보드의 좌표 범위 (외부에서 이동 가능 범위 체크용)
    public static float BoardMinX;
    public static float BoardMaxX;
    public static float BoardMinY;
    public static float BoardMaxY;

    void Start()
    {
        PositionSetting();
        PositionMove(3, 3);
    }

    //private GameObject BossCreate()
    //{

   // }

    private GameObject PlayerCreate(PlayerType Type)
    {
        GameObject newPlayer = Instantiate(PlayerPrefeb, new Vector2(0, 0), Quaternion.identity);
        

        return newPlayer;
    }

    private void PositionMove(int x, int y)
    {
        GameObject MainPlayer;
        MainPlayer = GameObject.Find("Player");
        MainPlayer.transform.position = position[x, y];
    }

    private void PositionSetting()
    {
        MainBoard = GameObject.Find("(0, 0)Board");
        MainX = MainBoard.transform.position.x;
        MainY = MainBoard.transform.position.y;

        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                position[x, y] = new Vector2(
                    MainX + x,
                    MainY + y
                );
            }
        }

        BoardMinX = MainX;
        BoardMaxX = MainX + 8;
        BoardMinY = MainY;
        BoardMaxY = MainY + 8;
    }

    private void Activate_Player()
    {

    }

    private void Activate_Boss()
    {

    }
}