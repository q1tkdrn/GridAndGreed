using UnityEngine;

public class Items : MonoBehaviour
{
    private GameObject[] Item = new GameObject[3];

    private List<int> Public_ItemNumber = new List<int>();

    public Sprite[] Item_Sprite;

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
    
    public void Activate_Items(int ItemNumber)
    {
        Item[ItemNumber] = GameObject.Find("Item"+(ItemNumber+1));
        SpriteRenderer sr = Item[ItemNumber].GetComponent<SpriteRenderer>();
        sr.sprite = Item_Sprite[ItemNumber];
       
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
