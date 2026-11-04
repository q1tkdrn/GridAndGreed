using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 힘이 바뀌는 순간 그 캐릭터 머리 위에 이유를 띄운다 (JSW, 2026-11-04). 예: "+3 힘 칸", "+1 도적", "+2 피격".
/// 같은 캐릭터에 여러 개가 한꺼번에 생기면 위로 쌓이고 조금씩 늦게 나온다. BellkeeperTrial.Gain에서 부른다.
/// </summary>
public class ChargePopup : MonoBehaviour
{
    public static readonly Color Cell = new Color(0.62f, 0.92f, 0.56f);    // 힘 칸
    public static readonly Color Greed = new Color(1f, 0.85f, 0.35f);      // 욕심 칸·버티기·행운 칸
    public static readonly Color Trait = new Color(0.96f, 0.96f, 0.96f);   // 캐릭터 특성
    public static readonly Color Item = new Color(0.68f, 0.85f, 1f);       // 아이템
    public static readonly Color Hit = new Color(1f, 0.52f, 0.48f);        // 피격
    public static readonly Color Lose = new Color(0.65f, 0.65f, 0.65f);    // 감소

    private const float Life = 1.35f;
    private const float Rise = 46f;
    private const float StackGap = 36f;   // 26pt 한 줄 + 여유
    private const float StaggerDelay = 0.14f;

    // 캐릭터별로 지금 떠 있는 것들 (쌓는 높이 계산용)
    private static readonly Dictionary<Image, List<ChargePopup>> Active = new Dictionary<Image, List<ChargePopup>>();

    private TextMeshProUGUI _text;
    private RectTransform _rect;
    private Image _owner;
    private Vector2 _start;
    private float _age, _delay;

    public static void Show(Image unit, float amount, string reason, Color color, TMP_FontAsset font)
    {
        if (unit == null || Mathf.Abs(amount) < 0.01f || !unit.gameObject.activeInHierarchy) return;
        if (!Active.TryGetValue(unit, out var list)) Active[unit] = list = new List<ChargePopup>();
        list.RemoveAll(p => p == null);

        var go = new GameObject("Charge Popup", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        var unitRect = unit.rectTransform;
        rect.SetParent(unitRect.parent, false);
        rect.SetAsLastSibling();   // 캐릭터·칸 글자보다 위
        rect.anchorMin = unitRect.anchorMin;
        rect.anchorMax = unitRect.anchorMax;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(220f, 34f);

        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        // 1.5처럼 반 칸 값도 그대로 (마법사 일반 힘 절반)
        string value = Mathf.Abs(amount - Mathf.Round(amount)) < 0.01f ? Mathf.RoundToInt(amount).ToString() : amount.ToString("0.#");
        text.text = (amount > 0 ? "+" : "") + value + " " + reason;
        text.fontSize = 26f;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.outlineWidth = 0.22f;   // 밝은 칸·어두운 칸 위 모두 읽히게
        text.outlineColor = new Color32(0, 0, 0, 230);

        var popup = go.AddComponent<ChargePopup>();
        popup._text = text;
        popup._rect = rect;
        popup._owner = unit;
        int slot = list.Count;
        popup._start = unitRect.anchoredPosition + new Vector2(0f, unitRect.rect.height * 0.5f + 30f + slot * StackGap);   // 머리 위 힘 숫자(+29) 위에서 시작
        popup._delay = slot * StaggerDelay;
        rect.anchoredPosition = popup._start;
        text.alpha = 0f;
        list.Add(popup);
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if (_delay > 0f) { _delay -= dt; return; }
        _age += dt;
        float k = Mathf.Clamp01(_age / Life);
        float up = 1f - Mathf.Pow(1f - k, 2f);
        _rect.anchoredPosition = _start + new Vector2(0f, Rise * up);
        // 금방 나타나서 잠시 머물다 사라짐
        _text.alpha = k < 0.12f ? k / 0.12f : k > 0.7f ? 1f - (k - 0.7f) / 0.3f : 1f;
        if (_age >= Life) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_owner != null && Active.TryGetValue(_owner, out var list)) list.Remove(this);
    }
}
