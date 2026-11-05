using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public struct Stage
{
    public BossTemp bossTemp;
    public TextMeshProUGUI stageName;
    public Image stageImage;
}

public class BattleDisplayManager : MonoBehaviour
{
#if UNITY_EDITOR
    public enum DebugStartStage { Normal, AfterlifePhase1, AfterlifePhase2, King, Bellkeeper }
    [Header("Editor Play Test")]
    [Tooltip("BattleTemp 씬에서 Play할 때 지정한 보스로 바로 진입합니다. 실행 중에는 Debug의 보스별 버튼으로도 시작할 수 있습니다.")]
    public DebugStartStage debugStartStage;

    private void StartEditorDebugStage()
    {
        if (debugStartStage == DebugStartStage.AfterlifePhase1) DebugEnterAfterlife(1);
        else if (debugStartStage == DebugStartStage.AfterlifePhase2) DebugEnterAfterlife(2);
        else if (debugStartStage == DebugStartStage.King) DebugEnterKing();
        else if (debugStartStage == DebugStartStage.Bellkeeper)
            DebugEnterBoss(remainBoss.FirstOrDefault(boss => boss != null && boss.bossId == "bellkeeper"));
    }

    public void DebugEnterAfterlife(int phaseNumber)
    {
        if (!Application.isPlaying) return;
        var targetBoss = phaseNumber == 2 ? bossDeath2 : bossDeath1;
        DebugEnterBoss(targetBoss);
    }

    public void DebugEnterKing()
    {
        if (!Application.isPlaying) return;
        DebugEnterBoss(bossKing);
    }

    public void DebugEnterBoss(BossTemp targetBoss)
    {
        if (!Application.isPlaying) return;
        if (targetBoss == null || currentUnits.Length != 3 || currentUnits.Any(unit => unit == null))
        {
            Debug.LogError("테스트할 보스와 기본 캐릭터 3명의 참조를 확인하세요.", this);
            return;
        }

        // 이전 전투/결과/컷신의 코루틴과 화면을 닫고 독립적인 테스트를 시작한다.
        boardPanel.gameObject.SetActive(false);
        cutScenePanel.gameObject.SetActive(false);
        entrancePanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(false);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        waysPanel.gameObject.SetActive(false);
        arrow.SetActive(false);
        _decidedStage = -1;
        appearedBoss.Clear();
        appearedBoss.Add(targetBoss);
        boardPanel.boss = targetBoss;
        boardPanel.reaperCurrentHp = boardPanel.reaperMaxHp;
        // 전투 씬만 실행한 경우 인벤토리 없이 기본 3인으로 테스트한다.
        if (InventoryManager.Instance == null) currentItems = new ItemData[3];
        boardPanel.gameObject.SetActive(true);
        boardPanel.ShowCutScene();
    }
#endif

    private void Start()
    {
        // 새 게임의 오프닝 뒤, 또는 마을의 튜토리얼 NPC에서 들어온 경우 (JSW/Script/Tutorial)
        if (TutorialLink.Consume())
        {
            StartTutorial();
            return;
        }
        // 마을의 여정 지도에서 들어온 경우 (JSW/Script/JourneyMap.cs, JourneyLink.cs)
        if (JourneyLink.FormationRequested)
        {
            JourneyLink.FormationRequested = false;
            OpenFormationFromMap();
            return;
        }
        if (!string.IsNullOrEmpty(JourneyLink.PendingBossId))
        {
            var bossId = JourneyLink.PendingBossId;
            JourneyLink.PendingBossId = null;
            StartFromMap(bossId);
            return;
        }
        // 마을에서 "여정 이어가기" 또는 ESC 메뉴의 "보스전 다시 시작하기"로 들어온 경우.
        if (RunSave.ConsumeResumeRequest())
        {
            ResumeRun();
            return;
        }
#if UNITY_EDITOR
        StartEditorDebugStage();
#endif
    }

    [Header("Panel")]
    public EntrancePanel entrancePanel;
    public BoardPanel boardPanel;
    [SerializeField] private GameObject unitBuildingPanel;
    public ItemBuildingPanel itemBuildingPanel;
    public CutScenePanel cutScenePanel;
    
    [Space]
    [SerializeField] private GameObject victoryPanel;
    [Header("Victory Reward")]
    [SerializeField, Min(0)] private int victorySoulReward = 500;
    // 욕심 아이템 소울 보너스 반영 (임시값, 소울 경제 확정 전)
    private int VictorySoul => Mathf.RoundToInt(victorySoulReward * (1f + BellkeeperTrial.SoulBonus));
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private Image waysPanel;
    [SerializeField] private GameObject waysBackButton;
    private bool _waysChoicesReady;
    public UnitTemp[] currentUnits = new UnitTemp[3];
    public ItemData[] currentItems = new ItemData[3];
    
    public List<BossTemp> remainBoss = new List<BossTemp>();
    public List<BossTemp> appearedBoss = new List<BossTemp>();
    public BossTemp bossKing;
    public BossTemp bossDeath1;
    public BossTemp bossDeath2;
    public Stage[] stages = new Stage[3];
    [SerializeField] private GameObject arrow;
    private int _decidedStage = -1;
    private bool _formationLoaded;
    private bool _victoryRewardClaimed;
    private GameObject _victoryContinueButton;
    private Button _soulRewardButton;

    [Tooltip("튜토리얼에서 빌려 주는 소금물(Item_4). 새 게임 첫 튜토리얼은 ItemManager가 없어서 여기서 가져온다.")]
    [SerializeField] private ItemData tutorialItem;
    private TextMeshProUGUI _soulRewardText;

    private UnitTemp[] FormationRoster() => unitBuildingPanel.GetComponentInChildren<UnitBuildingPanel>(true)
        .cards.Where(card => card != null).Select(card => card.unitTemp).Concat(currentUnits).Where(unit => unit != null).Distinct().ToArray();

    public void EnsureFormationLoaded()
    {
        if (_formationLoaded || unitBuildingPanel == null || ItemManager.Instance == null) return;
        FormationSave.Load(currentUnits, currentItems, FormationRoster(), ItemManager.Instance.items);
        _formationLoaded = true;
    }

    public void SaveFormation()
    {
        if (!_formationLoaded || TutorialDirector.Active) return;   // 튜토리얼 편성은 저장하지 않는다
        FormationSave.Save(currentUnits, currentItems, FormationRoster());
    }
    
    public AudioSource bgmSource;
    public AudioSource bgmSourceLoop;
    public AudioSource sfxSource;

    public AudioClip buildingClip;
    public AudioClip endingClip;
    
    
    //싱글톤-------------------------------------------------------------------------------
    private static BattleDisplayManager _instance;

    public static BattleDisplayManager GetInstance()
    {
        Init();
        return _instance;
    }

    static void Init()
    {
        if (_instance == null)
        {
            GameObject go = GameObject.Find("BattleDisplayManager");
            if (go == null)
            {
                go = new GameObject { name = "BattleDisplayManager" };
                go.AddComponent<BattleDisplayManager>();
            }

            _instance = go.GetComponent<BattleDisplayManager>();
        }
    }
    //-------------------------------------------------------------------------------싱글톤

    public void OnEnable()
    {
        PlayBGM(buildingClip);
    }
    
    [DebugButton]
    public void OpenGameBoard()
    {
        EnsureFormationLoaded();
        entrancePanel.gameObject.SetActive(false);
        boardPanel.gameObject.SetActive(true);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(false);
        ShowWaysPanel();
    }
    
    [DebugButton]
    public void OpenUnitBuilding()
    {
        EnsureFormationLoaded();
        entrancePanel.gameObject.SetActive(false);
        boardPanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(true);
        itemBuildingPanel.gameObject.SetActive(false);
    }
    
    [DebugButton]
    public void OpenItemBuilding()
    {
        EnsureFormationLoaded();
        entrancePanel.gameObject.SetActive(false);
        boardPanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(true);
    }
    
    [DebugButton]
    public void OpenEntrancePanel(bool isBuilding)
    {
        // 지도에서 연 편성: 캐릭터/아이템 화면에서 나가면 '캐릭터·아이템' 고르는 화면으로 (그 화면의 돌아가기는 지도로)
        if (JourneyRefit.FromMap) isBuilding = true;
        entrancePanel.gameObject.SetActive(true);
        boardPanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(false);
        entrancePanel.isBuilding = isBuilding;
        entrancePanel.Init();
        
        if(bgmSource.clip != buildingClip) PlayBGM(buildingClip);
    }
    
    [DebugButton]
    public void ShowVictoryPanel()
    {
        OpenVictoryScreen();
        if (appearedBoss.Count > 0)
        {
            var boss = appearedBoss[^1];
            ClearBoss(boss);
            if (IsEndingBoss(boss)) RunSave.Delete();   // 엔딩까지 끝낸 여정은 이어할 것이 없다.
            else RunSave.MarkCurrentCleared();         // 지급 직전에 꺼도 승리 화면부터 이어한다.
        }
        ClaimVictorySoul();   // 이기면 소울은 자동 지급 (2026-11-03: 보상 말풍선 클릭 없앰)
    }

    /// <summary>승리 화면을 연다. 소울 말풍선은 누르는 버튼이 아니라 받은 양을 보여 주는 표시.</summary>
    private void OpenVictoryScreen()
    {
        SetupVictoryReward();
        _victoryRewardClaimed = false;
        if (_victoryContinueButton != null) _victoryContinueButton.SetActive(true);
        if (_soulRewardText != null) _soulRewardText.text = "소울 +0";
        victoryPanel.SetActive(true);
        ShowVictoryLines(appearedBoss.Count > 0 ? appearedBoss[^1] : null);
    }

    // ── 승리 후 꼬마 사신의 혼잣말 (JSW/Script/StageText.cs, 2026-11-04) ──
    private RectTransform _victoryLinesRoot;
    private TextMeshProUGUI[] _victoryLines;
    private Coroutine _victoryLinesRoutine;

    private void ShowVictoryLines(BossTemp boss)
    {
        var lines = StageText.Find(boss)?.victory;
        if (_victoryLinesRoot == null)
        {
            TMP_FontAsset font = null;
            foreach (var t in victoryPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.font != null && t.font.name.Contains("Light")) { font = t.font; break; }
            if (font == null) font = victoryPanel.GetComponentInChildren<TextMeshProUGUI>(true)?.font;

            _victoryLinesRoot = new GameObject("Reaper Lines", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _victoryLinesRoot.SetParent(victoryPanel.transform, false);
            _victoryLinesRoot.anchorMin = _victoryLinesRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _victoryLinesRoot.anchoredPosition = new Vector2(0f, -150f);   // VICTORY와 소울 말풍선 사이
            _victoryLinesRoot.sizeDelta = new Vector2(1500f, 210f);
            var band = _victoryLinesRoot.GetComponent<Image>();
            band.sprite = FxSprites.FadeBar;   // 좌우 끝이 흐려지는 어두운 띠 (흰 붓 자국 위에서도 글이 읽히게)
            band.color = new Color(0f, 0f, 0f, 0.72f);
            band.raycastTarget = false;
            _victoryLines = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                var t = new GameObject("Line" + i, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var r = t.rectTransform;
                r.SetParent(_victoryLinesRoot, false);
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(0f, 60f - i * 60f);
                r.sizeDelta = new Vector2(1400f, 56f);
                if (font != null) t.font = font;
                t.fontSize = 38f;
                t.color = Color.white;
                t.alignment = TextAlignmentOptions.Center;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.raycastTarget = false;
                _victoryLines[i] = t;
            }
        }
        if (_victoryLinesRoutine != null) StopCoroutine(_victoryLinesRoutine);
        bool any = lines != null && lines.Length > 0;
        _victoryLinesRoot.gameObject.SetActive(any);
        if (any) _victoryLinesRoutine = StartCoroutine(PlayVictoryLines(lines));
    }

    /// <summary>한 줄씩 천천히 떠오른다. 사신이 혼잣말하는 속도.</summary>
    private System.Collections.IEnumerator PlayVictoryLines(string[] lines)
    {
        for (int i = 0; i < _victoryLines.Length; i++)
        {
            _victoryLines[i].text = i < lines.Length ? lines[i] : "";
            _victoryLines[i].alpha = 0f;
        }
        yield return new WaitForSecondsRealtime(1.0f);
        for (int i = 0; i < _victoryLines.Length && i < lines.Length; i++)
        {
            var r = _victoryLines[i].rectTransform;
            var home = new Vector2(0f, 60f - i * 60f);
            for (float e = 0f; e < 0.9f; e += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(e / 0.9f);
                _victoryLines[i].alpha = k;
                r.anchoredPosition = home + new Vector2(0f, -10f * (1f - k));
                yield return null;
            }
            _victoryLines[i].alpha = 1f;
            r.anchoredPosition = home;
            yield return new WaitForSecondsRealtime(lines[i].Trim('…', '.').Length == 0 ? 1.2f : 0.9f);   // "……" 한 줄은 조금 더 머문다
        }
        _victoryLinesRoutine = null;
    }

    private Coroutine _soulCountRoutine;

    private System.Collections.IEnumerator CountUpSoul(int target)
    {
        const float duration = 0.9f;
        yield return new WaitForSecondsRealtime(0.35f);   // VICTORY 그림이 먼저 자리 잡게
        for (float e = 0f; e < duration; e += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(e / duration), 3f);   // 끝에서 느려짐
            if (_soulRewardText != null) _soulRewardText.text = $"소울 +{Mathf.RoundToInt(target * k)}";
            yield return null;
        }
        if (_soulRewardText != null) _soulRewardText.text = $"소울 +{target}";
        GameSfx.PlayPickup();
        _soulCountRoutine = null;
    }

    private static bool IsEndingBoss(BossTemp boss) => boss != null && boss.bossId is "king" or "death2";

    private void SetupVictoryReward()
    {
        if (victoryPanel == null || _victorySetupDone) return;

        // The victory illustration already contains four hand-painted reward bubbles.
        // Turn them into the requested reward UI while preserving the scene artwork.
        Transform rewards = victoryPanel.transform.Find("Rewards");
        if (rewards == null || rewards.childCount < 2)
        {
            Debug.LogWarning("Victory/Rewards 오브젝트를 찾을 수 없어 전리품 UI를 만들지 못했습니다.", this);
            return;
        }

        _victorySetupDone = true;
        TMP_FontAsset font = victoryPanel.GetComponentInChildren<TextMeshProUGUI>(true)?.font;

        // 소울은 자동 지급이라 고를 것이 없다: '보상' 제목·회복·'상관없음' 말풍선은 끄고(그림은 남겨 둠, 나중에 보상 선택용),
        // 소울 말풍선 하나만 가운데에 두고 받은 양을 숫자로 올려 보여 준다. 누르는 버튼이 아니다.
        for (int i = 0; i < rewards.childCount; i++) rewards.GetChild(i).gameObject.SetActive(i == 1);
        Transform soulBubble = rewards.GetChild(1);
        var soulRect = (RectTransform)soulBubble;
        soulRect.anchoredPosition = new Vector2(0f, soulRect.anchoredPosition.y);
        var bubbleGraphic = soulBubble.GetComponent<Graphic>();
        if (bubbleGraphic != null) bubbleGraphic.raycastTarget = false;
        _soulRewardText = AddRewardText(soulBubble, "소울 +0", font, 44);

        // 넘어가는 길은 오른쪽 아래 화살표 하나. 게임 표준 버튼처럼 글자를 붙인다(패배 화면 '나가기' 글자 모양 사용).
        _victoryContinueButton = victoryPanel.transform.childCount > 0
            ? victoryPanel.transform.GetChild(0).gameObject
            : null;
        LabelContinueArrow(_victoryContinueButton, "계속");
    }

    private bool _victorySetupDone;

    private void LabelContinueArrow(GameObject arrowRoot, string value)
    {
        if (arrowRoot == null) return;
        var label = arrowRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        var arrow = arrowRoot.transform.childCount > 0 ? arrowRoot.transform.GetChild(0) as RectTransform : null;
        if (label == null || arrow == null) return;
        var like = defeatPanel != null ? defeatPanel.GetComponentInChildren<TextMeshProUGUI>(true) : null;   // 패배 화면 '나가기'
        if (like != null) { label.font = like.font; label.fontSize = like.fontSize; label.color = like.color; label.fontStyle = like.fontStyle; }
        label.text = value;
        label.alignment = TextAlignmentOptions.Left;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        var lr = label.rectTransform;
        lr.anchorMin = lr.anchorMax = new Vector2(1f, 0.5f);   // 화살표 오른쪽에 붙임
        lr.pivot = new Vector2(0f, 0.5f);
        lr.anchoredPosition = new Vector2(6f, 0f);
        lr.sizeDelta = new Vector2(160f, arrow.rect.height);
    }

    private static TextMeshProUGUI AddRewardText(Transform parent, string value, TMP_FontAsset font, float fontSize)
    {
        var textObject = new GameObject(value, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);
        var rect = (RectTransform)textObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.font = font;
        label.fontSize = fontSize;
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    public void ClaimVictorySoul()
    {
        if (_victoryRewardClaimed) return;
        _victoryRewardClaimed = true;
        RunSave.ClearRewardPending(); // 보상은 여정당 한 번만 받는다.

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddSoul(VictorySoul);
        }
        else
        {
            // Allows the battle scene's standalone editor test to exercise the reward.
            PlayerPrefs.SetInt("Soul", PlayerPrefs.GetInt("Soul", 0) + VictorySoul);
            PlayerPrefs.Save();
        }

        if (_victoryContinueButton != null) _victoryContinueButton.SetActive(true);
        if (_soulCountRoutine != null) StopCoroutine(_soulCountRoutine);
        _soulCountRoutine = StartCoroutine(CountUpSoul(VictorySoul));
    }

    [DebugButton]
    public void ShowDefeatPanel()
    {
        defeatPanel.SetActive(true);
        // 패배하면 여정은 끝난다. 다음 출격은 새 여정이다.
        RunSave.Delete();
        RecordAchievement("ACH-7", 100);
        StartCoroutine(FadeToDefeatMusic());   // 전투 음악 → 패배 음악 (JSW, 2026-11-05)
    }

    // 전투 음악을 1초 동안 줄인 뒤 패배 음악(Resources/패배시음악)을 반복 재생한다.
    private System.Collections.IEnumerator FadeToDefeatMusic()
    {
        var clip = Resources.Load<AudioClip>("패배시음악");
        if (clip == null) yield break;
        const float fade = 1f;
        float v1 = bgmSource != null ? bgmSource.volume : 1f;
        float v2 = bgmSourceLoop != null ? bgmSourceLoop.volume : 1f;
        for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / fade;
            if (bgmSource != null) bgmSource.volume = v1 * k;
            if (bgmSourceLoop != null) bgmSourceLoop.volume = v2 * k;
            yield return null;
        }
        PlayBGM(clip);   // 반복 재생 (loop)
        if (bgmSource != null) bgmSource.volume = v1;
        if (bgmSourceLoop != null) bgmSourceLoop.volume = v2;
    }

    // 승리 화면에서 넘어갈 때(화살표·"상관없음"): 마을의 여정 지도로 돌아가 다음 장소를 고른다.
    [DebugButton]
    public void ShowWaysPanel()
    {
        victoryPanel.SetActive(false);
        RunSave.ClearRewardPending();
        JourneyLink.ReturnToMap(justCleared: true);
    }

    /// <summary>지도에서 고른 보스와 바로 싸운다. 첫 보스면 여정을 시작하고 편성을 고정한다.</summary>
    private void StartFromMap(string bossId)
    {
        EnsureFormationLoaded();
        if (RunSave.HasRun) ApplySavedFormation();
        else RunSave.Begin(currentUnits, currentItems);

        var catalog = remainBoss.Concat(new[] { bossKing, bossDeath1, bossDeath2 })
            .Where(b => b != null).GroupBy(b => b.bossId).ToDictionary(g => g.Key, g => g.First());
        appearedBoss.Clear();
        foreach (var id in RunSave.ClearedBossIds)
            if (catalog.TryGetValue(id, out var cleared)) { appearedBoss.Add(cleared); remainBoss.Remove(cleared); }
        if (!catalog.TryGetValue(bossId, out var boss))
        {
            Debug.LogWarning($"지도에서 고른 보스 '{bossId}'를 찾지 못해 마을로 돌아갑니다.", this);
            JourneyLink.ReturnToMap(false);
            return;
        }
        if (boss == bossKing) RecordAchievement("ACH-18", 1);

        entrancePanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(false);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        waysPanel.gameObject.SetActive(false);
        arrow.SetActive(false);
        _decidedStage = -1;
        _waysChoicesReady = false;

        remainBoss.Remove(boss);
        appearedBoss.Add(boss);
        RunSave.SetCurrentBoss(boss);
        boardPanel.gameObject.SetActive(true);
        boardPanel.boss = boss;
        boardPanel.ShowCutScene();
    }

    /// <summary>튜토리얼: 기사·궁수·도적, 아이템 없음, 안내자 보스. 여정 저장(RunSave)은 건드리지 않는다.</summary>
    private void StartTutorial()
    {
        var roster = FormationRoster();
        for (int k = 0; k < 3; k++)
        {
            var unit = roster.FirstOrDefault(u => u.id == TutorialScript.UnitIds[k]);
            if (unit == null)
            {
                Debug.LogError($"튜토리얼 캐릭터(id {TutorialScript.UnitIds[k]})를 찾지 못해 마을로 갑니다.", this);
                SceneManager.LoadScene("Main");
                return;
            }
            currentUnits[k] = unit;
        }
        currentItems = new ItemData[3];
        // 튜토리얼 아이템: 소금물 (4턴에 HP +1)
        if (ItemManager.Instance != null) currentItems[0] = ItemManager.Instance.items.FirstOrDefault(it => it != null && it.id == TutorialScript.ItemId);
        // 새 게임 첫 튜토리얼은 마을을 안 거쳐서 ItemManager가 아직 없다 → 씬에 직접 연결해 둔 소금물을 쓴다 (2026-11-04)
        if (currentItems[0] == null) currentItems[0] = tutorialItem;

        entrancePanel.gameObject.SetActive(false);
        cutScenePanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(false);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        waysPanel.gameObject.SetActive(false);
        arrow.SetActive(false);
        _decidedStage = -1;

        var boss = TutorialScript.CreateBoss(remainBoss.FirstOrDefault(b => b != null && b.bgmLoop != null));
        appearedBoss.Clear();
        appearedBoss.Add(boss);
        boardPanel.boss = boss;
        boardPanel.reaperCurrentHp = boardPanel.reaperMaxHp;
        TutorialDirector.Create();
        boardPanel.gameObject.SetActive(true);
        boardPanel.ShowCutScene();
    }

    /// <summary>지도에서 꼬마 사신의 편성 버튼으로 들어온 경우. 여정 중이면 정비 제한이 걸린다.</summary>
    private void OpenFormationFromMap()
    {
        EnsureFormationLoaded();
        if (RunSave.HasRun) ApplySavedFormation();
        JourneyRefit.Begin(currentUnits, currentItems);
        OpenEntrancePanel(true);
    }

    // 예전 3지선다 화면 (지금은 쓰지 않음, 지도 기능으로 대체)
    private void ShowWaysPanelLegacy()
    {
        victoryPanel.SetActive(false);
        if (appearedBoss.Count == 4)
        {
            var boss = GetFinalBoss();
            if (boss == bossKing)
            {
                RecordAchievement("ACH-18", 1);
            } 
            appearedBoss.Add(boss);
            boardPanel.boss = boss;
            waysPanel.gameObject.SetActive(false);
            _decidedStage = -1;
            RunSave.SetCurrentBoss(boss);
            boardPanel.ShowCutScene();
            return;
        }
        
        // 승리 화면을 떠났으므로 보상 대기 상태를 해제한다("상관없음" 선택 포함).
        RunSave.ClearRewardPending();
        waysPanel.gameObject.SetActive(true);
        foreach (var stage in stages)
            if (stage.stageImage != null) UIHoverScale.Attach(stage.stageImage.gameObject, 1.03f);
        UIHoverScale.Attach(waysBackButton);
        if (waysBackButton != null) waysBackButton.SetActive(appearedBoss.Count == 0);
        if (_waysChoicesReady) return;
        if (TryRestoreSavedChoices()) return;
        remainBoss.Shuffle();
        for (int i = 0; i < 3; i++)
        {
            if (remainBoss.Count == 0)
            {
                waysPanel.gameObject.SetActive(false);
                Debug.LogWarning("선택 가능한 보스가 없습니다.", this);
                return;
            }
            var temp = remainBoss[i < remainBoss.Count ? i : 0];
            stages[i].bossTemp = temp;
            stages[i].stageImage.sprite = temp.stageSprite;
            stages[i].stageName.text = temp.stageName;
        }
        _waysChoicesReady = true;
        // 껐다 켜서 경로 후보를 다시 뽑는 것을 막는다. 첫 경로는 여정 시작 전이라 저장되지 않는다.
        RunSave.SaveChoices(stages.Select(stage => stage.bossTemp));
    }

    private bool TryRestoreSavedChoices()
    {
        var ids = RunSave.ChoiceIds;
        if (ids.Count != stages.Length) return false;
        var choices = ids.Select(id => remainBoss.FirstOrDefault(b => b != null && b.bossId == id)).ToArray();
        if (choices.Any(b => b == null)) return false;
        for (int i = 0; i < stages.Length; i++)
        {
            stages[i].bossTemp = choices[i];
            stages[i].stageImage.sprite = choices[i].stageSprite;
            stages[i].stageName.text = choices[i].stageName;
        }
        _waysChoicesReady = true;
        return true;
    }

    public void BackFromWays()
    {
        if (appearedBoss.Count != 0) return;
        _decidedStage = -1;
        arrow.SetActive(false);
        waysPanel.gameObject.SetActive(false);
        OpenEntrancePanel(false);
    }

    public BossTemp GetFinalBoss()
    {
        bool afterlife = currentItems.Any(i => i != null && i.id == "22")
            && currentItems.Any(i => i != null && i.id == "23")
            && currentUnits.Any(i => i != null && i.id == 9);
        return afterlife ? bossDeath1 : bossKing;
    }

    /// <summary>저장된 체크포인트에서 여정을 복원한다. 입구/편성 화면을 거치지 않으므로 편성은 바뀌지 않는다.</summary>
    private void ResumeRun()
    {
        EnsureFormationLoaded();
        ApplySavedFormation();

        var catalog = remainBoss.Concat(new[] { bossKing, bossDeath1, bossDeath2 })
            .Where(b => b != null).GroupBy(b => b.bossId).ToDictionary(g => g.Key, g => g.First());
        BossTemp Find(string id) => id != null && catalog.TryGetValue(id, out var boss) ? boss : null;

        appearedBoss.Clear();
        foreach (var id in RunSave.ClearedBossIds)
        {
            var boss = Find(id);
            if (boss == null) { Debug.LogWarning($"저장된 보스 '{id}'를 찾지 못해 건너뜁니다.", this); continue; }
            appearedBoss.Add(boss);
            remainBoss.Remove(boss);
        }

        entrancePanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(false);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        waysPanel.gameObject.SetActive(false);
        arrow.SetActive(false);
        _decidedStage = -1;
        _waysChoicesReady = false;

        var current = Find(RunSave.CurrentBossId);
        if (RunSave.HasCurrentBattle && current == null)
            Debug.LogWarning($"저장된 보스 '{RunSave.CurrentBossId}'를 찾지 못해 경로 선택부터 이어합니다.", this);

        boardPanel.gameObject.SetActive(true);
        if (current != null)
        {
            // 고른 보스와의 전투를 등장 장면부터 다시 시작한다.
            remainBoss.Remove(current);
            appearedBoss.Add(current);
            boardPanel.boss = current;
            boardPanel.ShowCutScene();
        }
        else if (RunSave.RewardPending && appearedBoss.Count > 0)
        {
            // 보상을 받기 전에 나간 경우: 업적/처치 기록은 이미 남았으므로 승리 화면만 다시 연다.
            boardPanel.boss = appearedBoss[^1];
            OpenVictoryScreen();
            ClaimVictorySoul();   // 지급 직전에 꺼졌던 경우: 여기서 한 번 지급 (대기 표시도 지워짐)
        }
        else
        {
            ShowWaysPanel();
        }
    }

    private void ApplySavedFormation()
    {
        var roster = FormationRoster();
        var unitIds = RunSave.UnitIds;
        if (unitIds.Count == currentUnits.Length)
        {
            var units = unitIds.Select(id => roster.FirstOrDefault(u => u.id == id)).ToArray();
            if (units.All(u => u != null) && units.Distinct().Count() == units.Length)
                for (int i = 0; i < units.Length; i++) currentUnits[i] = units[i];
            else Debug.LogWarning("저장된 여정 편성을 복원하지 못해 마지막 편성을 사용합니다.", this);
        }

        var itemIds = RunSave.ItemIds;
        var catalog = ItemManager.Instance != null ? ItemManager.Instance.items : null;
        if (catalog != null && itemIds.Count == currentItems.Length)
            for (int i = 0; i < currentItems.Length; i++)
                currentItems[i] = string.IsNullOrEmpty(itemIds[i])
                    ? null
                    : catalog.FirstOrDefault(item => item != null && item.id == itemIds[i]);
    }

    [DebugButton]
    public void ShowCutScene(string cutsceneName)
    {
        cutScenePanel.gameObject.SetActive(true);
        cutScenePanel.SetCutScene(cutsceneName);
        PlayBGM(endingClip);
    }

    public void OnClickStage(int i)
    {
        if (_decidedStage == i)
        {
            _waysChoicesReady = false;
            var boss = stages[i].bossTemp;
            // 첫 보스를 고른 순간 여정이 시작되고, 이때의 편성으로 여정 끝까지 고정된다.
            if (appearedBoss.Count == 0) RunSave.Begin(currentUnits, currentItems);
            remainBoss.Remove(boss);
            appearedBoss.Add(boss);
            RunSave.SetCurrentBoss(boss);
            boardPanel.boss = boss;
            arrow.SetActive(false);
            waysPanel.color = new Color(255, 255, 255, 250);
            waysPanel.gameObject.SetActive(false);
            _decidedStage = -1;
            boardPanel.ShowCutScene();
            return;
        }
        
        _decidedStage = i;
        arrow.SetActive(true);
        var vector3 = arrow.transform.position;
        vector3.x = stages[i].stageImage.transform.position.x;
        arrow.transform.position = vector3;
    }

    public void ClearBoss(BossTemp boss)
    {
        if (boss == null) return;
        RecordAchievement("ACH-2", 1);
        RecordAchievement("ACH-8", 1);
        RecordAchievement("ACH-9", 1);

        switch (boss.bossId)
        {
            case "king":
                PlayerPrefs.SetInt("IsEnding", 1);
                RecordAchievement("ACH-19", 1);
                RecordAchievement("ACH-20", 1);
                ShowCutScene("Ending1");
                if (PlayerPrefs.GetInt("IsEnding") == 0) PlayerPrefs.SetInt("IsEnding", 1);
                waysPanel.gameObject.SetActive(false);
                break;
            case "death2":
                Debug.Log("b");
                PlayerPrefs.SetInt("IsEnding", 2);
                RecordAchievement("ACH-28", 1);
                RecordAchievement("ACH-29", 1);
                ShowCutScene("Ending2");

                waysPanel.gameObject.SetActive(false);
                break;
            case "pope":
                RecordAchievement("ACH-11", 1);
                break;
            case "noble":
                RecordAchievement("ACH-12", 1);
                break;
            case "instructor":
                RecordAchievement("ACH-13", 1);
                break;
            case "subject":
                RecordAchievement("ACH-14", 1);
                break;
            case "secretary":
                RecordAchievement("ACH-15", 1);
                break;
            case "fusion":
                RecordAchievement("ACH-16", 1);
                break;
            case "door":
                RecordAchievement("ACH-17", 1);
                break;
        }

        PlayerPrefs.Save();
    }

    private bool _warnedMissingAchievements;

    private void RecordAchievement(string id, int amount)
    {
        // 전투 씬을 직접 실행한 테스트에서도 결과 화면은 정상적으로 진행한다.
        var achievements = AchievementManager.Instance;
        if (achievements != null)
        {
            achievements.AddProgress(id, amount);
        }
        else if (!_warnedMissingAchievements)
        {
            _warnedMissingAchievements = true;
            Debug.LogWarning("업적 관리자가 없어 이번 전투 테스트의 업적 기록을 건너뜁니다. 업적 검증은 시작 씬부터 실행하세요.", this);
        }
    }

    [DebugButton]
    public void BackToVillage()
    {
        if (JourneyRefit.FromMap)
        {
            JourneyRefit.Finish(currentUnits, currentItems);
            JourneyLink.ReturnToMap(false);
            return;
        }
        SceneManager.LoadScene("Main");
    }

    public void PlayBGM(AudioClip loopBgm, AudioClip introBgm = null)
    {
        bgmSourceLoop.loop = false;
        bgmSourceLoop.Stop();
        bgmSource.loop = false;
        bgmSource.Stop();
        
        if (introBgm != null)
        {
            var startTime = AudioSettings.dspTime + 0.1;
            bgmSource.clip = introBgm;
            bgmSource.loop = false;
            bgmSource.PlayScheduled(startTime);
        
            var loopStartTime = startTime + introBgm.length;
        
            bgmSourceLoop.clip = loopBgm;
            bgmSourceLoop.loop = true;
            bgmSourceLoop.PlayScheduled(loopStartTime);
        }
        else
        {
            bgmSource.clip = loopBgm;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }
}
