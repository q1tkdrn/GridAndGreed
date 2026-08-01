using System.Collections;
using UnityEngine;

public class Boss : MonoBehaviour
{
    public string Boss_Name;
    public int Boss_Hp;
    public int Boss_Atk;

    public void Activate()
    {
        switch (this.Boss_Name)
        {
            case "1":  Debug.Log("보스 1 출현!"); break;
        }
    }
}
