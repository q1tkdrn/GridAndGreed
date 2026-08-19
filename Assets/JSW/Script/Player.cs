using System;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{

    public MovingPoint mp;
    private float SceneTime;
    private float LastClickTime = 0f;
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
    public static float[] player_y = new float[3];

    public static int ClickedCharacterIndex;

    public int CharacterIndex;








    void Start()
    {
        originalScale = transform.localScale;
        mp = FindAnyObjectByType<MovingPoint>();
        tn = FindAnyObjectByType<Turn>();
        gm = FindAnyObjectByType<GameManager>();
    }

    void Update()
    {
        SceneTime = Time.time;
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.name == "Player1")
                {
                    ClickedCharacterIndex = 1;
                }
                if (hit.collider.gameObject.name == "Player2")
                {
                    ClickedCharacterIndex = 2;
                }
                if (hit.collider.gameObject.name == "Player3")
                {
                    ClickedCharacterIndex = 3;
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
                    targetPosition = hit.collider.bounds.center;
                    isMoving = true;
                    isSelected = false;
                    transform.localScale = originalScale;
                    mp.Remove_MovingPlate();
                }
            }
        }

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
                player_x[ClickedCharacterIndex - 1] = targetPosition.x;
                player_y[ClickedCharacterIndex - 1] = targetPosition.y;
                Debug.Log(gm.Change_Coordinate_X_To_Board_X() + " , " + gm.Change_Coordinate_Y_To_Board_Y());


            }
        }
    }

    private void DoubleClick()
    {
        GameManager.BossHP = GameManager.BossHP - Attck;
        Debug.Log("현제 보스 HP : " + GameManager.BossHP);
    }


}