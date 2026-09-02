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

    private string[] Player_Names = { "Kight", "Archer" , "Thief"};
    private int[] Player_Atk = { 6, 4, 3 };
    private int[] Player_HH = { 1, 3, 4 }; 
    private string[] Boss_Names = { "BigHouse" };
    private int[] Boss_Hp = { 100 };
    private int[] Boss_Atk = { 5 }; // 의지수치
    private int bossindex;
    private Player[] players = new Player[3];
    private Turn tn;
    private Player pl;
    private Plate pt;

    static public int PlayerHP;
    static public int BossHP;
    // 보드의 좌표 범위 (외부에서 이동 가능 범위 체크용)
    public static float BoardMinX;
    public static float BoardMaxX;
    public static float BoardMinY;
    public static float BoardMaxY;

    public int BossIndex
    {
        get { return bossindex; }
        set 
        { 
            switch (bossindex)
            {
                case 0:

                break;
            }
        }
    }
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
        PositionMove(2, 5, 5);
        Activate_Player(0, 0);
        Activate_Player(1, 1);
        Activate_Player(2, 2);
        Activate_Boss(0);
        PlayerHP = 15;
    }
    private void PositionMove(int CharacterIndex, int x, int y)
    {
        players[CharacterIndex].transform.position = position[x, y];
        Player.player_board_x[CharacterIndex] = x;
        Player.player_board_y[CharacterIndex] = y;

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
        bossindex = BossIndex;
    }
    
    //private void BigHouse_Passive()
    //{
     //   if(/*턴 종료를 알리는 것*/ && Turn.TurnCount <=3)
      //  {
           // Boss_Atk[0]++;
       // }
    //}


    public int Change_Coordinate_X_To_Board_X()
    {
        int i;
        MainBoard = GameObject.Find("(0, 0)Board");
        MainX = MainBoard.transform.position.x;

        for (i = 0; i < 9; i++)
        {
            if(Mathf.Approximately(MainX + i, Player.player_x[Player.ClickedCharacterIndex - 1]))
            {
                return i;
            }

        }
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
                return i;
            }
        }
        return -1;
    }

    public void Player_All_Attck()
    {
        for (int i = 0; i < 3; i++)
        {
            BossHP -= players[i].Attck;
        }
    }


}
