using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private GameObject Hitted_Object;
    private GameObject PlayerPrefeb;
    private GameObject BossPrefeb;

    public static Vector2[,] position = new Vector2[9, 9];

    private GameObject MainBoard;
    private float MainX;
    private float MainY;

    private float LastClickTime = 0f;
    private float SceneTime;

    private bool IsDoubleClicked;
    private bool isSelected = false;
    private bool isMoving;

    private Vector3 targetPosition;

    public float selectedScaleMultiplier = 1.2f;
    public float moveSpeed = 10f;

    private string[] Player_Names = { "Kight", "Archer", "Thief" };
    private int[] Player_Atk = { 6, 4, 3 };
    private int[] Player_HH = { 1, 3, 4 };

    private string[] Item_Name = { "Old_Sword" };

    private string[] Boss_Names = { "BigHouse" };
    private int[] Boss_Hp = { 100 };
    private int[] Boss_Atk = { 5 };

    private int bossindex;

    private Player[] players = new Player[3];

    private Turn tn;
    private Player pl;
    private Plate pt;
    private Items it;

    // 현재 선택된 Player
    private Player selectedPlayer;


    static public int PlayerHP;
    static public int BossHP;


    // 보드의 좌표 범위
    public static float BoardMinX;
    public static float BoardMaxX;
    public static float BoardMinY;
    public static float BoardMaxY;


    public static float[] player_x = new float[3];
    public static int[] player_board_x = new int[3];

    public static float[] player_y = new float[3];
    public static int[] player_board_y = new int[3];


    public int BossIndex
    {
        get
        {
            return bossindex;
        }

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
        tn = FindAnyObjectByType<Turn>();
        pl = FindAnyObjectByType<Player>();
        pt = FindAnyObjectByType<Plate>();
        it = FindAnyObjectByType<Items>();

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

        it.Activate_Items(0);

        PlayerHP = 15;
    }


    void Update()
    {
        SceneTime = Time.time;


        // 마우스 왼쪽 클릭
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos =
                Camera.main.ScreenToWorldPoint(Input.mousePosition);


            RaycastHit2D hit =
                Physics2D.Raycast(mousePos, Vector2.zero);


            // 아무것도 맞지 않았다면 종료
            if (hit.collider == null)
            {
                return;
            }


            Hitted_Object = hit.collider.gameObject;


            // 턴이 남아있을 때만 행동
            if (Turn.TurnCount > 0)
            {
                // =========================================
                // Player 클릭
                // =========================================
                if (Hitted_Object.name.Contains("Player"))
                {
                    // 실제로 클릭한 Player를 가져온다
                    selectedPlayer =
                        Hitted_Object.GetComponent<Player>();


                    // Player 컴포넌트가 없다면 종료
                    if (selectedPlayer == null)
                    {
                        return;
                    }


                    // Plate에게 현재 선택한 Player를 알려준다
                    pt.Check_Character(selectedPlayer);


                    // =========================================
                    // 더블클릭
                    // =========================================
                    if (SceneTime - LastClickTime <= 0.3f)
                    {
                        IsDoubleClicked = true;

                        LastClickTime = 0f;


                        // 실제 클릭한 Player가 공격
                        selectedPlayer.DoubleClick();


                        // 이동 가능 칸 제거
                        pt.Remove_MovingPlate();
                    }


                    // =========================================
                    // 첫 번째 클릭
                    // =========================================
                    else
                    {
                        IsDoubleClicked = false;

                        LastClickTime = SceneTime;


                        isSelected = true;


                        // 실제 클릭한 Player 확대
                        selectedPlayer.transform.localScale =
                            selectedPlayer.transform.localScale
                            * selectedScaleMultiplier;


                        // 기존 이동 가능 칸 제거
                        pt.Remove_MovingPlate();


                        // 새로운 이동 가능 칸 생성
                        pt.Create_MovingPlate(7);
                    }
                }


                // =========================================
                // MovingPoint 클릭
                // =========================================
                else if (isSelected &&
                         hit.collider.CompareTag("MovePoint"))
                {
                    // 이동할 위치 저장
                    targetPosition =
                        hit.collider.transform.position;


                    // 이동 시작
                    isMoving = true;


                    // 선택 해제
                    isSelected = false;


                    // Player 크기 원래대로
                    selectedPlayer.transform.localScale =
                        selectedPlayer.transform.localScale
                        / selectedScaleMultiplier;


                    // 이동 가능 칸 제거
                    pt.Remove_MovingPlate();


                    // Player 방향 전환
                    selectedPlayer.FlipX();
                }
            }
        }


        // =========================================
        // Player 이동
        // =========================================
        if (isMoving && selectedPlayer != null)
        {
            // 실제 선택된 Player를 이동시킨다
            selectedPlayer.transform.position =
                Vector3.MoveTowards(
                    selectedPlayer.transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );


            // 목표 위치에 도착했는지 확인
            if (Vector3.Distance(
                    selectedPlayer.transform.position,
                    targetPosition) < 0.01f)
            {
                // 정확히 목표 위치로 이동
                selectedPlayer.transform.position =
                    targetPosition;


                isMoving = false;


                // =========================================
                // 현재 Player의 Unity 좌표 저장
                // =========================================

                player_x[selectedPlayer.CharacterIndex - 1] =
                    targetPosition.x;

                player_y[selectedPlayer.CharacterIndex - 1] =
                    targetPosition.y;


                // =========================================
                // Unity 좌표 → Board 좌표
                // =========================================

                player_board_x[selectedPlayer.CharacterIndex - 1] =
                    Change_Coordinate_X_To_Board_X();

                player_board_y[selectedPlayer.CharacterIndex - 1] =
                    Change_Coordinate_Y_To_Board_Y();


                Debug.Log(
                    Change_Coordinate_X_To_Board_X()
                    + " , "
                    + Change_Coordinate_Y_To_Board_Y()
                );


                // 턴 1 감소
                tn.TurnCount_Subtract(1);
            }
        }
    }




    // =========================================
    // Player 초기 위치 설정
    // =========================================

    private void PositionMove(
        int CharacterIndex,
        int x,
        int y)
    {
        players[CharacterIndex].transform.position =
            position[x, y];


        player_board_x[CharacterIndex] = x;
        player_board_y[CharacterIndex] = y;
    }


    // =========================================
    // Board 좌표 설정
    // =========================================

    private void PositionSetting()
    {
        MainBoard =
            GameObject.Find("(0, 0)Board");


        MainX =
            MainBoard.transform.position.x;


        MainY =
            MainBoard.transform.position.y;


        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                position[x, y] =
                    new Vector2(
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


    // =========================================
    // Player 캐릭터 능력치 설정
    // =========================================

    private void Activate_Player(
        int SelectedNumber,
        int CharacterIndex)
    {
        players[SelectedNumber].CharacterName =
            Player_Names[CharacterIndex];


        players[SelectedNumber].Attck =
            Player_Atk[CharacterIndex];


        players[SelectedNumber].HHh =
            Player_HH[CharacterIndex];
    }


    // =========================================
    // Boss 설정
    // =========================================

    private void Activate_Boss(int BossIndex)
    {
        string name =
            Boss_Names[BossIndex];


        int hp =
            Boss_Hp[BossIndex];


        int atk =
            Boss_Atk[BossIndex];


        BossHP = hp;


        Debug.Log(
            "보스 체력 : " + hp
        );


        bossindex =
            BossIndex;
    }


    // =========================================
    // X 좌표 → Board X 좌표
    // =========================================

    public int Change_Coordinate_X_To_Board_X()
    {
        MainBoard =
            GameObject.Find("(0, 0)Board");


        MainX =
            MainBoard.transform.position.x;


        for (int i = 0; i < 9; i++)
        {
            if (Mathf.Approximately(
                    MainX + i,
                    player_x[selectedPlayer.CharacterIndex - 1]))
            {
                return i;
            }
        }


        return -1;
    }


    // =========================================
    // Y 좌표 → Board Y 좌표
    // =========================================

    public int Change_Coordinate_Y_To_Board_Y()
    {
        MainBoard =
            GameObject.Find("(0, 0)Board");


        MainY =
            MainBoard.transform.position.y;


        for (int i = 0; i < 9; i++)
        {
            if (Mathf.Approximately(
                    MainY + i,
                    player_y[selectedPlayer.CharacterIndex - 1]))
            {
                return i;
            }
        }


        return -1;
    }


    // =========================================
    // 모든 Player 공격
    // =========================================

    public void Player_All_Attck()
    {
        for (int i = 0; i < 3; i++)
        {
            BossHP -=
                players[i].Attck;
        }
    }
}