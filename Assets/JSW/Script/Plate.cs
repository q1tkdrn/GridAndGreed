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
    //대저택 보스
    public void Player_hit(int x, int y)
    {
        for (int i = 0; i < 3; i++)
        {
            if (x == Player.player_board_x[i])
            {
                if (y == Player.player_board_y[i])
                {
                    GameManager.PlayerHP -= 5;
                    Debug.Log("현제 플레이어 HP : " + GameManager.PlayerHP);
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
        Border
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
}
