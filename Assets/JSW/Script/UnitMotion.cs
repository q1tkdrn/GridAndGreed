using UnityEngine;
using UnityEngine.UI;

// 판 위 캐릭터의 작은 움직임: 숨쉬기(항상), 이동할 때 깡충, 맞으면 흔들림.
// RectTransform(위치·크기)은 건드리지 않고 그려지는 모양만 바꾸므로,
// 드래그 이동·공격/심판 연출·클릭 판정과 서로 겹치지 않는다.
[RequireComponent(typeof(Graphic))]
[DisallowMultipleComponent]
public sealed class UnitMotion : BaseMeshEffect
{
    [Header("숨쉬기")]
    [SerializeField, Min(0.1f)] private float breathPeriod = 1.8f;
    [SerializeField, Range(0f, 0.1f)] private float breathAmount = 0.025f; // 세로로 늘어나는 비율 (은은하게)

    [Header("깡충 (이동)")]
    [SerializeField, Min(0.05f)] private float hopDuration = 0.28f;
    [SerializeField, Min(0f)] private float hopHeight = 12f;
    [SerializeField, Min(0.01f)] private float landDuration = 0.12f;

    [Header("흔들림 (피격)")]
    [SerializeField, Min(0.05f)] private float shakeDuration = 0.35f;
    [SerializeField, Min(0f)] private float shakeAmount = 6f;

    private float phase;
    private float hopTime = -1f;
    private float shakeTime = -1f;

    /// <summary>그림에 움직임을 붙인다(이미 있으면 그대로 돌려준다).</summary>
    public static UnitMotion For(Graphic target)
    {
        if (target == null) return null;
        var motion = target.GetComponent<UnitMotion>();
        if (motion == null) motion = target.gameObject.AddComponent<UnitMotion>();
        return motion;
    }

    public void Hop() => hopTime = 0f;
    public void Shake() => shakeTime = 0f;

    protected override void Awake()
    {
        base.Awake();
        // 셋이 똑같이 숨 쉬면 기계처럼 보이므로 시작 박자와 빠르기를 조금씩 다르게 한다.
        phase = Random.value * Mathf.PI * 2f;
        breathPeriod *= Random.Range(0.9f, 1.1f);
    }

    protected override void OnDisable()
    {
        hopTime = shakeTime = -1f;
        base.OnDisable();
    }

    private void Update()
    {
        if (graphic == null) return;
        float dt = Time.deltaTime;
        phase += dt * Mathf.PI * 2f / breathPeriod;
        if (hopTime >= 0f && (hopTime += dt) > hopDuration + landDuration) hopTime = -1f;
        if (shakeTime >= 0f && (shakeTime += dt) > shakeDuration) shakeTime = -1f;
        graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        var rect = graphic.rectTransform.rect;
        float footY = rect.yMin;      // 발(아래쪽)을 기준으로 늘였다 줄인다
        float centerX = rect.center.x;

        float breath = Mathf.Sin(phase);
        float sy = 1f + breathAmount * breath;
        float sx = 1f - breathAmount * 0.6f * breath;
        float offsetX = 0f, offsetY = 0f;

        if (hopTime >= 0f)
        {
            if (hopTime < hopDuration)
            {
                // 포물선으로 뛰어오르며 살짝 길쭉해진다.
                float t = hopTime / hopDuration;
                offsetY = hopHeight * 4f * t * (1f - t);
                float stretch = Mathf.Sin(t * Mathf.PI);
                sy *= 1f + 0.06f * stretch;
                sx *= 1f - 0.04f * stretch;
            }
            else
            {
                // 착지하며 잠깐 눌렸다가 돌아온다.
                float t = (hopTime - hopDuration) / landDuration;
                float squash = 0.1f * (1f - t);
                sy *= 1f - squash;
                sx *= 1f + squash * 0.7f;
            }
        }

        if (shakeTime >= 0f)
        {
            // 좌우로 빠르게 흔들리다 잦아든다.
            float t = shakeTime / shakeDuration;
            offsetX = shakeAmount * (1f - t) * Mathf.Sin(t * Mathf.PI * 8f);
        }

        var vertex = new UIVertex();
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            var p = vertex.position;
            p.x = centerX + (p.x - centerX) * sx + offsetX;
            p.y = footY + (p.y - footY) * sy + offsetY;
            vertex.position = p;
            vh.SetUIVertex(vertex, i);
        }
    }
}
