using System.Collections.Generic;
using UnityEngine;

public enum TutorialAct { Next, Select, Place, Drag, Move, Judge }
public enum TutorialAnchor { Center, Waiting, Cell, Unit, Portrait, ActionPoints, WillPower, PlayerHp }

/// <summary>튜토리얼 한 단계. 말풍선이 anchor를 가리키고, act를 해야 다음 단계로 넘어간다.</summary>
public sealed class TutorialStep
{
    public int turn;            // 몇 턴에 나오는가
    public bool place;          // true면 배치 단계, false면 행동 단계
    public TutorialAct act;
    public int unit = -1;       // 0 기사, 1 궁수, 2 도적 (편성 순서)
    public Vector2Int cell;     // 배치·이동할 칸
    public TutorialAnchor anchor;
    public string text;
}

/// <summary>
/// 튜토리얼 대본 (2026-11-02 확정). 무작위 없음. 좌표는 (가로, 세로), (0,0)이 판 왼쪽 위.
/// 편성: 기사, 궁수, 도적. 안내자 HP 47 = 기사 3 + 궁수 11 + 기사 5 + 도적 28.
/// 공격 칸(BellkeeperTrial.Shape): 1턴 가로줄(세로 4, 가로 2~6) / 2턴 세로줄(가로 3, 세로 2~6) / 3턴 가운데 3×3(가로 3~5, 세로 2~4).
/// </summary>
public static class TutorialScript
{
    public const string BossId = "tutorial";
    public const string BossName = "안내자";
    public const int BossHp = 47;
    public static readonly int[] UnitIds = { 1, 2, 3 };   // 기사, 궁수, 도적
    public const string DoneKey = "Tutorial.Done";

    public static int ActionPoints(int turn) => turn switch { 1 => 4, 2 => 2, 3 => 3, _ => 1 };
    public static int WillPower(int turn) => turn <= 1 ? 0 : 2;

    /// <summary>턴별 힘 칸. true = 욕심 칸.</summary>
    public static IEnumerable<(Vector2Int cell, bool greed)> PowerCells(int turn)
    {
        switch (turn)
        {
            case 1: yield return (new Vector2Int(3, 5), false); yield return (new Vector2Int(4, 5), false); break;
            case 2: yield return (new Vector2Int(5, 4), false); break;
            case 3: yield return (new Vector2Int(4, 3), true); yield return (new Vector2Int(3, 6), false); break;
        }
    }

    public const string EndLine = "잘했어요. 피하고, 모으고, 언제 욕심낼지 고르는 것. 그게 전부예요.\n이제 마을로 가 볼까요?";

    private static TutorialStep P(TutorialAct act, int unit, int x, int y, TutorialAnchor anchor, string text)
        => new TutorialStep { turn = 1, place = true, act = act, unit = unit, cell = new Vector2Int(x, y), anchor = anchor, text = text };
    private static TutorialStep S(int turn, TutorialAct act, int unit, int x, int y, TutorialAnchor anchor, string text)
        => new TutorialStep { turn = turn, act = act, unit = unit, cell = new Vector2Int(x, y), anchor = anchor, text = text };
    private static TutorialStep Say(int turn, TutorialAnchor anchor, string text, int unit = -1, int x = 0, int y = 0)
        => S(turn, TutorialAct.Next, unit, x, y, anchor, text);

    public static readonly TutorialStep[] Steps =
    {
        // ── 1턴: 배치 ──
        P(TutorialAct.Select, 0, 0, 0, TutorialAnchor.Waiting, "어서 와요. 먼저 동료를 판 위에 세워 볼게요.\n기사를 클릭하세요."),
        P(TutorialAct.Place, 0, 3, 4, TutorialAnchor.Cell, "이 칸을 클릭하면 기사가 섭니다."),
        P(TutorialAct.Select, 1, 0, 0, TutorialAnchor.Waiting, "궁수도 데려올게요."),
        P(TutorialAct.Place, 1, 4, 4, TutorialAnchor.Cell, "기사 옆, 이 칸이에요."),
        P(TutorialAct.Select, 2, 0, 0, TutorialAnchor.Waiting, "마지막으로 도적."),
        P(TutorialAct.Place, 2, 5, 4, TutorialAnchor.Cell, "이 칸에 세워 주세요."),

        // ── 1턴: 이동, 행동력, 공격 칸, 힘 칸, 심판 ──
        Say(1, TutorialAnchor.Cell, "붉게 물든 칸이 보스가 이번 턴에 공격할 곳이에요.\n세 명 모두 그 위에 서 있네요.", -1, 2, 4),
        Say(1, TutorialAnchor.ActionPoints, "움직이거나 공격할 때마다 행동력을 1씩 써요.\n본편에서는 매 턴 6개지만, 지금은 연습이라 4개만 드릴게요.\n다 쓰면 보스가 공격해요."),
        S(1, TutorialAct.Move, 0, 3, 5, TutorialAnchor.Unit, "기사를 끌어서 아래 칸으로 옮기세요.\n한 번에 주변 한 칸까지 갈 수 있어요."),
        Say(1, TutorialAnchor.Unit, "방금 밟은 칸이 힘 칸이에요. 밟으면 힘이 3 쌓여요.\n힘은 0에서 시작하니, 공격하려면 먼저 힘을 모아야 해요.", 0),
        S(1, TutorialAct.Move, 1, 4, 5, TutorialAnchor.Unit, "궁수도 아래 힘 칸으로 피할게요."),
        S(1, TutorialAct.Move, 2, 5, 3, TutorialAnchor.Unit, "도적은 위쪽으로 피하세요."),
        S(1, TutorialAct.Judge, 0, 0, 0, TutorialAnchor.Unit, "이제 공격이에요. 기사를 더블클릭하면\n모은 힘을 한꺼번에 터뜨려요. 이걸 심판이라고 불러요."),

        // ── 2턴: 의지력, 특성, 피격 준비 ──
        Say(2, TutorialAnchor.Center, "모두 피했어요. 공격 칸 밖에 있으면 맞지 않아요."),
        Say(2, TutorialAnchor.WillPower, "이건 보스의 의지력이에요. 턴이 끝날 때마다 이 숫자만큼 HP를 잃어요.\n본편에서는 턴이 지날수록 커지니, 오래 끌수록 불리해요."),
        Say(2, TutorialAnchor.Portrait, "동료마다 특성이 있어요. 초상화에서 확인할 수 있어요.", 2),
        S(2, TutorialAct.Move, 2, 5, 4, TutorialAnchor.Unit, "도적을 아래 힘 칸으로 옮기세요.\n도적은 힘 칸에서 힘을 1 더 얻어요."),
        Say(2, TutorialAnchor.Unit, "기사는 공격 칸에 그대로 둘게요. 일부러 한 번 맞아 볼 거예요.", 0),
        S(2, TutorialAct.Judge, 1, 0, 0, TutorialAnchor.Unit, "궁수는 그 턴에 움직이지 않고 심판하면 피해가 8 늘어요.\n그대로 더블클릭하세요."),

        // ── 3턴: 피격 정리, 욕심 칸, 버티기 ──
        Say(3, TutorialAnchor.Unit, "기사가 맞았어요. 보통은 맞은 동료 한 명당 HP를 4 잃지만,\n기사는 특성 덕분에 첫 피격에 3만 잃었어요.", 0),
        Say(3, TutorialAnchor.Unit, "대신 맞으면 힘이 2 쌓여요. 두 번째 피격까지요.\n세 번째로 맞으면 쓰러지고, 모아 둔 힘도 사라져요.", 0),
        Say(3, TutorialAnchor.PlayerHp, "턴이 끝날 때 의지력만큼 HP도 2 줄었어요."),
        Say(3, TutorialAnchor.Cell, "금색 칸은 욕심 칸이에요. 공격 칸 깊은 곳에만 생기고,\n여기서 얻은 힘은 심판할 때 피해가 2배예요.", -1, 4, 3),
        S(3, TutorialAct.Move, 2, 4, 3, TutorialAnchor.Unit, "도적을 욕심 칸으로 보내세요."),
        Say(3, TutorialAnchor.Unit, "욕심 칸에 선 채로 공격을 버티면 힘을 7 더 얻어요.\n위험한 만큼 크게 돌아와요. 도적은 이대로 둘게요.", 2),
        S(3, TutorialAct.Move, 0, 3, 6, TutorialAnchor.Unit, "기사는 아래 힘 칸으로."),
        S(3, TutorialAct.Judge, 0, 0, 0, TutorialAnchor.Unit, "기사로 심판하세요."),

        // ── 4턴: 마무리 ──
        Say(4, TutorialAnchor.Unit, "도적이 버텼어요. 피격 힘 2에 버틴 힘 7까지, 힘이 17이 됐어요.", 2),
        Say(4, TutorialAnchor.Portrait, "초상화에 마우스를 올리면\n지금 심판했을 때의 피해를 미리 볼 수 있어요.", 2),
        S(4, TutorialAct.Judge, 2, 0, 0, TutorialAnchor.Unit, "욕심을 부린 값을 받을 차례예요.\n도적을 더블클릭하세요."),
    };

    /// <summary>안내자 보스 데이터. 에셋 없이 코드로 만든다. 배경음은 다른 보스 것을 빌린다.</summary>
    public static BossTemp CreateBoss(BossTemp musicFrom)
    {
        var boss = ScriptableObject.CreateInstance<BossTemp>();
        boss.name = "Tutorial";
        boss.bossId = BossId;
        boss.bossName = BossName;
        boss.maxHp = BossHp;
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
