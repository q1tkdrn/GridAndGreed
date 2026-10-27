using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// ESC 메뉴(Resources/UI/PauseMenu)의 프레임과 버튼을 복제해 만드는 확인 팝업.
/// 디자인을 ESC 메뉴와 통일하고, 씬 파일을 수정하지 않고도 어느 씬에서나 띄울 수 있다.
/// 문구의 줄바꿈은 호출하는 쪽에서 "\n"으로 직접 넣는다. (한글은 글자 단위로 줄바꿈되어 한 글자만 남을 수 있음)
/// </summary>
public sealed class ModalDialog : MonoBehaviour
{
    public readonly struct Option
    {
        public readonly string Label;
        public readonly Action OnClick;
        public Option(string label, Action onClick) { Label = label; OnClick = onClick; }
    }

    // 버튼 2개: ESC 메뉴의 다시 시작 확인 화면과 같은 배치.
    private const float TwoButtonFirstY = -83f;
    private const float TwoButtonSpacing = 93f;
    // 버튼 3개 이상: 마지막 버튼이 하단 구분선(-251, 본문 기준)과 겹치지 않도록 위로 올리고 간격을 좁힌다.
    private const float ManyButtonLastY = -199f;
    private const float ManyButtonSpacing = 86f;
    private const float ManyQuestionY = 125f;
    private const float ManyExplanationY = 56f;
    private const float ManyExplanationHeight = 70f;

    /// <summary>팝업이 열려 있으면 ESC 메뉴가 같은 키 입력으로 열리지 않게 한다.</summary>
    public static bool IsOpen => _current != null;
    /// <summary>ESC로 팝업을 닫은 프레임. 같은 프레임에 ESC 메뉴가 열리지 않게 한다.</summary>
    public static bool BlocksEscape => _current != null || Time.frameCount == _closedFrame;
    private static ModalDialog _current;
    private static int _closedFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _current = null; _closedFrame = -1; }

    private Action _onCancel;
    private bool _escapeHeld = true; // 팝업을 연 키 입력이 바로 닫기로 처리되지 않게 한다.

    /// <param name="onCancel">ESC로 닫을 때 실행. null이면 단순히 닫기만 한다.</param>
    public static ModalDialog Show(string heading, string question, string explanation, Action onCancel, params Option[] options)
    {
        if (_current != null) _current.Close();

        var prefab = Resources.Load<PauseMenuController>("UI/PauseMenu");
        if (prefab == null || prefab.overlay == null || prefab.restartConfirmation == null)
        {
            Debug.LogError("확인 팝업에 쓸 Resources/UI/PauseMenu 프리팹을 찾지 못했습니다.");
            return null;
        }

        var root = new GameObject("ModalDialog", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600; // ESC 메뉴(500)보다 위
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var dialog = root.AddComponent<ModalDialog>();
        dialog._onCancel = onCancel;
        _current = dialog;

        var overlay = Instantiate(prefab.overlay, root.transform, false);
        overlay.SetActive(true);
        var panel = overlay.transform.Find("Frame/Panel");

        SetText(panel, "Heading", heading);
        // ESC 메뉴 전용 부제(MEMORIAL)는 확인 팝업에 어울리지 않아 숨긴다.
        var subtitle = panel.Find("Subtitle");
        if (subtitle != null) subtitle.gameObject.SetActive(false);
        var actions = panel.Find("Actions");
        if (actions != null) actions.gameObject.SetActive(false);

        var body = panel.Find("RestartConfirmation");
        body.gameObject.SetActive(true);
        SetText(body, "Question", question);
        SetText(body, "Explanation", explanation);

        var template = body.Find("ConfirmRestartButton");
        var cancelTemplate = body.Find("CancelRestartButton");
        if (cancelTemplate != null) cancelTemplate.gameObject.SetActive(false);

        float firstY = TwoButtonFirstY;
        float spacing = TwoButtonSpacing;
        if (options.Length > 2)
        {
            spacing = ManyButtonSpacing;
            firstY = ManyButtonLastY + spacing * (options.Length - 1);
            SetY(body, "Question", ManyQuestionY);
            if (SetY(body, "Explanation", ManyExplanationY) is RectTransform explanationRect)
                explanationRect.sizeDelta = new Vector2(explanationRect.sizeDelta.x, ManyExplanationHeight);
        }

        for (int i = 0; i < options.Length; i++)
        {
            var option = options[i];
            var buttonObject = Instantiate(template.gameObject, body, false);
            buttonObject.name = "Option" + i;
            var rect = (RectTransform)buttonObject.transform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, firstY - spacing * i);
            buttonObject.GetComponentInChildren<TMP_Text>(true).text = option.Label;
            var button = buttonObject.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                dialog.Close();
                option.OnClick?.Invoke();
            });
            UIHoverScale.Attach(buttonObject);
        }
        template.gameObject.SetActive(false);
        return dialog;
    }

    private static void SetText(Transform parent, string child, string value)
    {
        var target = parent.Find(child);
        if (target == null) return;
        var text = target.GetComponent<TMP_Text>();
        if (text == null) return;
        text.text = value ?? "";
        target.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private static RectTransform SetY(Transform parent, string child, float y)
    {
        if (parent.Find(child) is not RectTransform rect) return null;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        return rect;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        bool escape = keyboard != null && keyboard.escapeKey.isPressed;
        if (!escape) { _escapeHeld = false; return; }
        if (_escapeHeld) return;
        _escapeHeld = true;
        var cancel = _onCancel;
        Close();
        cancel?.Invoke();
    }

    public void Close()
    {
        _closedFrame = Time.frameCount;
        if (_current == this) _current = null;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_current == this) _current = null;
    }
}
