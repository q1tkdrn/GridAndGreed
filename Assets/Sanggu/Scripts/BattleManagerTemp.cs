using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private Sprite attackPreviewSprite;
    private readonly Dictionary<Vector2Int, Image> _attackPreviewPlates = new();
    private readonly Dictionary<Vector2Int, Image> _attackPlates = new();
    private int _patternRevision;
    private HashSet<Vector2Int> _preparedDangerCells;
    private BossTemp _preparedBoss;
    private int _preparedTurn = -1;
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
        StopAllCoroutines();
        ClearBossPatternPreview();
        _attackPreviewPlates.Clear();
        _attackPlates.Clear();
        if (attackPreviewSprite == null)
        {
            attackPreviewSprite = Resources.Load<Sprite>("공격 예상");
        }
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

                // 이동 가능 표시와 분리한다. 부모 Image의 색 변경으로 예고가 지워지지 않는다.
                var previewObject = new GameObject($"AttackPreview{x},{y}", typeof(RectTransform), typeof(Image));
                previewObject.layer = go.layer;
                var previewRect = (RectTransform)previewObject.transform;
                previewRect.SetParent(go.transform, false);
                previewRect.anchorMin = Vector2.zero;
                previewRect.anchorMax = Vector2.one;
                previewRect.offsetMin = Vector2.zero;
                previewRect.offsetMax = Vector2.zero;
                var previewImage = previewObject.GetComponent<Image>();
                previewImage.sprite = attackPreviewSprite;
                previewImage.color = new Color(1f, 1f, 1f, 0.65f);
                previewImage.raycastTarget = false;
                previewImage.enabled = false;
                _attackPreviewPlates[new Vector2Int(x, y)] = previewImage;

                // 실제 공격도 이동 표시와 별도 Image를 사용한다.
                var attackObject = new GameObject($"BossAttack{x},{y}", typeof(RectTransform), typeof(Image));
                attackObject.layer = go.layer;
                var attackRect = (RectTransform)attackObject.transform;
                attackRect.SetParent(go.transform, false);
                attackRect.anchorMin = Vector2.zero;
                attackRect.anchorMax = Vector2.one;
                attackRect.offsetMin = Vector2.zero;
                attackRect.offsetMax = Vector2.zero;
                var attackImage = attackObject.GetComponent<Image>();
                attackImage.color = new Color(1f, 0.2f, 0.2f, 0.65f);
                attackImage.raycastTarget = false;
                attackImage.enabled = false;
                _attackPlates[new Vector2Int(x, y)] = attackImage;

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
        var tb = false;
        foreach (var unit in units)
        {
            if(!unit.isPlaced) tb = true;
        }

        boardPanel.NextTurn(tb ? 1 : 3);
        // 부활 대기 중인 유닛만 남아도 다음 부활 턴으로 진행할 수 있어야 한다.
        if (!tb && units.All(u => u.reviveRemainTurn > 0)) boardPanel.UpdateActionPoint(0);
    }

    public void OnUnitInReadyClicked(int i)
    {
        if (boardPanel.IsBattleOver || boardPanel.turn != BoardPanel.ETurn.Place) return;
        if (i < 0 || i >= units.Length) return;
        if (!TutorialDirector.Allow(TutorialAct.Select, i)) return;   // 튜토리얼: 말풍선이 시킨 캐릭터만
        if (!units[i].isPlaced && units[i].reviveRemainTurn == 0)
        {
            // Selection is reversible; only placement changes the unit's lifecycle state.
            _currentMoveIndex = i;
            TutorialDirector.Notify(TutorialAct.Select, i);
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
            if (!TutorialDirector.Allow(TutorialAct.Place, _currentMoveIndex, pos)) return;
            units[_currentMoveIndex].isPlaced = true;
            units[_currentMoveIndex].reviveRemainTurn = -1;
            units[_currentMoveIndex].pos = pos;
            units[_currentMoveIndex].unit.GetComponent<RectTransform>().anchoredPosition = _blankPos[pos].GetComponent<RectTransform>().anchoredPosition;
            units[_currentMoveIndex].unit.sprite = units[_currentMoveIndex].unitOnWaiting.sprite;
            units[_currentMoveIndex].unit.gameObject.SetActive(true);
            units[_currentMoveIndex].unitOnWaiting.GetComponent<Image>().color =  new Color(1, 1, 1, 0.5f);
            TutorialDirector.Notify(TutorialAct.Place, _currentMoveIndex);
            _currentMoveIndex = -1;
            
            var tb = false;
            foreach (var unit in units)
            {
                if(!unit.isPlaced) tb = true;
            }
            // 배치 완료 후 입력 가능한 행동 단계로 이동한다. 공격 예고는 Player 진입 시 준비된다.
            if(!tb) boardPanel.NextTurn(2);
        }
    }

    public void OnPointerEnter(int i)
    {
        boardPanel.ShowJudgmentPreview(i);
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
        boardPanel.HideJudgmentPreview(i);
        foreach (var blank in _movableBlank)
        {
            blank.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        }
        _movableBlank.Clear();
    }

    //유닛 클릭 감지
    public void OnPointerDown(int i)
    {
        if (boardPanel.IsBattleOver) return;
        if(i < 0 || i >= units.Length || !units[i].isPlaced || units[i].reviveRemainTurn > 0) return;
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        if (!TutorialDirector.Allow(TutorialAct.Drag, i)) return;
        _currentMoveIndex = i;
        units[_currentMoveIndex].unit.transform.SetAsLastSibling();
    }
    
    //유닛 드래그 감지
    public void OnPointerDrag(int i)
    {
        if (boardPanel.IsBattleOver) return;
        if (_currentMoveIndex != i) return;
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        if(boardPanel.actionPoint <= 0) return;
        units[i].unit.transform.position = Input.mousePosition;
    }
    
    //유닛 클릭 헤제 감지
    public void OnPointerUp(int i)
    {
        if (boardPanel.IsBattleOver) return;
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
        if (target.Value != null && _movableBlank.Contains(target.Value) && TutorialDirector.Allow(TutorialAct.Move, _currentMoveIndex, target.Key))
        {
            units[_currentMoveIndex].unit.GetComponent<RectTransform>().anchoredPosition = target.Value.GetComponent<RectTransform>().anchoredPosition;
            units[_currentMoveIndex].pos = target.Key;
            GameSfx.PlayMove();
            UnitMotion.For(units[_currentMoveIndex].unit).Hop();
            boardPanel.Trial?.OnUnitMoved(_currentMoveIndex);
            OnPointerExit(_currentMoveIndex);
            OnPointerEnter(_currentMoveIndex);
            TutorialDirector.Notify(TutorialAct.Move, _currentMoveIndex);
            boardPanel.SpendAction();
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
        if (boardPanel.IsBattleOver) return;
        if(i < 0 || i >= units.Length || !units[i].isPlaced || units[i].reviveRemainTurn > 0) return;
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        if(boardPanel.IsAnimPlaying(i)) return;
        if(boardPanel.actionPoint <= 0) return;
        if (_currentClickIndex == i && _isClicked)
        {
            if (!TutorialDirector.Allow(TutorialAct.Judge, i))
            {
                _isClicked = false;
                _currentClickIndex = -1;
                return;
            }
            int damage = 0;
            // 피해는 모은 힘·특성·아이템으로 규칙(BellkeeperTrial)이 계산한다.
            if (boardPanel.Trial == null || !boardPanel.Trial.TryPrepareJudgment(i, ref damage))
            {
                _isClicked = false;
                _currentClickIndex = -1;
                return;
            }
            boardPanel.JudgeBossFromUnit(damage, i);
            boardPanel.PlayJudgeAnim(i);
            TutorialDirector.Notify(TutorialAct.Judge, i);
            boardPanel.SpendAction(judgment: true);
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

    public void OnHit(int i)
    {
        if (boardPanel.IsBattleOver) return;
        if (i < 0 || i >= units.Length || !units[i].isPlaced || units[i].reviveRemainTurn > 0) return;
        var trial = boardPanel.Trial;
        if (trial != null && !trial.RegisterHit(i))
        {
            // 1·2번째 피격은 부상만. 판에 남는다.
            boardPanel.OnUnitRevived(i);
            return;
        }
        // 3번째 피격 = 쓰러짐. 이번 전투에서 돌아오지 않는다.
        BattleJuice.UnitDown(units[i].unit);   // 잔상이 쓰러지며 사라진다
        units[i].unit.gameObject.SetActive(false);
        units[i].reviveRemainTurn = int.MaxValue / 2;
        trial?.OnUnitDown(i);
    }

    public void OnTurnEnd() => boardPanel.Trial?.OnUnitTurnEnd();

    public void PrepareBossPattern(BossTemp boss, int turnCount)
    {
        // 준비와 행동 단계 양쪽에서 호출해도 이번 턴의 패턴은 한 번만 선택한다.
        if (_preparedDangerCells != null && _preparedBoss == boss && _preparedTurn == turnCount) return;

        ClearBossPatternPreview();
        _preparedDangerCells = boardPanel.Trial != null ? boardPanel.Trial.BuildDangerCells(turnCount) : new HashSet<Vector2Int>();
        _preparedBoss = boss;
        _preparedTurn = turnCount;
        foreach (var cell in _preparedDangerCells)
        {
            if (_attackPreviewPlates.TryGetValue(cell, out var preview))
            {
                preview.color = GetPatternColor(true);
                // 예고는 기본적으로 숨긴다(귀 기울이기로만 공개).
                preview.enabled = boardPanel.Trial != null && boardPanel.Trial.PreviewRevealed;
            }
        }
    }

    public void ClearBossPatternPreview()
    {
        _patternRevision++;
        foreach (var preview in _attackPreviewPlates.Values)
        {
            if (preview != null) preview.enabled = false;
        }
        _preparedDangerCells = null;
        _preparedBoss = null;
        _preparedTurn = -1;
        foreach (var attack in _attackPlates.Values)
        {
            if (attack != null) attack.enabled = false;
        }
    }

    public IEnumerator PlayBossPattern(BossTemp boss, int turnCount)
    {
        if (boardPanel.IsBattleOver) yield break;
        PrepareBossPattern(boss, turnCount);
        // 예고한 좌표를 그대로 공격하며, 다음 패턴을 새로 뽑지 않는다.
        var dangerCells = _preparedDangerCells;
        var revision = _patternRevision;
        foreach (var preview in _attackPreviewPlates.Values)
        {
            if (preview != null) preview.enabled = false;
        }
        if (dangerCells.Count == 0) yield break;

        foreach (var cell in dangerCells)
        {
            if (_attackPlates.TryGetValue(cell, out var attack) && attack != null)
            {
                attack.enabled = true;
                attack.color = GetPatternColor(false);
            }
        }

        var previewSeconds = boss != null && boss.patternPreviewSeconds > 0f
            ? boss.patternPreviewSeconds
            : 1f;
        // 공격 칸이 가운데부터 번지고 맞기 직전 깜빡인다 (JSW/Script/BattleJuice.cs). 걸리는 시간은 그대로.
        var telegraph = new List<(Image, Vector2Int)>();
        foreach (var cell in dangerCells)
            if (_attackPlates.TryGetValue(cell, out var plate) && plate != null) telegraph.Add((plate, cell));
        yield return BattleJuice.Telegraph(telegraph, previewSeconds);
        if (revision != _patternRevision) yield break;

        var hitUnits = new List<int>();
        var hitAnimations = new List<Coroutine>();
        for (int i = 0; i < units.Length; i++)
        {
            if (!units[i].isPlaced || units[i].reviveRemainTurn > 0) continue;
            if (!dangerCells.Contains(units[i].pos)) continue;
            hitUnits.Add(i);
            var hitAnimation = boardPanel.HitAttack(i);
            if (hitAnimation != null) hitAnimations.Add(hitAnimation);
        }

        var hitImages = new List<Image>();
        foreach (int h in hitUnits) hitImages.Add(units[h].unit);
        BattleJuice.UnitsHit(boardPanel, hitImages);   // 맞은 캐릭터 번쩍임 + 판 흔들림

        // 맞은 캐릭터 1명당 플레이어 HP를 깎는다.
        var trial = boardPanel.Trial;
        if (hitUnits.Count > 0 && trial != null) boardPanel.OnBossAttack(trial.HitDamageTotal(hitUnits));
        trial?.AfterBossAttack(hitUnits);

        // 동일한 1초 타이머끼리 경쟁하지 않고 실제 피격/사망 처리가 끝날 때까지 기다린다.
        foreach (var hitAnimation in hitAnimations)
        {
            yield return hitAnimation;
            if (revision != _patternRevision) yield break;
        }
        if (hitAnimations.Count == 0) yield return new WaitForSeconds(1f);
        if (revision != _patternRevision) yield break;

        foreach (var cell in dangerCells)
        {
            if (_attackPlates.TryGetValue(cell, out var attack) && attack != null)
            {
                attack.enabled = false;
            }
        }
        // 다음 턴의 Prepare 호출까지 확정 좌표를 유지한다.
    }

    // ───── 시험 규칙(BellkeeperTrial)용 조회 ─────
    public bool IsUnitOnBoard(int i) => i >= 0 && i < units.Length && units[i].isPlaced && units[i].reviveRemainTurn <= 0;
    public Vector2Int GetUnitPos(int i) => units[i].pos;
    // v3 보스 특성(문)용: 빈칸이면 지정 칸으로 이동. 행동력·이동 효과 없음.
    public bool MoveUnitTo(int i, Vector2Int cell)
    {
        if (i < 0 || i >= units.Length || !units[i].isPlaced || units[i].reviveRemainTurn > 0) return false;
        if (!_blankPos.ContainsKey(cell)) return false;
        for (int k = 0; k < units.Length; k++) if (k != i && units[k].isPlaced && units[k].pos == cell) return false;
        units[i].pos = cell;
        units[i].unit.rectTransform.anchoredPosition = _blankPos[cell].GetComponent<RectTransform>().anchoredPosition;
        UnitMotion.For(units[i].unit).Hop();
        return true;
    }

    public int GetUnitId(int i) => units[i].unitTemp != null ? units[i].unitTemp.id : 0;
    public string GetUnitName(int i) => units[i].unitTemp != null ? units[i].unitTemp.unitName : "";
    public Image GetUnitImage(int i) => units[i].unit;
    public RectTransform GetWaitingRect(int i) => i >= 0 && i < units.Length ? units[i].unitOnWaiting.rectTransform : null;
    public RectTransform GetCellRect(Vector2Int cell) => _blankPos.TryGetValue(cell, out var go) ? (RectTransform)go.transform : null;
    public bool IsPreparedDanger(Vector2Int cell) => _preparedDangerCells != null && _preparedDangerCells.Contains(cell);

    public void RevealPreparedPreview()
    {
        if (_preparedDangerCells == null) return;
        foreach (var cell in _preparedDangerCells)
            if (_attackPreviewPlates.TryGetValue(cell, out var preview) && preview != null)
            {
                preview.color = GetPatternColor(true);
                preview.enabled = true;
            }
    }

    private static Color GetPatternColor(bool preview)
        => preview ? new Color(1f, 1f, 1f, 0.65f) : new Color(1f, 0.2f, 0.2f, 0.65f);
}
