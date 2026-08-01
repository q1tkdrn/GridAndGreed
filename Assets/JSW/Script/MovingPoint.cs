using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MovingPoint : MonoBehaviour
{
    public GameObject MovingPoint1;
    private GameObject PlayerMan;
    private List<GameObject> spawnedList = new List<GameObject>();
    float Player_x;
    float Player_y;
    void Awake()
    {
       
        PlayerMan = GameObject.Find("Player");
  
    }

    void Update()
    {
        Player_x = PlayerMan.transform.position.x;
        Player_y = PlayerMan.transform.position.y;
    }

    public void Create_MovingPlate(int x, int y)
    {
        for(int i = 1; i<x; i++)
        {
            GameObject obj = Instantiate(MovingPoint1, new Vector2(Player_x + i, Player_y), Quaternion.identity);
            spawnedList.Add(obj);
        }
        for (int i = 1; i < x; i++)
        {
            GameObject obj = Instantiate(MovingPoint1, new Vector2(Player_x - i, Player_y), Quaternion.identity);
            spawnedList.Add(obj);
        }
        for (int i = 1; i < y; i++)
        {
            GameObject obj = Instantiate(MovingPoint1, new Vector2(Player_x, Player_y + i), Quaternion.identity);
            spawnedList.Add(obj);
        }
        for (int i = 1; i < y; i++)
        {
            GameObject obj = Instantiate(MovingPoint1, new Vector2(Player_x, Player_y - i), Quaternion.identity);
            spawnedList.Add(obj);
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
