using UnityEngine;
using UnityEngine.UI;

/// <summary>편성창 배경에 천천히 떠오르는 먼지·재 (JSW, 2026-11-03). 캐릭터 뒤 가장 아래 층.</summary>
public class FormationDust : MonoBehaviour
{
    private const int Count = 36;
    private RectTransform[] _motes;
    private Image[] _images;
    private float[] _speed, _sway, _seed, _alpha;
    private Vector2 _area;

    public static void Ensure(RectTransform parent)
    {
        if (parent == null || parent.GetComponentInChildren<FormationDust>(true) != null) return;
        var go = new GameObject("FX Dust", typeof(RectTransform));
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.SetAsFirstSibling();
        go.AddComponent<FormationDust>();
    }

    private void Start()
    {
        _area = ((RectTransform)transform).rect.size;
        _motes = new RectTransform[Count];
        _images = new Image[Count];
        _speed = new float[Count]; _sway = new float[Count]; _seed = new float[Count]; _alpha = new float[Count];
        for (int i = 0; i < Count; i++)
        {
            var img = new GameObject("Mote", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.sprite = FxSprites.Dot;
            img.raycastTarget = false;
            var r = img.rectTransform;
            r.SetParent(transform, false);
            float size = Random.Range(4f, 11f);
            r.sizeDelta = new Vector2(size, size);
            r.anchoredPosition = new Vector2(Random.Range(-0.5f, 0.5f) * _area.x, Random.Range(-0.5f, 0.5f) * _area.y);
            _motes[i] = r; _images[i] = img;
            _speed[i] = Random.Range(10f, 28f);
            _sway[i] = Random.Range(8f, 24f);
            _seed[i] = Random.Range(0f, 100f);
            _alpha[i] = Random.Range(0.06f, 0.28f);
        }
    }

    private void Update()
    {
        if (_motes == null) return;
        float t = Time.unscaledTime, dt = Time.unscaledDeltaTime;
        for (int i = 0; i < Count; i++)
        {
            var p = _motes[i].anchoredPosition;
            p.y += _speed[i] * dt;
            p.x += Mathf.Sin(t * 0.6f + _seed[i]) * _sway[i] * dt;
            if (p.y > _area.y * 0.5f + 10f) { p.y = -_area.y * 0.5f - 10f; p.x = Random.Range(-0.5f, 0.5f) * _area.x; }
            _motes[i].anchoredPosition = p;
            // 반짝이듯 천천히 밝아졌다 어두워짐
            float a = _alpha[i] * (0.55f + 0.45f * Mathf.Sin(t * 0.9f + _seed[i] * 3f));
            _images[i].color = new Color(0.92f, 0.91f, 0.87f, a);
        }
    }
}
