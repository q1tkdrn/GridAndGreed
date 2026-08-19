using NUnit.Framework;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    private GameObject PlayerPrefeb;
    private GameObject BossPrefeb;
    public static Vector2[,] position = new Vector2[9, 9];
    private GameObject MainBoard;
    private float MainX;
    private float MainY;

    private string[] Player_Names = { "Kight", "Archer" };
    private int[] Player_Atk = { 10, 5 };
    private int[] Player_HH = { 7, 7 };
    private string[] Boss_Names = { "BigHouse" };
    private int[] Boss_Hp = { 100 };
    private int[] Boss_Atk = { 5 };
    private Player[] players = new Player[3];
    private Player pl;
    static public int BossHP;




    // 보드의 좌표 범위 (외부에서 이동 가능 범위 체크용)
    public static float BoardMinX;
    public static float BoardMaxX;
    public static float BoardMinY;
    public static float BoardMaxY;

    void Start()
    {
        players[0] = GameObject.Find("Player1").GetComponent<Player>();
        players[1] = GameObject.Find("Player2").GetComponent<Player>();
        players[2] = GameObject.Find("Player3").GetComponent<Player>();
        players[0].CharacterIndex = 1;
        players[1].CharacterIndex = 2;
        players[2].CharacterIndex = 3;
        PositionSetting();
        PositionMove(0, 3, 3);
        PositionMove(1, 4, 4);
        Activate_Player(0, 0);
        Activate_Player(1, 1);
        Activate_Boss(0);
    }

    private void Position_AfterClick()
    {

    }
    private void PositionMove(int CharacterIndex, int x, int y)
    {
        players[CharacterIndex].transform.position = position[x, y];
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


    private void Activate_Player(int SelectedNumber, int CharacterIndex)
    {
        players[SelectedNumber].CharacterName = Player_Names[CharacterIndex];
        players[SelectedNumber].Attck = Player_Atk[CharacterIndex];
        players[SelectedNumber].HHh = Player_HH[CharacterIndex];
    }
    private void Activate_Boss(int BossIndex)
    {

        string name = Boss_Names[BossIndex];
        int hp = Boss_Hp[BossIndex];
        int atk = Boss_Atk[BossIndex];
        BossHP = hp;
        Debug.Log("보스 체력 : " + hp);


    }

    public int Change_Coordinate_X_To_Board_X()
    {
        int i;
        MainBoard = GameObject.Find("(0, 0)Board");
        MainX = MainBoard.transform.position.x;

        for (i = 0; i < 9; i++)
        {
            if(Mathf.Approximately(MainX + i, Player.player_x[Player.ClickedCharacterIndex - 1]))
            {
                Debug.Log("성공!" + MainX + i + "       " + Player.player_x[Player.ClickedCharacterIndex - 1]);
                return i;
            }

        }
        Debug.Log(MainX + i + "       " + Player.player_x[Player.ClickedCharacterIndex - 1]);
        return -1;
    }
    public int Change_Coordinate_Y_To_Board_Y()
    {
        MainBoard = GameObject.Find("(0, 0)Board");
        MainY = MainBoard.transform.position.y;
        int i;
        for (i = 0; i < 9; i++)
        {
            if (Mathf.Approximately(MainY + i, Player.player_y[Player.ClickedCharacterIndex -1]))
            {
                Debug.Log("성공!" + MainY + i + "       " + Player.player_y[Player.ClickedCharacterIndex - 1]);
                return i;

            }

        }
        Debug.Log(MainY + i + "       " + Player.player_y[Player.ClickedCharacterIndex - 1]);

        return -1;

    }
}
