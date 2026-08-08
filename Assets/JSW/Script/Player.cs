using UnityEngine;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{

    public MovingPoint sc;
    private float SceneTime;
    private float LastClickTime = 0f;
    private bool IsDoubleClicked;
    private bool isSelected = false;
    private Vector3 originalScale;
    public float selectedScaleMultiplier = 1.2f;
    public float moveSpeed = 10f;
    private Turn tn;

    public float doubleClickThreshold = 0.3f; // 더블클릭으로 인정할 시간 간격(초)

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        originalScale = transform.localScale;
        sc = FindAnyObjectByType<MovingPoint>();
        tn = FindAnyObjectByType<Turn>();
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

                if (hit.collider.gameObject == gameObject)
                {
                    // 더블클릭 판정
                    if (SceneTime - LastClickTime <= doubleClickThreshold)
                    {
                        IsDoubleClicked = true;
                        LastClickTime = 0f; // 연속 트리플클릭 등으로 오작동하지 않도록 초기화
                        DoubleClick();
                        sc.Remove_MovingPlate();
                    }
                    else
                    {
                        IsDoubleClicked = false;
                        LastClickTime = SceneTime;

                        isSelected = true;
                        transform.localScale = originalScale * selectedScaleMultiplier;
                        sc.Remove_MovingPlate();
                        sc.Create_MovingPlate(7);
                    }
                }
                else if (isSelected && hit.collider.CompareTag("MovePoint"))
                {
                    Debug.Log("MovingPoint 클릭됨!");
                    targetPosition = hit.collider.bounds.center;
                    isMoving = true;
                    isSelected = false;
                    transform.localScale = originalScale;
                    sc.Remove_MovingPlate();

                    tn.TurnCount_Subtract(1);
                }
            }
        }

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
            }
        }
    }

    public void DoubleClick()
    {
        Debug.Log("더블클릭됨!");
    }

    public void Player_Attack()
    {

    }


}