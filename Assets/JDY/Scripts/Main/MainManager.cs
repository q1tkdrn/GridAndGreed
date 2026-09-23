using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject[] main;

    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;
    [Header("Main screen light")]
    [SerializeField] private bool ambientLightEnabled = true;
    [SerializeField, Range(0f, 1f)] private float ambientLightIntensity = 0.55f;
    [SerializeField, Range(0f, 1f)] private float ambientLightMotion = 0.35f;
    private Material ambientLightMaterial;
    private int currentIndex = 1;
    void Start()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
            UIHoverScale.Attach(button.gameObject);
        CreateAmbientLight();
        currentIndex = PlayerPrefs.GetInt("mainCurrentIndex", 1);
        SetMain();
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
            main[i].SetActive(false);
        }

        main[currentIndex].SetActive(true);
        PlayerPrefs.SetInt("mainCurrentIndex", currentIndex);

        leftButton.SetActive(currentIndex > 0);
        rightButton.SetActive(currentIndex < main.Length - 1);
    }
    public void LeftButton()
    {
        if (currentIndex <= 0) return;

        currentIndex--;
        SetMain();
    }
    public void RightButton()
    {
        if (currentIndex >= main.Length - 1) return;

        currentIndex++;
        SetMain();
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
        SceneManager.LoadScene("BattleTemp");
        AchievementManager.Instance.AddProgress("ACH-1", 1);
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
