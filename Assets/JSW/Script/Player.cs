using UnityEngine;

public class Player : MonoBehaviour
{
    public MovingPoint sc;
    private float LastClickTime = 0f;
    private bool IsDoubleClicked;
    private bool isSelected = false;
    private Vector3 originalScale;
    public float selectedScaleMultiplier = 1.2f;
    public float moveSpeed = 10f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        originalScale = transform.localScale;
        sc = FindAnyObjectByType<MovingPoint>();
    }

    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if(Time.time - LastClickTime < 0.3f)
            {
                Player_Attack();
            }
            LastClickTime = Time.time;
            if (hit.collider != null)
            {
                if (hit.collider.gameObject == gameObject)
                {
                    sc.Create_MovingPlate(3, 3);
                    isSelected = true;
                    transform.localScale = originalScale * selectedScaleMultiplier;
                }
                else if (isSelected && hit.collider.CompareTag("Board"))
                {                    targetPosition = hit.collider.bounds.center;
                    isMoving = true;
                    isSelected = false;
                    transform.localScale = originalScale;
                    sc.Remove_MovingPlate();
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

    public void Player_Attack()
    {
        
    }
}
