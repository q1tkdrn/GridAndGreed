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
    public int actionPoint;
    [SerializeField] private GameObject actionPointPrefab;
    [SerializeField] private GameObject actionPoints;
    [SerializeField] private Sprite[] actionPointsList;
    private List<GameObject> _actionPointsList = new List<GameObject>();

    [SerializeField] private Image[] units = new Image[3];
    
    [Header("Boss")]
    public int bossMaxHp;
    public int bossCurrentHp;
    [SerializeField] private TextMeshProUGUI bossHp;

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
            units[i].sprite = BattleDisplayManager.GetInstance().currentUnits[i].currentSkin switch
            {
                0 => BattleDisplayManager.GetInstance().currentUnits[i].skin1,
                1 => BattleDisplayManager.GetInstance().currentUnits[i].skin2,
                2 => BattleDisplayManager.GetInstance().currentUnits[i].skin3,
                _ => units[i].sprite
            };
        }
    }

    public void NextTurn()
    {
        turn++;
        if(turn > ETurn.End) 
        {
            turn = ETurn.Start;
            turnCount++;
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

    public void UpdateReaperHp(int hp)
    {
        reaperCurrentHp = hp;
        reaperHp.text = $"{reaperCurrentHp} / {reaperMaxHp}";
    }
    
    public void UpdateBossHp(int hp)
    {
        bossCurrentHp = hp;
        bossHp.text = $"{bossCurrentHp} / {bossMaxHp}";
    }
    
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
}