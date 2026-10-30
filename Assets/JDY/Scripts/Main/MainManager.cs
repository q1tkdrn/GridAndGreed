using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject[] main;
    [SerializeField] private GameObject[] nameplateGroups;

    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;
    [Header("Town nameplates")]
    [SerializeField] private RectTransform townBanner;
    [SerializeField] private Image townBannerImage;
    [SerializeField] private Sprite[] townBannerSprites;
    [SerializeField] private Image achievementNameplate;
    [SerializeField] private Sprite[] achievementNameplateSprites;
    [Header("Town transition")]
    [SerializeField] private CanvasGroup transitionOverlay;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.2f;
    [SerializeField, Min(0f)] private float darkHoldDuration = 0.08f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float bannerSlideDuration = 0.4f;
    [SerializeField, Min(0f)] private float bannerHoldDuration = 2f;
    [Header("Main screen light")]
    [SerializeField] private bool ambientLightEnabled = true;
    [SerializeField, Range(0f, 1f)] private float ambientLightIntensity = 0.55f;
    [SerializeField, Range(0f, 1f)] private float ambientLightMotion = 0.35f;
    private Material ambientLightMaterial;
    private int currentIndex = 1;
    private bool isTransitioning;
    private Coroutine bannerRoutine;

    void Start()
    {
        // 마을에 한 번이라도 도착했다면 타이틀의 "이어하기"를 활성화한다.
        // 아래 UI 초기화에서 예외가 나도 저장 여부는 기록되도록 가장 먼저 둔다.
        SaveData.MarkStarted();
        PlayerPrefs.Save();
        foreach (var button in GetComponentsInChildren<Button>(true))
            UIHoverScale.Attach(button.gameObject);
        // NPC는 조각을 묶어 함께 커지게 하고, 마우스를 올리면 색이 채워지게 한다 (문·화살표 등은 제외).
        NpcHoverEffect.Setup(gameObject);
        CreateAmbientLight();
        currentIndex = Mathf.Clamp(PlayerPrefs.GetInt("mainCurrentIndex", 1), 0, main.Length - 1);
        achievementNameplate.sprite = achievementNameplateSprites[PlayerPrefs.GetInt("IsEnding", 0) == 2 ? 1 : 0];
        SetMain();
        townBannerImage.sprite = townBannerSprites[currentIndex];
        townBanner.anchoredPosition = new Vector2(-townBanner.rect.width, 0f);
        transitionOverlay.alpha = 0f;
        transitionOverlay.blocksRaycasts = false;
        bannerRoutine = StartCoroutine(AnimateBanner());
    }

    private void CreateAmbientLight()
    {
        if (!ambientLightEnabled) return;
        // Resources keeps the UI shader available in player builds as well as the editor.
        var shader = Resources.Load<Shader>("MainMenuAmbientLight");
        if (shader == null || !shader.isSupported) return;
        ambientLightMaterial = new Material(shader) { name = "Main menu ambient light" };
        UpdateAmbientLight();
        foreach (var panel in main)
        {
            if (panel == null) continue;
            var overlay = new GameObject("Ambient Light", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.layer = panel.layer;
            var rect = (RectTransform)overlay.transform;
            rect.SetParent(panel.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            var graphic = overlay.GetComponent<Image>();
            graphic.raycastTarget = false;
            graphic.material = ambientLightMaterial;
        }
    }

    private void OnValidate() => UpdateAmbientLight();

    private void UpdateAmbientLight()
    {
        if (ambientLightMaterial == null) return;
        ambientLightMaterial.SetFloat("_Intensity", ambientLightEnabled ? ambientLightIntensity : 0f);
        ambientLightMaterial.SetFloat("_Motion", ambientLightMotion);
    }

    private void OnDestroy()
    {
        if (ambientLightMaterial != null) Destroy(ambientLightMaterial);
    }
    private void SetMain()
    {
        for (int i = 0; i < main.Length; i++)
        {
            main[i].SetActive(i == currentIndex);
            nameplateGroups[i].SetActive(i == currentIndex);
        }
        PlayerPrefs.SetInt("mainCurrentIndex", currentIndex);

        leftButton.SetActive(currentIndex > 0);
        rightButton.SetActive(currentIndex < main.Length - 1);
    }
    public void LeftButton()
    {
        if (isTransitioning || currentIndex <= 0) return;
        StartCoroutine(ChangeMain(currentIndex - 1));
    }
    public void RightButton()
    {
        if (isTransitioning || currentIndex >= main.Length - 1) return;
        StartCoroutine(ChangeMain(currentIndex + 1));
    }

    private IEnumerator ChangeMain(int nextIndex)
    {
        isTransitioning = true;
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        transitionOverlay.blocksRaycasts = true;

        yield return FadeTo(1f, fadeOutDuration);
        currentIndex = nextIndex;
        SetMain();
        townBannerImage.sprite = townBannerSprites[currentIndex];
        townBanner.anchoredPosition = new Vector2(-townBanner.rect.width, 0f);
        yield return new WaitForSecondsRealtime(darkHoldDuration);
        yield return FadeTo(0f, fadeInDuration);

        transitionOverlay.blocksRaycasts = false;
        bannerRoutine = StartCoroutine(AnimateBanner());
        isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = transitionOverlay.alpha;
        if (duration <= 0f)
        {
            transitionOverlay.alpha = targetAlpha;
            yield break;
        }

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transitionOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        transitionOverlay.alpha = targetAlpha;
    }

    private IEnumerator AnimateBanner()
    {
        float hiddenX = -townBanner.rect.width;
        yield return MoveBanner(hiddenX, 0f);
        yield return new WaitForSecondsRealtime(bannerHoldDuration);
        yield return MoveBanner(0f, hiddenX);
        bannerRoutine = null;
    }

    private IEnumerator MoveBanner(float fromX, float toX)
    {
        if (bannerSlideDuration <= 0f)
        {
            townBanner.anchoredPosition = new Vector2(toX, 0f);
            yield break;
        }

        for (float elapsed = 0f; elapsed < bannerSlideDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / bannerSlideDuration));
            townBanner.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, t), 0f);
            yield return null;
        }
        townBanner.anchoredPosition = new Vector2(toX, 0f);
    }
    public void ItemShopButton()
    {
        SceneManager.LoadScene("Item");
    }
    public void CharacterShopButton()
    {
        SceneManager.LoadScene("Character");
    }
    public void AchievementButton()
    {
        SceneManager.LoadScene("Achievement");
    }
    public void MemorialButton()
    {
        SceneManager.LoadScene("Memorial");
    }
    public void MusicButton()
    {
        SceneManager.LoadScene("Music");
    }
    public void SkinButton()
    {
        SceneManager.LoadScene("Skin");
    }
    public void DoorButton()
    {
        // 여정 지도 (JSW/Script/JourneyMap.cs). 지도에서 건물을 고르는 기능이 생기기 전까지는 지도만 보여 준다.
        JourneyMap.Open();
    }

    // 예전 문 동작 (바로 전투 장면으로). 지도에서 입장하는 기능을 만들 때 다시 쓴다.
    private void OpenJourneyLegacy()
    {
        if (SceneFade.IsFading) return; // 여정으로 넘어가는 중
        if (!RunSave.HasRun)
        {
            EnterBattle(resume: false);
            return;
        }

        // 진행 중인 여정이 있으면 이어갈지, 포기하고 새로 떠날지 고른다.
        int cleared = RunSave.ClearedBossIds.Count;
        string next = RunSave.HasCurrentBattle ? $"\n{WithParticle(RunSave.CurrentBossName, "과", "와")}의 전투부터 다시 시작해요." 
            : RunSave.RewardPending ? "\n받지 않은 승리 보상부터 이어가요."
            : "\n다음 길을 고르는 곳부터 이어가요.";
        ModalDialog.Show("여정",
            "진행 중인 여정이 있어요",
            $"처치한 보스 {cleared}명{next}",
            null,
            new ModalDialog.Option("여정 이어가기", () => EnterBattle(resume: true)),
            new ModalDialog.Option("새 여정 시작", ConfirmAbandonRun),
            new ModalDialog.Option("돌아가기", null));
    }

    private void ConfirmAbandonRun()
    {
        ModalDialog.Show("여정",
            "지금 여정을 포기할까요?",
            "보스 처치 기록이 사라지고 편성부터 다시 해요.\n소울과 구매한 것은 그대로 남아요.",
            DoorButton,
            new ModalDialog.Option("포기하고 새로 시작", () =>
            {
                RunSave.Delete();
                EnterBattle(resume: false);
            }),
            new ModalDialog.Option("돌아가기", DoorButton));
    }

    // 받침 유무에 따라 조사를 고른다. 한글이 아니면 받침 없는 쪽을 쓴다.
    private static string WithParticle(string word, string withBatchim, string withoutBatchim)
    {
        if (string.IsNullOrEmpty(word)) return "보스" + withoutBatchim;
        char last = word[word.Length - 1];
        bool batchim = last >= '\uAC00' && last <= '\uD7A3' && (last - 0xAC00) % 28 != 0;
        return word + (batchim ? withBatchim : withoutBatchim);
    }

    private static void EnterBattle(bool resume)
    {
        if (resume) RunSave.RequestResume();
        if (AchievementManager.Instance != null) AchievementManager.Instance.AddProgress("ACH-1", 1);
        // 화면이 1초 동안 하얘진 뒤 여정 창이 열리고, 흰빛이 0.8초 동안 걷힌다.
        SceneFade.LoadScene("BattleTemp", Color.white, 1f, 0.8f);
    }
    public void InitPlay()
    {
        InventoryManager.Instance.AddItem("1");
        InventoryManager.Instance.AddItem("2");
        InventoryManager.Instance.AddItem("3");
        InventoryManager.Instance.UnlockCharacter("1");
        InventoryManager.Instance.UnlockCharacter("2");
        InventoryManager.Instance.UnlockCharacter("3");
    }
}
