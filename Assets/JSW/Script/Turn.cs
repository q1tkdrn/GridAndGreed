using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class Turn : MonoBehaviour
{
    private GameObject GameController;
    public static int TurnCount = 5;


    private void Boss_Start()
    {

    }

    public void TurnCount_Subtract(int x)
    {
        TurnCount -= x;
    }

    public void TurnCount_Add(int x)
    {
        TurnCount += x;
    }
}
