using UnityEngine;

public class ClickSelectAndMove2D : MonoBehaviour
{
    private bool isSelected = false;
    private Vector3 originalScale;
    public float selectedScaleMultiplier = 1.2f;
    public float moveSpeed = 10f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                // 오브젝트 클릭 시 선택 효과
                if (hit.collider.gameObject == gameObject)
                {
                    isSelected = true;
                    transform.localScale = originalScale * selectedScaleMultiplier;
                }
                // 선택된 상태에서 "Board" 태그를 가진 오브젝트 클릭 시 이동
                else if (isSelected && hit.collider.CompareTag("Board"))
                {
                    // Board 오브젝트의 중심 좌표로 이동
                    targetPosition = hit.collider.bounds.center;
                    isMoving = true;
                    isSelected = false;
                    transform.localScale = originalScale;
                }
            }
        }

        // 이동 처리
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
            }
        }
    }
}
