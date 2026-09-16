using UnityEngine;
public class FloatingObject : MonoBehaviour
{
    private Vector3 startPosition;
    private float currentTime = 0f;

    [SerializeField] private float moveDistance = 10f;
    [SerializeField] private float moveSpeed = 7f;

    void Awake()
    {
        startPosition = transform.localPosition;
    }
    void OnEnable()
    {
        transform.localPosition = startPosition;
        currentTime = 0f;
    }
    void Update()
    {
        currentTime += Time.deltaTime;

        float offset = Mathf.PingPong(currentTime * moveSpeed, moveDistance);

        transform.localPosition = startPosition + Vector3.down * offset;
    }
}