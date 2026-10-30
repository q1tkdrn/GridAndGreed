using UnityEngine;
using UnityEngine.UI;

// 맞았을 때 흔들림 + 붉은 번쩍임. RectTransform은 건드리지 않고 그리는 정점만 옮긴다.
// (보스 그림처럼 다른 코드가 위치·크기를 쓰는 그래픽에도 안전하게 붙일 수 있다.)
[DisallowMultipleComponent]
public sealed class HitShake : BaseMeshEffect
{
    private static readonly Color FlashColor = new Color(1f, 0.55f, 0.5f, 1f);

    private float _time = -1f;
    private float _duration;
    private float _amplitude;
    private Vector2 _offset;
    private float _flash;

    public static HitShake For(Graphic graphic)
    {
        if (graphic == null) return null;
        var shake = graphic.GetComponent<HitShake>();
        if (shake == null) shake = graphic.gameObject.AddComponent<HitShake>();
        return shake;
    }

    /// <summary>amplitude: 흔들리는 거리(px), duration: 길이(초)</summary>
    public void Play(float amplitude = 14f, float duration = 0.3f)
    {
        _amplitude = amplitude;
        _duration = Mathf.Max(0.01f, duration);
        _time = 0f;
    }

    private void Update()
    {
        if (_time < 0f) return;
        _time += Time.deltaTime;
        float t = _time / _duration;
        if (t >= 1f)
        {
            _time = -1f;
            _offset = Vector2.zero;
            _flash = 0f;
        }
        else
        {
            float fall = (1f - t) * (1f - t);
            // 처음 한 번은 크게 밀리고(화살이 박히는 방향), 이후 좌우로 잦아든다.
            _offset = new Vector2(Mathf.Sin(t * Mathf.PI * 9f) * _amplitude * fall, Mathf.Cos(t * Mathf.PI * 7f) * _amplitude * 0.25f * fall);
            _flash = Mathf.Clamp01(1f - t * 2.5f);
        }
        if (graphic != null) graphic.SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        _time = -1f;
        _offset = Vector2.zero;
        _flash = 0f;
        base.OnDisable();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || (_offset == Vector2.zero && _flash <= 0f)) return;
        var v = new UIVertex();
        var tint = Color.Lerp(Color.white, FlashColor, _flash);
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            v.position += (Vector3)_offset;
            Color c = v.color;
            v.color = c * tint;
            vh.SetUIVertex(v, i);
        }
    }
}
