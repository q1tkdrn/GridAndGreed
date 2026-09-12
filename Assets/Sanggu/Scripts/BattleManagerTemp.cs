using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BattleManagerTemp : MonoBehaviour
{
    [Serializable]
    private struct Unit
    {
        public UnitTemp unitTemp;
        public bool isPlaced;
        public int reviveRemainTurn;
        public Vector2Int pos;
        public Image unit;
        public Image unitOnWaiting;
    }
    
    private Dictionary<Vector2Int, GameObject> _blankPos = new Dictionary<Vector2Int, GameObject>();
    
    [SerializeField] private BoardPanel boardPanel;
    [SerializeField] private GameObject boardObject;
    [SerializeField] private GameObject blankPrefab;
    [SerializeField] private Unit[] units = new Unit[3];
    
    [SerializeField] private EventSystem eventSystem;

    private List<GameObject> _movableBlank = new();
    private int _currentMoveIndex = -1;
    private int _currentClickIndex = -1;

    private bool _isClicked = false;
    [SerializeField] private float doubleClickDelay = 0.1f;

    //활성하시 클릭 가능한 칸 생성
    public void Init()
    {
        foreach (var blank in _blankPos.ToList())
        {
            Destroy(blank.Value);
        }
        _blankPos.Clear();
        _movableBlank.Clear();
        _currentMoveIndex = -1;
        _currentClickIndex = -1;
        _isClicked = false;
        
        for (int x = 0; x < 9; x++)
        {
            for (int y = 0; y < 9; y++)
            {
                var go = Instantiate(blankPrefab, boardObject.transform);
                go.GetComponent<RectTransform>().anchoredPosition = new Vector2(-330 + 82 * x, 331 - 82 * y);
                go.name = $"blank{x},{y}";
                var trigger = go.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = go.AddComponent<EventTrigger>();
                }
                trigger.triggers ??= new List<EventTrigger.Entry>();

                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                
                entry.callback.AddListener(_ =>
                {
                    OnBoardClicked(go);
                });
                trigger.triggers.Add(entry);
                go.transform.SetAsFirstSibling();
                _blankPos[new Vector2Int(x, y)] = go;
            }
        }
        
        for (int i = 0; i < 3; i++)
        {
            units[i].unitTemp = BattleDisplayManager.GetInstance().currentUnits[i];
            units[i].isPlaced = false;
            units[i].reviveRemainTurn = 0;
            units[i].pos = new Vector2Int(-1, -1);
            units[i].unit.gameObject.SetActive(false);
            units[i].unitOnWaiting.GetComponent<Image>().color = new Color(1, 1, 1, 1);
        }
        
        boardPanel.NextTurn();
    }

    public void OnTurnStart()
    {
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i].reviveRemainTurn <= 0) continue;

            units[i].reviveRemainTurn--;
            if (units[i].reviveRemainTurn != 0) continue;

            units[i].unit.gameObject.SetActive(true);
            units[i].unit.GetComponent<RectTransform>().anchoredPosition =
                _blankPos[units[i].pos].GetComponent<RectTransform>().anchoredPosition;
            boardPanel.OnUnitRevived(i);
        }

        var tb = false;
        foreach (var unit in units)
        {
            if(!unit.isPlaced) tb = true;
        }

        boardPanel.NextTurn(tb ? 1 : 3);
    }

    public void OnUnitInReadyClicked(int i)
    {
        if (i < 0 || i >= units.Length) return;
        if (!units[i].isPlaced && units[i].reviveRemainTurn == 0)
        {
            _currentMoveIndex = i;
            units[i].reviveRemainTurn = -1;
        }
    }

    public void OnBoardClicked(GameObject go)
    {
        if(_currentMoveIndex == -1) return;

        var blank = _blankPos.FirstOrDefault(pair => pair.Value == go);
        if (blank.Value == null) return;

        var pos = blank.Key;
        
        foreach (var unit in units)
        {
            if (unit.pos == pos) return;
        }

        if (boardPanel.turn == BoardPanel.ETurn.Place)
        {
            units[_currentMoveIndex].isPlaced = true;
            units[_currentMoveIndex].reviveRemainTurn = -1;
            units[_currentMoveIndex].pos = pos;
            units[_currentMoveIndex].unit.GetComponent<RectTransform>().anchoredPosition = _blankPos[pos].GetComponent<RectTransform>().anchoredPosition;
            units[_currentMoveIndex].unit.sprite = units[_currentMoveIndex].unitOnWaiting.sprite;
            units[_currentMoveIndex].unit.gameObject.SetActive(true);
            units[_currentMoveIndex].unitOnWaiting.GetComponent<Image>().color =  new Color(1, 1, 1, 0.5f);
            _currentMoveIndex = -1;
            
            var tb = false;
            foreach (var unit in units)
            {
                if(!unit.isPlaced) tb = true;
            }
            if(!tb) boardPanel.NextTurn(2);
        }
    }

    public void OnPointerEnter(int i)
    {
        if (i < 0 || i >= units.Length || !units[i].isPlaced || units[i].reviveRemainTurn > 0) return;
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        var pos = units[i].pos;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var nPos = pos + new Vector2Int(x, y);
                if (_blankPos.TryGetValue(nPos, out var po))
                {
                    if (units.Count(u => u.pos == nPos) == 0)
                    {
                        po.GetComponent<Image>().color = new Color(1, 1, 1, 0.5f);
                        _movableBlank.Add(po);
                    }
                }
            }
        }
    }

    public void OnPointerExit(int i)
    {
        foreach (var blank in _movableBlank)
        {
            blank.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        }
        _movableBlank.Clear();
    }

    //유닛 클릭 감지
    public void OnPointerDown(int i)
    {
        if(i < 0 || i >= units.Length || !units[i].isPlaced || units[i].reviveRemainTurn > 0) return;
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        _currentMoveIndex = i;
        units[_currentMoveIndex].unit.transform.SetAsLastSibling();
    }
    
    //유닛 드래그 감지
    public void OnPointerDrag(int i)
    {
        if (_currentMoveIndex != i) return;
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        if(boardPanel.actionPoint <= 0) return;
        units[i].unit.transform.position = Input.mousePosition;
    }
    
    //유닛 클릭 헤제 감지
    public void OnPointerUp(int i)
    {
        if (_currentMoveIndex != i) return;
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        if(boardPanel.actionPoint <= 0) return;
        PointerEventData data = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };
        
        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(data, results);
        var boardHit = results.FirstOrDefault(result => result.gameObject != null && result.gameObject.CompareTag("Board"));
        var target = _blankPos.FirstOrDefault(pair => pair.Value == boardHit.gameObject);
        if (target.Value != null && _movableBlank.Contains(target.Value))
        {
            units[_currentMoveIndex].unit.GetComponent<RectTransform>().anchoredPosition = target.Value.GetComponent<RectTransform>().anchoredPosition;
            units[_currentMoveIndex].pos = target.Key;
            OnPointerExit(_currentMoveIndex);
            OnPointerEnter(_currentMoveIndex);
            boardPanel.UpdateActionPoint(boardPanel.actionPoint - 1);
        }
        else
        {
            units[_currentMoveIndex].unit.GetComponent<RectTransform>().anchoredPosition = _blankPos[units[_currentMoveIndex].pos].GetComponent<RectTransform>().anchoredPosition;
        }
        _currentMoveIndex = -1;
    }

    //더블 클릭 감지용
    public void OnPointerClick(int i)
    {
        if(i < 0 || i >= units.Length || !units[i].isPlaced || units[i].reviveRemainTurn > 0) return;
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        if(boardPanel.IsAnimPlaying(i)) return;
        if(boardPanel.actionPoint <= 0) return;
        if (_currentClickIndex == i && _isClicked)
        {
            Debug.Log("db");
            boardPanel.JudgeBoss(units[i].unitTemp.intelligence);
            boardPanel.UpdateActionPoint(boardPanel.actionPoint - 1);
            boardPanel.PlayJudgeAnim(i);
        }
        _isClicked = true;
        _currentClickIndex = i;
        StartCoroutine(CheckDoubleClick());
    }

    //더블 클릭 간격 감지
    IEnumerator CheckDoubleClick()
    {
        yield return new WaitForSeconds(doubleClickDelay);
        _isClicked = false;
        _currentClickIndex = -1;
    }

    public void Attack()
    {
        foreach (var unit in units.Where(x => x.isPlaced).ToList())
        {
            boardPanel.AttackBoss(unit.unitTemp.power);
            boardPanel.PlayAttackAnim(units.ToList().IndexOf(unit));
        }
    }

    public void OnHit(int i)
    {
        if (i < 0 || i >= units.Length || !units[i].isPlaced) return;
        units[i].unit.gameObject.SetActive(false);
        units[i].reviveRemainTurn = Mathf.Max(1, units[i].unitTemp.reviveCool);
    }

    public IEnumerator PlayBossPattern(BossTemp boss, int turnCount)
    {
        var pattern = GetPattern(boss, turnCount);
        var dangerCells = BattlePatternRules.GetDangerCells(pattern);

        foreach (var cell in dangerCells)
        {
            if (_blankPos.TryGetValue(cell, out var blank))
            {
                blank.GetComponent<Image>().color = new Color(1f, 0.2f, 0.2f, 0.65f);
            }
        }

        var previewSeconds = boss != null && boss.patternPreviewSeconds > 0f
            ? boss.patternPreviewSeconds
            : 1f;
        yield return new WaitForSeconds(previewSeconds);

        var damage = boss != null && boss.patternDamage > 0 ? boss.patternDamage : 5;
        for (int i = 0; i < units.Length; i++)
        {
            if (!units[i].isPlaced || units[i].reviveRemainTurn > 0) continue;
            if (!dangerCells.Contains(units[i].pos)) continue;

            boardPanel.OnBossAttack(damage);
            boardPanel.HitAttack(i);
        }

        // HitAttack의 글리치 애니메이션이 끝날 때까지 다음 턴 입력을 막는다.
        yield return new WaitForSeconds(1f);

        foreach (var cell in dangerCells)
        {
            if (_blankPos.TryGetValue(cell, out var blank))
            {
                blank.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    private BattlePatternRules.Pattern GetPattern(BossTemp boss, int turnCount)
    {
        var patterns = boss?.patterns;
        if (patterns != null && patterns.Length > 0)
        {
            return patterns[(turnCount - 1) % patterns.Length];
        }

        return BattlePatternRules.GetDefaultPattern(turnCount);
    }
}
