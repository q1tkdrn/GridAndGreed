using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Source = AfterlifePassiveRules.DamageSource;

/// <summary>
/// 전투 개정안 v2 시험 규칙. 종지기(bellkeeper) 전투에서만 생성되며 다른 보스 전투에는 관여하지 않는다.
///
/// - 방향: 보스를 읽고, 어디까지 욕심낼지 정한다.
/// - 힘(공격력)은 0에서 시작. 힘 칸을 밟아 모으고, 심판(더블클릭)으로 전부 터뜨린다. 5 이상 x1.5, 10 이상 x2.
/// - 매 턴 힘 칸: 안전한 칸 3개(+2, 1턴은 5개), 욕심 칸 2개(공격 범위 안쪽 깊은 곳, 밟으면 +3, 버티면 +5 더).
/// - 피격: 맞은 캐릭터 1명당 플레이어 HP -4. 1·2번째 피격 힘 +2, 3번째 피격 쓰러짐(모은 힘 소멸).
/// - 의지력: 턴 끝 HP 감소. 1,1,2,2,3,3 이후 5,7,9…
/// - 공격 범위는 항상 표시. 캐릭터 특성과 아이템 효과는 끈다.
/// - 수치 근거: 시뮬레이션 D안 (숙련 89%, 보통 71%, 처음 수준 37%).
/// </summary>
public sealed class BellkeeperTrial
{
    public const int PlayerMaxHp = 30;
    public const int BossMaxHp = 100;
    public const int StartWillPower = 1;
    public const int HitDamage = 4;
    private const int ActionPointsPerTurn = 6;
    private const float PhaseTwoRatio = 0.5f;
    private const int SafeCells = 3;
    private const int FirstTurnExtraSafeCells = 2;
    private const int SafeValue = 2;
    private const int GreedCells = 2;
    private const int GreedStepValue = 3;
    private const int GreedStandValue = 5;
    private const int GreedDepth = 2;
    private static readonly int[] HitBonus = { 2, 2 };
    private const int DownAtHit = 3;
    private const int FuneralQuietCells = 5;

    public static bool IsTrialBoss(BossTemp boss) => boss != null && boss.bossId == "bellkeeper";
    public static int WillPowerAt(int turn) => turn <= 6 ? 1 + (turn - 1) / 2 : 3 + 2 * (turn - 6);
    public static int ReleaseDamage(int charge) => charge >= 10 ? charge * 2 : charge >= 5 ? charge * 3 / 2 : charge;

    private enum Toll { Ripple, Pendulum, Crack, Funeral }
    private static readonly Toll[] PhaseOneCycle = { Toll.Ripple, Toll.Pendulum, Toll.Crack };
    private static readonly Toll[] PhaseTwoCycle = { Toll.Pendulum, Toll.Funeral, Toll.Crack, Toll.Ripple };

    private readonly BoardPanel _board;
    private readonly BattleManagerTemp _battle;

    private int _phase = 1;
    private int _pendingPhase = 1;
    private int _cycleIndex;
    private Toll _toll;
    private bool _pendulumLeft;
    private bool _lastPendulumLeft = true;
    private bool _phaseLinePrinted2;

    private readonly int[] _charge = new int[3];
    private readonly int[] _hits = new int[3];
    private readonly bool[] _down = new bool[3];
    private readonly bool[] _standing = new bool[3];
    private readonly Vector2Int[] _standCell = new Vector2Int[3];

    private readonly Dictionary<Vector2Int, bool> _powerCells = new(); // true = 욕심 칸
    private readonly HashSet<Vector2Int> _quiet = new();

    private readonly List<GameObject> _markers = new();
    private readonly List<GameObject> _chargeLabels = new();

    private TrialHud _hud;
    private GameObject _report;

    // 공격 범위는 항상 보인다.
    public bool PreviewRevealed => true;

    // 패배 분석용 기록
    private int _turnsPlayed;
    private int _maxPhase = 1;
    private int _willDamageTotal;
    private int _hitHpTotal;
    private int _releaseCount;
    private int _bestRelease;
    private int _greedStepped;
    private int _greedStood;
    private int _wastedCharge;
    private readonly Dictionary<Toll, int> _hitsByToll = new();
    private readonly List<string> _downLog = new();

    public BellkeeperTrial(BoardPanel board, BattleManagerTemp battle)
    {
        _board = board;
        _battle = battle;
    }

    // ───────────────────────── 전투 흐름 ─────────────────────────

    public void OnBattleStart()
    {
        ResetUnitTint();
        _hud = TrialHud.Create(_board);
        _board.PrintText("[시험 규칙 v2] 힘은 0에서 시작. 힘 칸을 밟아 모으고, 더블클릭 심판으로 한 번에 터뜨린다 (5 이상 x1.5, 10 이상 x2)");
        _board.PrintText("[욕심 칸] 공격 범위 안의 금색 칸. 밟으면 힘 +3, 종이 울릴 때까지 버티면 +5 더");
        _board.PrintText("[피격] 맞은 캐릭터 1명당 HP -4. 1·2번째 피격은 힘 +2, 3번째 피격에 쓰러진다 (모은 힘 소멸)");
    }

    public void BeginTurn(int turnCount)
    {
        _turnsPlayed = turnCount;
        if (_pendingPhase != _phase)
        {
            _phase = _pendingPhase;
            _cycleIndex = 0;
            _maxPhase = Math.Max(_maxPhase, _phase);
        }
        var cycle = _phase == 1 ? PhaseOneCycle : PhaseTwoCycle;
        _toll = cycle[_cycleIndex % cycle.Length];

        // 지난 턴의 힘 칸은 사라진다.
        _powerCells.Clear();
        _quiet.Clear();
        for (int i = 0; i < 3; i++) _standing[i] = false;

        _board.UpdateBossWillPower(WillPowerAt(turnCount));
        if (_toll == Toll.Pendulum) _pendulumLeft = ChoosePendulumSide();

        _board.UpdateActionPoint(ActionPointsPerTurn);
        RefreshMarkers();
        RefreshChargeLabels();
        var (title, hint) = Signal();
        _board.PrintText($"[{title}] {hint}");
        _hud?.SetSignal(title, hint, _phase, _board.willPower);
    }

    public void EndTurn()
    {
        _willDamageTotal += Math.Max(0, _board.willPower);
        _cycleIndex++;
    }

    public void OnUnitTurnEnd() { }

    public void OnBossHpChanged()
    {
        if (_board.bossMaxHp <= 0 || _board.bossCurrentHp <= 0) return;
        float ratio = (float)_board.bossCurrentHp / _board.bossMaxHp;
        if (ratio <= PhaseTwoRatio && _pendingPhase < 2)
        {
            _pendingPhase = 2;
            if (!_phaseLinePrinted2)
            {
                _phaseLinePrinted2 = true;
                _board.PrintText("\"…종이, 너무 무거워졌어. 이번엔 내 장례까지 울려야겠군.\"");
                _board.PrintText("[2페이즈] 다음 턴부터 진자 → 장례 → 균열 → 파문 순서로 울린다. 장례는 고요한 칸 다섯 곳만 안전하다.");
            }
        }
    }

    public string VictoryLine => "\"…들리나. 이제야 종을 내려놓는구나.\"";

    // ───────────────────────── 공격 패턴 + 힘 칸 ─────────────────────────

    public HashSet<Vector2Int> BuildDangerCells(int turnCount)
    {
        var cells = new HashSet<Vector2Int>();
        switch (_toll)
        {
            case Toll.Ripple:
            {
                // 파문은 아군 무리의 중심에서 퍼진다. 중심에서 2칸, 4칸 떨어진 고리.
                var c = GroupCenter();
                for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                {
                    int d = Math.Max(Math.Abs(x - c.x), Math.Abs(y - c.y));
                    if (d == 2 || d == 4) cells.Add(new Vector2Int(x, y));
                }
                break;
            }
            case Toll.Pendulum:
                for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    if (_pendulumLeft ? x <= 3 : x >= 5) cells.Add(new Vector2Int(x, y));
                break;
            case Toll.Crack:
                for (int i = 0; i < 9; i++)
                {
                    cells.Add(new Vector2Int(4, i));
                    cells.Add(new Vector2Int(i, 4));
                }
                break;
            case Toll.Funeral:
                _quiet.Clear();
                while (_quiet.Count < FuneralQuietCells)
                    _quiet.Add(new Vector2Int(UnityEngine.Random.Range(0, 9), UnityEngine.Random.Range(0, 9)));
                for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!_quiet.Contains(cell)) cells.Add(cell);
                }
                break;
        }

        SpawnPowerCells(cells, turnCount);
        RefreshMarkers();
        RefreshChargeLabels();
        return cells;
    }

    private void SpawnPowerCells(HashSet<Vector2Int> danger, int turnCount)
    {
        _powerCells.Clear();
        var occupied = new HashSet<Vector2Int>(Enumerable.Range(0, 3).Where(_battle.IsUnitOnBoard).Select(_battle.GetUnitPos));
        var depth = EscapeDepth(danger);

        var greedPool = danger.Where(c => !occupied.Contains(c) && depth[c.x, c.y] >= GreedDepth).ToList();
        if (greedPool.Count == 0) greedPool = danger.Where(c => !occupied.Contains(c)).ToList();
        foreach (var cell in PickRandom(greedPool, GreedCells)) _powerCells[cell] = true;

        var safePool = new List<Vector2Int>();
        for (int x = 0; x < 9; x++)
        for (int y = 0; y < 9; y++)
        {
            var cell = new Vector2Int(x, y);
            if (!danger.Contains(cell) && !occupied.Contains(cell)) safePool.Add(cell);
        }
        int safeCount = SafeCells + (turnCount == 1 ? FirstTurnExtraSafeCells : 0);
        foreach (var cell in PickRandom(safePool, safeCount)) _powerCells[cell] = false;
    }

    // 각 공격 칸에서 가장 가까운 안전한 칸까지 필요한 이동 수 (8방향).
    private static int[,] EscapeDepth(HashSet<Vector2Int> danger)
    {
        var dist = new int[9, 9];
        var queue = new Queue<Vector2Int>();
        for (int x = 0; x < 9; x++)
        for (int y = 0; y < 9; y++)
        {
            if (danger.Contains(new Vector2Int(x, y))) dist[x, y] = -1;
            else { dist[x, y] = 0; queue.Enqueue(new Vector2Int(x, y)); }
        }
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = p.x + dx, ny = p.y + dy;
                if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= 9 || ny >= 9 || dist[nx, ny] >= 0) continue;
                dist[nx, ny] = dist[p.x, p.y] + 1;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }
        for (int x = 0; x < 9; x++)
        for (int y = 0; y < 9; y++)
            if (dist[x, y] < 0) dist[x, y] = 99;
        return dist;
    }

    private static List<Vector2Int> PickRandom(List<Vector2Int> pool, int count)
    {
        var copy = new List<Vector2Int>(pool);
        var result = new List<Vector2Int>();
        while (result.Count < count && copy.Count > 0)
        {
            int k = UnityEngine.Random.Range(0, copy.Count);
            result.Add(copy[k]);
            copy.RemoveAt(k);
        }
        return result;
    }

    private Vector2Int GroupCenter()
    {
        var positions = Enumerable.Range(0, 3).Where(_battle.IsUnitOnBoard).Select(_battle.GetUnitPos).ToList();
        if (positions.Count == 0) return new Vector2Int(4, 4);
        int cx = Mathf.RoundToInt((float)positions.Average(p => p.x));
        int cy = Mathf.RoundToInt((float)positions.Average(p => p.y));
        return new Vector2Int(Mathf.Clamp(cx, 2, 6), Mathf.Clamp(cy, 2, 6));
    }

    // 종은 아군이 더 많이 있는 쪽으로 기운다. 같으면 지난번과 반대쪽.
    private bool ChoosePendulumSide()
    {
        int left = 0, right = 0;
        for (int i = 0; i < 3; i++)
        {
            if (!_battle.IsUnitOnBoard(i)) continue;
            var p = _battle.GetUnitPos(i);
            if (p.x < 4) left++;
            else if (p.x > 4) right++;
        }
        bool result = left == right ? !_lastPendulumLeft : left > right;
        _lastPendulumLeft = result;
        return result;
    }

    private (string title, string hint) Signal() => _toll switch
    {
        Toll.Ripple => ("첫 번째 종, 파문", "아군이 모인 곳을 중심으로 종소리가 고리로 퍼진다."),
        Toll.Pendulum => ("두 번째 종, 진자", _pendulumLeft ? "종이 왼쪽으로 크게 기운다. 왼쪽 네 줄." : "종이 오른쪽으로 크게 기운다. 오른쪽 네 줄."),
        Toll.Crack => ("세 번째 종, 균열", "종에 금이 간다. 판 한가운데를 십자로 친다."),
        _ => ("자신의 장례", "종이 쉬지 않고 울린다. 고요한 칸 다섯 곳만 조용하다.")
    };

    // ───────────────────────── 이동 · 심판 ─────────────────────────

    public void OnUnitMoved(int i)
    {
        var pos = _battle.GetUnitPos(i);
        if (_standing[i] && pos != _standCell[i]) _standing[i] = false;

        if (_powerCells.TryGetValue(pos, out bool greed))
        {
            _powerCells.Remove(pos);
            if (greed)
            {
                _charge[i] += GreedStepValue;
                _standing[i] = true;
                _standCell[i] = pos;
                _greedStepped++;
                _board.PrintText($"[욕심 칸] {_battle.GetUnitName(i)} 힘 +{GreedStepValue} (종이 울릴 때까지 버티면 +{GreedStandValue})");
            }
            else
            {
                _charge[i] += SafeValue;
                _board.PrintText($"[힘 칸] {_battle.GetUnitName(i)} 힘 +{SafeValue}");
            }
        }
        RefreshMarkers();
        RefreshChargeLabels();
    }

    public bool TryPrepareJudgment(int i, ref int damage)
    {
        if (!_battle.IsUnitOnBoard(i)) return false;
        int charge = _charge[i];
        if (charge <= 0)
        {
            _board.PrintText($"{_battle.GetUnitName(i)}: 모은 힘이 없다. 힘 칸을 밟아 모으자.");
            return false;
        }
        damage = ReleaseDamage(charge);
        string mult = charge >= 10 ? " x2" : charge >= 5 ? " x1.5" : "";
        _board.PrintText($"{_battle.GetUnitName(i)} 심판: 힘 {charge}{mult} → 피해 {damage}");
        _charge[i] = 0;
        _releaseCount++;
        _bestRelease = Math.Max(_bestRelease, damage);
        RefreshChargeLabels();
        return true;
    }

    /// <summary>시험 규칙에서는 심판만 보스에게 피해를 준다.</summary>
    public int ResolveIncomingDamage(Source source, int damage)
    {
        if (source != Source.Judgment || damage <= 0) return 0;
        return damage;
    }

    // ───────────────────────── 피격 · 쓰러짐 ─────────────────────────

    // 보스 공격 직후(피격 연출 전) 호출된다. 플레이어 HP는 BattleManagerTemp가 1명당 HitDamage씩 깎는다.
    public void AfterBossAttack(List<int> hitUnits)
    {
        if (hitUnits != null && hitUnits.Count > 0)
        {
            _hitHpTotal += HitDamage * hitUnits.Count;
            _hitsByToll[_toll] = (_hitsByToll.TryGetValue(_toll, out var n) ? n : 0) + hitUnits.Count;
            foreach (int i in hitUnits)
            {
                _hits[i]++;
                if (_hits[i] < DownAtHit)
                {
                    int gain = HitBonus[_hits[i] - 1];
                    bool stood = _standing[i] && _battle.GetUnitPos(i) == _standCell[i];
                    if (stood) { gain += GreedStandValue; _greedStood++; }
                    _charge[i] += gain;
                    _board.PrintText($"[피격] {_battle.GetUnitName(i)} {_hits[i]}번째 피격: 힘 +{gain}{(stood ? " (욕심 칸에서 버팀)" : "")}");
                    var image = _battle.GetUnitImage(i);
                    if (image != null) image.color = _hits[i] == 1 ? new Color(1f, 0.8f, 0.8f, 1f) : new Color(1f, 0.55f, 0.55f, 1f);
                }
                else
                {
                    _board.PrintText($"[피격] {_battle.GetUnitName(i)} {_hits[i]}번째 피격: 쓰러진다");
                }
            }
        }
        for (int i = 0; i < 3; i++) _standing[i] = false;
        RefreshMarkers();
        RefreshChargeLabels();
    }

    /// <returns>true면 쓰러진다. false면 판에 남는다.</returns>
    public bool RegisterHit(int i) => _hits[i] >= DownAtHit;

    public void OnUnitDown(int i)
    {
        _down[i] = true;
        int lost = _charge[i];
        _wastedCharge += lost;
        _charge[i] = 0;
        _downLog.Add($"{_turnsPlayed}턴 {TollName(_toll)}: {_battle.GetUnitName(i)} 쓰러짐 (힘 {lost} 소멸)");
        _board.PrintText($"[쓰러짐] {_battle.GetUnitName(i)}: 이번 전투에서 빠진다. 모은 힘 {lost} 소멸");
        RefreshChargeLabels();
        if (!_board.IsBattleOver && Enumerable.Range(0, 3).All(k => !_battle.IsUnitOnBoard(k)))
        {
            _board.PrintText("[패배] 모두 쓰러졌다.");
            _board.LoseBattle();
        }
    }

    public string DescribeUnit(int i)
    {
        int charge = _charge[i];
        string next = _hits[i] >= DownAtHit - 1 ? "다음 피격: 쓰러짐" : $"다음 피격: 힘 +{HitBonus[_hits[i]]}";
        string state = _down[i] ? "쓰러짐" : $"피격 {_hits[i]}/{DownAtHit}, {next}";
        return $"<color=#FFD27A>모은 힘 {charge}</color> → 지금 심판하면 피해 {ReleaseDamage(charge)}\n{state}"
            + (_standing[i] ? $"\n욕심 칸에서 버티는 중: 맞으면 힘 +{GreedStandValue} 더" : "");
    }

    // ───────────────────────── 화면 표시 ─────────────────────────

    private void RefreshMarkers()
    {
        foreach (var marker in _markers) if (marker != null) UnityEngine.Object.Destroy(marker);
        _markers.Clear();
        // 고요한 칸은 회색이 아니라서 보인다. 힘 칸과 겹치면 힘 칸 표시만 남긴다.
        foreach (var cell in _quiet.Where(c => !_powerCells.ContainsKey(c)))
            Add(_markers, CreateCellLabel(cell, "고요", 16f, new Color(0.85f, 0.95f, 1f), new Color(0.1f, 0.2f, 0.3f, 0.92f), new Vector2(0f, -28f), new Vector2(56f, 22f)));
        foreach (var pair in _powerCells)
        {
            if (pair.Value)
                Add(_markers, CreateCellLabel(pair.Key, $"+{GreedStepValue}/+{GreedStandValue}", 17f, new Color(1f, 0.86f, 0.35f), new Color(0.36f, 0.24f, 0.02f, 0.95f), new Vector2(0f, -27f), new Vector2(66f, 24f)));
            else
                Add(_markers, CreateCellLabel(pair.Key, $"힘+{SafeValue}", 17f, new Color(0.72f, 1f, 0.76f), new Color(0.06f, 0.28f, 0.12f, 0.92f), new Vector2(0f, -27f), new Vector2(60f, 24f)));
        }
        for (int i = 0; i < 3; i++)
        {
            if (!_standing[i] || !_battle.IsUnitOnBoard(i)) continue;
            Add(_markers, CreateCellLabel(_standCell[i], $"버티면+{GreedStandValue}", 15f, new Color(1f, 0.86f, 0.35f), new Color(0.36f, 0.24f, 0.02f, 0.95f), new Vector2(0f, -28f), new Vector2(72f, 22f)));
        }
    }

    private void RefreshChargeLabels()
    {
        foreach (var label in _chargeLabels) if (label != null) UnityEngine.Object.Destroy(label);
        _chargeLabels.Clear();
        for (int i = 0; i < 3; i++)
        {
            if (!_battle.IsUnitOnBoard(i)) continue;
            int c = _charge[i];
            string text = c > 0 ? $"힘 {c}→{ReleaseDamage(c)}" : "힘 0";
            var color = c >= 10 ? new Color(1f, 0.82f, 0.3f) : c >= 5 ? new Color(1f, 0.65f, 0.4f) : Color.white;
            Add(_chargeLabels, CreateCellLabel(_battle.GetUnitPos(i), text, 17f, color, new Color(0f, 0f, 0f, 0.8f), new Vector2(0f, 29f), new Vector2(84f, 22f)));
        }
    }

    private static void Add(List<GameObject> list, GameObject go)
    {
        if (go != null) list.Add(go);
    }

    private GameObject CreateCellLabel(Vector2Int cell, string value, float size, Color textColor, Color background, Vector2 offset, Vector2 boxSize)
    {
        var cellRect = _battle.GetCellRect(cell);
        if (cellRect == null || cellRect.parent == null) return null;
        var go = new GameObject("TrialLabel", typeof(RectTransform), typeof(Image));
        go.layer = cellRect.gameObject.layer;
        var rect = (RectTransform)go.transform;
        // 캐릭터 그림 위에 보이도록 칸의 부모(보드)에 맨 마지막으로 붙인다.
        rect.SetParent(cellRect.parent, false);
        rect.anchorMin = cellRect.anchorMin;
        rect.anchorMax = cellRect.anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = boxSize;
        rect.anchoredPosition = cellRect.anchoredPosition + offset;
        rect.SetAsLastSibling();
        var image = go.GetComponent<Image>();
        image.color = background;
        image.raycastTarget = false;

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = go.layer;
        var textRect = (RectTransform)textObject.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        var text = textObject.GetComponent<TextMeshProUGUI>();
        if (_board.UiFont != null) text.font = _board.UiFont;
        text.text = value;
        text.fontSize = size;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return go;
    }

    // ───────────────────────── 패배 분석 ─────────────────────────

    public void ShowDefeatReport()
    {
        if (_report != null) return;
        var lines = new List<string>
        {
            "<color=#FFD27A>패배 분석, 장송의 종지기</color>",
            $"버틴 턴 {_turnsPlayed}, 도달한 페이즈 {_maxPhase}, 보스 남은 HP {_board.bossCurrentHp}/{_board.bossMaxHp}",
            "",
            "<color=#FFD27A>무엇에 졌나</color>",
            $"피격으로 잃은 HP {_hitHpTotal}, 의지력으로 잃은 HP {_willDamageTotal}",
        };
        if (_hitsByToll.Count > 0)
            lines.Add("기술별 피격: " + string.Join(", ", _hitsByToll.Select(p => $"{TollName(p.Key)} {p.Value}명")));
        lines.AddRange(_downLog);
        lines.Add("");
        lines.Add("<color=#FFD27A>욕심</color>");
        lines.Add($"심판 {_releaseCount}회, 가장 큰 한 방 {_bestRelease}");
        lines.Add($"욕심 칸 밟음 {_greedStepped}회, 끝까지 버팀 {_greedStood}회");
        if (_wastedCharge > 0) lines.Add($"쓰러지며 잃은 힘 {_wastedCharge}");
        lines.Add("");
        lines.Add("<color=#FFD27A>힌트</color>");
        lines.Add(Hint());
        _report = TrialHud.CreateReport(_board, string.Join("\n", lines));
    }

    private string Hint()
    {
        if (_wastedCharge >= 8) return "쓰러지면 모은 힘이 사라진다. 두 번 맞은 캐릭터는 먼저 터뜨리자.";
        if (_turnsPlayed >= 7) return "의지력은 7턴부터 크게 오른다. 힘을 모아 그 전에 몰아치자.";
        var worst = _hitsByToll.OrderByDescending(p => p.Value).FirstOrDefault();
        if (worst.Value >= 3 && worst.Key == Toll.Ripple) return "파문은 아군이 모인 곳을 중심으로 퍼진다. 흩어지면 고리를 읽기 쉽다.";
        if (worst.Value >= 3 && worst.Key == Toll.Pendulum) return "종은 아군이 많은 쪽으로 기운다. 기우는 쪽을 미리 정할 수 있다.";
        if (worst.Value >= 3 && worst.Key == Toll.Crack) return "균열은 가운데 줄과 가운데 칸만 친다. 판 구석은 안전하다.";
        if (worst.Value >= 3 && worst.Key == Toll.Funeral) return "장례는 고요한 칸 다섯 곳만 안전하다. 전 턴에 가운데 쪽에 서 있으면 닿기 쉽다.";
        if (_greedStood == 0) return "욕심 칸에서 끝까지 버티면 힘 +5와 피격 보너스 +2를 함께 얻는다.";
        if (_bestRelease < 20) return "힘을 10 이상 모아 터뜨리면 피해가 2배다.";
        return "맞을수록 힘은 커지지만 HP도 줄어든다. 누가 맞을지 나눠서 정하자.";
    }

    private static string TollName(Toll toll) => toll switch
    {
        Toll.Ripple => "파문",
        Toll.Pendulum => "진자",
        Toll.Crack => "균열",
        _ => "장례"
    };

    private void ResetUnitTint()
    {
        for (int i = 0; i < 3; i++)
        {
            var image = _battle.GetUnitImage(i);
            if (image != null) image.color = Color.white;
        }
    }

    public void Dispose()
    {
        ResetUnitTint();
        foreach (var marker in _markers) if (marker != null) UnityEngine.Object.Destroy(marker);
        _markers.Clear();
        foreach (var label in _chargeLabels) if (label != null) UnityEngine.Object.Destroy(label);
        _chargeLabels.Clear();
        if (_hud != null) UnityEngine.Object.Destroy(_hud.gameObject);
        if (_report != null) UnityEngine.Object.Destroy(_report);
    }
}

/// <summary>시험 규칙 전용 화면: 종소리 신호, 패배 분석.</summary>
public sealed class TrialHud : MonoBehaviour
{
    private BoardPanel _board;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _hint;

    public static TrialHud Create(BoardPanel board)
    {
        var root = NewCanvas("BellkeeperTrialHud", 40);
        var hud = root.AddComponent<TrialHud>();
        hud._board = board;

        // 오른쪽 보스 영역, HP 막대 아래에 둔다(가운데 판과 로그를 가리지 않게).
        var panel = NewBox(root.transform, "Signal", new Vector2(1f, 1f), new Vector2(-290f, -280f), new Vector2(540f, 140f), new Color(0f, 0f, 0f, 0.78f));
        hud._title = NewText(panel.transform, "Title", board.UiFont, 30f, Color.white, new Vector2(0f, 38f), new Vector2(520f, 44f));
        hud._hint = NewText(panel.transform, "Hint", board.UiFont, 20f, new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, -20f), new Vector2(510f, 76f));
        hud._hint.textWrappingMode = TextWrappingModes.Normal;
        return hud;
    }

    public void SetSignal(string title, string hint, int phase, int willPower)
    {
        if (_title != null) _title.text = $"{title}  <size=70%>/ {phase}페이즈</size>";
        if (_hint != null) _hint.text = $"{hint}\n<color=#FFB080>이번 턴 끝 의지력 피해 {willPower}</color>";
    }

    private bool _shown = true;

    private void Update()
    {
        if (_board == null) return;
        // 승리/패배 화면을 가리지 않도록 전투가 끝나면 숨긴다.
        bool show = !_board.IsBattleOver;
        if (show != _shown)
        {
            _shown = show;
            foreach (Transform child in transform) child.gameObject.SetActive(show);
        }
    }

    public static GameObject CreateReport(BoardPanel board, string body)
    {
        var root = NewCanvas("BellkeeperTrialReport", 45);
        var dim = NewBox(root.transform, "Dim", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f), new Color(0f, 0f, 0f, 0.55f));
        dim.GetComponent<Image>().raycastTarget = true;
        var box = NewBox(root.transform, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 760f), new Color(0.05f, 0.05f, 0.05f, 1f));
        var text = NewText(box.transform, "Body", board.UiFont, 24f, Color.white, new Vector2(0f, 30f), new Vector2(820f, 640f));
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.text = body;
        var close = NewBox(box.transform, "Close", new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(260f, 60f), new Color(0.85f, 0.85f, 0.85f, 1f));
        NewText(close.transform, "Label", board.UiFont, 24f, Color.black, Vector2.zero, new Vector2(250f, 56f)).text = "확인";
        close.GetComponent<Image>().raycastTarget = true;
        close.AddComponent<Button>().onClick.AddListener(() => Destroy(root));
        return root;
    }

    private static GameObject NewCanvas(string name, int order)
    {
        // 화면 기준으로 배치하려면 다른 Canvas 안에 넣지 않는다(씬을 떠나면 함께 사라진다).
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return go;
    }

    private static GameObject NewBox(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    private static TextMeshProUGUI NewText(Transform parent, string name, TMP_FontAsset font, float size, Color color, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = pos;
        rect.sizeDelta = box;
        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }
}
