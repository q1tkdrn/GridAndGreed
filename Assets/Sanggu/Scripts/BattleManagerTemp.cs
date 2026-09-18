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
        public CharacterPassiveState passive;
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
    private BattlePatternRules.ColorEffect _preparedEffect;
    [SerializeField] private Unit[] units = new Unit[3];
    
    [SerializeField] private EventSystem eventSystem;

    private List<GameObject> _movableBlank = new();
    private int _currentMoveIndex = -1;
    private int _currentClickIndex = -1;

    private bool _isClicked = false;
    private readonly BattlePatternRules.PlazaSequence _plazaSequence = new();
    private readonly BattlePatternRules.MansionSequence _mansionSequence = new();
    private readonly BattlePatternRules.CathedralSequence _cathedralSequence = new();
    private readonly BattlePatternRules.DoorSequence _doorSequence = new();
    private readonly BattlePatternRules.BasementSequence _basementSequence = new();
    private readonly BattlePatternRules.LibrarySequence _librarySequence = new();
    private readonly BattlePatternRules.TrainingSequence _trainingSequence = new();
    private readonly BattlePatternRules.AfterlifePhase1Sequence _afterlifePhase1Sequence = new();
    private readonly BattlePatternRules.AfterlifePhase2Sequence _afterlifePhase2Sequence = new();
    [SerializeField] private float doubleClickDelay = 0.1f;

    //활성하시 클릭 가능한 칸 생성
    public void Init()
    {
        ClearBossPatternPreview();
        _attackPreviewPlates.Clear();
        _attackPlates.Clear();
        if (attackPreviewSprite == null)
        {
            attackPreviewSprite = Resources.Load<Sprite>("공격 예상");
        }
        _plazaSequence.Reset();
        _mansionSequence.Reset();
        _cathedralSequence.Reset();
        _doorSequence.Reset();
        _basementSequence.Reset();
        _librarySequence.Reset();
        _trainingSequence.Reset();
        _afterlifePhase1Sequence.Reset();
        _afterlifePhase2Sequence.Reset();
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
            units[i].passive = new CharacterPassiveState(units[i].unitTemp.id);
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
            units[i].passive.StartTurn();
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
        // 부활 대기 중인 유닛만 남아도 다음 부활 턴으로 진행할 수 있어야 한다.
        if (!tb && units.All(u => u.reviveRemainTurn > 0)) boardPanel.UpdateActionPoint(0);
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
        if (boardPanel.IsBattleOver) return;
        if(i < 0 || i >= units.Length || !units[i].isPlaced || units[i].reviveRemainTurn > 0) return;
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
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
        if (target.Value != null && _movableBlank.Contains(target.Value))
        {
            units[_currentMoveIndex].unit.GetComponent<RectTransform>().anchoredPosition = target.Value.GetComponent<RectTransform>().anchoredPosition;
            units[_currentMoveIndex].pos = target.Key;
            var passive = units[_currentMoveIndex].passive;
            passive.Move();
            boardPanel.AttackBoss(passive.MoveDamage);
            boardPanel.HealReaper(passive.MoveHealing);
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
        if (boardPanel.IsBattleOver) return;
        if(i < 0 || i >= units.Length || !units[i].isPlaced || units[i].reviveRemainTurn > 0) return;
        if(boardPanel.turn != BoardPanel.ETurn.Player) return;
        if(boardPanel.IsAnimPlaying(i)) return;
        if(boardPanel.actionPoint <= 0) return;
        if (_currentClickIndex == i && _isClicked)
        {
            Debug.Log("db");
            var passive = units[i].passive;
            boardPanel.JudgeBoss(units[i].unitTemp.intelligence + passive.IntelligenceBonus);
            boardPanel.HealReaper(passive.JudgeHealing);
            passive.AfterJudge();
            boardPanel.PlayJudgeAnim(i);
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
        foreach (var unit in units.Where(x => x.isPlaced && x.reviveRemainTurn <= 0).ToList())
        {
            if (boardPanel.IsBattleOver) break;
            boardPanel.AutomaticAttackBoss(unit.unitTemp.power + unit.passive.PowerBonus);
            boardPanel.PlayAttackAnim(units.ToList().IndexOf(unit));
        }
    }

    public void OnHit(int i)
    {
        if (i < 0 || i >= units.Length || !units[i].isPlaced || units[i].reviveRemainTurn > 0) return;
        units[i].unit.gameObject.SetActive(false);
        units[i].reviveRemainTurn = Mathf.Max(1, units[i].unitTemp.reviveCool);
        units[i].passive.OnDeath();
        // 전투 내 누적 효과는 자신이 사망/부활해도 유지한다.
        foreach (var unit in units) unit.passive.OnAllyDeath();
        boardPanel.OnAllyDeath();
        if (units[i].passive.LosesBattleOnDeath) boardPanel.LoseBattle();
    }

    public bool TryBlockHit(int i)
    {
        return units[i].passive.TryBlockHit();
    }

    public void OnTurnEnd()
    {
        foreach (var unit in units)
        {
            if (boardPanel.IsBattleOver) break;
            if (unit.isPlaced && unit.reviveRemainTurn <= 0)
                boardPanel.AttackBoss(unit.passive.TurnEndDamage);
        }
    }

    public void PrepareBossPattern(BossTemp boss, int turnCount)
    {
        // 준비와 행동 단계 양쪽에서 호출해도 이번 턴의 패턴은 한 번만 선택한다.
        if (_preparedDangerCells != null && _preparedBoss == boss && _preparedTurn == turnCount) return;

        ClearBossPatternPreview();
        var pattern = GetPattern(boss, turnCount);
        _preparedEffect = BattlePatternRules.GetColorEffect(pattern);
        _preparedDangerCells = BattlePatternRules.GetDangerCells(pattern);
        _preparedBoss = boss;
        _preparedTurn = turnCount;
        foreach (var cell in _preparedDangerCells)
        {
            if (_attackPreviewPlates.TryGetValue(cell, out var preview))
            {
                preview.color = GetPatternColor(_preparedEffect, true);
                preview.enabled = true;
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
        _preparedEffect = BattlePatternRules.ColorEffect.None;
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
        var effect = _preparedEffect;
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
                attack.color = GetPatternColor(effect, false);
            }
        }

        var previewSeconds = boss != null && boss.patternPreviewSeconds > 0f
            ? boss.patternPreviewSeconds
            : 1f;
        yield return new WaitForSeconds(previewSeconds);
        if (revision != _patternRevision) yield break;

        var damage = boss != null && boss.patternDamage > 0 ? boss.patternDamage : 5;
        var hitUnits = new List<int>();
        var hitAnimations = new List<Coroutine>();
        for (int i = 0; i < units.Length; i++)
        {
            if (!units[i].isPlaced || units[i].reviveRemainTurn > 0) continue;
            if (!dangerCells.Contains(units[i].pos)) continue;
            if (TryBlockHit(i)) continue;

            hitUnits.Add(i);
            var hitAnimation = boardPanel.HitAttack(i);
            if (hitAnimation != null) hitAnimations.Add(hitAnimation);
        }

        // 피격 캐릭터 수와 관계없이 이번 패턴의 공유 HP 피해는 한 번만 적용한다.
        if (hitUnits.Count > 0) boardPanel.OnBossAttack(damage);

        if (!boardPanel.IsBattleOver)
        {
            switch (effect)
            {
                case BattlePatternRules.ColorEffect.Relocate:
                    RelocateHitUnits(hitUnits);
                    break;
                case BattlePatternRules.ColorEffect.ReduceNextActionPoints:
                    boardPanel.ReduceNextTurnActionPoints(hitUnits.Count);
                    break;
                case BattlePatternRules.ColorEffect.DamageBossOnDodge:
                    if (hitUnits.Count == 0) boardPanel.AttackBoss(3);
                    break;
            }
        }

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

    private BattlePatternRules.Pattern GetPattern(BossTemp boss, int turnCount)
    {
        // 기존 저승 2연전 전환에 대응한다. 페이즈별 보스 패시브는 별도로 적용한다.
        if (boss != null && boss.bossId == "death1") return _afterlifePhase1Sequence.Next();
        if (boss != null && boss.bossId == "death2") return _afterlifePhase2Sequence.Next();
        // 대저택의 A/B/C는 대저택 전투에서만 사용한다.
        if (boss != null && boss.bossId == "noble")
        {
            return _mansionSequence.Next();
        }

        // Fusion.asset은 광장 스테이지다. 배열 순환 대신 A/B/C 묶음을 진행한다.
        if (boss != null && boss.bossId == "fusion")
        {
            return _plazaSequence.Next();
        }

        if (boss != null && boss.bossId == "door")
        {
            return _doorSequence.Next();
        }

        if (boss != null && boss.bossId == "subject")
        {
            return _basementSequence.Next();
        }

        if (boss != null && boss.bossId == "secretary")
        {
            return _librarySequence.Next();
        }

        if (boss != null && boss.bossId == "instructor")
        {
            return _trainingSequence.Next();
        }

        var patterns = boss?.patterns;
        // Pope.asset은 성당 스테이지다. 다른 스테이지와 진행 상태를 공유하지 않는다.
        if (boss != null && boss.bossId == "pope")
        {
            return _cathedralSequence.Next();
        }

        if (patterns != null && patterns.Length > 0)
        {
            return patterns[(turnCount - 1) % patterns.Length];
        }

        // 미설정 보스에게 대저택 패턴을 대신 적용하지 않는다.
        return BattlePatternRules.Pattern.None;
    }

    private void RelocateHitUnits(List<int> hitUnits)
    {
        var occupied = new HashSet<Vector2Int>();
        for (int i = 0; i < units.Length; i++)
            if (units[i].isPlaced && !hitUnits.Contains(i)) occupied.Add(units[i].pos);
        var destinations = BattlePatternRules.PickRelocationCells(hitUnits.Count, occupied);
        for (int n = 0; n < destinations.Count; n++)
        {
            int i = hitUnits[n];
            units[i].pos = destinations[n];
            units[i].unit.rectTransform.anchoredPosition = _blankPos[destinations[n]].GetComponent<RectTransform>().anchoredPosition;
        }
        // 강제 이동은 아군의 이동 행동이 아니므로 이동 패시브/행동력은 발동하지 않는다.
        OnPointerExit(-1);
        _currentMoveIndex = -1;
    }

    private static Color GetPatternColor(BattlePatternRules.ColorEffect effect, bool preview)
    {
        return effect switch
        {
            BattlePatternRules.ColorEffect.Relocate => new Color(1f, 0.2f, 0.2f, 0.65f),
            BattlePatternRules.ColorEffect.ReduceNextActionPoints => new Color(0.2f, 0.85f, 0.35f, 0.65f),
            BattlePatternRules.ColorEffect.DamageBossOnDodge => new Color(1f, 0.85f, 0.15f, 0.65f),
            _ => preview ? new Color(1f, 1f, 1f, 0.65f) : new Color(1f, 0.2f, 0.2f, 0.65f)
        };
    }
}
