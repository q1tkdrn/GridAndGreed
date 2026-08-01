using UnityEngine;

public class GameManager : MonoBehaviour
{
    private GameObject Man;
    private GameObject Boss;
    private GameObject[] Player = new GameObject[3];


    private GameObject BossCreate(string name, int Hp, int Atk)
    {
        GameObject obj = Instantiate(Man, new Vector2(0, 0), Quaternion.identity);
        Boss boss = GetComponent<Boss>();
        boss.Boss_Name = name;
        boss.Boss_Hp = Hp;
        boss.Boss_Atk = Atk;
        boss.Activate();
        return obj;
    }
}
