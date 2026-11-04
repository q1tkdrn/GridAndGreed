using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardPanel : MonoBehaviour
{
    [SerializeField] private BattleManagerTemp battleManagerTemp;
    
    [Header("CutScene")]
    [SerializeField] private GameObject cutScene;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private Image bossCutSceneImage;
    [SerializeField] private RectTransform reaperCutScene;
    [SerializeField] private RectTransform bossCutScene;
    [SerializeField] private float speed;
    [SerializeField] private Animator animator;
    
    [Header("Turn")]
    public ETurn turn = ETurn.Start;
    public int turnCount = 1;
    [SerializeField] private TextMeshProUGUI turnTextUI;
    
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI textBox;
    [Tooltip("보스가 직접 하는 말(따옴표로 시작하는 줄)을 출력할 대사창. 비워 두면 위 대사창에 모두 출력한다.")]
    [SerializeField] private TextMeshProUGUI bossTextBox;
    [Tooltip("보스 대사창(검은 판 + 테두리 + 글자)을 묶은 그룹. 보스가 말할 때만 나타났다가 서서히 사라진다.")]
    [SerializeField] private CanvasGroup bossDialogGroup;
    [SerializeField, Min(0f)] private float bossTypingDelay = 0.06f;   // 보스 대사 글자 사이 시간 (천천히)
    [SerializeField, Min(0f)] private float bossLineHold = 2.5f;       // 다 나온 뒤 머무는 시간
    [SerializeField, Min(0f)] private float bossFadeDuration = 0.6f;   // 사라지는 시간
    private Coroutine _bossFadeRoutine;
    private bool _bossTyping;        // 보스 대사 글자가 찍히는 중
    private bool _bossSkipHold;      // 머무는 중에 새 대사가 와서 바로 넘어가야 함
    private bool _bossInterrupt;     // 승리·패배·페이즈 대사가 와서 하던 말을 끊어야 함
    private bool _printingImportant; // PrintImportantText 로 들어온 대사인지
    [SerializeField] private float delay = 0.1f;
    private float _delay;
    private Queue<string> _textQueue = new Queue<string>();
    private bool _isPrinting = false; 
    private float _bossDelay;
    private Queue<string> _bossTextQueue = new Queue<string>();
    private bool _isBossPrinting = false;
    private bool _isFinishingBattle;
    // 위 기록창: 줄은 한 번에 출력하고, 새 줄은 잠깐 흐리게 나타나며 금빛으로 강조됐다가 원래 색으로 돌아온다.
    private struct LogEntry { public string text; public float time; }
    private readonly List<LogEntry> _logEntries = new List<LogEntry>();
    private string _pinnedLog;   // 위 대사창 맨 위에 고정되는 줄 (보스 특성)
    private Coroutine _logRenderRoutine;
    private const float LogFadeInTime = 0.15f;
    private const float LogHighlightTime = 0.8f;
    private static readonly Color LogHighlightColor = new Color(1f, 0.85f, 0.55f);
    
    [Space(1)]
    
    [Header("Reaper")]
    public int reaperMaxHp;
    public int reaperCurrentHp;
    [SerializeField] private TextMeshProUGUI reaperHp;
    [SerializeField] private Slider reaperSlider;
    public int actionPoint;
    [SerializeField] private GameObject actionPointPrefab;
    [SerializeField] private GameObject actionPoints;
    [SerializeField] private Sprite[] actionPointsList;
    private List<GameObject> _actionPointsList = new List<GameObject>();
    
    private static readonly int EnableGlitch = Shader.PropertyToID("_EnableGlitch");
    private Material[] _unitMaterials;
    private Vector2[] _unitSizes;
    private Coroutine[] _moveAnimations;
    private Coroutine[] _hitAnimations;
    private Sprite[] _shotRestoreSprites = new Sprite[3]; // 심판 동작 중 원래 그림 (중간에 끊겨도 되돌리기 위해)

    [Serializable]
    private struct Unit
    {
        public Image image;
        public GameObject popUp;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public Image unit;
        public bool isAnimPlaying;
    }
    
    [SerializeField] private Unit[] units = new Unit[3];
    private UnitInfoView _info;   // 초상화 정보창 + 피격 해골
    [SerializeField] private ItemUI[] items = new ItemUI[3];
    
    [Header("Boss")]
    public int bossMaxHp;
    public int bossCurrentHp;
    public int willPower;
    public BossTemp boss;
    public int phase;
    [SerializeField] private TextMeshProUGUI bossHp;
    [SerializeField] private TextMeshProUGUI willPowerText;
    [SerializeField] private Slider bossSlider;
    [SerializeField] private Image bossImage;
    private BossHpBarFx _bossHpFx;        // 잔상 + 심판 미리보기 (JSW/Script/BossHpBarFx.cs)
    private int _judgmentPreviewUnit = -1; // 마우스를 올린 캐릭터 (-1이면 없음)

    private bool _isWin = false;
    private bool _isLose = false; 
    public bool IsBattleOver => _isWin || _isLose;
    public ItemBattleState ItemEffects { get; private set; } = new ItemBattleState(null);
    // 전투 규칙 (모든 보스 + 튜토리얼). JSW/Script/Trial/BellkeeperTrial.cs
    public BellkeeperTrial Trial { get; private set; }
    private int _baseReaperMaxHp = -1;
    public TMP_FontAsset UiFont => turnTextUI.font;

    /// <summary>튜토리얼: 미뤄 둔 턴 끝(보스 공격)을 이어서 진행한다.</summary>
    public void TutorialContinue() => UpdateActionPoint(actionPoint);

    /// <summary>튜토리얼 말풍선이 가리킬 화면 요소.</summary>
    public RectTransform TutorialAnchorRect(TutorialAnchor anchor, int unit) => anchor switch
    {
        TutorialAnchor.Portrait => unit >= 0 && unit < units.Length ? units[unit].image.rectTransform : null,
        TutorialAnchor.ActionPoints => actionPoints != null ? (RectTransform)actionPoints.transform : null,
        TutorialAnchor.WillPower => willPowerText != null ? willPowerText.rectTransform : null,
        TutorialAnchor.PlayerHp => reaperSlider != null ? (RectTransform)reaperSlider.transform : null,
        TutorialAnchor.Item => items.Length > 0 && items[0] != null ? (RectTransform)items[0].transform : null,
        _ => null
    };

    private void BeginBossTurn() => Trial?.BeginTurn(turnCount);

    private void EndBossTurn()
    {
        if (IsBattleOver) return;
        Trial?.EndTurn();
        if (IsBattleOver) return;
        // A defeated boss cannot deal turn-end damage. Use the usual HP path for the contract.
        if (!IsBattleOver && willPower > 0)
        {
            UpdateReaperHp(reaperCurrentHp - willPower);
        }
    }

    public void SpendAction(bool judgment = false)
    {
        if (IsBattleOver) return;
        UpdateActionPoint(Mathf.Max(0, actionPoint - 1));
    }
    private bool _isResolvingBossPattern;
    private Coroutine _bossPatternCoroutine;

    public enum ETurn
    {
        Start = 0,
        Place = 1,
        BossReady = 2,
        Player = 3,
        Attack = 4,
        BossPattern = 5,
        End = 6
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _bossPatternCoroutine = null;
        _isResolvingBossPattern = false;
        _isPrinting = false;
        _textQueue.Clear();
        _isBossPrinting = false;
        _bossTextQueue.Clear();
        _bossTyping = _bossSkipHold = _bossInterrupt = false;
        _bossFadeRoutine = null;
        if (bossDialogGroup != null) bossDialogGroup.alpha = 0f;
        _isFinishingBattle = false;
        _logRenderRoutine = null;
        _judgmentPreviewUnit = -1;
        JudgeShot.Cleanup();
        JudgeCutIn.Abort();
        if (battleManagerTemp != null) battleManagerTemp.ClearBossPatternPreview();
    }

    private void OnEnable()
    {
        if (_unitMaterials == null)
        {
            _unitMaterials = new Material[units.Length];
            _unitSizes = new Vector2[units.Length];
            _moveAnimations = new Coroutine[units.Length];
            _hitAnimations = new Coroutine[units.Length];
            for (int i = 0; i < units.Length; i++)
            {
                _unitMaterials[i] = Instantiate(units[i].unit.material);
                units[i].unit.material = _unitMaterials[i];
                _unitSizes[i] = units[i].unit.rectTransform.sizeDelta;
            }
        }
        // 판 위 캐릭터: 숨쉬기 · 이동 깡충 · 피격 흔들림 (JSW/Script/UnitMotion.cs)
        for (int i = 0; i < units.Length; i++) UnitMotion.For(units[i].unit);
        bossHp.textWrappingMode = TextWrappingModes.NoWrap;
        bossHp.enableAutoSizing = true;
        bossHp.fontSizeMin = 18;
        bossHp.fontSizeMax = 40;
        _bossHpFx = BossHpBarFx.Attach(bossSlider, bossHp, () => bossCurrentHp, () => bossMaxHp);
        for(int i=0;i<3;i++)
        {
            var selectedUnit = BattleDisplayManager.GetInstance().currentUnits[i];
            if (!selectedUnit.IsSkinUnlocked(selectedUnit.currentSkin)) selectedUnit.currentSkin = 0;
            units[i].image.sprite = BattleDisplayManager.GetInstance().currentUnits[i].currentSkin switch
            {
                0 => BattleDisplayManager.GetInstance().currentUnits[i].defaultSkin,
                1 => BattleDisplayManager.GetInstance().currentUnits[i].skin1,
                2 => BattleDisplayManager.GetInstance().currentUnits[i].skin2,
                3 => BattleDisplayManager.GetInstance().currentUnits[i].skin3,
                _ => units[i].image.sprite
            };
            units[i].nameText.text = BattleDisplayManager.GetInstance().currentUnits[i].unitName;
            units[i].descriptionText.text = BattleDisplayManager.GetInstance().currentUnits[i].abilityText;
            units[i].descriptionText.enableAutoSizing = true;
            units[i].descriptionText.fontSizeMin = 20;
            units[i].descriptionText.fontSizeMax = 36;
            
            items[i].itemData = BattleDisplayManager.GetInstance().currentItems[i];
            items[i].Init();
            items[i].ShowAsEquipped();   // 전투 칸은 들고 온 아이템이라 자물쇠 없음 (튜토리얼은 소금물을 빌려 줌)
        }
        if (_info == null) _info = new UnitInfoView(units.Select(u => u.image).ToArray(), UiFont);
        ResetUnitVisuals();
    }

    private void ResetUnitVisuals()
    {
        if (_unitMaterials == null) return;
        for (int i = 0; i < units.Length; i++)
        {
            if (_moveAnimations[i] != null) StopCoroutine(_moveAnimations[i]);
            if (_hitAnimations[i] != null) StopCoroutine(_hitAnimations[i]);
            _moveAnimations[i] = _hitAnimations[i] = null;
            if (i < _shotRestoreSprites.Length && _shotRestoreSprites[i] != null)
            {
                units[i].unit.sprite = _shotRestoreSprites[i];
                _shotRestoreSprites[i] = null;
            }
            units[i].unit.material.SetFloat(EnableGlitch, 0);
            units[i].unit.rectTransform.sizeDelta = _unitSizes[i];
            units[i].isAnimPlaying = false;
            units[i].popUp.SetActive(false);
            _info?.Hide(i);
        }
    }

    private void OnDestroy()
    {
        if (_unitMaterials == null) return;
        foreach (var material in _unitMaterials) Destroy(material);
    }

    public void ShowCutScene()
    {
        bossCutSceneImage.sprite = BattleDisplayManager.GetInstance().appearedBoss[^1].bossSprite;
        bossImage.sprite = BattleDisplayManager.GetInstance().appearedBoss[^1].bossSprite;
        bossNameText.text = BattleDisplayManager.GetInstance().appearedBoss[^1].bossName;
        cutScene.SetActive(true);
        BattleDisplayManager.GetInstance().PlayBGM(boss.bgmLoop, boss.bgmStart);
        // 입장 화면 (JSW/Script/StageIntro.cs, 2026-11-04): 보스 일러스트 + 스테이지 문구. 예전 VS 애니메이션(PlayCutSceneAnim)은 안 씀
        var intro = cutScene.GetComponent<StageIntro>();
        if (intro == null) intro = cutScene.AddComponent<StageIntro>();
        StartCoroutine(intro.Play(BattleDisplayManager.GetInstance().appearedBoss[^1], () =>
        {
            cutScene.SetActive(false);
            Init();
        }));
    }

    public IEnumerator PlayCutSceneAnim()
    {
        animator.Play("CutScene");
        float length = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(length);
        
        StartCoroutine(HideCutScene());
    }

    private IEnumerator HideCutScene()
    {
        yield return new WaitForSeconds(1.5f); 
        cutScene.SetActive(false);
        Init();
    }

    public void Init()
    {
        ResetUnitVisuals();
        Trial?.Dispose();
        if (_baseReaperMaxHp < 0) _baseReaperMaxHp = reaperMaxHp;
        Trial = BellkeeperTrial.IsTrialBoss(boss) ? new BellkeeperTrial(this, battleManagerTemp) : null;
        if (Trial == null) BellkeeperTrial.SoulBonus = 0f;
        reaperMaxHp = Trial != null ? Trial.MaxPlayerHp : _baseReaperMaxHp;
        if (_bossPatternCoroutine != null) StopCoroutine(_bossPatternCoroutine);
        _bossPatternCoroutine = null;
        // The two afterlife phases are one battle: do not refill the contract in phase two.
        // 아이템 효과는 규칙(BellkeeperTrial)이 처리하고, 여기서는 계약서(23)만 다룬다.
        if (boss.bossId != "death2")
            ItemEffects = new ItemBattleState(BattleDisplayManager.GetInstance().currentItems
                .Where(item => item != null).Select(item => item.id));
        _isLose = _isWin = false;
        _isResolvingBossPattern = false;
        bossImage.sprite = BattleDisplayManager.GetInstance().appearedBoss[^1].bossSprite;
        if (boss.bossId != "death2")
        {
            reaperCurrentHp = reaperMaxHp;
        }
        // 각 보스 에셋의 스탯을 새로 읽어 이전 페이즈 HP/의지력이 남지 않도록 한다.
        bossMaxHp = Trial != null ? Trial.MaxBossHp : 100;
        willPower = Trial != null ? BellkeeperTrial.StartWillPower : 0;
        willPowerText.text = $"의지력: {willPower}";
        // 기존 OnPhaseChange의 0 기반 규약: 0 = 1페이즈, 1 = 2페이즈.
        phase = boss.bossId == "death2" ? 1 : 0;
        bossCurrentHp = bossMaxHp;
        UpdateBossHp(bossCurrentHp);
        UpdateReaperHp(reaperCurrentHp);
        _logEntries.Clear();
        _pinnedLog = Trial != null && !string.IsNullOrEmpty(Trial.TraitText) ? "<size=75%><color=#D9B8FF>" + Trial.TraitText + "</color></size>" : null;
        textBox.text = "";
        if (bossTextBox != null) bossTextBox.text = "";
        HideBossDialog(instant: true);
        // 전투 시작 서술("당신은 ~에게 달려들었다!")은 나중에 별도 연출로 보여줄 예정이라 지금은 빼고, 보스가 직접 하는 말만 남긴다.
        PrintText(OnlyBossSpeech(boss.battleStart) + (Trial != null && !string.IsNullOrEmpty(Trial.TraitSpeech) ? "\n" + Trial.TraitSpeech : ""));
        if (_pinnedLog != null) RenderLog();
        PrintText(GetBossText(boss.turnStart, 0));
        Trial?.OnBattleStart();
        turnCount = 1;
        turn = ETurn.Start;
        BeginBossTurn();
        if (IsBattleOver) return;
        battleManagerTemp.Init();
    }

    [DebugButton("다음 턴")]
    public void NextTurn(int i = 1)
    {
        if (IsBattleOver) return;
        if (_isResolvingBossPattern) return;
        turn += i;
        if(turn > ETurn.End) 
        {
            turn = ETurn.Start;
            turnCount++;
            PrintText(GetBossText(boss.turnStart, turnCount / 5));
        }

        var turnText = "";

        var nt = false;
        
        switch (turn)
        {
            case ETurn.Start:
                turnText = "턴 시작";
                BeginBossTurn();
                if (IsBattleOver) return;
                break;
            case ETurn.Place:
                turnText = "유닛 배치";
                TutorialDirector.OnPhase(turnCount, true);
                break;
            case ETurn.BossReady:
                turnText = "보스 공격 준비";
                battleManagerTemp.PrepareBossPattern(boss, turnCount);
                break;
            case ETurn.Player:
                turnText = "행동";
                battleManagerTemp.PrepareBossPattern(boss, turnCount);
                TutorialDirector.OnPhase(turnCount, false);
                break;
            case ETurn.Attack:
                turnText = "공격";
                PrintText(GetBossText(boss.attackedAD, turnCount / 5));
                break;
            case ETurn.BossPattern:
                turnText = "보스 공격";
                _bossPatternCoroutine = StartCoroutine(ResolveBossPattern());
                break;
            case ETurn.End:
                turnText = "턴 종료";
                battleManagerTemp.OnTurnEnd();
                EndBossTurn();
                if (IsBattleOver) return;
                nt = true;
                break;
        }

        turnTextUI.text = turnCount + " - " + turnText;
        if(nt)
        {
            NextTurn();
            return;
        }
        if(turn == ETurn.Start) battleManagerTemp.OnTurnStart();
    }
    
    [DebugButton("텍스트 출력")]
    public void PrintText(string text)
    {
        if(_isLose || _isWin) return;
        if (string.IsNullOrWhiteSpace(text)) return;

        // 보스가 직접 하는 말(따옴표로 시작하는 줄)은 보스 대사창으로, 시스템·서술은 위 대사창으로 보낸다.
        // 여러 줄이 섞인 대사는 연속된 같은 종류의 줄끼리 묶어서 순서대로 보낸다.
        var chunk = new StringBuilder();
        bool chunkIsBoss = false;
        foreach (var line in text.Split('\n'))
        {
            bool isBoss = string.IsNullOrWhiteSpace(line) ? chunkIsBoss : IsBossSpeech(line);
            if (isBoss != chunkIsBoss && chunk.Length > 0)
            {
                EnqueueText(chunk.ToString(), chunkIsBoss);
                chunk.Clear();
            }
            chunkIsBoss = isBoss;
            if (chunk.Length > 0) chunk.Append('\n');
            chunk.Append(line);
        }
        EnqueueText(chunk.ToString(), chunkIsBoss);
    }

    // 승리·패배·페이즈 전환처럼 꼭 보여야 하는 대사: 보스가 말하는 중이어도 끊고 바로 출력한다.
    public void PrintImportantText(string text)
    {
        _printingImportant = true;
        try { PrintText(text); }
        finally { _printingImportant = false; }
    }

    // 줄이 따옴표로 시작하면 보스가 직접 하는 말로 본다. ("…" “…” 「…」 『…』)
    private static bool IsBossSpeech(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.Length == 0) return false;
        char c = trimmed[0];
        return c == '"' || c == '“' || c == '”' || c == '「' || c == '『';
    }

    private void EnqueueText(string text, bool toBoss)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (toBoss && bossTextBox != null)
        {
            if (_printingImportant)
            {
                // 승리·패배·페이즈 대사: 하던 말을 끊고 바로 말한다.
                _bossTextQueue.Clear();
                if (_isBossPrinting) _bossInterrupt = true;
            }
            else if (_bossTyping)
            {
                return; // 보스가 말하는 중(글자가 찍히는 중)에 들어온 새 대사는 버린다.
            }
            else if (_isBossPrinting)
            {
                _bossSkipHold = true; // 다 말하고 머무는 중이면 바로 새 대사로 넘어간다.
            }
            _bossTextQueue.Enqueue(text);
            if (!_isBossPrinting) StartCoroutine(ProcessBossTextQueue());
        }
        else
        {
            _textQueue.Enqueue(text);
            if (!_isPrinting) StartCoroutine(ProcessTextQueue());
        }
    }

    IEnumerator ProcessTextQueue()
    {
        _isPrinting = true;
        while (_textQueue.Count > 0)
        {
            AddLogEntry(_textQueue.Dequeue());
            yield return null;
        }
        _isPrinting = false;
        yield return FinishBattleWhenTextDone();
    }

    private static string OnlyBossSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        return string.Join("\n", text.Split('\n').Where(IsBossSpeech));
    }

    private void AddLogEntry(string text)
    {
        _logEntries.Add(new LogEntry { text = text, time = Time.unscaledTime });
        TrimLog();
        RenderLog();
        if (_logRenderRoutine == null && isActiveAndEnabled) _logRenderRoutine = StartCoroutine(AnimateLog());
    }

    // 창 높이를 넘으면 가장 오래된 줄부터 통째로 지운다. (위쪽 줄이 반쯤 잘려 보이지 않게)
    private void TrimLog()
    {
        var rect = textBox.rectTransform.rect;
        float width = rect.width - textBox.margin.x - textBox.margin.z;
        float height = rect.height - textBox.margin.y - textBox.margin.w;
        while (_logEntries.Count > 1 && textBox.GetPreferredValues((_pinnedLog != null ? _pinnedLog + "\n" : "") + string.Join("\n", _logEntries.Select(e => e.text)), width, 0f).y > height)
            _logEntries.RemoveAt(0);
    }

    private void RenderLog()
    {
        var builder = new StringBuilder();
        if (_pinnedLog != null) { builder.Append(_pinnedLog); if (_logEntries.Count > 0) builder.Append('\n'); }
        float now = Time.unscaledTime;
        for (int i = 0; i < _logEntries.Count; i++)
        {
            if (i > 0) builder.Append('\n');
            var entry = _logEntries[i];
            float age = now - entry.time;
            if (age >= LogHighlightTime)
            {
                builder.Append(entry.text);
                continue;
            }
            // 새 줄: 흐리게 나타나며 금빛 → 원래 색
            var color = Color.Lerp(LogHighlightColor, textBox.color, age / LogHighlightTime);
            color.a = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(age / LogFadeInTime));
            builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(color)).Append('>').Append(entry.text).Append("</color>");
        }
        textBox.text = builder.ToString();
    }

    private IEnumerator AnimateLog()
    {
        while (_logEntries.Count > 0 && Time.unscaledTime - _logEntries[_logEntries.Count - 1].time < LogHighlightTime)
        {
            RenderLog();
            yield return null;
        }
        RenderLog();
        _logRenderRoutine = null;
    }

    // 보스 대사는 한 줄씩: 대사창이 한 번에 나타나고 → 글자가 천천히 나오고 → 잠시 머문 뒤 → 창과 글자가 함께 서서히 사라진다.
    IEnumerator ProcessBossTextQueue()
    {
        _isBossPrinting = true;
        while (_bossTextQueue.Count > 0)
        {
            var lines = _bossTextQueue.Dequeue().Split('\n');
            _bossInterrupt = false;
            for (int i = 0; i < lines.Length && !_bossInterrupt; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;
                ShowBossDialog();
                bossTextBox.text = "";
                _bossDelay = _bossTextQueue.Count >= 5 ? 0 : bossTypingDelay;
                _bossTyping = true;
                _bossSkipHold = false;
                foreach (char c in line)
                {
                    if (_bossInterrupt) break;
                    bossTextBox.text += c;
                    if (_bossDelay > 0f) yield return new WaitForSeconds(_bossDelay);
                }
                _bossTyping = false;
                if (_bossInterrupt) break;
                // 다 말한 뒤 머문다. 같은 대사의 다음 줄이 남았으면 짧게, 새 대사가 들어오면 바로 넘어간다.
                float hold = i < lines.Length - 1 ? bossLineHold * 0.5f : bossLineHold;
                for (float t = 0f; t < hold && !_bossSkipHold && !_bossInterrupt; t += Time.deltaTime)
                    yield return null;
                _bossSkipHold = false;
            }
        }
        _isBossPrinting = false;
        _bossTyping = false;
        _bossInterrupt = false;
        HideBossDialog();
        yield return FinishBattleWhenTextDone();
    }

    private void ShowBossDialog()
    {
        if (_bossFadeRoutine != null) StopCoroutine(_bossFadeRoutine);
        _bossFadeRoutine = null;
        if (bossDialogGroup != null) bossDialogGroup.alpha = 1f;
    }

    private void HideBossDialog(bool instant = false)
    {
        if (bossDialogGroup == null) return;
        if (_bossFadeRoutine != null) StopCoroutine(_bossFadeRoutine);
        _bossFadeRoutine = null;
        if (instant || bossFadeDuration <= 0f || !isActiveAndEnabled)
        {
            bossDialogGroup.alpha = 0f;
            if (bossTextBox != null) bossTextBox.text = "";
            return;
        }
        _bossFadeRoutine = StartCoroutine(FadeOutBossDialog());
    }

    private IEnumerator FadeOutBossDialog()
    {
        float from = bossDialogGroup.alpha;
        for (float t = 0f; t < bossFadeDuration; t += Time.deltaTime)
        {
            bossDialogGroup.alpha = Mathf.Lerp(from, 0f, t / bossFadeDuration);
            yield return null;
        }
        bossDialogGroup.alpha = 0f;
        if (bossTextBox != null) bossTextBox.text = "";
        _bossFadeRoutine = null;
    }

    // 두 대사창이 모두 다 출력된 뒤에 승리/패배를 처리한다.
    IEnumerator FinishBattleWhenTextDone()
    {
        if (_isPrinting || _isBossPrinting || _isFinishingBattle) yield break;
        if (!_isWin && !_isLose) yield break;
        _isFinishingBattle = true;
        yield return new WaitForSeconds(0.1f);
        _isFinishingBattle = false;
        if (TutorialDirector.Active)
        {
            // 튜토리얼: 보상·승리 화면 없이 마지막 말풍선 뒤 마을로. 지면 처음부터 다시.
            if (_isWin) TutorialDirector.Instance.Finish();
            else TutorialLink.Begin();
            yield break;
        }
        if (_isWin)
        {
            if (boss.bossId == "death1")
            {
                // 1페이즈에서는 승리 화면/보상을 표시하지 않고 다음 보스 데이터로 전환한다.
                boss = BattleDisplayManager.GetInstance().bossDeath2;
                BattleDisplayManager.GetInstance().appearedBoss.Add(boss);
                BattleDisplayManager.GetInstance().PlayBGM(boss.bgmLoop, boss.bgmStart);
                Init();
            }
            else
            {
                BattleDisplayManager.GetInstance().ShowVictoryPanel();
            }
        }
        else if(_isLose)
        {
            Trial?.ShowDefeatReport();
            BattleDisplayManager.GetInstance().ShowDefeatPanel();
        }
    }

    public void PrintDistinctText(int i = 0)
    {
        PrintText(GetBossText(boss.distinctText, i));
    }
    
    [DebugButton("플레이어 HP 업데이트")]
    public void UpdateReaperHp(int hp)
    {
        if (IsBattleOver) return;
        reaperCurrentHp = ItemEffects.ResolveHp(hp, reaperMaxHp);
        if (reaperCurrentHp <= 0)
        {
            LoseBattle();
        }
        reaperHp.text = $"{reaperCurrentHp} / {reaperMaxHp}";
        reaperSlider.value = (float) reaperCurrentHp / reaperMaxHp;
    }
    
    public void UpdateBossHp(int hp)
    {
        if (IsBattleOver) return;
        bossCurrentHp = hp;
        if (bossCurrentHp <= 0)
        {
            PrintImportantText(Trial != null ? Trial.VictoryLine : string.IsNullOrWhiteSpace(boss.win) ? "승리" : boss.win);
            bossCurrentHp = 0;
            _isWin = true;
        }
        bossHp.text = $"{bossCurrentHp} / {bossMaxHp}";
        bossSlider.value = (float) bossCurrentHp / bossMaxHp;
        Trial?.OnBossHpChanged();
    }

    [DebugButton("보스 심판")]
    public void JudgeBoss(int dmg)
    {
        JudgeBossFromUnit(dmg, -1);
    }

    public void JudgeBossFromUnit(int dmg, int unitIndex)
    {
        if (IsBattleOver) return;
        PrintText(GetBossText(boss.attackedAP, turnCount / 5));
        ApplyBossDamage(dmg, AfterlifePassiveRules.DamageSource.Judgment);
    }
    
    private int ApplyBossDamage(int dmg, AfterlifePassiveRules.DamageSource source)
    {
        if (IsBattleOver) return 0;
        int damage = Trial != null ? Trial.ResolveIncomingDamage(source, dmg) : dmg;
        if (damage <= 0) return 0;
        int previousHp = bossCurrentHp;
        UpdateBossHp(bossCurrentHp - damage);
        return previousHp - bossCurrentHp;
    }

    public void HealReaper(int amount)
    {
        if (amount <= 0 || IsBattleOver) return;
        UpdateReaperHp(reaperCurrentHp + amount);
    }

    public void HealBoss(int amount)
    {
        if (amount <= 0 || IsBattleOver) return;
        UpdateBossHp(Mathf.Min(bossCurrentHp + amount, bossMaxHp));
    }

    public void LoseBattle()
    {
        if (IsBattleOver) return;
        // HP를 강제로 0으로 만들지 않아도 꼬마 사신 사망으로 패배할 수 있다.
        PrintImportantText(string.IsNullOrWhiteSpace(boss.lose) ? "패배" : boss.lose);
        _isLose = true;
    }

    public void OnBossAttack(int dmg)
    {
        PrintText(GetBossText(boss.attack, turnCount / 5));
        UpdateReaperHp(reaperCurrentHp-dmg);
    }

    [DebugButton("보스 의지력 업데이트")]
    public void UpdateBossWillPower(int value)
    {
        value = Mathf.Max(0, value);
        if (value == 0)
        {
            PrintText(GetBossText(boss.willZero, turnCount / 5));
        }
        else if (value < willPower)
        {
            PrintText(GetBossText(boss.willDecline, turnCount / 5));
        }
        willPower = value;
        willPowerText.text = $"의지력: {willPower}";
    }
    
    [DebugButton("행동력 변경")]
    public void UpdateActionPoint(int point)
    {
        actionPoint = point;
        var temp = _actionPointsList.ToList();
        foreach (var p in temp)
        {
            Destroy(p);
        }
        _actionPointsList.Clear();
        for(int i = 0; i < actionPoint; i++)
        {
            GameObject go = Instantiate(actionPointPrefab, actionPoints.transform);
            _actionPointsList.Add(go);
            var image = go.GetComponent<Image>();
            image.sprite = actionPointsList[i % actionPointsList.Length];
        }
        
        if(actionPoint <= 0 && turn == ETurn.Player && !_isWin && !_isLose && !TutorialDirector.HoldsTurnEnd)
        {
            NextTurn();
            NextTurn();
        }
    }

    private IEnumerator ResolveBossPattern()
    {
        _isResolvingBossPattern = true;
        try
        {
            yield return battleManagerTemp.PlayBossPattern(boss, turnCount);
        }
        finally
        {
            _isResolvingBossPattern = false;
            _bossPatternCoroutine = null;
        }

        if (!_isWin && !_isLose)
        {
            NextTurn();
        }
    }

    // 캐릭터에 마우스를 올리면 지금 심판했을 때 깎일 보스 HP를 HP 바에 미리 보여준다.
    public void ShowJudgmentPreview(int i) => _judgmentPreviewUnit = i;
    public void HideJudgmentPreview(int i)
    {
        if (_judgmentPreviewUnit == i) _judgmentPreviewUnit = -1;
    }

    // 실제 심판(BattleManagerTemp.OnPointerClick → JudgeBossFromUnit → ApplyBossDamage)과 같은 규칙으로
    // 깎일 HP를 계산한다. 아무 상태도 바꾸지 않는다. 지금 심판할 수 없으면 0.
    public int PreviewJudgmentDamage(int i)
    {
        if (IsBattleOver || turn != ETurn.Player || actionPoint <= 0 || boss == null) return 0;
        if (!battleManagerTemp.IsUnitOnBoard(i) || IsAnimPlaying(i)) return 0;
        if (Trial == null) return 0;
        int damage = Trial.ResolveIncomingDamage(AfterlifePassiveRules.DamageSource.Judgment, Trial.PreviewJudgment(i));
        if (damage <= 0) return 0;
        return bossCurrentHp - Mathf.Max(0, bossCurrentHp - damage);
    }

    public void OnMouseEnterUnit(int i)
    {
        ShowJudgmentPreview(i);
        _info?.Show(i);
        RefreshUnitDescription(i);
    }

    private void LateUpdate()
    {
        for (int i = 0; i < units.Length; i++)
        {
            if (_info == null) break;
            _info.SetPips(i, Trial != null ? Trial.HitsOf(i) : -1);
            if (_info.Shown == i) RefreshUnitDescription(i);
        }
        if (_bossHpFx != null)
            _bossHpFx.SetPreview(_judgmentPreviewUnit >= 0 ? PreviewJudgmentDamage(_judgmentPreviewUnit) : 0);
    }

    private void RefreshUnitDescription(int i)
    {
        var selectedUnit = BattleDisplayManager.GetInstance().currentUnits[i];
        string body = Trial != null ? Trial.InfoBody(i, selectedUnit.abilityText) : selectedUnit.abilityText;
        _info?.SetContent(selectedUnit.unitName, body, Trial != null ? Trial.HitsOf(i) : -1);
    }

    public void OnMouseExitUnit(int i)
    {
        HideJudgmentPreview(i);
        units[i].popUp.SetActive(false);
        _info?.Hide(i);
    }

    public void OnPhaseChange(int i)
    {
        phase = i;
        var t = "";
        if (phase == 1)
        {
            t = GetBossText(boss.phaseTwo, turnCount / 5);
        } else if (phase == 2)
        {
            t = GetBossText(boss.phaseThree, turnCount / 5);
        }
        PrintImportantText(t);
    }

    [DebugButton("유닛 처치")]
    public Coroutine HitAttack(int i)
    {
        if (i < 0 || i >= units.Length || units[i].unit == null) return null;
        UnitMotion.For(units[i].unit).Shake();
        _hitAnimations[i] = StartCoroutine(SetGlitch(i));
        return _hitAnimations[i];
    }

    public void OnUnitRevived(int i)
    {
        if (i < 0 || i >= units.Length) return;
        units[i].unit.material.SetFloat(EnableGlitch, 0);
        units[i].isAnimPlaying = false;
    }

    IEnumerator SetGlitch(int i)
    {
        units[i].unit.material.SetFloat(EnableGlitch, 1);
        yield return new WaitForSeconds(1f);
        battleManagerTemp.OnHit(i);
    }

    [DebugButton("공격 실행")]
    public void PlayAttackAnim(int i)
    {
        if(units[i].isAnimPlaying) return;
        GameSfx.PlayAttack();
        units[i].isAnimPlaying = true;
        
        _moveAnimations[i] = StartCoroutine(MoveUI(i, new Vector2(0, 50f), 0.2f, 0.1f));
    }
    
    [DebugButton("심판 실행")]
    public void PlayJudgeAnim(int i)
    {
        if(units[i].isAnimPlaying) return;
        if (TryPlayJudgeShot(i)) return;
        units[i].isAnimPlaying = true;
        _moveAnimations[i] = StartCoroutine(PlainJudgeSequence(i));
    }

    // 4번째 스킨 칸은 기본 그림 임시
    private static int JudgeSkin(UnitTemp data) => data.currentSkin == 3 ? 0 : data.currentSkin;

    // 판 위 연출 그림이 없는 캐릭터(스킨 등): 컷인이 있으면 컷인(없으면 기본 스킨 컷인) → 기존의 통통 튀는 동작
    private IEnumerator PlainJudgeSequence(int i)
    {
        var data = BattleDisplayManager.GetInstance().currentUnits[i];
        var cut = data == null ? null : JudgeShot.FindCut(data.id, JudgeSkin(data));
        var cutFrames = JudgeShot.CutFrames(cut);
        if (cutFrames != null)
        {
            // 컷인 동안에는 HP 바가 미리 줄지 않게 붙잡아 두고, 컷인이 끝나면 줄인다
            if (_bossHpFx != null) _bossHpFx.HoldDisplay(3f);
            yield return JudgeCutIn.Play(cutFrames, cut.cutTimes, cut.cutRelease, GameSfx.PlayJudgment);
            if (_bossHpFx != null) _bossHpFx.ReleaseDisplay();
        }
        else GameSfx.PlayJudgment();

        yield return MoveUI(i, new Vector2(0, -10f), 0.2f, 0.1f, new Vector2(100f, 60f));
    }
    
    // 기본 스킨 캐릭터의 심판 연출 (JSW/Script/JudgeShot.cs, JudgeCutIn.cs)
    // 1) 컷인: 게임이 잠시 멈추고 화면이 어두워지며 캐릭터가 왼쪽에서 크게 들어와 긴 동작을 한다 (그림이 있는 캐릭터만)
    // 2) 판 위: 작은 캐릭터가 한 번 더 동작하고, 날아간 것이 보스에 닿는 순간 보스가 흔들리고 HP 바가 줄어든다
    // 피해 계산은 클릭한 순간 이미 끝나 있고, 보이는 것만 닿을 때까지 미룬다.
    private bool TryPlayJudgeShot(int i)
    {
        var data = BattleDisplayManager.GetInstance().currentUnits[i];
        if (data == null || i >= _shotRestoreSprites.Length) return false;
        var profile = JudgeShot.Find(data.id, JudgeSkin(data));
        if (profile == null) return false;

        units[i].isAnimPlaying = true;
        _shotRestoreSprites[i] = units[i].unit.sprite;
        if (_bossHpFx != null) _bossHpFx.HoldDisplay(3f);
        _moveAnimations[i] = StartCoroutine(JudgeSequence(i, profile));
        return true;
    }

    private IEnumerator JudgeSequence(int i, JudgeShot.Profile profile)
    {
        // 판 위 그림은 있는데 컷인 그림이 없는 스킨이면 기본 스킨 컷인으로 대신한다
        var cut = JudgeShot.FindCut(profile.unitId, profile.skin);
        var cutFrames = JudgeShot.CutFrames(cut);
        if (cutFrames != null)
            yield return JudgeCutIn.Play(cutFrames, cut.cutTimes, cut.cutRelease, GameSfx.PlayJudgment);
        yield return JudgeShot.Play(this, profile, units[i].unit, bossImage.rectTransform,
            onRelease: cutFrames == null ? GameSfx.PlayJudgment : (Action)null,
            onHit: index =>
            {
                // 여러 개가 날아가면(도적 단검 2자루) 첫 번째가 닿을 때 HP가 줄고, 닿을 때마다 보스가 흔들린다.
                if (index == 0 && _bossHpFx != null) _bossHpFx.ReleaseDisplay();
                HitShake.For(bossImage).Play(profile.shakeAmplitude, profile.shakeDuration);
            },
            onDone: () =>
            {
                _shotRestoreSprites[i] = null;
                units[i].isAnimPlaying = false;
            });
    }

    public bool IsAnimPlaying(int i)
    {
        return units[i].isAnimPlaying;
    }

    IEnumerator MoveUI(int i, Vector2 posOffset, float startTime, float endTime, Vector2 scaleOffset = default)
    {
        var target = units[i].unit.rectTransform;
        Vector2 start = target.anchoredPosition;
        Vector2 end = start + posOffset;
        
        Vector2 scale = target.sizeDelta;
        
        if(scaleOffset == default) scaleOffset = scale;

        float time = 0f;

        while (time < startTime)
        {
            time += Time.deltaTime;
            
            float t = time / startTime;
            target.anchoredPosition = Vector2.Lerp(start, end, t);
            target.sizeDelta = Vector2.Lerp(scale, scaleOffset, t);
            
            yield return null;
        }
        
        target.anchoredPosition = end;
        target.sizeDelta = scaleOffset;

        time = 0f;

        while (time < endTime)
        {
            time += Time.deltaTime;

            float t = time / endTime;
            target.anchoredPosition = Vector2.Lerp(end, start, t);
            target.sizeDelta = Vector2.Lerp(scaleOffset, scale, t);
            
            yield return null;
        }
        target.anchoredPosition = start;
        target.sizeDelta = scale;
        
        units[i].isAnimPlaying = false;
    }

    private static string GetBossText(string[] texts, int index)
    {
        if (texts == null || texts.Length == 0) return string.Empty;
        return texts[Mathf.Clamp(index, 0, texts.Length - 1)];
    }

    
}
