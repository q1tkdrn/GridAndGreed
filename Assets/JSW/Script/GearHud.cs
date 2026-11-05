using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 화면 오른쪽 위 톱니바퀴 (2026-11-04). 누르면 ESC를 누른 것과 같은 메뉴가 열린다 (EscMenu.OpenFromHud).
/// - ESC 메뉴가 열리는 씬 중, 오른쪽 위가 비어 있는 곳에만 띄운다. 지금은 마을(Main)만.
///   전투(BattleTemp)는 오른쪽 위에 보스 HP·의지력이 있어 띄우지 않는다 (ESC로만).
/// - 평소엔 반투명, 마우스를 올리면 진해지고 살짝 커진다.
/// - 마을 화면을 넘길 때(MainManager 어두운 막)는 화면과 같이 사라졌다가 같이 나타난다.
///   다른 씬으로 갈 때는 장면 전환·로딩 화면이 위를 덮어 자연히 같이 사라지고 나타난다.
/// - 여정 지도(90)·ESC 메뉴(550)·확인 팝업(600)·장면 전환보다 아래에 그려져서, 그것들이 뜨면 자연히 가려진다.
/// 그림: Resources/Images/Main/SettingButton (옛 설정창 톱니).
/// </summary>
public sealed class GearHud : MonoBehaviour
{
    private const int SortingOrder = 80;          // 마을 UI(0) 위, 여정 지도(90) 아래
    private const float IdleAlpha = 0.6f;
    private const float HoverScale = 1.1f;
    private static readonly string[] Scenes = { "Main" };

    private static GearHud _instance;
    private GameObject _button;
    private Image _image;
    private bool _hover;
    private MainManager _main;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (_instance != null) return;
        new GameObject("GearHud").AddComponent<GearHud>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        var sprite = Resources.Load<Sprite>("Images/Main/SettingButton");
        if (sprite == null)
        {
            Debug.LogWarning("[GearHud] Resources/Images/Main/SettingButton 스프라이트가 없어 톱니바퀴를 띄우지 않습니다.");
            return;
        }

        _button = new GameObject("Gear", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rect = (RectTransform)_button.transform;
        rect.SetParent(transform, false);
        // ESC 창의 톱니와 같은 크기·같은 자리: ESC를 열어도 톱니가 제자리에 있는 것처럼 보인다.
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = EscMenu.GearCenter;
        rect.sizeDelta = new Vector2(EscMenu.GearSize, EscMenu.GearSize);
        _image = _button.GetComponent<Image>();
        _image.sprite = sprite;
        _image.preserveAspect = true;
        var button = _button.GetComponent<Button>();
        button.transition = Selectable.Transition.None;   // 밝기·크기는 아래 Update에서 직접
        button.onClick.AddListener(() => EscMenu.OpenFromHud());
        _button.AddComponent<Hover>().owner = this;
        Apply();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        if (_button == null) return;
        bool show = !EscMenu.IsOpen && System.Array.IndexOf(Scenes, SceneManager.GetActiveScene().name) >= 0;
        if (_button.activeSelf != show)
        {
            _button.SetActive(show);
            _hover = false;
        }
        if (show) Apply();
    }

    private void Apply()
    {
        if (_main == null || _main.gameObject.scene != SceneManager.GetActiveScene()) _main = FindFirstObjectByType<MainManager>();
        float cover = _main != null ? Mathf.Clamp01(_main.TransitionAlpha) : 0f;   // 마을 화면 넘김 중이면 1에 가까움
        _image.raycastTarget = cover < 0.01f;
        var c = _image.color;
        float a = (_hover ? 1f : IdleAlpha) * (1f - cover);
        if (!Mathf.Approximately(c.a, a)) _image.color = new Color(c.r, c.g, c.b, a);
        float s = _hover ? HoverScale : 1f;
        _button.transform.localScale = new Vector3(s, s, 1f);
    }

    private sealed class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GearHud owner;
        public void OnPointerEnter(PointerEventData eventData) => owner._hover = true;
        public void OnPointerExit(PointerEventData eventData) => owner._hover = false;
    }
}
