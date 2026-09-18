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
    [SerializeField] private float delay = 0.1f;
    private float _delay;
    private Queue<string> _textQueue = new Queue<string>();
    private bool _isPrinting = false; 
    
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

    private bool _isWin = false;
    private bool _isLose = false; 
    public bool IsBattleOver => _isWin || _isLose;
    private int _nextTurnActionPenalty;
    [SerializeField] private AfterlifePassiveRules.PhaseOneEffect afterlifeTurnPassive;

    private void BeginBossTurn()
    {
        afterlifeTurnPassive = boss.bossId == "death1"
            ? (AfterlifePassiveRules.PhaseOneEffect)UnityEngine.Random.Range(1, 4)
            : AfterlifePassiveRules.PhaseOneEffect.None;
        int maximum = AfterlifePassiveRules.GetActionPointMaximum(afterlifeTurnPassive);
        UpdateActionPoint(Mathf.Max(0, maximum - _nextTurnActionPenalty));
        _nextTurnActionPenalty = 0;
        if (boss.bossId == "death1")
        {
            string effect = afterlifeTurnPassive switch
            {
                AfterlifePassiveRules.PhaseOneEffect.LimitActions => "최대 행동력 4",
                AfterlifePassiveRules.PhaseOneEffect.JudgmentWeakness => "심판 피해 +2 / 힘 자동 공격 면역",
                _ => "턴 종료 시 보스 HP 4 회복"
            };
            PrintText("[저승 1페이즈 패시브] " + effect);
        }
        else if (boss.bossId == "death2")
            PrintText("[저승 2페이즈 패시브] 받는 피해 0 / 턴 종료 HP -1 / 아군 사망 시 의지력 +1");
    }

    private void EndBossTurn()
    {
        if (IsBattleOver) return;
        // 자신의 턴 종료 HP 변화는 받는 피해가 아니므로 피해 면역을 거치지 않는다.
        UpdateBossHp(AfterlifePassiveRules.GetTurnEndHp(boss.bossId, afterlifeTurnPassive, bossCurrentHp, bossMaxHp));
    }

    public void OnAllyDeath()
    {
        if (!IsBattleOver && boss.bossId == "death2") UpdateBossWillPower(willPower + 1);
    }

    public void ReduceNextTurnActionPoints(int amount)
    {
        _nextTurnActionPenalty += Mathf.Max(0, amount);
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
        if (battleManagerTemp != null) battleManagerTemp.ClearBossPatternPreview();
    }

    private void OnEnable()
    {
        for(int i=0;i<3;i++)
        {
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
            
            items[i].itemData = BattleDisplayManager.GetInstance().currentItems[i];
            items[i].Init();
            var mat = Instantiate(units[i].unit.material);
            units[i].unit.material = mat;
            units[i].unit.material.SetFloat(EnableGlitch, 0);
            units[i].isAnimPlaying = false;
        }
    }

    public void ShowCutScene()
    {
        bossCutSceneImage.sprite = BattleDisplayManager.GetInstance().appearedBoss[^1].bossSprite;
        bossImage.sprite = BattleDisplayManager.GetInstance().appearedBoss[^1].bossSprite;
        bossNameText.text = BattleDisplayManager.GetInstance().appearedBoss[^1].bossName;
        cutScene.SetActive(true);
        reaperCutScene.anchoredPosition = new Vector2(-1440, 0);
        bossCutScene.anchoredPosition = new Vector2(1440, 0);
        BattleDisplayManager.GetInstance().PlayBGM(boss.bgmLoop, boss.bgmStart);
        StartCoroutine(PlayCutSceneAnim());
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
        if (_bossPatternCoroutine != null) StopCoroutine(_bossPatternCoroutine);
        _bossPatternCoroutine = null;
        foreach (var item in items)
        {
            item.OnTurnStart();
        }
        _isLose = _isWin = false;
        _nextTurnActionPenalty = 0;
        _isResolvingBossPattern = false;
        bossImage.sprite = BattleDisplayManager.GetInstance().appearedBoss[^1].bossSprite;
        if (boss.bossId != "death2")
        {
            reaperCurrentHp = reaperMaxHp;
        }
        // 각 보스 에셋의 스탯을 새로 읽어 이전 페이즈 HP/의지력이 남지 않도록 한다.
        bossMaxHp = Mathf.Max(1, boss.maxHp);
        willPower = Mathf.Max(0, boss.initialWillPower);
        willPowerText.text = $"의지력: {willPower}";
        // 기존 OnPhaseChange의 0 기반 규약: 0 = 1페이즈, 1 = 2페이즈.
        phase = boss.bossId == "death2" ? 1 : 0;
        bossCurrentHp = bossMaxHp;
        UpdateBossHp(bossCurrentHp);
        UpdateReaperHp(reaperCurrentHp);
        textBox.text = "";
        PrintText(boss.battleStart);
        PrintText(GetBossText(boss.turnStart, 0));
        turnCount = 1;
        turn = ETurn.Start;
        BeginBossTurn();
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
            foreach (var item in items)
            {
                item.OnTurnStart();
            }
            PrintText(GetBossText(boss.turnStart, turnCount / 5));
        }

        var turnText = "";

        var nt = false;
        
        switch (turn)
        {
            case ETurn.Start:
                turnText = "턴 시작";
                BeginBossTurn();
                break;
            case ETurn.Place:
                turnText = "유닛 배치";
                break;
            case ETurn.BossReady:
                turnText = "보스 공격 준비";
                battleManagerTemp.PrepareBossPattern(boss, turnCount);
                break;
            case ETurn.Player:
                turnText = "행동";
                battleManagerTemp.PrepareBossPattern(boss, turnCount);
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
        
        _textQueue.Enqueue(text);

        if (!_isPrinting)
        {
            StartCoroutine(ProcessTextQueue());
        }
    }

    IEnumerator ProcessTextQueue()
    {
        _isPrinting = true;

        while (_textQueue.Count > 0)
        {
            string text = _textQueue.Dequeue();

            _delay = _textQueue.Count >= 5 ? 0 : delay;
            
            yield return StartCoroutine(TypeEffect(text));
        }
        
        _isPrinting = false;
        if (_isWin)
        {
            yield return new WaitForSeconds(0.1f);
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
            yield return new WaitForSeconds(0.1f);
            BattleDisplayManager.GetInstance().ShowDefeatPanel();
        }
    }

    IEnumerator TypeEffect(string text)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(textBox.text+"\n");
        for (int i = 0; i < text.Length; i++)
        {
            stringBuilder.Append(text[i]);
            textBox.text = stringBuilder.ToString();
            yield return new WaitForSeconds(_delay);
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
        reaperCurrentHp = Mathf.Clamp(hp, 0, reaperMaxHp);
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
            PrintText(string.IsNullOrWhiteSpace(boss.win) ? "승리" : boss.win);
            bossCurrentHp = 0;
            _isWin = true;
        }
        bossHp.text = $"{bossCurrentHp} / {bossMaxHp}";
        bossSlider.value = (float) bossCurrentHp / bossMaxHp;
    }

    [DebugButton("보스 심판")]
    public void JudgeBoss(int dmg)
    {
        if (IsBattleOver) return;
        PrintText(GetBossText(boss.attackedAP, turnCount / 5));
        ApplyBossDamage(dmg, AfterlifePassiveRules.DamageSource.Judgment);
    }
    
    [DebugButton("보스 공격")]
    public void AttackBoss(int dmg)
    {
        ApplyBossDamage(dmg, AfterlifePassiveRules.DamageSource.Other);
    }

    public void AutomaticAttackBoss(int dmg)
    {
        ApplyBossDamage(dmg, AfterlifePassiveRules.DamageSource.AutomaticAttack);
    }

    private void ApplyBossDamage(int dmg, AfterlifePassiveRules.DamageSource source)
    {
        if (IsBattleOver) return;
        int damage = AfterlifePassiveRules.ResolveDamage(boss.bossId, afterlifeTurnPassive, source, dmg);
        if (damage > 0) UpdateBossHp(bossCurrentHp - damage);
    }

    public void HealReaper(int amount)
    {
        if (amount <= 0 || IsBattleOver) return;
        UpdateReaperHp(reaperCurrentHp + amount);
    }

    public void LoseBattle()
    {
        if (IsBattleOver) return;
        // HP를 강제로 0으로 만들지 않아도 꼬마 사신 사망으로 패배할 수 있다.
        PrintText(string.IsNullOrWhiteSpace(boss.lose) ? "패배" : boss.lose);
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
        
        if(actionPoint <= 0 && turn == ETurn.Player && !_isWin && !_isLose)
        {
            NextTurn();
            battleManagerTemp.Attack();
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

    public void OnMouseEnterUnit(int i)
    {
        units[i].popUp.SetActive(true);
    }

    public void OnMouseExitUnit(int i)
    {
        units[i].popUp.SetActive(false);
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
        PrintText(t);
    }

    [DebugButton("유닛 처치")]
    public Coroutine HitAttack(int i)
    {
        if (i < 0 || i >= units.Length || units[i].unit == null) return null;
        return StartCoroutine(SetGlitch(i));
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
        units[i].isAnimPlaying = true;
        
        StartCoroutine(MoveUI(i, new Vector2(0, 50f), 0.2f, 0.1f));
    }
    
    [DebugButton("심판 실행")]
    public void PlayJudgeAnim(int i)
    {
        if(units[i].isAnimPlaying) return;
        units[i].isAnimPlaying = true;
        
        StartCoroutine(MoveUI(i, new Vector2(0, -10f), 0.2f, 0.1f, new Vector2(100f, 60f)));
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
