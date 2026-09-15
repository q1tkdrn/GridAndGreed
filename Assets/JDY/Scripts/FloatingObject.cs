using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    private Vector3 startPosition;

    [SerializeField] private float moveDistance = 10f;
    [SerializeField] private float moveSpeed = 2f;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;

        transform.localPosition = startPosition + Vector3.down * (offset + moveDistance) / 2f;
    }
}