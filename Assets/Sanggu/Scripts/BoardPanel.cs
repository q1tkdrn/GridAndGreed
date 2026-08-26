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
    [SerializeField] private TextMeshProUGUI bossHp;
    [SerializeField] private TextMeshProUGUI willPowerText;
    [SerializeField] private Slider bossSlider;
    [SerializeField] private Image bossImage;

    public enum ETurn
    {
        Start,
        Place,
        BossReady,
        Player,
        Attack,
        BossPattern,
        End
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
        foreach (var item in items)
        {
            item.OnTurnStart();
        }
        UpdateActionPoint(7);
    }

    [DebugButton("다음 턴")]
    public void NextTurn()
    {
        turn++;
        if(turn > ETurn.End) 
        {
            turn = ETurn.Start;
            turnCount++;
            foreach (var item in items)
            {
                item.OnTurnStart();
            }
        }

        var turnText = "";
        
        switch (turn)
        {
            case ETurn.Start:
                turnText = "턴 시작";
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
                break;
            case ETurn.BossPattern:
                turnText = "보스 공격";
                break;
            case ETurn.End:
                turnText = "턴 종료";
                break;
        }

        turnTextUI.text = turnCount + " - " + turnText;
    }
    
    [DebugButton("텍스트 출력")]
    public void PrintText(string text)
    {
        StartCoroutine(TypeEffect(text));
    }

    IEnumerator TypeEffect(string text)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(textBox.text+"\n");
        for (int i = 0; i < text.Length; i++)
        {
            stringBuilder.Append(text[i]);
            textBox.text = stringBuilder.ToString();
            yield return new WaitForSeconds(delay);
        }
    }
    
    [DebugButton("플레이어 HP 업데이트")]
    public void UpdateReaperHp(int hp)
    {
        reaperCurrentHp = hp;
        reaperHp.text = $"{reaperCurrentHp} / {reaperMaxHp}";
        reaperSlider.value = (float) reaperCurrentHp / reaperMaxHp;
    }
    
    [DebugButton("보스 HP 업데이트")]
    public void UpdateBossHp(int hp)
    {
        bossCurrentHp = hp;
        bossHp.text = $"{bossCurrentHp} / {bossMaxHp}";
        bossSlider.value = (float) bossCurrentHp / bossMaxHp;
    }

    [DebugButton("보스 의지력 업데이트")]
    public void UpdateBossWillPower(int value)
    {
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
            image.sprite = actionPointsList[i%actionPointsList.Length];
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
}