using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 화면 오른쪽 위 톱니바퀴 (2026-11-04). 누르면 ESC를 누른 것과 같은 메뉴가 열린다 (EscMenu.OpenFromHud).
/// - 오른쪽 위가 비어 있는 곳에만 띄운다: 마을(Main).
///   전투(BattleTemp)는 오른쪽 위에 보스 HP·의지력이, 상점(아이템·캐릭터·스킨)은 소울 상자가,
///   음악·추모는 제목 붓 띠가, 업적은 두루마리 끝이 있어 띄우지 않는다 (2026-11-05).
/// - 여정 지도는 자기 캔버스 안에 같은 톱니(GearButton)를 직접 만든다 (JourneyMap.BuildGear). 지도 전환 연출과 함께 나타나고 사라진다.
/// - 평소엔 반투명, 마우스를 올리면 진해지고 살짝 커진다.
/// - 마을 화면을 넘길 때(MainManager 어두운 막)는 화면과 같이 사라졌다가 같이 나타난다.
///   다른 씬으로 갈 때는 장면 전환·로딩 화면이 위를 덮어 자연히 같이 사라지고 나타난다.
/// - 여정 지도(90)·ESC 메뉴(550)·확인 팝업(600)·장면 전환보다 아래에 그려져서, 그것들이 뜨면 자연히 가려진다.
/// 그림: Resources/Images/Main/SettingButton (옛 설정창 톱니).
/// </summary>
public sealed class GearHud : MonoBehaviour
{
    private const int SortingOrder = 80;          // 마을 UI(0) 위, 여정 지도(90) 아래
    private static readonly string[] Scenes = { "Main", "Item", "Character", "Skin" };   // 상점은 소울 창을 왼쪽으로 옮겨 자리를 냈다 (ShopHud, 2026-11-06)

    private static GearHud _instance;
    private GearButton _gear;
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

        _gear = GearButton.Create(transform, () => EscMenu.OpenFromHud());
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        if (_gear == null) return;
        bool show = !EscMenu.IsOpen && Array.IndexOf(Scenes, SceneManager.GetActiveScene().name) >= 0;
        if (_gear.gameObject.activeSelf != show) _gear.gameObject.SetActive(show);
        if (!show) return;
        if (_main == null || _main.gameObject.scene != SceneManager.GetActiveScene()) _main = FindFirstObjectByType<MainManager>();
        _gear.cover = _main != null ? Mathf.Clamp01(_main.TransitionAlpha) : 0f;   // 마을 화면 넘김 중이면 1에 가까움
    }

    /// <summary>
    /// 톱니 버튼 하나. ESC 창의 톱니와 같은 크기·같은 자리(오른쪽 위)라 ESC를 열어도 톱니가 제자리에 있는 것처럼 보인다.
    /// cover(0~1)만큼 흐려지고, 1에 가까우면 눌리지 않는다.
    /// </summary>
    public sealed class GearButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float IdleAlpha = 0.6f;
        private const float HoverScale = 1.1f;

        public float cover;
        private Image _image;
        private bool _hover;

        public static GearButton Create(Transform parent, Action onClick)
        {
            var sprite = Resources.Load<Sprite>("Images/Main/SettingButton");
            if (sprite == null)
            {
                Debug.LogWarning("[GearHud] Resources/Images/Main/SettingButton 스프라이트가 없어 톱니바퀴를 띄우지 않습니다.");
                return null;
            }

            var go = new GameObject("Gear", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = EscMenu.GearCenter;
            rect.sizeDelta = new Vector2(EscMenu.GearSize, EscMenu.GearSize);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;   // 밝기·크기는 아래 LateUpdate에서 직접
            button.onClick.AddListener(() => onClick?.Invoke());
            var gear = go.AddComponent<GearButton>();
            gear._image = image;
            gear.Apply();
            return gear;
        }

        private void OnDisable() => _hover = false;
        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (_image == null) return;
            _image.raycastTarget = cover < 0.01f;
            var c = _image.color;
            float a = (_hover ? 1f : IdleAlpha) * (1f - cover);
            if (!Mathf.Approximately(c.a, a)) _image.color = new Color(c.r, c.g, c.b, a);
            float s = _hover ? HoverScale : 1f;
            transform.localScale = new Vector3(s, s, 1f);
        }

        public void OnPointerEnter(PointerEventData eventData) => _hover = true;
        public void OnPointerExit(PointerEventData eventData) => _hover = false;
    }
}
