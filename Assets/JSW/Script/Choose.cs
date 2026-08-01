using System.Runtime.CompilerServices;
using UnityEditor.Rendering;
using UnityEngine;

public class Choose : MonoBehaviour
{
    private int BossCount = 0;
    private int PlayerCount = 0;
    void Update()
    { 
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                switch (hit.collider.gameObject.name)
                {
                    case "Boss1": ChoosePlayer("Boss1"); break;
                    case "Boss2": ChoosePlayer("Boss2"); break;
                    case "Boss3": ChoosePlayer("Boss3"); break;
                    case "Player1": ChoosePlayer("Player1"); break;
                    case "Player2": ChoosePlayer("Player2"); break;
                    case "Player3": ChoosePlayer("Player3"); break;
                    case "Player4": ChoosePlayer("Player4"); break;
                }
            }
        }
    }
    public void ChoosePlayer(string name)
    {
        if(BossCount >= 2)
        {
            Debug.Log("이미 보스는 선택하셨습니다!");
        }
        if (PlayerCount >= 4)
        {
            Debug.Log("이미 플레이어는 선택하셨습니다!");
        }
        if (name == "Boss1" || name == "Boss2" || name == "Boss3")
        {
            BossCount++;
        }
        if (name == "Player1" || name == "Player2" || name == "Player3" || name == "Player4")
        {
            PlayerCount++;
        }
    }
}
      
    

