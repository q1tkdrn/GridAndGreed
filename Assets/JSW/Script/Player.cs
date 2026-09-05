using System;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{

    public Plate mp;
    private float SceneTime;
    
    private bool IsDoubleClicked;
    private bool isSelected = false;
    private Vector3 originalScale;
    public float selectedScaleMultiplier = 1.2f;
    public float moveSpeed = 10f;
    private Turn tn;
    private GameManager gm;
    

    public float doubleClickThreshold = 0.3f; // 더블클릭으로 인정할 시간 간격(초)

    private Vector3 targetPosition;
    private bool isMoving = false;

    public string CharacterName;
    public int Attck;
    public int HHh;

    public static float[] player_x = new float[3];
    public static int[] player_board_x = new int[3];
    public static float[] player_y = new float[3];
    public static int[] player_board_y = new int[3];


    public static int ClickedCharacterIndex;

    public int CharacterIndex;
    private SpriteRenderer sr;








    void Start()
    {
        originalScale = transform.localScale;
        mp = FindAnyObjectByType<Plate>();
        tn = FindAnyObjectByType<Turn>();
        gm = FindAnyObjectByType<GameManager>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        SceneTime = Time.time;
        if (Input.GetMouseButtonDown(0) && Turn.TurnCount > 0)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.name == "Player1")
                {
                    ClickedCharacterIndex = 1;
                    player_x[0] = transform.position.x;
                    player_x[0] = transform.position.y;
                }
                if (hit.collider.gameObject.name == "Player2")
                {
                    ClickedCharacterIndex = 2;
                    player_x[1] = transform.position.x;
                    player_x[1] = transform.position.y;

                }
                if (hit.collider.gameObject.name == "Player3")
                {
                    ClickedCharacterIndex = 3;
                    player_x[2] = transform.position.x;
                    player_x[2] = transform.position.y;

                }
                if (hit.collider.gameObject == gameObject)
                {
                    mp.Check_Character(this);
                    // 더블클릭 판정
                    if (SceneTime - LastClickTime <= doubleClickThreshold)
                    {
                        IsDoubleClicked = true;
                        LastClickTime = 0f; // 연속 트리플클릭 등으로 오작동하지 않도록 초기화
                        DoubleClick();
                        mp.Remove_MovingPlate();
                    }
                    else
                    {
                        IsDoubleClicked = false;
                        LastClickTime = SceneTime;

                        isSelected = true;
                        transform.localScale = originalScale * selectedScaleMultiplier;
                        mp.Remove_MovingPlate();
                        mp.Create_MovingPlate(7);
                    }
                }
                else if (isSelected && hit.collider.CompareTag("MovePoint"))
                {
                    targetPosition = hit.collider.transform.position;
                    isMoving = true;
                    isSelected = false;
                    transform.localScale = originalScale;
                    mp.Remove_MovingPlate();
                    FlipX();
                }
            }
        }

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
                player_x[ClickedCharacterIndex - 1] = targetPosition.x;
                player_y[ClickedCharacterIndex - 1] = targetPosition.y;
                player_board_x[ClickedCharacterIndex -1] = gm.Change_Coordinate_X_To_Board_X();
                player_board_y[ClickedCharacterIndex - 1] = gm.Change_Coordinate_Y_To_Board_Y();

                Debug.Log(gm.Change_Coordinate_X_To_Board_X() + " , " + gm.Change_Coordinate_Y_To_Board_Y());
                tn.TurnCount_Subtract(1);

            }
        }
    }

    public void DoubleClick()
    {
        GameManager.BossHP = GameManager.BossHP - HHh;
        Debug.Log("현제 보스 HP : " + GameManager.BossHP);
        tn.TurnCount_Subtract(1);
    }

    private void FlipX()
    {
        int RandomNumber = UnityEngine.Random.Range(0, 2);
        if(RandomNumber == 1)
        {
            sr.flipX = true;
        }
        if(RandomNumber == 0)
        {
            sr.flipX = false;
        }

    }

    public void Check_Hitted_Player(int CharacterIndex)
    {
        ClickedCharacterIndex = CharacterIndex;
        player_x[CharacterIndex-1] = transform.position.x;
        player_x[CharacterIndex-1] = transform.position.y;
    }
}