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

    [Serializable]
    private struct Unit
    {
        public Image image;
        public GameObject popUp;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
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

    private void OnEnable()
    {
        for(int i=0;i<3;i++)
        {
            units[i].image.sprite = BattleDisplayManager.GetInstance().currentUnits[i].currentSkin switch
            {
                0 => BattleDisplayManager.GetInstance().currentUnits[i].skin1,
                1 => BattleDisplayManager.GetInstance().currentUnits[i].skin2,
                2 => BattleDisplayManager.GetInstance().currentUnits[i].skin3,
                _ => units[i].image.sprite
            };
            units[i].nameText.text = BattleDisplayManager.GetInstance().currentUnits[i].unitName;
            units[i].descriptionText.text = BattleDisplayManager.GetInstance().currentUnits[i].abilityText;
            
            items[i].itemData = BattleDisplayManager.GetInstance().currentItems[i];
            items[i].Init();
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
        foreach (var item in items)
        {
            item.OnTurnStart();
        }
        _isLose = _isWin = false;
        bossImage.sprite = BattleDisplayManager.GetInstance().appearedBoss[^1].bossSprite;
        if (boss.bossId != "death2")
        {
            reaperCurrentHp = reaperMaxHp;
        }
        bossCurrentHp = bossMaxHp;
        UpdateBossHp(bossCurrentHp);
        UpdateReaperHp(reaperCurrentHp);
        UpdateActionPoint(7);
        textBox.text = "";
        PrintText(boss.battleStart);
        PrintText(boss.turnStart[0]);
        turnCount = 1;
        turn = ETurn.Start;
        battleManagerTemp.Init();
    }

    [DebugButton("다음 턴")]
    public void NextTurn(int i = 1)
    {
        turn += i;
        if(turn > ETurn.End) 
        {
            turn = ETurn.Start;
            turnCount++;
            foreach (var item in items)
            {
                item.OnTurnStart();
            }
            PrintText(boss.turnStart[turnCount/5]);
        }

        var turnText = "";

        var nt = false;
        
        switch (turn)
        {
            case ETurn.Start:
                turnText = "턴 시작";
                UpdateActionPoint(7);
                break;
            case ETurn.Place:
                turnText = "유닛 배치";
                break;
            case ETurn.BossReady:
                turnText = "보스 공격 준비";
                break;
            case ETurn.Player:
                turnText = "행동";
                break;
            case ETurn.Attack:
                turnText = "공격";
                PrintText(boss.attackedAD[turnCount/5]);
                break;
            case ETurn.BossPattern:
                turnText = "보스 공격";
                break;
            case ETurn.End:
                turnText = "턴 종료";
                nt = true;
                break;
        }

        turnTextUI.text = turnCount + " - " + turnText;
        if(nt) NextTurn();
        if(turn == 0) battleManagerTemp.OnTurnStart();
    }
    
    [DebugButton("텍스트 출력")]
    public void PrintText(string text)
    {
        if(_isLose || _isWin) return;
        
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
                boss = BattleDisplayManager.GetInstance().bossDeath2;
                BattleDisplayManager.GetInstance().appearedBoss.Add(boss);
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
        PrintText(boss.distinctText[i]);
    }
    
    [DebugButton("플레이어 HP 업데이트")]
    public void UpdateReaperHp(int hp)
    {
        if(reaperCurrentHp == 0) return;
        reaperCurrentHp = hp;
        if (reaperCurrentHp <= 0)
        {
            PrintText(boss.lose);
            _isLose = true;
            reaperCurrentHp = 0;

        }
        reaperHp.text = $"{reaperCurrentHp} / {reaperMaxHp}";
        reaperSlider.value = (float) reaperCurrentHp / reaperMaxHp;
    }
    
    public void UpdateBossHp(int hp)
    {
        if(bossCurrentHp == 0) return;
        bossCurrentHp = hp;
        if (bossCurrentHp <= 0)
        {
            PrintText(boss.win);
            bossCurrentHp = 0;
            _isWin = true;
        }
        bossHp.text = $"{bossCurrentHp} / {bossMaxHp}";
        bossSlider.value = (float) bossCurrentHp / bossMaxHp;
    }

    [DebugButton("보스 심판")]
    public void JudgeBoss(int dmg)
    {
        PrintText(boss.attackedAP[turnCount/5]);
        UpdateBossHp(bossCurrentHp-dmg);
    }
    
    [DebugButton("보스 공격")]
    public void AttackBoss(int dmg)
    {
        UpdateBossHp(bossCurrentHp-dmg);
    }

    public void OnBossAttack(int dmg)
    {
        PrintText(boss.attack[turnCount/5]);
        UpdateReaperHp(reaperCurrentHp-dmg);
    }

    [DebugButton("보스 의지력 업데이트")]
    public void UpdateBossWillPower(int value)
    {
        if (value == 0)
        {
            PrintText(boss.willZero[turnCount/5]);
        }
        else if (value < willPower)
        {
            PrintText(boss.willDecline[turnCount/5]);
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
        
        if(actionPoint <= 0)
        {
            NextTurn();
            battleManagerTemp.Attack();
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
            t = boss.phaseTwo[turnCount/5];
        } else if (phase == 2)
        {
            t = boss.phaseThree[turnCount/5];
        }
        PrintText(t);
    }
}