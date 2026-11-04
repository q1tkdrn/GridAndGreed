using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 욕심 칸에 서 있는 캐릭터 발밑의 금빛 (JSW, 2026-11-04). 예전 '버티면+7' 글자 상자 대신.
/// 맥박처럼 은은하게 깜빡이고, 공격 예고가 그 칸을 덮으면 더 밝고 빨라진다(지금 도박 중).
/// </summary>
public class StandGlow : MonoBehaviour
{
    private static readonly Color Gold = new Color(1f, 0.82f, 0.32f);

    private Image _image;
    private Func<bool> _inDanger;
    private float _phase;

    public static GameObject Create(RectTransform cell, Func<bool> inDanger)
    {
        if (cell == null) return null;
        var go = new GameObject("Stand Glow", typeof(RectTransform), typeof(Image));
        go.layer = cell.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(cell, false);
        rect.SetAsLastSibling();   // 칸 위 공격 예고보다 위, 캐릭터(칸과 형제, 뒤쪽)보다는 아래
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = cell.rect.size;   // 칸 크기 (더 크면 옆 칸에 가려져 잘려 보임)
        var image = go.GetComponent<Image>();
        image.sprite = FxSprites.Dot;
        image.raycastTarget = false;
        image.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
        var glow = go.AddComponent<StandGlow>();
        glow._image = image;
        glow._inDanger = inDanger;
        glow._phase = UnityEngine.Random.value * 6f;
        return go;
    }

    private void Update()
    {
        bool danger = _inDanger != null && _inDanger();
        float speed = danger ? 4.2f : 2.2f;
        _phase += Time.unscaledDeltaTime * speed;
        float pulse = 0.5f + 0.5f * Mathf.Sin(_phase);
        float a = danger ? Mathf.Lerp(0.38f, 0.75f, pulse) : Mathf.Lerp(0.18f, 0.40f, pulse);
        _image.color = new Color(Gold.r, Gold.g, Gold.b, a);
    }
}
