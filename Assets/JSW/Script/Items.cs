using UnityEngine;

public class Items : MonoBehaviour
{
    private int ItemNumber;


    void Start()
    {
        Player pl = GetComponenet<Player>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit != null)
            {
                ItemNumber = int.Parse(hit.collider.gameObject.name);
                Use_Item(ItemNumber);
            }
        }

    }

    private void Old_Sword()
    {
        for (int i = 0; i < 3; i++) {
            Player.Attck[i] += 2;
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
            case 0:
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
