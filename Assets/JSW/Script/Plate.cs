using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Net;
using UnityEngine.UIElements.Experimental;

public class Plate : MonoBehaviour
{
    private GameObject MovingPoint;
    private GameObject BossPlate;
    private List<GameObject> Player_spawnedList = new List<GameObject>();
    private List<GameObject> Boss_spawnedList = new List<GameObject>();
    float Player_x;
    float Player_y;
    SpriteRenderer sr;
    private Turn tn;
    private bool bossAttackDamageApplied;

    private void Awake()
    {
        if (FindAnyObjectByType<BoardPanel>() != null)
        {
            enabled = false;
        }
    }

    void Start()
    {
        MovingPoint = GameObject.Find("MovePoint");
        BossPlate = GameObject.Find("BossPlate");
        sr = GetComponent<SpriteRenderer>();
        tn = FindAnyObjectByType<Turn>();
    }
    public void Check_Character(Player player)
    {
        if (player != null)
        { 
            Player_x = player.transform.position.x;
            Player_y = player.transform.position.y;
        }
    }

    public void Create_MovingPlate(int n)
    {
        int start = -(n / 2);

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                int dx = start + i;
                int dy = start + j;

                if (dx == 0 && dy == 0) continue; // 플레이어 자기 칸 제외

                float targetX = Player_x + dx;
                float targetY = Player_y + dy;

                int boardX = Mathf.RoundToInt(targetX - GameManager.BoardMinX);
                int boardY = Mathf.RoundToInt(targetY - GameManager.BoardMinY);

                bool isOccupied = false;

                // 보드 범위를 벗어나면 생성하지 않음
                if (targetX < GameManager.BoardMinX || targetX > GameManager.BoardMaxX ||
                    targetY < GameManager.BoardMinY || targetY > GameManager.BoardMaxY)
                {
                    continue;
                }
                for (int h = 0; h < 3; h++)
                {
                    if (Player.player_board_x[h] == boardX &&
                        Player.player_board_y[h] == boardY)
                    {
                        isOccupied = true;
                        break;
                    }
                }

                if (isOccupied)
                {
                    continue;
                } 



                GameObject obj = Instantiate(
                    MovingPoint,
                    new Vector2(targetX, targetY),
                    Quaternion.identity
                );
                Player_spawnedList.Add(obj);
            }
        }
    }

    public void Remove_MovingPlate()
    {
        foreach (GameObject obj in Player_spawnedList)
        {
            if (obj != null)
            {
                Destroy(obj, 0f);
            }
        }
        Player_spawnedList.Clear();
    }

    public void Boss_RemovePlate()
    {
        foreach (GameObject obj in Boss_spawnedList)
        {
            if (obj != null)
            {
                Destroy(obj, 0f);
            }
        }
        Boss_spawnedList.Clear();
        tn.Turn_On();
    }

 //대저택 보스       
    public void Boss_PlateCreate1()
    {
        bossAttackDamageApplied = false;
        for (int a = 0; a < 3; a++)
        {
            for (int b = 0; b < 3; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }

        for (int a = 6; a < 9; a++)
        {
            for (int b = 6; b < 9; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }

        for (int a = 3; a < 6; a++)
        {
            for (int b = 3; b < 6; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }

        for (int a = 0; a < 3; a++)
        {
            for (int b = 0; b < 3; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }

        for (int a = 6; a < 9; a++)
        {
            for (int b = 0; b < 3; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }

        for (int a = 0; a < 3; a++)
        {
            for (int b = 6; b < 9; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        Invoke("Boss_RemovePlate", 1f);
    }

    public void Boss_PlateCreate2()
    {
        bossAttackDamageApplied = false;
        for (int a = 3; a < 6; a++)
        {
            for (int b = 0; b < 3; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        for (int a = 0; a < 3; a++)
        {
            for (int b = 3; b < 6; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        for (int a = 3; a < 6; a++)
        {
            for (int b = 6; b < 9; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        for (int a = 6; a < 9; a++)
        {
            for (int b = 3; b < 6; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        Invoke("Boss_RemovePlate", 1f);

    }

    public void Boss_PlateCreate3()
    {
        bossAttackDamageApplied = false;
        for (int a = 0; a < 3; a++)
        {
            for (int b = 0; b < 9; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        for (int a = 6; a < 9; a++)
        {
            for (int b = 0; b < 9; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        Invoke("Boss_RemovePlate", 1f);

    }

    public void Boss_PlateCreate4()
    {
        bossAttackDamageApplied = false;
        for (int a = 3; a < 6; a++)
        {
            for (int b = 0; b < 9; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        Invoke("Boss_RemovePlate", 1f);

    }

    public void Boss_PlateCreate5()
    {
        bossAttackDamageApplied = false;
        for (int a = 2; a < 7; a++)
        {
            for (int b = 2; b < 7; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        Invoke("Boss_RemovePlate", 1f);

    }

    public void Boss_PlateCreate6()
    {
        bossAttackDamageApplied = false;
        for (int a = 0; a < 9; a++)
        {
            for (int b = 0; b < 2; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        for (int a = 0; a < 2; a++)
        {
            for (int b = 2; b < 7; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        for (int a = 7; a < 9; a++)
        {
            for (int b = 2; b < 7; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        for (int a = 0; a < 9; a++)
        {
            for (int b = 7; b < 9; b++)
            {
                GameObject obj = Instantiate(BossPlate,
                GameManager.position[a, b], Quaternion.identity);
                Boss_spawnedList.Add(obj);
                Player_hit(a, b);
            }
        }
        Invoke("Boss_RemovePlate", 1f);

    }
    // 광장: 이미지의 밝은 칸이 공격 영역. 각 함수는 한 턴의 공격만 생성한다.
    public void Boss_CreatePlate_Plaza_A1() => Boss_CreatePlate(BattlePatternRules.Pattern.PlazaA1);
    public void Boss_CreatePlate_Plaza_A2() => Boss_CreatePlate(BattlePatternRules.Pattern.PlazaA2);
    public void Boss_CreatePlate_Plaza_B1() => Boss_CreatePlate(BattlePatternRules.Pattern.PlazaB1);
    public void Boss_CreatePlate_Plaza_B2() => Boss_CreatePlate(BattlePatternRules.Pattern.PlazaB2);
    public void Boss_CreatePlate_Plaza_B3() => Boss_CreatePlate(BattlePatternRules.Pattern.PlazaB3);
    public void Boss_CreatePlate_Plaza_C1() => Boss_CreatePlate(BattlePatternRules.Pattern.PlazaC1);
    public void Boss_CreatePlate_Plaza_C2() => Boss_CreatePlate(BattlePatternRules.Pattern.PlazaC2);
    public void Boss_CreatePlate_Plaza_C3() => Boss_CreatePlate(BattlePatternRules.Pattern.PlazaC3);
    public void Boss_CreatePlate_Plaza_C4() => Boss_CreatePlate(BattlePatternRules.Pattern.PlazaC4);
    public void Boss_CreatePlate_Plaza_C5() => Boss_CreatePlate(BattlePatternRules.Pattern.PlazaC5);

    // 성당: A 2단계, B 2단계, C 3단계. 밝은 칸에 공격 Plate를 생성한다.
    public void Boss_CreatePlate_Cathedral_A1() => Boss_CreatePlate(BattlePatternRules.Pattern.CathedralA1);
    public void Boss_CreatePlate_Cathedral_A2() => Boss_CreatePlate(BattlePatternRules.Pattern.CathedralA2);
    public void Boss_CreatePlate_Cathedral_B1() => Boss_CreatePlate(BattlePatternRules.Pattern.CathedralB1);
    public void Boss_CreatePlate_Cathedral_B2() => Boss_CreatePlate(BattlePatternRules.Pattern.CathedralB2);
    public void Boss_CreatePlate_Cathedral_C1() => Boss_CreatePlate(BattlePatternRules.Pattern.CathedralC1);
    public void Boss_CreatePlate_Cathedral_C2() => Boss_CreatePlate(BattlePatternRules.Pattern.CathedralC2);
    public void Boss_CreatePlate_Cathedral_C3() => Boss_CreatePlate(BattlePatternRules.Pattern.CathedralC3);

    public void Boss_CreatePlate_Door_A1() => Boss_CreatePlate(BattlePatternRules.Pattern.DoorA1);
    public void Boss_CreatePlate_Door_A2() => Boss_CreatePlate(BattlePatternRules.Pattern.DoorA2);
    public void Boss_CreatePlate_Door_B1() => Boss_CreatePlate(BattlePatternRules.Pattern.DoorB1);
    public void Boss_CreatePlate_Door_B2() => Boss_CreatePlate(BattlePatternRules.Pattern.DoorB2);
    public void Boss_CreatePlate_Door_B3() => Boss_CreatePlate(BattlePatternRules.Pattern.DoorB3);
    public void Boss_CreatePlate_Door_B4() => Boss_CreatePlate(BattlePatternRules.Pattern.DoorB4);
    public void Boss_CreatePlate_Door_B5() => Boss_CreatePlate(BattlePatternRules.Pattern.DoorB5);
    public void Boss_CreatePlate_Door_C1() => Boss_CreatePlate(BattlePatternRules.Pattern.DoorC1);
    public void Boss_CreatePlate_Door_C2() => Boss_CreatePlate(BattlePatternRules.Pattern.DoorC2);
    public void Boss_CreatePlate_Door_C3() => Boss_CreatePlate(BattlePatternRules.Pattern.DoorC3);
    public void Boss_CreatePlate_Door_C4() => Boss_CreatePlate(BattlePatternRules.Pattern.DoorC4);
    public void Boss_CreatePlate_Basement_A1() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementA1);
    public void Boss_CreatePlate_Basement_A2() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementA2);
    public void Boss_CreatePlate_Basement_A3() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementA3);
    public void Boss_CreatePlate_Basement_A4() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementA4);
    public void Boss_CreatePlate_Basement_B1() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementB1);
    public void Boss_CreatePlate_Basement_B2() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementB2);
    public void Boss_CreatePlate_Basement_B3() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementB3);
    public void Boss_CreatePlate_Basement_B4() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementB4);
    public void Boss_CreatePlate_Basement_B5() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementB5);
    public void Boss_CreatePlate_Basement_C1() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementC1);
    public void Boss_CreatePlate_Basement_C2() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementC2);
    public void Boss_CreatePlate_Basement_C3() => Boss_CreatePlate(BattlePatternRules.Pattern.BasementC3);
    public void Boss_CreatePlate_Library_A1() => Boss_CreatePlate(BattlePatternRules.Pattern.LibraryA1);
    public void Boss_CreatePlate_Library_A2() => Boss_CreatePlate(BattlePatternRules.Pattern.LibraryA2);
    public void Boss_CreatePlate_Library_B1() => Boss_CreatePlate(BattlePatternRules.Pattern.LibraryB1);
    public void Boss_CreatePlate_Library_B2() => Boss_CreatePlate(BattlePatternRules.Pattern.LibraryB2);
    public void Boss_CreatePlate_Library_C1() => Boss_CreatePlate(BattlePatternRules.Pattern.LibraryC1);
    public void Boss_CreatePlate_Library_C2() => Boss_CreatePlate(BattlePatternRules.Pattern.LibraryC2);
    public void Boss_CreatePlate_Training_A1() => Boss_CreatePlate(BattlePatternRules.Pattern.TrainingA1);
    public void Boss_CreatePlate_Training_A2() => Boss_CreatePlate(BattlePatternRules.Pattern.TrainingA2);
    public void Boss_CreatePlate_Training_A3() => Boss_CreatePlate(BattlePatternRules.Pattern.TrainingA3);
    public void Boss_CreatePlate_Training_B1() => Boss_CreatePlate(BattlePatternRules.Pattern.TrainingB1);
    public void Boss_CreatePlate_Training_B2() => Boss_CreatePlate(BattlePatternRules.Pattern.TrainingB2);
    public void Boss_CreatePlate_Training_C1() => Boss_CreatePlate(BattlePatternRules.Pattern.TrainingC1);
    public void Boss_CreatePlate_Training_C2() => Boss_CreatePlate(BattlePatternRules.Pattern.TrainingC2);

    private void Boss_CreatePlate(BattlePatternRules.Pattern pattern)
    {
        bossAttackDamageApplied = false;
        foreach (var cell in BattlePatternRules.GetDangerCells(pattern))
        {
            // UI는 위에서 y=0, 월드 보드는 아래에서 y=0이다.
            int worldY = BattlePatternRules.BoardSize - 1 - cell.y;
            var obj = Instantiate(BossPlate, GameManager.position[cell.x, worldY], Quaternion.identity);
            Boss_spawnedList.Add(obj);
            Player_hit(cell.x, worldY);
        }
        Invoke(nameof(Boss_RemovePlate), 1f);
    }

    //대저택 보스
    public void Player_hit(int x, int y)
    {
        if (bossAttackDamageApplied) return;
        for (int i = 0; i < 3; i++)
        {
            if (x == Player.player_board_x[i])
            {
                if (y == Player.player_board_y[i])
                {
                    GameManager.PlayerHP -= 5;
                    bossAttackDamageApplied = true;
                    Debug.Log("현제 플레이어 HP : " + GameManager.PlayerHP);
                    return;
                }
            }
        }
    }
    
}

/// <summary>
/// JSW 전투 규칙의 단일 원본입니다. 월드 오브젝트와 UI 보드 모두 이 좌표 규칙을 공유합니다.
/// </summary>
public static class BattlePatternRules
{
    public const int BoardSize = 9;

    public enum Pattern
    {
        Corners,
        Cross,
        SideColumns,
        MiddleRows,
        CenterSquare,
        Border,
        PlazaA1, PlazaA2,
        PlazaB1, PlazaB2, PlazaB3,
        PlazaC1, PlazaC2, PlazaC3, PlazaC4, PlazaC5,
        MansionA1, MansionA2,
        None,
        CathedralA1, CathedralA2,
        CathedralB1, CathedralB2,
        CathedralC1, CathedralC2, CathedralC3,
        DoorA1, DoorA2, DoorB1, DoorB2, DoorB3, DoorB4, DoorB5, DoorC1, DoorC2, DoorC3, DoorC4,
        BasementA1, BasementA2, BasementA3, BasementA4, BasementB1, BasementB2, BasementB3, BasementB4, BasementB5, BasementC1, BasementC2, BasementC3,
        LibraryA1, LibraryA2, LibraryB1, LibraryB2, LibraryC1, LibraryC2,
        TrainingA1, TrainingA2, TrainingA3, TrainingB1, TrainingB2, TrainingC1, TrainingC2
    }

    public static HashSet<Vector2Int> GetDangerCells(Pattern pattern)
    {
        var cells = new HashSet<Vector2Int>();

        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                var isDangerous = pattern switch
                {
                    Pattern.Corners => (x / 3 != 1) && (y / 3 != 1),
                    Pattern.Cross => x / 3 == 1 || y / 3 == 1,
                    Pattern.SideColumns => x < 3 || x > 5,
                    Pattern.MiddleRows => x is >= 3 and <= 5,
                    Pattern.CenterSquare => x is >= 2 and <= 6 && y is >= 2 and <= 6,
                    Pattern.Border => x < 2 || x > 6 || y < 2 || y > 6,
                    Pattern.PlazaA1 => y < 3,
                    Pattern.PlazaA2 => x is >= 2 and <= 6 && y is >= 2 and <= 6,
                    Pattern.PlazaB1 => x % 3 == 1,
                    Pattern.PlazaB2 => y % 3 == 1,
                    Pattern.PlazaB3 => x is >= 3 and <= 5,
                    Pattern.PlazaC1 => x == 0 || x == 8,
                    Pattern.PlazaC2 => x < 2 || x > 6,
                    Pattern.PlazaC3 => x < 3 || x > 5,
                    Pattern.PlazaC4 => x != 4,
                    Pattern.PlazaC5 => x is >= 3 and <= 5,
                    Pattern.MansionA1 => (x / 3 != 1 && y / 3 != 1) || (x / 3 == 1 && y / 3 == 1),
                    Pattern.MansionA2 => (x / 3 == 1) != (y / 3 == 1),
                    Pattern.CathedralA1 => x == y || x + y == 8,
                    Pattern.CathedralA2 => x == 4 || y == 4,
                    // 중앙 3x3 테두리 8칸만 안전하며, 정중앙 한 칸은 공격한다.
                    Pattern.CathedralB1 => x < 3 || x > 5 || y < 3 || y > 5 || (x == 4 && y == 4),
                    Pattern.CathedralB2 => x == 0 || x == 8 || y == 0 || y == 8,
                    Pattern.CathedralC1 => y is >= 3 and <= 5,
                    Pattern.CathedralC2 => y < 3,
                    Pattern.CathedralC3 => y > 5,
                    Pattern.DoorA1 => x < 3 || x > 5 || y < 3 || y > 5 || (x == 4 && y == 4),
                    Pattern.DoorA2 => x < 2 || x > 6 || y < 2 || y > 6,
                    Pattern.DoorB1 => x == 0 || x == 8,
                    Pattern.DoorB2 => x < 2 || x > 6,
                    Pattern.DoorB3 => x < 3 || x > 5,
                    Pattern.DoorB4 => x != 4,
                    Pattern.DoorB5 => x is >= 3 and <= 5,
                    Pattern.DoorC1 => (x / 3 != 1 && y / 3 != 1) || (x / 3 == 1 && y / 3 == 1),
                    Pattern.DoorC2 => (x / 3 == 1) != (y / 3 == 1),
                    Pattern.DoorC3 => x < 3 || x > 5,
                    Pattern.DoorC4 => x is >= 3 and <= 5,
                    Pattern.BasementA1 => x is >= 3 and <= 5,
                    Pattern.BasementA2 => x < 3,
                    Pattern.BasementA3 => x is >= 3 and <= 5,
                    Pattern.BasementA4 => x > 5,
                    Pattern.BasementB1 => x == 0 || x == 8,
                    Pattern.BasementB2 => x < 2 || x > 6,
                    Pattern.BasementB3 => x < 3 || x > 5,
                    Pattern.BasementB4 => x != 4,
                    Pattern.BasementB5 => x is >= 3 and <= 5,
                    Pattern.BasementC1 => x % 4 != 0,
                    Pattern.BasementC2 => y % 4 != 0,
                    Pattern.BasementC3 => x is >= 2 and <= 6 && y is >= 2 and <= 6,
                    Pattern.LibraryA1 => x % 4 != 0,
                    Pattern.LibraryA2 => y % 4 != 0,
                    Pattern.LibraryB1 => x == y || x + y == 8,
                    Pattern.LibraryB2 => x == 4 || y == 4,
                    Pattern.LibraryC1 => x == 0 || x == 8 || y == 0 || y == 8,
                    Pattern.LibraryC2 => x is >= 1 and <= 7 && y is >= 1 and <= 7,
                    Pattern.TrainingA1 => x == y || x + y == 8,
                    Pattern.TrainingA2 => x == 4 || y == 4,
                    Pattern.TrainingA3 => x != 4,
                    Pattern.TrainingB1 => (x / 3 != 1 && y / 3 != 1) || (x / 3 == 1 && y / 3 == 1),
                    Pattern.TrainingB2 => (x / 3 == 1) != (y / 3 == 1),
                    Pattern.TrainingC1 => x is >= 2 and <= 6 && y is >= 2 and <= 6,
                    Pattern.TrainingC2 => x < 2 || x > 6 || y < 2 || y > 6,
                    _ => false
                };

                if (isDangerous)
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }
        }

        return cells;
    }

    public static Pattern GetDefaultPattern(int turnCount)
    {
        return (Pattern)((turnCount - 1) % 6);
    }

    // 전투마다 별도 인스턴스를 사용한다. 한 묶음이 끝난 뒤에만 다시 추첨한다.
    public sealed class MansionSequence : PatternSequence
    {
        public MansionSequence() : base(new[]
        {
            new[] { Pattern.MansionA1, Pattern.MansionA2 },
            new[] { Pattern.SideColumns, Pattern.MiddleRows },
            new[] { Pattern.CenterSquare, Pattern.Border }
        }) { }
    }

    public sealed class PlazaSequence : PatternSequence
    {
        public PlazaSequence() : base(new[]
        {
            new[] { Pattern.PlazaA1, Pattern.PlazaA2 },
            new[] { Pattern.PlazaB1, Pattern.PlazaB2, Pattern.PlazaB3 },
            new[] { Pattern.PlazaC1, Pattern.PlazaC2, Pattern.PlazaC3, Pattern.PlazaC4, Pattern.PlazaC5 }
        }) { }
    }

    public sealed class CathedralSequence : PatternSequence
    {
        public CathedralSequence() : base(new[]
        {
            new[] { Pattern.CathedralA1, Pattern.CathedralA2 },
            new[] { Pattern.CathedralB1, Pattern.CathedralB2 },
            new[] { Pattern.CathedralC1, Pattern.CathedralC2, Pattern.CathedralC3 }
        }) { }
    }

    public sealed class DoorSequence : PatternSequence
    {
        public DoorSequence() : base(new[]
        {
            new[] { Pattern.DoorA1, Pattern.DoorA2 },
            new[] { Pattern.DoorB1, Pattern.DoorB2, Pattern.DoorB3, Pattern.DoorB4, Pattern.DoorB5 },
            new[] { Pattern.DoorC1, Pattern.DoorC2, Pattern.DoorC3, Pattern.DoorC4 }
        }) { }
    }

    public sealed class BasementSequence : PatternSequence
    {
        public BasementSequence() : base(new[]
        {
            new[] { Pattern.BasementA1, Pattern.BasementA2, Pattern.BasementA3, Pattern.BasementA4 },
            new[] { Pattern.BasementB1, Pattern.BasementB2, Pattern.BasementB3, Pattern.BasementB4, Pattern.BasementB5 },
            new[] { Pattern.BasementC1, Pattern.BasementC2, Pattern.BasementC3 }
        }) { }
    }

    public sealed class LibrarySequence : PatternSequence
    {
        public LibrarySequence() : base(new[]
        {
            new[] { Pattern.LibraryA1, Pattern.LibraryA2 },
            new[] { Pattern.LibraryB1, Pattern.LibraryB2 },
            new[] { Pattern.LibraryC1, Pattern.LibraryC2 }
        }) { }
    }

    public sealed class TrainingSequence : PatternSequence
    {
        public TrainingSequence() : base(new[]
        {
            new[] { Pattern.TrainingA1, Pattern.TrainingA2, Pattern.TrainingA3 },
            new[] { Pattern.TrainingB1, Pattern.TrainingB2 },
            new[] { Pattern.TrainingC1, Pattern.TrainingC2 }
        }) { }
    }

    public abstract class PatternSequence
    {
        private readonly Pattern[][] Groups;

        protected PatternSequence(Pattern[][] groups)
        {
            Groups = groups;
        }

        private int group = -1;
        private int step;

        public void Reset()
        {
            group = -1;
            step = 0;
        }

        public Pattern Next()
        {
            if (group < 0 || step >= Groups[group].Length)
            {
                group = Random.Range(0, Groups.Length);
                step = 0;
            }
            return Groups[group][step++];
        }
    }
}
