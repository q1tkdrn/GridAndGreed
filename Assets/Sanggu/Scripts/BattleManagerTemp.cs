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
        for (int x = 0; x < 9; x++)
        {
            for (int y = 0; y < 9; y++)
            {
                var go = Instantiate(blankPrefab, boardObject.transform);
                go.GetComponent<RectTransform>().anchoredPosition = new Vector2(-330 + 82 * x, 331 - 82 * y);
                go.name = $"blank{x},{y}";
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                
                entry.callback.AddListener(_ =>
                {
                    OnBoardClicked(go);
                });
                go.GetComponent<EventTrigger>().triggers.Add(entry);
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
        }
        
        boardPanel.NextTurn();
    }

    public void OnTurnStart()
    {
        var tb = false;
        foreach (var unit in units)
        {
            if(!unit.isPlaced) tb = true;
        }

        boardPanel.NextTurn(tb ? 1 : 2);
    }

    public void OnUnitInReadyClicked(int i)
    {
        if (!units[i].isPlaced && units[i].reviveRemainTurn == 0)
        {
            _currentMoveIndex = i;
            units[i].reviveRemainTurn = -1;
        }
    }

    public void OnBoardClicked(GameObject go)
    {
        if(_currentMoveIndex == -1) return;
        
        var pos = _blankPos.FirstOrDefault(x => x.Value == go).Key;
        
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
        foreach (var blank in _movableBlank.ToList())
        {
            blank.GetComponent<Image>().color = new Color(1, 1, 1, 0);
            _movableBlank.Remove(blank);
        }
    }

    //유닛 클릭 감지
    public void OnPointerDown(int i)
    {
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        _currentMoveIndex = i;
        units[_currentMoveIndex].unit.transform.SetAsLastSibling();
    }
    
    //유닛 드래그 감지
    public void OnPointerDrag(int i)
    {
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        if(boardPanel.actionPoint <= 0) return;
        units[i].unit.transform.position = Input.mousePosition;
    }
    
    //유닛 클릭 헤제 감지
    public void OnPointerUp(int i)
    {
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        if(boardPanel.actionPoint <= 0) return;
        PointerEventData data = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };
        
        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(data, results);
        var pos = _blankPos.FirstOrDefault(x =>
            x.Value == results.FirstOrDefault(y => y.gameObject.CompareTag("Board")).gameObject).Key;
        if (_movableBlank.Contains(_blankPos[pos]))
        {
            units[_currentMoveIndex].unit.GetComponent<RectTransform>().anchoredPosition = _blankPos[pos].GetComponent<RectTransform>().anchoredPosition;
            units[_currentMoveIndex].pos = pos;
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
        if(boardPanel.actionPoint <= 0) return;
        if (_currentClickIndex == i && _isClicked)
        {
            Debug.Log("db");
            boardPanel.JudgeBoss(units[i].unitTemp.intelligence);
            boardPanel.UpdateActionPoint(boardPanel.actionPoint - 1);
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
        }
        boardPanel.NextTurn(3);
    }
}