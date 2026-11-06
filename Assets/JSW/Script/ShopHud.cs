using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점(아이템·캐릭터·스킨) 화면 정리 (2026-11-06). 씬은 건드리지 않고 각 상점 Buy 스크립트의 Start에서 붙인다.
/// - 오른쪽 위 톱니(GearHud)와 겹치지 않게 소울 창(테두리+글자)을 모양 그대로 왼쪽으로 옮긴다.
/// - 소울 숫자는 구매하면 바로 바뀌지 않고 줄어드는 게 보이게 센다.
/// - 구매 버튼: ESC 메뉴와 같은 붓 해골 버튼에 "구매 · 가격". 소울이 모자라면 흐리고 가격이 붉게, 이미 가졌으면 "보유 중".
///   가격이 버튼으로 옮겨 가서 설명 카드의 가격 글자는 숨긴다.
/// - '나가기' → '돌아가기' (게임 표준 문구)
/// - 설명 글을 이름 아래 칸으로 내리고 넘치면 줄인다 (스킨 상점에서 긴 설명이 이름과 겹치던 것).
/// </summary>
public sealed class ShopHud : MonoBehaviour
{
    private const float SoulShiftX = -70f;          // 톱니(오른쪽 위, 폭 108)를 피하는 만큼
    private const float CountDuration = 0.6f;
    // 설명 카드 글자 배치 (아이템 상점 기준, 카드 칸 안쪽 여백)
    private const float CardSide = 165f, NameTop = 56f, CardBottom = 30f, TextSize = 50f;
    private static readonly Color PoorPrice = new Color(0.75f, 0.2f, 0.2f, 1f);

    private Button _button;
    private Image _buttonImage;
    private TMP_Text _label;
    private TMP_Text _soulText;
    private Func<int> _price;
    private Func<bool> _owned;
    private Func<bool> _available;
    private float _shownSoul = -1f;
    private float _countFrom, _countTo, _countTime = 999f;
    private float _pop;
    private TMP_Text _name, _body;
    private Vector4 _bodyMargin;

    public static ShopHud Attach(Button buy, TMP_Text soulText, Func<int> price, Func<bool> owned, Func<bool> available = null)
    {
        if (buy == null) return null;
        var hud = buy.GetComponent<ShopHud>();
        if (hud == null) hud = buy.gameObject.AddComponent<ShopHud>();
        hud._button = buy;
        hud._soulText = soulText;
        hud._price = price;
        hud._owned = owned;
        hud._available = available;
        hud.Build();
        return hud;
    }

    private void Build()
    {
        // 1) 소울 창: 테두리(Soul Frame)와 글자를 함께 왼쪽으로
        if (_soulText != null)
        {
            var parent = _soulText.transform.parent;
            Shift(_soulText.rectTransform);
            var frame = parent != null ? parent.Find("Soul Frame") as RectTransform : null;
            if (frame != null) Shift(frame);

            // 2) 설명 카드: 가격 글자 숨김, 설명은 이름 아래로
            var describe = parent != null ? parent.Find("Describe") : null;
            if (describe != null)
            {
                var price = describe.Find("Price Text");
                if (price != null) price.gameObject.SetActive(false);
                if (describe.Find("Describe Text")?.GetComponent<TMP_Text>() is TMP_Text body)
                {
                    // 세 상점 글자 배치 통일 (2026-11-06): 아이템 상점 배치를 기준으로 이름은 카드 가운데 위, 설명은 그 아래 가운데.
                    // 캐릭터 상점은 이름이 따로 Name Panel 안에 있어서 카드로 꺼낸다.
                    foreach (var t in describe.GetComponentsInChildren<TMP_Text>(true)) if (t.name == "Name Text") { _name = t; break; }
                    _body = body;
                    if (_name != null)
                    {
                        var oldParent = _name.transform.parent;
                        if (oldParent != describe)
                        {
                            _name.transform.SetParent(describe, false);
                            if (oldParent != null) oldParent.gameObject.SetActive(false);
                        }
                        Fill(_name.rectTransform);
                        _name.margin = new Vector4(CardSide, NameTop, CardSide, 0f);
                        _name.alignment = TextAlignmentOptions.Top;
                        _name.textWrappingMode = TextWrappingModes.NoWrap;
                        _name.font = GameFonts.Bold;
                        _name.fontSharedMaterial = GameFonts.Bold.material;
                        _name.color = new Color(0.08f, 0.08f, 0.08f, 1f);
                        _name.enableAutoSizing = true;
                        _name.fontSizeMax = TextSize;
                        _name.fontSizeMin = 30f;
                    }
                    // 설명: 이름 아래끝(+틈)부터 위에서 채우고, 칸을 넘으면 글자를 줄인다. 위 여백은 LateUpdate에서 이름에 맞춘다.
                    Fill(body.rectTransform);
                    _bodyMargin = new Vector4(CardSide, NameTop + 70f, CardSide, CardBottom);
                    body.margin = _bodyMargin;
                    body.alignment = TextAlignmentOptions.Top;
                    body.textWrappingMode = TextWrappingModes.Normal;
                    body.font = GameFonts.Body;
                    body.fontSharedMaterial = GameFonts.Body.material;
                    body.enableAutoSizing = true;
                    body.fontSizeMax = TextSize;
                    body.fontSizeMin = 24f;
                }
            }
        }

        // 3) 구매 버튼: 붓 해골 버튼
        var rect = (RectTransform)_button.transform;
        rect.sizeDelta = new Vector2(430f, 105f);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 70f);
        _buttonImage = _button.GetComponent<Image>();
        var brush = Resources.Load<Sprite>("UI/EscButton");
        if (_buttonImage != null && brush != null)
        {
            _buttonImage.sprite = brush;
            _buttonImage.type = Image.Type.Sliced;
            _buttonImage.preserveAspect = false;
        }
        var colors = _button.colors;
        colors.normalColor = new Color(0.94f, 0.94f, 0.94f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
        _button.colors = colors;
        _label = _button.GetComponentInChildren<TMP_Text>(true);
        if (_label != null)
        {
            var lr = _label.rectTransform;
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(92f, 0f);   // 왼쪽 해골을 피해서
            lr.offsetMax = new Vector2(-40f, 0f);
            _label.font = GameFonts.Bold;
            _label.fontSharedMaterial = GameFonts.Bold.material;
            _label.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            _label.alignment = TextAlignmentOptions.Center;
            _label.enableAutoSizing = true;
            _label.fontSizeMin = 26f;
            _label.fontSizeMax = 40f;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.characterSpacing = 0f;
        }

        // 4) 나가기 → 돌아가기
        var panel = _button.transform.parent;
        if (panel != null && panel.Find("Exit Button")?.GetComponentInChildren<TMP_Text>(true) is TMP_Text exit)
            exit.text = "돌아가기";
    }

    private static void Shift(RectTransform r) => r.anchoredPosition += new Vector2(SoulShiftX, 0f);

    private static void Fill(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private void LateUpdate()
    {
        var inv = InventoryManager.Instance;
        int soul = inv != null ? inv.GetSoul() : 0;

        // 소울: 바뀌면 이전 값에서 새 값까지 센다
        if (_shownSoul < 0f) _shownSoul = soul;
        if (!Mathf.Approximately(_countTo, soul) && _countTime >= CountDuration && !Mathf.Approximately(_shownSoul, soul))
        {
            _countFrom = _shownSoul; _countTo = soul; _countTime = 0f; _pop = 1f;
        }
        if (_countTime < CountDuration)
        {
            _countTime += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_countTime / CountDuration);
            _shownSoul = Mathf.Lerp(_countFrom, _countTo, 1f - (1f - k) * (1f - k));
        }
        else _shownSoul = soul;
        if (_soulText != null)
        {
            _soulText.text = "소울 " + Mathf.RoundToInt(_shownSoul).ToString("N0");
            _pop = Mathf.MoveTowards(_pop, 0f, Time.unscaledDeltaTime * 2.5f);
            _soulText.rectTransform.localScale = Vector3.one * (1f + 0.12f * _pop);
        }

        // 설명 글: 이름 아래끝에서 시작
        if (_name != null && _body != null && _name.textInfo != null && _name.textInfo.characterCount > 0)
        {
            var b = _name.textBounds;
            float nameBottom = _body.rectTransform.InverseTransformPoint(_name.rectTransform.TransformPoint(new Vector3(0f, b.min.y, 0f))).y;
            float top = Mathf.Max(_bodyMargin.y, _body.rectTransform.rect.yMax - (nameBottom - 14f));
            if (Mathf.Abs(_body.margin.y - top) > 0.5f) _body.margin = new Vector4(_bodyMargin.x, top, _bodyMargin.z, _bodyMargin.w);
        }

        // 구매 버튼 상태
        if (_label == null) return;
        bool available = _available == null || _available();
        bool owned = available && _owned != null && _owned();
        int price = _price != null ? _price() : 0;
        if (!available)
        {
            _button.interactable = false;
            _label.text = "준비 중";
        }
        else if (owned)
        {
            _button.interactable = false;
            _label.text = "보유 중";
        }
        else
        {
            _button.interactable = true;   // 모자라도 누르면 '거절' 소리 (각 Buy 스크립트)
            bool poor = soul < price;
            _label.text = poor
                ? $"구매 · <color=#{ColorUtility.ToHtmlStringRGB(PoorPrice)}>{price:N0}</color>"
                : $"구매 · {price:N0}";
            if (_buttonImage != null) _buttonImage.color = poor ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;
        }
    }
}
