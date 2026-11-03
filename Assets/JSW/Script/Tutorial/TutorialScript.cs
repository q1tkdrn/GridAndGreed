using System.Collections.Generic;
using UnityEngine;

public enum TutorialAct { Next, Select, Place, Drag, Move, Judge }
public enum TutorialAnchor { Center, Waiting, Cell, Unit, Portrait, ActionPoints, WillPower, PlayerHp, Item }

/// <summary>튜토리얼 한 단계. 말풍선이 anchor를 가리키고, act를 해야 다음 단계로 넘어간다.</summary>
public sealed class TutorialStep
{
    public int turn;            // 몇 턴에 나오는가
    public bool place;          // true면 배치 단계, false면 행동 단계
    public TutorialAct act;
    public int unit = -1;       // 0 기사, 1 궁수, 2 도적 (편성 순서)
    public Vector2Int cell;     // 배치·이동할 칸
    public Vector2Int? cellTo;  // 설명할 칸이 여러 개면 cell~cellTo 사각형을 밝힌다
    public TutorialAnchor anchor;
    public string text;
}

/// <summary>
/// 튜토리얼 대본 (2026-11-03, 사용자 대사 흐름 반영). 무작위 없음. 좌표는 (가로, 세로), (0,0)이 판 왼쪽 위.
/// 편성: 기사, 궁수, 도적 + 소금물. 안내자 HP 43 = 기사 3 + 궁수 11 + 기사 5 + 도적 24.
/// 공격 칸(BellkeeperTrial.Shape): 1·4·5턴 가로줄(세로 4, 가로 2~6) / 2턴 세로줄(가로 3, 세로 2~6) / 3턴 가운데 3×3(가로 3~5, 세로 2~4).
/// 피격: 2턴 기사(HP -3, 힘 +2), 3턴 도적(욕심 칸 버팀, HP -4, 힘 +9). 의지력 1턴 0, 이후 1. 소금물은 4턴에만 HP +1.
/// 턴이 끝난 뒤 이어지는 대사(같은 턴의 Next)가 남아 있으면 보스 공격을 미룬다 (TutorialDirector.HoldsTurnEnd).
/// </summary>
public static class TutorialScript
{
    public const string BossId = "tutorial";
    public const string BossName = "안내자";
    public const int BossHp = 43;
    public const string ItemId = "4";   // 소금물
    public static readonly int[] UnitIds = { 1, 2, 3 };   // 기사, 궁수, 도적
    public const string DoneKey = "Tutorial.Done";

    public static int ActionPoints(int turn) => turn switch { 1 => 4, 3 => 2, _ => 1 };
    public static int WillPower(int turn) => turn <= 1 ? 0 : 1;

    /// <summary>턴별 힘 칸. true = 욕심 칸.</summary>
    public static IEnumerable<(Vector2Int cell, bool greed)> PowerCells(int turn)
    {
        switch (turn)
        {
            case 1: yield return (new Vector2Int(3, 5), false); yield return (new Vector2Int(4, 5), false); break;
            case 3: yield return (new Vector2Int(4, 3), true); yield return (new Vector2Int(3, 6), false); break;
        }
    }

    /// <summary>안내자를 쓰러뜨린 뒤 차례로 보여 주는 대사. 마지막 대사를 클릭하면 마을로.</summary>
    public static readonly string[] EndLines =
    {
        "커허억!!",
        "……허어? 요것 봐라?",
        "제법 야무지게 때리는구먼……!",
        "크흐흐…… 아따, 제대로 한 방 먹었네.",
        "거참, 꼬맹이한테 얻어맞고 뻗을 줄이야!",
        "허허허! 이거 참, 기분 한번 더럽게 좋구먼!",
        "좋아, 좋아! 이만하면 됐어!",
        "이제 가서 다른 놈들도 실컷 두들겨 패 보라고!",
        "크하하! 다음에 만나면 나도 제대로 상대해 줄 테니께!",
    };

    // 해야 할 행동 글자 강조
    private const string H = "<color=#FFD27A>", E = "</color>";

    private static TutorialStep P(TutorialAct act, int unit, int x, int y, TutorialAnchor anchor, string text)
        => new TutorialStep { turn = 1, place = true, act = act, unit = unit, cell = new Vector2Int(x, y), anchor = anchor, text = text };
    private static TutorialStep S(int turn, TutorialAct act, int unit, int x, int y, TutorialAnchor anchor, string text)
        => new TutorialStep { turn = turn, act = act, unit = unit, cell = new Vector2Int(x, y), anchor = anchor, text = text };
    private static TutorialStep Area(int turn, int x, int y, int x2, int y2, string text)
    {
        var s = S(turn, TutorialAct.Next, -1, x, y, TutorialAnchor.Cell, text);
        s.cellTo = new Vector2Int(x2, y2);
        return s;
    }
    private static TutorialStep PSay(string text)
        => P(TutorialAct.Next, -1, 0, 0, TutorialAnchor.Center, text);
    private static TutorialStep Say(int turn, TutorialAnchor anchor, string text, int unit = -1, int x = 0, int y = 0)
        => S(turn, TutorialAct.Next, unit, x, y, anchor, text);

    public static readonly TutorialStep[] Steps =
    {
        // ── 1턴: 전투 시작 · 배치 ──
        PSay("어이쿠, 요 쬐끄만 게 신참 사신이라고?"),
        PSay("크흐흐! 좋다, 좋아! 어디 한번 놀아보자고!"),
        PSay("어허! 뭘 그리 멀뚱히 서 있어?"),
        PSay("일단 대원들부터 배치해야 할 거 아녀!"),
        P(TutorialAct.Select, 0, 0, 0, TutorialAnchor.Waiting, $"대원을 {H}클릭{E}하고, 원하는 자리를 다시 {H}클릭{E}해 봐!\n먼저 기사부터!"),
        P(TutorialAct.Place, 0, 3, 4, TutorialAnchor.Cell, $"요 자리를 {H}클릭{E}혀!"),
        P(TutorialAct.Select, 1, 0, 0, TutorialAnchor.Waiting, $"다음은 궁수여! {H}클릭{E}!"),
        P(TutorialAct.Place, 1, 4, 4, TutorialAnchor.Cell, "기사 옆자리에 세워!"),
        P(TutorialAct.Select, 2, 0, 0, TutorialAnchor.Waiting, $"마지막은 도적이고! {H}클릭{E}!"),
        P(TutorialAct.Place, 2, 5, 4, TutorialAnchor.Cell, "궁수 옆자리여!"),

        // ── 1턴: 이동 · 행동력 · 공격 칸 · 심판 ──
        Say(1, TutorialAnchor.Center, "자리 잡았으면 발도 좀 움직여야지!"),
        Say(1, TutorialAnchor.Center, "싸움판에서 가만히 서 있다간 얻어맞기 딱 좋은 법이여!"),
        Say(1, TutorialAnchor.Unit, $"배치한 대원 보이지? 그 녀석한테 {H}마우스를 올리면{E}\n움직일 수 있는 칸이 허옇게 표시될 거여!", 0),
        Say(1, TutorialAnchor.ActionPoints, "근디 말이여, 움직이는 것도 다 대가가 있는 법이거든!"),
        Say(1, TutorialAnchor.ActionPoints, "행동력은 잘 생각해서 써야 혀. 멋모르고 써버렸다간\n정작 필요할 때 손가락만 빨게 될 테니께!"),
        Area(1, 2, 4, 6, 4, "거기 공격 칸 보이지?"),
        Area(1, 2, 4, 6, 4, "거긴 내가 후려칠 자리니께, 맞기 싫으면 냉큼 피해 보라고!"),
        S(1, TutorialAct.Move, 0, 3, 5, TutorialAnchor.Unit, $"기사를 꾹 눌러서 아래 칸으로 {H}끌어다 놔{E}!"),
        S(1, TutorialAct.Move, 1, 4, 5, TutorialAnchor.Unit, $"궁수도 아래로 {H}끌어다 놔{E}!"),
        S(1, TutorialAct.Move, 2, 5, 3, TutorialAnchor.Unit, $"도적은 위로 {H}끌어다 놔{E}!"),
        Say(1, TutorialAnchor.Unit, "자, 준비 다 했으면 심판을 내려야지!", 0),
        S(1, TutorialAct.Judge, 0, 0, 0, TutorialAnchor.Unit, $"어디, 얼마나 매운맛인지 한번 보자고!\n기사를 {H}더블클릭{E}하면 심판이여!"),
        Say(1, TutorialAnchor.Center, "어이쿠야! 요거 제법 얼얼한디?"),
        Say(1, TutorialAnchor.Center, "크흐흐! 좋아, 좋아! 그럼 이제 내 차례도 와야 쓰겄지?"),

        // ── 2턴: 의지력 · 보스 특성 · 피격 ──
        Say(2, TutorialAnchor.WillPower, "어이, 꼬맹이! 내 의지력 보이냐?"),
        Say(2, TutorialAnchor.WillPower, "첫 턴엔 0이었는디, 지금은 1이지?\n네 차례가 끝날 때마다 그만큼 피해를 줄 거여!"),
        Say(2, TutorialAnchor.Center, "아차차! 중요한 걸 하나 빼먹을 뻔했구먼."),
        Say(2, TutorialAnchor.Center, "보스마다 가진 특성이 다르거든? 싸우기 전에 잘 살펴보는 게 좋을 거여!"),
        Say(2, TutorialAnchor.Center, "뭐, 나야 아무것도 모르고 덤비는 놈이 더 반갑지만서도! 크흐흐!"),
        S(2, TutorialAct.Judge, 1, 0, 0, TutorialAnchor.Unit, $"궁수는 가만히 서서 쏘면 더 아프거든!\n궁수를 {H}더블클릭{E}혀!"),
        Say(2, TutorialAnchor.Center, "자아, 이번엔 내 차례구먼!"),
        Say(2, TutorialAnchor.Center, "이빨 꽉 깨물어라!"),

        // ── 3턴: 피격 정리 · 퇴각 · 힘 칸 · 욕심 칸 ──
        Say(3, TutorialAnchor.Unit, "어이쿠! 아프지? 크하하!", 0),
        Say(3, TutorialAnchor.PlayerHp, "대원이 얻어맞으면 너도 같이 피해를 입는 거여!"),
        Say(3, TutorialAnchor.PlayerHp, "남 얻어맞는다고 구경만 하다간 큰코다친다니까!"),
        Say(3, TutorialAnchor.Unit, "아, 참! 같은 녀석이 세 번이나 맞으면 그대로 퇴각이여!", 0),
        Say(3, TutorialAnchor.Unit, "잘 챙겨라. 하나둘씩 나가떨어지면 곤란할 테니께!", 0),
        Say(3, TutorialAnchor.Center, "어이쿠, 아직도 팔팔하구먼!"),
        Say(3, TutorialAnchor.Center, "좋아! 그럼 이번엔 좀 더 재밌는 걸 해보자고!"),
        Say(3, TutorialAnchor.Cell, "저기 힘 칸 보이지?", -1, 3, 6),
        Say(3, TutorialAnchor.Cell, "힘이 모자라면 거기서 끌어다 쓰면 되는 거여!", -1, 3, 6),
        Say(3, TutorialAnchor.Cell, "원래 세상일이란 게 힘 좀 있으면 편해지는 법이거든! 크흐흐!", -1, 3, 6),
        S(3, TutorialAct.Move, 0, 3, 6, TutorialAnchor.Unit, $"기사를 힘 칸으로 {H}끌어다 놔{E}!"),
        Say(3, TutorialAnchor.Cell, "오호, 이번엔 욕심 칸이구먼!", -1, 4, 3),
        Say(3, TutorialAnchor.Cell, "눈앞에 좋은 게 있으면 욕심도 좀 부려야지!", -1, 4, 3),
        S(3, TutorialAct.Move, 2, 4, 3, TutorialAnchor.Unit, $"도적을 욕심 칸으로 {H}끌어다 놔{E}!"),
        Say(3, TutorialAnchor.Unit, "더 큰 힘을 얻을 수 있을 거여!\n물론, 그때까지 버틸 수 있다면 말이제. 크흐흐!", 2),

        // ── 4턴: 피격 시 추가 힘 · 소금물 · 아이템 패시브 ──
        Say(4, TutorialAnchor.Unit, "잉? 얻어맞았더니 힘이 더 붙었다고?", 2),
        Say(4, TutorialAnchor.Unit, "크하하! 당연한 거 아녀! 아주 악이 받쳐버렸구먼!", 2),
        Say(4, TutorialAnchor.Portrait, "좋아! 어디 얼마나 더 세졌는지 한번 보자고!", 2),
        Say(4, TutorialAnchor.Center, "아따, 요 꼬맹이가 생각보다 질기구먼."),
        Say(4, TutorialAnchor.Center, "근디 아직 보여줄 게 하나 남았지?"),
        Say(4, TutorialAnchor.Item, "잉? 그건 또 뭐여?"),
        Say(4, TutorialAnchor.Item, "소금물? 허어, 싸움판에 별 희한한 걸 다 들고 왔구먼!"),
        Say(4, TutorialAnchor.PlayerHp, "크흐흐, 근디 그거 제법 쓸 만한 물건인갑네?"),
        Say(4, TutorialAnchor.Item, "아이템이란 건 갖고만 있어도 알아서 효과가 발동하거든!"),
        Say(4, TutorialAnchor.Item, "그러니 쓸 만한 게 보이면 냉큼 챙겨두라고!"),
        S(4, TutorialAct.Judge, 0, 0, 0, TutorialAnchor.Unit, $"기사로 한 방 더 {H}더블클릭{E}!"),
        Say(4, TutorialAnchor.Center, "자아, 이제 슬슬 끝장을 볼 때가 됐구먼!"),
        Say(4, TutorialAnchor.Center, "어디, 지금까지 배운 걸 제대로 써먹을 수 있을랑가?"),

        // ── 5턴: 최종 공격 ──
        Say(5, TutorialAnchor.Center, "크흐흐! 이제 얼추 감이 잡힌 모양이구먼!"),
        Say(5, TutorialAnchor.Center, "좋아, 꼬맹이! 이번엔 가진 걸 몽땅 쏟아부어 봐!"),
        S(5, TutorialAct.Judge, 2, 0, 0, TutorialAnchor.Unit, $"어설프게 덤볐다간 한 대 더 얻어맞을 테니께!\n도적을 {H}더블클릭{E}혀!"),
    };

    /// <summary>안내자 보스 데이터. 에셋 없이 코드로 만든다. 배경음은 다른 보스 것을 빌린다.</summary>
    public static BossTemp CreateBoss(BossTemp musicFrom)
    {
        var boss = ScriptableObject.CreateInstance<BossTemp>();
        boss.name = "Tutorial";
        boss.bossId = BossId;
        boss.bossName = BossName;
        boss.bossSprite = Resources.Load<Sprite>("튜토리얼_보스");
        if (boss.bossSprite == null)   // 가져오기 설정이 Multiple이면 첫 조각을 쓴다
        {
            var parts = Resources.LoadAll<Sprite>("튜토리얼_보스");
            if (parts.Length > 0) boss.bossSprite = parts[0];
        }
        boss.battleStart = "";
        boss.win = "";
        if (musicFrom != null) { boss.bgmStart = musicFrom.bgmStart; boss.bgmLoop = musicFrom.bgmLoop; }
        return boss;
    }
}

/// <summary>튜토리얼 전투로 들어가는 길. 오프닝(새 게임)과 마을 NPC에서 부른다.</summary>
public static class TutorialLink
{
    private static bool _pending;

    public static void Begin()
    {
        _pending = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene("BattleTemp");
    }

    public static bool Consume()
    {
        bool p = _pending;
        _pending = false;
        return p;
    }
}
