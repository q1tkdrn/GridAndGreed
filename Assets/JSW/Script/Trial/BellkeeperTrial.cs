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
    public int MaxPlayerHp => HasItem("13") ? 27 : PlayerMaxHp;
    private const int BellBossHp = 100;
    public const int StartWillPower = 1;
    public const int HitDamage = 4;
    private const int ActionPointsPerTurn = 6;
    private const float PhaseTwoRatio = 0.5f;
    private const int SafeCells = 3;
    private const int FirstTurnExtraSafeCells = 2;
    private const int SafeValue = 3;
    private const int GreedCells = 2;
    private const int GreedStepValue = 3;
    private const int GreedStandValue = 7;
    private const int BigJudgmentDamage = 30;   // 이 이상이면 심판 소리에 큰 타격음을 얹는다
    private const int GreedDepth = 2;
    private static readonly int[] HitBonus = { 2, 2 };
    private const int DownAtHit = 3;
    private const int FuneralQuietCells = 5;

    // v2 패턴 확정안 (2026-10-31): 보스별 HP와 패턴 묶음. 묶음 하나를 무작위로 고르고 안의 패턴을 순서대로 쓴다.
    // 특성·아이템·색 기믹은 끈다. 죽음은 아직 제외.
    private static readonly Dictionary<string, (int hp, string[][] groups)> Plans = new()
    {
        ["instructor"] = (109, new[] { new[] { "Diag", "Center5" }, new[] { "Cross1", "Mansion1" }, new[] { "Mansion2", "Pursuit5" }, new[] { "Border2", "Pursuit5" } }),
        ["noble"] = (130, new[] { new[] { "Mansion1", "Mansion2", "Pursuit3" }, new[] { "SideCols", "MidRows", "PendX" }, new[] { "Center5", "Border2", "PendX" } }),
        ["pope"] = (181, new[] { new[] { "Diag", "Ring3" }, new[] { "Cross1", "Mansion1" }, new[] { "MidY3", "Top3", "Bot3" }, new[] { "Border1", "Ring3full" }, new[] { "Diag" } }),
        ["secretary"] = (141, new[] { new[] { "Xm4", "Ym4", "Pursuit3" }, new[] { "Diag", "Inner7" }, new[] { "Border1", "Ring3full" }, new[] { "Cross1", "Pursuit3" }, new[] { "Diag" } }),
        ["fusion"] = (160, new[] { new[] { "Top3", "Center5" }, new[] { "Xm3", "Ym3", "Pursuit5" }, new[] { "Edge1x", "Border2x", "SideCols", "Not4", "MidRows" }, new[] { "Diag" } }),
        ["door"] = (172, new[] { new[] { "Ring3", "Border2" }, new[] { "Edge1x", "Border2x", "SideCols", "Not4" }, new[] { "Mansion1", "Mansion2", "PendX" }, new[] { "Diag", "Center5" } }),
        ["subject"] = (162, new[] { new[] { "MidRows", "Left3", "MidRows", "Right3" }, new[] { "Edge1x", "Pursuit3" }, new[] { "Xm4", "Ym4", "Center5" }, new[] { "Diag" } }),
        ["death1"] = (130, new[] { new[] { "Mansion1", "Mansion2", "Pursuit3" }, new[] { "Top3", "SideCols", "Inner7" }, new[] { "Diag", "Inner7" }, new[] { "Cross1", "Pursuit3" } }),
        ["death2"] = (55, new[] { new[] { "PendX", "Pursuit3" }, new[] { "Border2", "Center5" }, new[] { "Mansion1", "Mansion2" }, new[] { "Ring3", "Pursuit3" } }),
        ["tutorial"] = (TutorialScript.BossHp, new[] { new[] { "TutRow", "TutCol", "Tut3x3", "TutRow", "TutRow" } }),   // 튜토리얼: 묶음 하나, 순서 고정
        ["king"] = (157, new[] { new[] { "MidRows", "KingA2", "Border2x" }, new[] { "Diag", "Ring3full" }, new[] { "Cross1", "Pursuit3" }, new[] { "Xm4", "Ym4", "Center5", "Pursuit3" }, new[] { "Diag" } }),
    };

    public static bool IsTrialBoss(BossTemp boss) => boss != null && (boss.bossId == "bellkeeper" || Plans.ContainsKey(boss.bossId));

    private static bool Shape(string name, int x, int y) => name switch
    {
        "Diag" => x == y || x + y == 8,
        "Cross1" => x == 4 || y == 4,
        "Border1" => x == 0 || x == 8 || y == 0 || y == 8,
        "Edge1x" => x == 0 || x == 8,
        "Xm3" => x % 3 == 1,
        "Ym3" => y % 3 == 1,
        "Center5" => x >= 2 && x <= 6 && y >= 2 && y <= 6,
        "Border2" => x < 2 || x > 6 || y < 2 || y > 6,
        "Mansion1" => (x / 3 != 1 && y / 3 != 1) || (x / 3 == 1 && y / 3 == 1),
        "Mansion2" => (x / 3 == 1) != (y / 3 == 1),
        "SideCols" => x < 3 || x > 5,
        "MidRows" => x >= 3 && x <= 5,
        "Left3" => x < 3,
        "Right3" => x > 5,
        "Top3" => y < 3,
        "Bot3" => y > 5,
        "MidY3" => y >= 3 && y <= 5,
        "Border2x" => x < 2 || x > 6,
        "Not4" => x != 4,
        "Ring3" => x < 3 || x > 5 || y < 3 || y > 5 || (x == 4 && y == 4),
        "Ring3full" => x < 3 || x > 5 || y < 3 || y > 5,
        "Inner7" => x >= 1 && x <= 7 && y >= 1 && y <= 7,
        "Xm4" => x % 4 != 0,
        "Ym4" => y % 4 != 0,
        "TutRow" => y == 4 && x >= 2 && x <= 6,
        "TutCol" => x == 3 && y >= 2 && y <= 6,
        "Tut3x3" => x >= 3 && x <= 5 && y >= 2 && y <= 4,
        "KingA2" => x == 0 || x == 8 || y == 0 || y == 8 || (x >= 2 && x <= 6 && y >= 2 && y <= 6),
        _ => false
    };

    private static string PatternName(string name) => name switch
    {
        "Diag" => "대각선", "Cross1" => "십자", "Border1" => "테두리 1줄", "Edge1x" => "양끝 1줄",
        "Xm3" => "3칸 간격 세로", "Ym3" => "3칸 간격 가로", "Center5" => "가운데 5x5", "Border2" => "테두리 2줄",
        "Mansion1" => "체크", "Mansion2" => "체크 반대", "SideCols" => "양옆 3줄", "MidRows" => "가운데 3줄",
        "Left3" => "왼쪽 3줄", "Right3" => "오른쪽 3줄", "Top3" => "위 3줄", "Bot3" => "아래 3줄", "MidY3" => "가로 가운데 3줄",
        "Border2x" => "양끝 2줄", "Not4" => "가운데만 안전", "Ring3" => "중앙 고리(8)", "Ring3full" => "중앙 고리(9)",
        "Inner7" => "안쪽 7x7", "Xm4" => "세로 책장", "Ym4" => "가로 책장", "KingA2" => "테두리+중앙",
        "Pursuit5" => "추격 5x5", "Pursuit3" => "추격 3x3", "PendX" => "진자", _ => name
    };
    public static int WillPowerAt(int turn) => turn <= 6 ? 1 + (turn - 1) / 2 : 3 + 2 * (turn - 6);
    // v3: 배율 없음. 욕심 칸에서 얻은 힘은 심판할 때 2배.
    public static int ReleaseDamage(int charge) => charge;

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
    private readonly int[] _greed = new int[3];          // 모은 힘 중 욕심 칸에서 얻은 몫 (심판 시 2배)
    private readonly bool[] _movedThisTurn = new bool[3];
    private readonly int[] _movesThisTurn = new int[3];
    private readonly bool[] _danceGiven = new bool[3];
    private readonly int[] _lastJudgeTurn = { -9, -9, -9 };
    private int _judgesThisTurn, _turnDamage, _willBonus, _bookTurn = -1;
    private bool _opening, _nextOpening, _cottonUsed, _survivalUsed;
    private Vector2Int? _lucky;
    private HashSet<string> _items;
    private string _bossId;
    private readonly int[] _hits = new int[3];
    private readonly bool[] _down = new bool[3];
    private readonly bool[] _standing = new bool[3];
    private readonly Vector2Int[] _standCell = new Vector2Int[3];

    private readonly Dictionary<Vector2Int, bool> _powerCells = new(); // true = 욕심 칸
    private readonly HashSet<Vector2Int> _quiet = new();

    private readonly List<GameObject> _markers = new();
    private readonly List<GameObject> _chargeLabels = new();

    private TrialHud _hud;

    public int MaxBossHp { get; }
    private readonly bool _bell;
    private readonly string[][] _groups;
    private readonly Queue<string> _queue = new();
    private string _pattern = "";
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
    private readonly Dictionary<string, int> _hitsByToll = new();
    private readonly List<string> _downLog = new();

    public BellkeeperTrial(BoardPanel board, BattleManagerTemp battle)
    {
        _board = board;
        _battle = battle;
        string id = board.boss != null ? board.boss.bossId : null;
        _bossId = id;
        _items = new HashSet<string>();
        var display = BattleDisplayManager.GetInstance();
        if (display != null && display.currentItems != null)
            foreach (var item in display.currentItems) if (item != null) _items.Add(item.id);
        _bell = id == "bellkeeper" || id == null || !Plans.ContainsKey(id);
        if (!_bell) { MaxBossHp = Plans[id].hp; _groups = Plans[id].groups; }
        else MaxBossHp = BellBossHp;
    }

    private readonly Vector2Int?[] _lastPos = new Vector2Int?[3];
    private HashSet<Vector2Int> _lastDanger;
    private bool _logEnded;

    private string UnitTag(int i) => $"{i + 1}({_battle.GetUnitName(i)})";
    private string Units()
    {
        var parts = new List<string>();
        for (int i = 0; i < 3; i++)
            parts.Add(_battle.IsUnitOnBoard(i) ? $"{UnitTag(i)}@{P(_battle.GetUnitPos(i))} 힘{_charge[i]} 피격{_hits[i]}" : $"{UnitTag(i)} {(_down[i] ? "쓰러짐" : "판 밖")}");
        return string.Join(" | ", parts);
    }
    private static string P(Vector2Int c) => $"({c.x},{c.y})";
    private void LogEnd(string result)
    {
        if (_logEnded) return;
        _logEnded = true;
        BattleLog.End($"{result}, {_turnsPlayed}턴, 플레이어 HP {_board.reaperCurrentHp}, 보스 HP {_board.bossCurrentHp}/{_board.bossMaxHp}, 심판 {_releaseCount}회(최대 {_bestRelease}), 욕심 밟음 {_greedStepped} 버팀 {_greedStood}");
    }

    private bool HasItem(string id) => _items != null && _items.Contains(id);
    private int UnitId(int i) => _battle.GetUnitId(i);
    /// <summary>욕심 아이템 소울 보너스 (담배 +20%, 마지막 잎새 +30%). 소울 경제 확정 전 임시값.</summary>
    public static float SoulBonus { get; set; }
    private bool IsTutorial => _bossId == TutorialScript.BossId;
    private int WillAt(int turn) => IsTutorial ? TutorialScript.WillPower(turn) : _bossId == "death2" ? 0 : WillPowerAt(Math.Max(1, turn - (HasItem("21") ? 1 : 0))) + _willBonus;   // 죽음 2페이즈: 의지력 없음
    private bool IsDeath => _bossId == "death1" || _bossId == "death2";
    private const int DeathHourglassTurns = 3;   // 죽음 2페이즈: 이 턴 안에 쓰러뜨리지 못하면 패배

    // 지금 심판하면 나갈 피해 (보스 특성 제외 전, 상태 변경 없음)
    private int RawDamage(int i, List<string> used = null)
    {
        int charge = _charge[i]; if (charge <= 0) return 0;
        int g = Math.Min(_greed[i], charge), safe = charge - g;
        int d = UnitId(i) == 7 ? safe / 2 + (g * 5) / 2 : safe + g * 2;     // 마법사: 욕심 힘 x2.5, 안전 힘 절반
        var pos = _battle.GetUnitPos(i);
        bool inDanger = _lastDanger != null && _lastDanger.Contains(pos);
        if (UnitId(i) == 2 && !_movedThisTurn[i]) { d += 8; used?.Add("궁수 특성 +8"); }                    // 궁수: 움직이지 않고 심판 +8
        if (UnitId(i) == 9 && HasItem("22")) { d += 5; used?.Add("사신의 낫 +5"); }                         // 사신의 낫
        if (HasItem("1") && charge >= 10) { d += 3; used?.Add("녹슨 검 +3"); }                            // 녹슨 검
        if (HasItem("2") && !inDanger) { d += 3; used?.Add("화살 +3"); }                               // 화살
        if (HasItem("8") && inDanger) { d += 4; used?.Add("광기 +4"); }                                // 광기
        if (HasItem("9") && _opening) { d += 6; used?.Add("회중시계 +6"); }                                // 회중시계
        if (HasItem("15") && _judgesThisTurn >= 1) { d += 6; used?.Add("너클 +6"); }                   // 너클
        if (HasItem("5") && _lastJudgeTurn[i] == _turnsPlayed - 1) { d += 5; used?.Add("부채 +5"); }   // 부채
        return d;
    }

    // 보스 특성 적용 (심판 피해)
    private int BossAdjust(int d)
    {
        if (d <= 0) return 0;
        switch (_bossId)
        {
            case "instructor": if (_judgesThisTurn >= 1) d /= 2; break;                       // 교정
            case "secretary": if (_turnsPlayed % 2 == 1) d = 1; break;                        // 기록
            case "pope": d = _turnsPlayed % 3 == 0 ? d * 3 / 2 : Math.Max(1, d - 5); break;   // 심판의 날
            case "noble": d = Math.Min(d, Math.Max(0, 40 - _turnDamage)); break;              // 불멸
            case "king": d = Math.Min(d, Math.Max(0, 50 - _turnDamage)); break;               // 왕의 위엄
            case "subject":                                                                    // 두꺼운 살갗
            {
                int before = _turnDamage, after = before + d;
                int Eff(int t) => t <= 30 ? t : 30 + (t - 30) / 2;
                d = Eff(after) - Eff(before); break;
            }
        }
        return Math.Max(0, d);
    }

    private int TotalHitDamage(List<int> hitUnits)
    {
        int total = 0, wine = 0, scale = 0; bool cotton = false;
        foreach (int i in hitUnits)
        {
            int hd = HitDamage;
            if (UnitId(i) == 1 && _hits[i] == 0) hd = 3;                         // 기사: 첫 피격 3
            if (IsDeath && _standing[i] && _battle.GetUnitPos(i) == _standCell[i]) { hd += 2; scale++; }   // 저승의 저울: 욕심 칸에서 맞으면 -2 더
            if (HasItem("11") && _board.reaperCurrentHp <= 15) { hd -= 1; wine++; }           // 포도주
            if (HasItem("20") && !_cottonUsed) { _cottonUsed = true; hd -= 2; cotton = true; }   // 솜뭉치 (전투당 1번)
            total += Math.Max(0, hd);
        }
        if (wine > 0) _board.PrintText($"[포도주] 잃는 HP -{wine}");
        if (cotton) _board.PrintText("[솜뭉치] 잃는 HP -2");
        if (scale > 0) { _board.PrintText($"[저승의 저울] 욕심 칸에서 맞았다: 잃는 HP +{2 * scale}"); GameSfx.PlayTrait(); }
        return total;
    }
    public int HitDamageTotal(List<int> hitUnits) => hitUnits == null ? 0 : TotalHitDamage(hitUnits);

    private void CheckSurvival()
    {
        if (HasItem("6") && !_survivalUsed && _board.reaperCurrentHp > 0 && _board.reaperCurrentHp <= 10)
        {
            _survivalUsed = true;
            _board.HealReaper(3);
            _board.PrintText("[생존 법칙] 플레이어 HP +3");
        }
    }

    private string CurrentName => _bell ? TollName(_toll) : PatternName(_pattern);

    // ───────────────────────── 전투 흐름 ─────────────────────────

    private const string RulesSeenKey = "Trial.RulesSeen";

    public void OnBattleStart()
    {
        ResetUnitTint();
        SoulBonus = (HasItem("16") ? 0.2f : 0f) + (HasItem("13") ? 0.3f : 0f);
        BattleLog.Begin(_board.boss != null ? _board.boss.bossId : "?", _board.boss != null ? _board.boss.bossName : "?", MaxBossHp, PlayerMaxHp);
        var passive = new List<string>();
        if (HasItem("19")) passive.Add("재봉틀: 매 턴 힘 칸 +1");
        if (HasItem("14")) passive.Add("안경: 매 턴 욕심 칸 +1");
        if (HasItem("12")) passive.Add("주사위: 매 턴 행운 칸");
        if (HasItem("21")) passive.Add("낡은 기타: 의지력 1턴 늦게");
        if (HasItem("13")) passive.Add("마지막 잎새: 최대 HP 27, 승리 소울 +30%");
        if (HasItem("16")) passive.Add("담배: 승리 소울 +20%");
        if (passive.Count > 0) _board.PrintText("[아이템] " + string.Join(" · ", passive));
        // 보스 일러스트 위 종소리·페이즈 신호 창은 쓰지 않는다. 같은 안내는 매 턴 위 대사창에 출력된다.
        _hud = null;
        // 규칙 설명은 처음 한 번만 보여준다. ('처음부터'로 저장을 지우면 다시 보인다.)
        if (!IsTutorial && PlayerPrefs.GetInt(RulesSeenKey, 0) == 0)
        {
            PlayerPrefs.SetInt(RulesSeenKey, 1);
            _board.PrintText("[전투 규칙] 힘은 0에서 시작. 힘 칸을 밟아 모으고, 더블클릭 심판으로 터뜨린다. 욕심 칸에서 얻은 힘은 피해가 2배");
            _board.PrintText("[욕심 칸] 공격 범위 안의 금색 칸. 밟으면 힘 +3, 공격을 버티면 +7 더");
            _board.PrintText("[피격] 맞은 캐릭터 1명당 HP -4. 1·2번째 피격은 힘 +2, 3번째 피격에 쓰러진다 (모은 힘 소멸)");
        }
    }

    public void BeginTurn(int turnCount)
    {
        _turnsPlayed = turnCount;
        if (!_bell)
        {
            _powerCells.Clear();
            _quiet.Clear();
            for (int i = 0; i < 3; i++) _standing[i] = false;
            if (_queue.Count == 0)
                foreach (var p in _groups[UnityEngine.Random.Range(0, _groups.Length)]) _queue.Enqueue(p);
            _pattern = _queue.Dequeue();
            _opening = _nextOpening; _nextOpening = false;
            _judgesThisTurn = 0; _turnDamage = 0;
            for (int i = 0; i < 3; i++) { _movedThisTurn[i] = false; _movesThisTurn[i] = 0; _danceGiven[i] = false; }
            if (turnCount % 2 == 0 && HasItem("4") && (!IsTutorial || turnCount == 4)) { _board.HealReaper(1); _board.PrintText("[소금물] 플레이어 HP +1"); }                       // 소금물
            if (turnCount % 2 == 0 && HasItem("16")) { _board.UpdateReaperHp(_board.reaperCurrentHp - 1); _board.PrintText("[담배] 플레이어 HP -1"); } // 담배
            CheckSurvival();
            _board.UpdateBossWillPower(WillAt(turnCount));
            if (_pattern == "PendX") _pendulumLeft = ChoosePendulumSide();
            _board.UpdateActionPoint(IsTutorial ? TutorialScript.ActionPoints(turnCount) : ActionPointsPerTurn);
            RefreshMarkers();
            RefreshChargeLabels();
            AnnounceTurnTrait();
            if (HasItem("9") && _opening) _board.PrintText("[회중시계] 보스 공격 묶음이 끝났다. 이번 턴 심판 피해 +6");
            return;
        }
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

        _board.UpdateBossWillPower(WillAt(turnCount));
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
        if (_bossId == "death2" && _turnsPlayed >= DeathHourglassTurns && !_board.IsBattleOver)   // 최후의 모래시계
        {
            _board.PrintText("[최후의 모래시계] 모래가 다 떨어졌다.");
            GameSfx.PlayTrait();
            _board.LoseBattle();
            return;
        }
        if (!_bell && !_board.IsBattleOver)
        {
            for (int i = 0; i < 3; i++)
                if (_battle.IsUnitOnBoard(i) && UnitId(i) == 4 && _board.reaperCurrentHp <= 10) _board.HealReaper(1);   // 비숍
            if (_bossId == "secretary")                                                                             // 장부
                for (int i = 0; i < 3; i++)
                    if (_battle.IsUnitOnBoard(i) && _charge[i] >= 15) { Gain(i, -4, "장부", ChargePopup.Lose, tag: "보스 특성"); _board.PrintText($"[장부] {_battle.GetUnitName(i)}의 힘 -4"); GameSfx.PlayTrait(); }
            if (_bossId == "door")                                                                                  // 차원 이동
            {
                int top = -1;
                for (int i = 0; i < 3; i++) if (_battle.IsUnitOnBoard(i) && _charge[i] > 0 && (top < 0 || _charge[i] > _charge[top])) top = i;
                if (top >= 0)
                {
                    var p = _battle.GetUnitPos(top); var m = new Vector2Int(8 - p.x, 8 - p.y);
                    if (_battle.MoveUnitTo(top, m)) { _lastPos[top] = m; _board.PrintText($"[차원 이동] {_battle.GetUnitName(top)}가 반대편으로 끌려간다"); GameSfx.PlayTrait(); }
                }
            }
            RefreshChargeLabels();
        }
        BattleLog.Line($"턴 끝 의지력 {_board.willPower} → 플레이어 HP {_board.reaperCurrentHp - Math.Max(0, _board.willPower)} | " + Units());
        _willDamageTotal += Math.Max(0, _board.willPower);
        _cycleIndex++;
    }

    public void OnUnitTurnEnd() { }

    public void OnBossHpChanged()
    {
        if (!_bell) return;
        if (_board.bossMaxHp <= 0 || _board.bossCurrentHp <= 0) return;
        float ratio = (float)_board.bossCurrentHp / _board.bossMaxHp;
        if (ratio <= PhaseTwoRatio && _pendingPhase < 2)
        {
            _pendingPhase = 2;
            if (!_phaseLinePrinted2)
            {
                _phaseLinePrinted2 = true;
                _board.PrintImportantText("\"…종이, 너무 무거워졌어. 이번엔 내 장례까지 울려야겠군.\"");
                _board.PrintText("[2페이즈] 다음 턴부터 진자 → 장례 → 균열 → 파문 순서로 울린다. 장례는 고요한 칸 다섯 곳만 안전하다.");
            }
        }
    }

    public string VictoryLine { get { LogEnd("승리"); return VictoryText; } }
    private string VictoryText => _bell ? "\"…들리나. 이제야 종을 내려놓는구나.\""
        : _board.boss != null && !string.IsNullOrWhiteSpace(_board.boss.win) ? _board.boss.win : "승리";

    // ───────────────────────── 공격 패턴 + 힘 칸 ─────────────────────────

    public HashSet<Vector2Int> BuildDangerCells(int turnCount)
    {
        var cells = new HashSet<Vector2Int>();
        if (!_bell)
        {
            cells = PatternCells(_pattern);
            _lastDanger = cells;
            SpawnPowerCells(cells, turnCount);
            var pos = new Vector2Int?[3];
            for (int i = 0; i < 3; i++) { pos[i] = _battle.IsUnitOnBoard(i) ? _battle.GetUnitPos(i) : (Vector2Int?)null; _lastPos[i] = pos[i]; }
            BattleLog.Raw("");
            BattleLog.Line($"=== {turnCount}턴 패턴 {PatternName(_pattern)} (묶음 남은 {_queue.Count}) | 플레이어 HP {_board.reaperCurrentHp}, 보스 HP {_board.bossCurrentHp}, 의지력 {_board.willPower}, 행동력 {_board.actionPoint}");
            BattleLog.Raw("  " + Units());
            BattleLog.Raw("  욕심 칸 " + string.Join(" ", _powerCells.Where(p => p.Value).Select(p => P(p.Key))) + " / 안전 힘 칸 " + string.Join(" ", _powerCells.Where(p => !p.Value).Select(p => P(p.Key))));
            BattleLog.Raw(BattleLog.Board(cells, _powerCells, pos));
            RefreshMarkers();
            RefreshChargeLabels();
            return cells;
        }
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
        if (IsTutorial)   // 튜토리얼: 대본에 정한 칸만
        {
            _lucky = null;
            foreach (var (cell, greed) in TutorialScript.PowerCells(turnCount)) _powerCells[cell] = greed;
            return;
        }
        var occupied = new HashSet<Vector2Int>(Enumerable.Range(0, 3).Where(_battle.IsUnitOnBoard).Select(_battle.GetUnitPos));
        var depth = EscapeDepth(danger);

        var greedPool = danger.Where(c => !occupied.Contains(c) && depth[c.x, c.y] >= GreedDepth).ToList();
        if (greedPool.Count == 0)
        {
            greedPool = danger.Where(c => !occupied.Contains(c)).ToList();
            // 얇은 패턴: 욕심 칸을 다음 패턴의 안전 지대에서 가장 먼 곳에 둔다 (미끼와 덫).
            var next = _bell ? null : NextDepth();
            if (next != null && greedPool.Count > 0)
            {
                float best = greedPool.Max(c => next[c.x, c.y]);
                if (best > 0)
                {
                    var top = greedPool.Where(c => next[c.x, c.y] >= Math.Max(1f, best - 1f)).ToList();
                    if (top.Count >= GreedCells) greedPool = top;
                }
            }
        }
        foreach (var cell in PickRandom(greedPool, GreedCells + (HasItem("14") ? 1 : 0) + (IsDeath ? 1 : 0))) _powerCells[cell] = true;   // 안경 / 저승의 저울: 욕심 칸 +1

        var safePool = new List<Vector2Int>();
        for (int x = 0; x < 9; x++)
        for (int y = 0; y < 9; y++)
        {
            var cell = new Vector2Int(x, y);
            if (!danger.Contains(cell) && !occupied.Contains(cell)) safePool.Add(cell);
        }
        int safeCount = SafeCells + (turnCount == 1 ? FirstTurnExtraSafeCells : 0) + (HasItem("19") ? 1 : 0);
        foreach (var cell in PickRandom(safePool, safeCount)) _powerCells[cell] = false;
        _lucky = null;
        if (HasItem("12"))   // 주사위: 행운 칸 (힘 +4)
        {
            var luckyPool = safePool.Where(c => !_powerCells.ContainsKey(c)).ToList();
            if (luckyPool.Count > 0) _lucky = luckyPool[UnityEngine.Random.Range(0, luckyPool.Count)];
        }
    }

    private HashSet<Vector2Int> PatternCells(string name)
    {
        var cells = new HashSet<Vector2Int>();
        if (name == "Pursuit5" || name == "Pursuit3")
        {
            // 힘을 가장 많이 모은 캐릭터를 노린다 (같으면 무작위).
            int target = -1;
            for (int i = 0; i < 3; i++)
            {
                if (!_battle.IsUnitOnBoard(i)) continue;
                if (target < 0 || _charge[i] > _charge[target] || (_charge[i] == _charge[target] && UnityEngine.Random.value < 0.5f)) target = i;
            }
            if (target < 0) return cells;
            int r = name == "Pursuit5" ? 2 : 1;
            var c = _battle.GetUnitPos(target);
            for (int x = 0; x < 9; x++)
            for (int y = 0; y < 9; y++)
                if (Math.Max(Math.Abs(x - c.x), Math.Abs(y - c.y)) <= r) cells.Add(new Vector2Int(x, y));
            return cells;
        }
        for (int x = 0; x < 9; x++)
        for (int y = 0; y < 9; y++)
        {
            bool hit = name == "PendX" ? (_pendulumLeft ? x <= 3 : x >= 5) : Shape(name, x, y);
            if (hit) cells.Add(new Vector2Int(x, y));
        }
        return cells;
    }

    // 다음 패턴의 탈출 거리 (묶음 안이면 확정, 묶음이 끝나면 각 묶음 첫 패턴의 평균). 위치에 따라 바뀌는 패턴은 제외.
    private float[,] NextDepth()
    {
        var names = _queue.Count > 0 ? new[] { _queue.Peek() } : _groups.Select(g => g[0]).ToArray();
        var acc = new float[9, 9];
        int used = 0;
        foreach (var n in names)
        {
            if (n.StartsWith("Pursuit") || n == "PendX") continue;
            var set = new HashSet<Vector2Int>();
            for (int x = 0; x < 9; x++)
            for (int y = 0; y < 9; y++)
                if (Shape(n, x, y)) set.Add(new Vector2Int(x, y));
            var d = EscapeDepth(set);
            for (int x = 0; x < 9; x++)
            for (int y = 0; y < 9; y++)
                acc[x, y] += Math.Min(d[x, y], 6);
            used++;
        }
        if (used == 0) return null;
        for (int x = 0; x < 9; x++)
        for (int y = 0; y < 9; y++)
            acc[x, y] /= names.Length;
        return acc;
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
        string from = _lastPos[i].HasValue ? P(_lastPos[i].Value) : "배치";
        _lastPos[i] = pos;
        string got = _powerCells.TryGetValue(pos, out bool g0) ? (g0 ? $" 욕심 칸 +{GreedStepValue}" : $" 힘 칸 +{SafeValue}") : "";
        bool inDanger = _battle != null && _lastDanger != null && _lastDanger.Contains(pos);
        BattleLog.Line($"이동 {UnitTag(i)} {from}→{P(pos)}{got}{(inDanger ? " [공격 칸]" : "")} | 힘 {_charge[i] + (got.Length > 0 ? (g0 ? GreedStepValue : SafeValue) : 0)}, 행동력 남음 {_board.actionPoint - 1}");

        _movedThisTurn[i] = true;
        _movesThisTurn[i]++;
        if (UnitId(i) == 5 && _movesThisTurn[i] >= 3 && !_danceGiven[i]) { _danceGiven[i] = true; Gain(i, 2, "무희", ChargePopup.Trait); }   // 무희
        if (_lucky.HasValue && _lucky.Value == pos) { _lucky = null; Gain(i, 4, "행운 칸", ChargePopup.Greed); _board.PrintText($"[주사위] 행운 칸, {_battle.GetUnitName(i)} 힘 +4"); GameSfx.PlayGreedPickup(); }                            // 주사위 행운 칸
        if (_powerCells.TryGetValue(pos, out bool greed))
        {
            _powerCells.Remove(pos);
            int extra = UnitId(i) == 3 ? 1 : 0;                                                                  // 도적
            if (greed)
            {
                // 욕심 칸 힘은 심판 피해가 2배(마법사 2.5배)라, 떠오르는 숫자도 그만큼 (머리 위 피해 숫자와 같게)
                Gain(i, GreedStepValue, "욕심 칸", ChargePopup.Greed, greed: true);
                if (extra > 0) Gain(i, extra, "도적", ChargePopup.Trait, greed: true);
                if (HasItem("3")) Gain(i, 1, "세 번째 단검", ChargePopup.Item, greed: true);   // 세 번째 단검
                if (HasItem("3")) _board.PrintText("[세 번째 단검] 욕심 칸 힘 +1");
                _standing[i] = true;
                _standCell[i] = pos;
                _greedStepped++;
                GameSfx.PlayGreedPickup();
            }
            else
            {
                int sv = SafeValue + extra;
                bool book = HasItem("7") && _bookTurn != _turnsPlayed;                                            // 마법서: 턴의 첫 힘 칸은 욕심 힘
                Gain(i, SafeValue, "힘 칸", ChargePopup.Cell, greed: book, tag: book ? "마법서" : null);
                if (extra > 0) Gain(i, extra, "도적", ChargePopup.Trait, greed: book);
                GameSfx.PlayPickup();
                if (book)
                {
                    _bookTurn = _turnsPlayed;
                    _board.PrintText($"[마법서] {_battle.GetUnitName(i)}의 힘 +{sv}은 욕심 힘 (피해 2배)");
                }
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
            GameSfx.PlayDenied();
            return false;
        }
        var usedItems = new List<string>();
        int raw = RawDamage(i, usedItems);
        damage = BossAdjust(raw);
        if (usedItems.Count > 0) _board.PrintText("[추가 피해] " + string.Join(" · ", usedItems));
        if (damage <= 0) damage = 0;
        AnnounceJudgeTrait(raw, damage);
        GameSfx.NextJudgmentBig = damage >= BigJudgmentDamage;
        GameSfx.NextJudgmentTrait = damage < raw || _bossId == "fusion";
        _turnDamage += damage;
        _judgesThisTurn++;
        _lastJudgeTurn[i] = _turnsPlayed;
        if (_bossId == "fusion") { _willBonus++; _board.UpdateBossWillPower(_board.willPower + 1); _board.PrintText("[흡수] 의지력 +1"); }
        for (int k = 0; k < 3; k++) if (k != i && _battle.IsUnitOnBoard(k) && UnitId(k) == 9) Gain(k, 3, "꼬마 사신", ChargePopup.Trait);   // 꼬마 사신
        BattleLog.Line($"심판 {UnitTag(i)} @{P(_battle.GetUnitPos(i))} 힘 {charge} → 피해 {damage} | 행동력 남음 {_board.actionPoint - 1}, 보스 HP {_board.bossCurrentHp}→{Math.Max(0, _board.bossCurrentHp - damage)}");
        string mult = charge >= 10 ? " x2" : charge >= 5 ? " x1.5" : "";
        _charge[i] = 0; _greed[i] = 0;
        _releaseCount++;
        _bestRelease = Math.Max(_bestRelease, damage);
        RefreshChargeLabels();
        return true;
    }

    // ───────────────────────── 보스 특성 안내 ─────────────────────────

    /// <summary>전투 시작 때 보스 대사창에 나오는 특성 대사.</summary>
    public string TraitSpeech => _bossId switch
    {
        "instructor" => "「한 번 친 자리는 내가 바로잡는다.」",
        "noble" => "「영원한 몸에 상처는 오래 남지 않지.」",
        "secretary" => "「홀수 장은 기록만 할 뿐, 벌하지 않는다.」\n「넘치는 욕심은 장부에서 지운다.」",
        "pope" => "「심판은 정해진 날에만 내려진다.」",
        "fusion" => "「너희의 심판도 내 일부가 된다.」",
        "door" => "「가장 무거운 자는 저편으로 간다.」",
        "subject" => "「작은 상처는 통하지 않아… 큰 상처도.」",
        "king" => "「왕은 한 번에 무너지지 않는다.」",
        "death1" => "「욕심의 무게를 달아 보자.」",
        "death2" => "「모래가 다 떨어지면, 너도 끝이다.」",
        _ => null
    };

    /// <summary>위 대사창 맨 위에 전투 내내 고정되는 특성 설명.</summary>
    public string TraitText => _bossId switch
    {
        "instructor" => "[교정] 한 턴에 두 번째 심판부터 피해 절반",
        "noble" => "[불멸] 한 턴에 받는 피해는 최대 40",
        "secretary" => "[기록] 홀수 턴의 심판 피해는 1  [장부] 턴 끝에 힘 15 이상인 캐릭터 힘 -4",
        "pope" => "[심판의 날] 3턴마다 심판 피해 1.5배, 다른 턴은 -5",
        "fusion" => "[흡수] 심판할 때마다 의지력 +1",
        "door" => "[차원 이동] 턴 끝에 힘이 가장 높은 캐릭터를 반대편 칸으로 옮긴다",
        "subject" => "[두꺼운 살갗] 한 턴에 30을 넘는 피해는 절반",
        "king" => "[왕의 위엄] 한 턴 피해 최대 50 / 테두리+중앙에 맞으면 1명당 보스 HP +3 / 중앙 고리에 맞으면 1명당 의지력 +1",
        "death1" => "[저승의 저울] 욕심 칸 3개 / 욕심 칸에서 맞으면 잃는 HP +2 (총 -6)",
        "death2" => "[최후의 모래시계] 3턴 안에 쓰러뜨리지 못하면 패배 · 의지력 없음  [저승의 저울] 유지",
        _ => null
    };

    // 턴마다 바뀌는 특성은 그 턴 시작에 다시 알린다.
    private void AnnounceTurnTrait()
    {
        if (_bossId == "death2")
        {
            int left = DeathHourglassTurns - _turnsPlayed + 1;
            _board.PrintText(left <= 1 ? "[최후의 모래시계] 마지막 턴이다. 이번 턴에 쓰러뜨려야 한다" : $"[최후의 모래시계] 남은 턴 {left} (이번 턴 포함)");
            return;
        }
        if (_bossId == "secretary")
            _board.PrintText(_turnsPlayed % 2 == 1 ? "[기록] 이번 턴 심판 피해는 1" : "[기록] 이번 턴은 심판이 통한다");
        else if (_bossId == "pope")
        {
            if (_turnsPlayed % 3 == 0) _board.PrintText("[심판의 날] 이번 턴 심판 피해 1.5배!");
            else if (_turnsPlayed % 3 == 2) _board.PrintText("[심판의 날] 다음 턴이 심판의 날");
        }
    }

    // 심판 피해가 특성 때문에 바뀌면 알린다. (_turnDamage·_judgesThisTurn 갱신 전에 호출)
    private void AnnounceJudgeTrait(int raw, int damage)
    {
        if (raw <= 0) return;
        switch (_bossId)
        {
            case "instructor": if (_judgesThisTurn >= 1) _board.PrintText($"[교정] 두 번째 심판부터 피해 절반 ({raw}→{damage})"); break;
            case "secretary": if (_turnsPlayed % 2 == 1) _board.PrintText($"[기록] 홀수 턴, 심판 피해 1 ({raw}→{damage})"); break;
            case "pope": _board.PrintText(_turnsPlayed % 3 == 0 ? $"[심판의 날] 피해 1.5배 ({raw}→{damage})" : $"[심판의 날] 그날이 아니다, 피해 -5 ({raw}→{damage})"); break;
            case "noble": if (damage < raw) _board.PrintText(damage == 0 ? "[불멸] 이번 턴 피해 한도 40 도달, 더 들어가지 않는다" : $"[불멸] 이번 턴 피해 한도 40 ({raw}→{damage})"); break;
            case "king": if (damage < raw) _board.PrintText(damage == 0 ? "[왕의 위엄] 이번 턴 피해 한도 50 도달, 더 들어가지 않는다" : $"[왕의 위엄] 이번 턴 피해 한도 50 ({raw}→{damage})"); break;
            case "subject": if (damage < raw) _board.PrintText($"[두꺼운 살갗] 한 턴 30을 넘는 피해는 절반 ({raw}→{damage})"); break;
        }
    }

    /// <summary>지금 심판하면 나갈 피해 (모은 힘이 없으면 0). 상태는 바꾸지 않는다.</summary>
    public int PreviewJudgment(int i) => _battle.IsUnitOnBoard(i) && _charge[i] > 0 ? BossAdjust(RawDamage(i)) : 0;

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
        {
            var who = new List<string>();
            if (hitUnits != null)
                foreach (int i in hitUnits)
                {
                    bool st = _standing[i] && _battle.GetUnitPos(i) == _standCell[i];
                    who.Add($"{UnitTag(i)}{(st ? "(욕심 칸 버팀)" : "")} {_hits[i] + 1}번째");
                }
            BattleLog.Line($"보스 공격 {CurrentName}: " + (who.Count > 0 ? "맞음 " + string.Join(", ", who) : "아무도 안 맞음") + $" | 플레이어 HP {_board.reaperCurrentHp}");
        }
        if (hitUnits != null && hitUnits.Count > 0)
        {
            _hitHpTotal += HitDamage * hitUnits.Count;
            foreach (int k in hitUnits) for (int s2 = 0; s2 < 3; s2++) if (s2 != k && _battle.IsUnitOnBoard(s2) && UnitId(s2) == 6) Gain(s2, 2, "스캐빈저", ChargePopup.Trait);   // 스캐빈져
            if (_pattern.EndsWith("KingA2")) { _board.HealBoss(3 * hitUnits.Count); _board.PrintText($"[왕의 위엄] 보스 HP +{3 * hitUnits.Count}"); GameSfx.PlayTrait(); }                                         // 왕 회복
            if (_pattern.EndsWith("Ring3full") && _bossId == "king") { _willBonus += hitUnits.Count; _board.UpdateBossWillPower(_board.willPower + hitUnits.Count); _board.PrintText($"[왕의 위엄] 의지력 +{hitUnits.Count}"); GameSfx.PlayTrait(); }
            _hitsByToll[CurrentName] = (_hitsByToll.TryGetValue(CurrentName, out var n) ? n : 0) + hitUnits.Count;
            GameSfx.PlayHit();
            foreach (int i in hitUnits)
            {
                _hits[i]++;
                if (_hits[i] < DownAtHit)
                {
                    int gain = HitBonus[_hits[i] - 1] + (UnitId(i) == 8 ? 5 : 0);                       // 광신도 +5
                    bool stood = _standing[i] && _battle.GetUnitPos(i) == _standCell[i];
                    if (stood) { Gain(i, GreedStandValue, "버티기", ChargePopup.Greed, greed: true); _greedStood++; GameSfx.PlayGreedStand(); }
                    Gain(i, HitBonus[_hits[i] - 1], "피격", ChargePopup.Hit);
                    if (UnitId(i) == 8) Gain(i, 5, "광신도", ChargePopup.Trait);
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
        if (!_bell && _queue.Count == 0) _nextOpening = true;   // 묶음 끝 → 다음 턴은 "틈" (회중시계)
        CheckSurvival();
        RefreshMarkers();
        RefreshChargeLabels();
    }

    /// <returns>true면 쓰러진다. false면 판에 남는다.</returns>
    public bool RegisterHit(int i) => _hits[i] >= DownAtHit;

    public void OnUnitDown(int i)
    {
        _down[i] = true;
        GameSfx.PlayDown();
        int lost = _charge[i];
        _wastedCharge += lost;
        _charge[i] = 0; _greed[i] = 0;
        BattleLog.Line($"쓰러짐 {UnitTag(i)} 힘 {lost} 소멸");
        _downLog.Add($"{_turnsPlayed}턴 {CurrentName}: {_battle.GetUnitName(i)} 쓰러짐 (힘 {lost} 소멸)");
        _board.PrintText($"[쓰러짐] {_battle.GetUnitName(i)}: 이번 전투에서 빠진다. 모은 힘 {lost} 소멸");
        RefreshChargeLabels();
        if (!_board.IsBattleOver && UnitId(i) == 9)
        {
            _board.PrintText("[패배] 꼬마 사신이 쓰러졌다.");
            _board.LoseBattle();
            return;
        }
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
        return $"<color=#FFD27A>모은 힘 {charge} (욕심 {Math.Min(_greed[i], charge)})</color> → 지금 심판하면 피해 {PreviewJudgment(i)}\n{state}"
            + (_standing[i] ? $"\n욕심 칸에서 버티는 중: 맞으면 힘 +{GreedStandValue} 더" : "");
    }

    // ───────────────────────── 초상화 정보창 ─────────────────────────

    public int HitsOf(int i) => _down[i] ? DownAtHit : _hits[i];

    private string BossTraitShortName => _bossId switch
    {
        "instructor" => "교정", "secretary" => "기록", "pope" => "심판의 날", "noble" => "불멸",
        "king" => "왕의 위엄", "subject" => "두꺼운 살갗", _ => "보스 특성"
    };

    private static string Row(string label, string value) => $"  {label}<pos=80%>{value}";

    /// <summary>초상화 정보창 본문: 특성, 심판 피해와 그 내역, 다음 피격.</summary>
    public string InfoBody(int i, string trait)
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(trait)) sb.Append("<size=80%><color=#A8A8B8>").Append(trait).Append("</color></size>\n");
        if (_down[i]) { sb.Append("<color=#FF6B6B>쓰러짐</color>  이번 전투에서 빠졌다"); return sb.ToString(); }

        int charge = _charge[i];
        int dmg = PreviewJudgment(i);
        sb.Append($"<size=125%><color=#FFD27A>심판 피해 {dmg}</color></size>\n");
        if (charge <= 0)
        {
            sb.Append("<color=#A8A8B8>  모은 힘이 없다. 힘 칸을 밟자.</color>\n");
        }
        else
        {
            int g = Math.Min(_greed[i], charge), safe = charge - g;
            bool wizard = UnitId(i) == 7;
            int baseDmg = wizard ? safe / 2 + (g * 5) / 2 : safe + g * 2;
            string what = g <= 0 ? $"힘 {charge}" : wizard ? $"힘 {charge} (욕심 {g} ×2.5, 일반 {safe} ×0.5)" : $"힘 {charge} (욕심 {g} ×2)";
            sb.Append(Row(what, baseDmg.ToString())).Append('\n');
            var used = new List<string>();
            int raw = RawDamage(i, used);
            foreach (var u in used)
            {
                int cut = u.LastIndexOf(' ');
                sb.Append(Row(cut > 0 ? u.Substring(0, cut) : u, cut > 0 ? u.Substring(cut + 1) : "")).Append('\n');
            }
            if (dmg != raw) sb.Append(Row($"<color=#C9A7FF>{BossTraitShortName}</color>", $"<color=#C9A7FF>{dmg - raw:+#;-#;0}</color>")).Append('\n');
        }
        if (_hits[i] >= DownAtHit - 1) sb.Append("<color=#FF6B6B>다음 피격: 쓰러짐 (모은 힘 소멸)</color>");
        else sb.Append($"다음 피격: 힘 +{HitBonus[_hits[i]] + (UnitId(i) == 8 ? 5 : 0)}");
        if (_standing[i]) sb.Append($"\n<color=#FFB84D>욕심 칸 버티는 중: 맞으면 힘 +{GreedStandValue}</color>");
        return sb.ToString();
    }

    // ───────────────────────── 화면 표시 ─────────────────────────

    /// <summary>힘을 바꾸고 그 캐릭터 머리 위에 이유를 띄운다 (JSW/Script/Trial/ChargePopup.cs). 힘을 바꾸는 곳은 모두 여기로.</summary>
    /// <param name="greed">욕심 힘으로 쌓이는지 (욕심 칸·버티기·마법서). 심판 피해 2배(마법사 2.5배).</param>
    /// <param name="tag">이유 옆 괄호 (예: 보스 특성). 없으면 캐릭터 특성으로 값이 달라질 때만 '캐릭터 특성'을 붙인다.</param>
    private void Gain(int unit, int amount, string reason, Color color, bool greed = false, string tag = null)
    {
        float before = PowerWorth(unit);
        _charge[unit] += amount;
        if (greed) _greed[unit] += amount;
        _greed[unit] = Math.Max(0, Math.Min(_greed[unit], _charge[unit]));
        if (_battle == null) return;
        // 떠오르는 숫자 = 머리 위 '심판 피해' 숫자가 오르는 만큼 (힘 자체가 아니라 힘의 피해 값)
        float worth = PowerWorth(unit) - before;
        float plain = greed ? amount * 2f : amount;   // 보통 캐릭터라면 오를 값
        if (tag == null && UnitId(unit) == 7 && Mathf.Abs(worth - plain) > 0.01f) tag = "캐릭터 특성";   // 마법사
        ChargePopup.Show(_battle.GetUnitImage(unit), worth, reason + (tag != null ? $" ({tag})" : ""), color, _board.UiFont);
    }

    /// <summary>쌓인 힘이 심판 피해로 몇이 되는지 (RawDamage의 힘 부분과 같은 식, 소수점 그대로). 고정 보너스(궁수 +8 등)는 뺀다.</summary>
    private float PowerWorth(int i)
    {
        int charge = Math.Max(0, _charge[i]);
        int g = Math.Min(_greed[i], charge), safe = charge - g;
        return UnitId(i) == 7 ? safe * 0.5f + g * 2.5f : safe + g * 2f;
    }

    private void RefreshMarkers()
    {
        foreach (var marker in _markers) if (marker != null) UnityEngine.Object.Destroy(marker);
        _markers.Clear();
        // 고요한 칸은 회색이 아니라서 보인다. 힘 칸과 겹치면 힘 칸 표시만 남긴다.
        foreach (var cell in _quiet.Where(c => !_powerCells.ContainsKey(c)))
            Add(_markers, CreateCellLabel(cell, "고요", 16f, new Color(0.85f, 0.95f, 1f), new Color(0.1f, 0.2f, 0.3f, 0.92f), new Vector2(0f, -28f), new Vector2(56f, 22f)));
        if (_lucky.HasValue)
            Add(_markers, CreateCellLabel(_lucky.Value, "행운+4", 17f, new Color(0.85f, 0.95f, 1f), new Color(0.1f, 0.25f, 0.4f, 0.92f), new Vector2(0f, -27f), new Vector2(66f, 24f)));
        foreach (var pair in _powerCells)
        {
            // 칸 그림(JSW/Resources/전투_힘칸·전투_욕심칸)이 있으면 칸 전체를 그 그림으로, 없으면 예전 글자 표시
            var tile = CreatePowerTile(pair.Key, pair.Value);
            if (tile != null) { Add(_markers, tile); continue; }
            if (pair.Value)
                Add(_markers, CreateCellLabel(pair.Key, $"+{GreedStepValue}/+{GreedStandValue}", 17f, new Color(1f, 0.86f, 0.35f), new Color(0.36f, 0.24f, 0.02f, 0.95f), new Vector2(0f, -27f), new Vector2(66f, 24f)));
            else
                Add(_markers, CreateCellLabel(pair.Key, $"힘+{SafeValue}", 17f, new Color(0.72f, 1f, 0.76f), new Color(0.06f, 0.28f, 0.12f, 0.92f), new Vector2(0f, -27f), new Vector2(60f, 24f)));
        }
        for (int i = 0; i < 3; i++)
        {
            if (!_standing[i] || !_battle.IsUnitOnBoard(i)) continue;
            // 욕심 칸에서 버티는 중: 글자 상자 대신 발밑 금빛 (2026-11-04). 버틴 뒤 값은 머리 위 숫자에 '8 → 24'로
            var cell = _standCell[i];
            Add(_markers, StandGlow.Create(_battle.GetCellRect(cell), () => _lastDanger != null && _lastDanger.Contains(cell)));
        }
    }

    private void RefreshChargeLabels()
    {
        foreach (var label in _chargeLabels) if (label != null) UnityEngine.Object.Destroy(label);
        _chargeLabels.Clear();
        for (int i = 0; i < 3; i++)
        {
            if (!_battle.IsUnitOnBoard(i)) continue;
            // 지금 심판하면 보스에게 들어갈 피해만 보여준다.
            int d = _charge[i] > 0 ? PreviewJudgment(i) : 0;
            var color = d >= 20 ? new Color(1f, 0.82f, 0.3f) : d >= 10 ? new Color(1f, 0.65f, 0.4f) : Color.white;
            if (_standing[i] && _battle.GetUnitPos(i) == _standCell[i])
            {
                // 욕심 칸에서 버티는 중: '지금 치면 d → 맞고 버티면 s'. 다음 피격에 쓰러지면 '→ 쓰러짐'
                int s = StandPreview(i);
                string next = s < 0 ? "<color=#FF8A80>→ 쓰러짐</color>" : $"<color=#FFD27A>→ {s}</color>";
                Add(_chargeLabels, CreateCellLabel(_battle.GetUnitPos(i), $"{d} {next}", 22f, color, Color.clear, new Vector2(0f, 29f), new Vector2(s < 0 ? 118f : 92f, 24f)));
                continue;
            }
            Add(_chargeLabels, CreateCellLabel(_battle.GetUnitPos(i), d.ToString(), 22f, color, Color.clear, new Vector2(0f, 29f), new Vector2(44f, 24f)));
        }
    }

    /// <summary>욕심 칸에서 다음 공격을 맞고 버티면 심판 피해가 얼마가 되는지. 다음 피격에 쓰러지면 -1.</summary>
    private int StandPreview(int i)
    {
        if (_hits[i] + 1 >= DownAtHit) return -1;
        int charge = _charge[i], greed = _greed[i];
        _charge[i] += GreedStandValue + HitBonus[_hits[i]] + (UnitId(i) == 8 ? 5 : 0);   // 버티기 + 피격 (+ 광신도)
        _greed[i] += GreedStandValue;
        int value = PreviewJudgment(i);
        _charge[i] = charge; _greed[i] = greed;
        return value;
    }

    private static void Add(List<GameObject> list, GameObject go)
    {
        if (go != null) list.Add(go);
    }

    // ── 힘 칸 · 욕심 칸 그림 ──
    // 숫자 없는 그림(_빈)이 있으면 숫자는 코드로 쓴다 → 수치·아이템이 바뀌어도 그림을 다시 그릴 필요 없음.
    // _빈 그림은 원래 그림에서 숫자만 지운 것 (2026-11-03). 사용자가 직접 그린 숫자 없는 그림으로 같은 이름에 덮어써도 됨.
    // 숫자 위치·높이는 원래 그림 속 숫자 자리 (그림 픽셀, 왼쪽 아래 기준 가운데 x, y, 글자 높이)
    private static readonly Color PowerInk = new Color(0.60f, 0.72f, 0.54f);
    private static readonly Color GreedInk = new Color(0.74f, 0.73f, 0.56f);
    private static readonly Color BoostInk = new Color(1f, 0.97f, 0.86f);   // 아이템으로 늘어난 값은 더 밝게
    private static Sprite _powerBlank, _greedBlank;
    private static Sprite PowerBlankSprite => _powerBlank != null ? _powerBlank : _powerBlank = FirstSprite("전투_힘칸_빈");
    private static Sprite GreedBlankSprite => _greedBlank != null ? _greedBlank : _greedBlank = FirstSprite("전투_욕심칸_빈");

    /// <summary>이 판에서 누구나 받는 값 (도적처럼 캐릭터마다 다른 보너스는 빼고, 모두에게 붙는 아이템 보너스는 넣음)</summary>
    private int ShownGreedStep => GreedStepValue + (HasItem("3") ? 1 : 0);   // 세 번째 단검

    private GameObject CreatePowerTile(Vector2Int cell, bool greed)
    {
        var blank = greed ? GreedBlankSprite : PowerBlankSprite;
        if (blank == null) return CreateCellTile(cell, greed ? GreedTileSprite : PowerTileSprite);   // 숫자가 그려진 옛 그림
        var go = CreateCellTile(cell, blank);
        if (go == null) return null;
        if (greed)
        {
            AddTileDigit(go, blank, ShownGreedStep, GreedStepValue, 42.5f, 60.5f, 20f, GreedInk);
            AddTileDigit(go, blank, GreedStandValue, GreedStandValue, 64f, 17.5f, 24f, GreedInk);
        }
        else
        {
            AddTileDigit(go, blank, SafeValue, SafeValue, 49f, 56f, 25f, PowerInk);
        }
        return go;
    }

    private void AddTileDigit(GameObject tile, Sprite art, int value, int baseValue, float cx, float cy, float height, Color ink)
    {
        var tileRect = (RectTransform)tile.transform;
        float sx = tileRect.sizeDelta.x / art.rect.width, sy = tileRect.sizeDelta.y / art.rect.height;
        var go = new GameObject("Digit", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = tile.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(tileRect, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2((cx + 0.5f - art.rect.width * 0.5f) * sx, (cy + 0.5f - art.rect.height * 0.5f) * sy);
        rect.sizeDelta = new Vector2(height * 1.5f * sx, height * 1.5f * sy);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (_board.UiFont != null) text.font = _board.UiFont;
        text.text = value.ToString();
        float size = height / 0.8f * sy;    // 숫자 높이 ≈ 글자 크기의 80% (그림 속 손글씨 숫자 높이에 맞춤)
        text.enableAutoSizing = true;       // 두 자리가 되면 칸 안에서 줄어듦
        text.fontSizeMax = size;
        text.fontSizeMin = size * 0.5f;
        text.fontSize = size;
        text.color = value > baseValue ? BoostInk : ink;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
    }

    private static Sprite _powerTile, _greedTile;
    private static Sprite PowerTileSprite => _powerTile != null ? _powerTile : _powerTile = FirstSprite("전투_힘칸");
    private static Sprite GreedTileSprite => _greedTile != null ? _greedTile : _greedTile = FirstSprite("전투_욕심칸");

    private static Sprite FirstSprite(string path)
    {
        var all = Resources.LoadAll<Sprite>(path);
        return all != null && all.Length > 0 ? all[0] : null;
    }

    /// <summary>
    /// 칸 그림을 칸 바로 아래 층에 깐다. 칸 자체는 평소 투명이라 그림이 보이고,
    /// 이동 가능 표시(칸 흰색)·공격 예고·실제 공격(칸의 자식)·캐릭터는 모두 그 위에 그대로 보인다.
    /// </summary>
    private GameObject CreateCellTile(Vector2Int cell, Sprite sprite)
    {
        if (sprite == null) return null;
        var cellRect = _battle.GetCellRect(cell);
        if (cellRect == null || cellRect.parent == null) return null;
        var go = new GameObject("TrialTile", typeof(RectTransform), typeof(Image));
        go.layer = cellRect.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(cellRect.parent, false);
        rect.anchorMin = cellRect.anchorMin;
        rect.anchorMax = cellRect.anchorMax;
        rect.pivot = cellRect.pivot;
        rect.sizeDelta = cellRect.sizeDelta;
        rect.anchoredPosition = cellRect.anchoredPosition;
        rect.SetSiblingIndex(cellRect.GetSiblingIndex());   // 칸 바로 앞 = 칸 아래
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return go;
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
        if (background.a <= 0f)
        {
            // 바탕 상자 없이 (머리 위 힘 숫자, 2026-11-04): 굵게 + 검은 테두리로 어느 칸 위에서도 읽히게
            image.enabled = false;
            text.fontStyle = FontStyles.Bold;
            text.outlineWidth = 0.25f;
            text.outlineColor = new Color32(0, 0, 0, 235);
        }
        return go;
    }

    // ───────────────────────── 패배 분석 ─────────────────────────

    public void ShowDefeatReport()
    {
        LogEnd("패배");
        if (_report != null) return;
        var lines = new List<string>
        {
            $"<color=#FFD27A>패배 분석, {(_bell ? "장송의 종지기" : _board.boss?.bossName)}</color>",
            $"버틴 턴 {_turnsPlayed}, 도달한 페이즈 {_maxPhase}, 보스 남은 HP {_board.bossCurrentHp}/{_board.bossMaxHp}",
            "",
            "<color=#FFD27A>무엇에 졌나</color>",
            $"피격으로 잃은 HP {_hitHpTotal}, 의지력으로 잃은 HP {_willDamageTotal}",
        };
        if (_hitsByToll.Count > 0)
            lines.Add("기술별 피격: " + string.Join(", ", _hitsByToll.Select(p => $"{p.Key} {p.Value}명")));
        lines.AddRange(_downLog);
        lines.Add("");
        lines.Add("<color=#FFD27A>욕심</color>");
        lines.Add($"심판 {_releaseCount}회, 가장 큰 한 방 {_bestRelease}");
        lines.Add($"욕심 칸 밟음 {_greedStepped}회, 끝까지 버팀 {_greedStood}회");
        if (_wastedCharge > 0) lines.Add($"쓰러지며 잃은 힘 {_wastedCharge}");
        lines.Add("");
        lines.Add("<color=#FFD27A>힌트</color>");
        lines.Add(Hint());
        _report = TrialHud.CreateReport(_board, string.Join("\n", lines), _bell ? null : StageText.Art(_board.boss));
    }

    private string Hint()
    {
        if (_wastedCharge >= 8) return "쓰러지면 모은 힘이 사라진다. 두 번 맞은 캐릭터는 먼저 터뜨리자.";
        if (_turnsPlayed >= 7) return "의지력은 7턴부터 크게 오른다. 힘을 모아 그 전에 몰아치자.";
        var worst = _hitsByToll.OrderByDescending(p => p.Value).FirstOrDefault();
        if (worst.Value >= 3 && _bell && worst.Key == TollName(Toll.Ripple)) return "파문은 아군이 모인 곳을 중심으로 퍼진다. 흩어지면 고리를 읽기 쉽다.";
        if (worst.Value >= 3 && _bell && worst.Key == TollName(Toll.Pendulum)) return "종은 아군이 많은 쪽으로 기운다. 기우는 쪽을 미리 정할 수 있다.";
        if (worst.Value >= 3 && _bell && worst.Key == TollName(Toll.Crack)) return "균열은 가운데 줄과 가운데 칸만 친다. 판 구석은 안전하다.";
        if (worst.Value >= 3 && _bell && worst.Key == TollName(Toll.Funeral)) return "장례는 고요한 칸 다섯 곳만 안전하다. 전 턴에 가운데 쪽에 서 있으면 닿기 쉽다.";
        if (_greedStood == 0) return "욕심 칸에서 공격을 끝까지 버티면 힘 +7을 더 얻는다.";
        if (_bestRelease < 20) return "욕심 칸(금색)에서 얻은 힘은 피해가 2배다.";
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
        LogEnd("중단");
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

    public static GameObject CreateReport(BoardPanel board, string body, Sprite place = null)
    {
        var root = NewCanvas("BellkeeperTrialReport", 45);
        var dim = NewBox(root.transform, "Dim", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f), new Color(0f, 0f, 0f, 0.55f));
        dim.GetComponent<Image>().raycastTarget = true;
        // 장소 그림이 있으면 상자를 위로 200 늘리고 맨 위에 머리 그림으로 (어디서 졌는지 한눈에, 2026-11-04)
        float head = place != null ? 200f : 0f;
        var box = NewBox(root.transform, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 760f + head), new Color(0.05f, 0.05f, 0.05f, 1f));
        if (place != null)
        {
            var art = NewBox(box.transform, "Place", new Vector2(0.5f, 1f), new Vector2(0f, -30f - 85f), new Vector2(place.rect.width, place.rect.height) * (170f / place.rect.height), Color.white);
            var artImage = art.GetComponent<Image>();
            artImage.sprite = place;
            artImage.preserveAspect = true;
            artImage.color = new Color(1f, 1f, 1f, 0.85f);
        }
        var text = NewText(box.transform, "Body", board.UiFont, 24f, Color.white, new Vector2(0f, 30f - head * 0.5f), new Vector2(820f, 640f));
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
