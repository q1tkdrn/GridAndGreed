using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Net;
using UnityEngine.UIElements.Experimental;

public class MovingPoint : MonoBehaviour
{
    public GameObject MovingPoint1;
    public GameObject PlayerMan;
    private List<GameObject> spawnedList = new List<GameObject>();
    float Player_x;
    float Player_y;



    void Update()
    {
        Player_x = PlayerMan.transform.position.x;
        Player_y = PlayerMan.transform.position.y;
    }

    public void Create_MovingPlate(int n)
    {
        int start = -(n / 2); // n=1→0, n=2→-1, n=3→-1, n=4→-2 ...

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                int dx = start + i;
                int dy = start + j;

                if (dx == 0 && dy == 0) continue; // 플레이어 자기 칸 제외

                float targetX = Player_x + dx;
                float targetY = Player_y + dy;

                // 보드 범위를 벗어나면 생성하지 않음
                if (targetX < GameManager.BoardMinX || targetX > GameManager.BoardMaxX ||
                    targetY < GameManager.BoardMinY || targetY > GameManager.BoardMaxY)
                {
                    continue;
                }

                GameObject obj = Instantiate(
                    MovingPoint1,
                    new Vector2(targetX, targetY),
                    Quaternion.identity
                );
                spawnedList.Add(obj);
            }
        }
    }

    public void Remove_MovingPlate()
    {
        foreach (GameObject obj in spawnedList)
        {
            if (obj != null)
            {
                Destroy(obj, 0f);
            }
        }
        spawnedList.Clear();
    }
}