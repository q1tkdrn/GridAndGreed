using UnityEngine;

public class Items : MonoBehaviour
{
    private int ItemNumber;
    private Player[] players = new Player[3];
    private Player pl;

    void Start()
    {
        players[0] = GameObject.Find("Player1").GetComponent<Player>();
        players[1] = GameObject.Find("Player2").GetComponent<Player>();
        players[2] = GameObject.Find("Player3").GetComponent<Player>();
    }

    void Update()
    {

    }

    private void Old_Sword()
    {
        for (int i = 0; i < 3; i++) {
            players[i].Attck += 2;
        }
    }

    private void ArroBottle()
    {
        GameManager.BossHP -= 3;

    }

    private void Activate_Items(int ItemIndex)
    {
        switch (ItemIndex)
        {
            case 0: break;
        }
     }

    public void Use_Item(int ItemIndex)
    {
        switch (ItemIndex)
        {
            case 0: Old_Sword(); break;
            case 1: ArroBottle(); break;
        }
    }
}
