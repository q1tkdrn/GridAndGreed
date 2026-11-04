using System.Collections.Generic;

/// <summary>
/// 스테이지 입장 문구와 승리 후 꼬마 사신의 혼잣말 (JSW, 2026-11-04, 사용자 제공 문구 그대로).
/// 열쇠는 보스 에셋 이름(소문자): instructor, secretary, door, subject, fusion, pope, noble, king, death1 ...
/// </summary>
public static class StageText
{
    public sealed class Entry
    {
        public string stage;          // 스테이지 이름 (입장 화면 제목)
        public string intro;          // 입장 문구
        public string[] victory;      // 승리 화면 혼잣말 (없으면 null)
        public string art;            // 장소 그림 (JSW/Resources/<이름>.png, 1920 캔버스에서 그림 부분만 잘라 둔 조각)
    }

    private static readonly Dictionary<string, Entry> Table = new Dictionary<string, Entry>
    {
        ["instructor"] = new Entry
        {
            art = "훈련장",
            stage = "훈련장",
            intro = "전쟁은 끝났지만 훈련은 계속된다. 마지막 명령을 수행할 때까지.",
            victory = new[] { "……끝까지 물러서지 않았어.", "이젠 지킬 이유도, 지켜야 할 사람도 없는데.", "……그런데도 계속 싸우는 거야?" },
        },
        ["secretary"] = new Entry
        {
            art = "서고",
            stage = "서고",
            intro = "왕국의 모든 역사가 이곳에 기록되어 있다. 다만 지워진 기록에 대해서는 아무도 말하지 않는다.",
            victory = new[] { "이렇게나 많은 기록이 있는데……", "정작 이 사람에 대한 이야기는 하나도 없네.", "……이름은 어디에 적혀 있을까?" },
        },
        ["door"] = new Entry
        {
            art = "왕실연구소",
            stage = "왕실 연구소",
            intro = "영원한 생명을 향한 연구는 성공했다. 그 결과를 성공이라 부를 수 있다면 말이다.",
            victory = new[] { "문은 원래 누군가를 보내주기 위한 거잖아.", "그런데 넌 왜 아무도 보내주지 않는 거야?", "……너도 열리고 싶었던 걸까?" },
        },
        ["subject"] = new Entry
        {
            art = "지하실",
            stage = "지하실",
            intro = "왕국의 지하 깊은 곳, 버려진 것들은 아직도 자신이 버려졌다는 사실을 모른다.",
            victory = new[] { "……", "대체 얼마나 아팠던 걸까.", "……이름이라도 물어볼 수 있었다면 좋았을 텐데." },
        },
        ["fusion"] = new Entry
        {
            art = "광장",
            stage = "광장",
            intro = "한때 사람들로 북적이던 광장에는 이제 끝나지 않는 하루만이 반복되고 있다.",
            victory = new[] { "이렇게나 많은 사람이 하나가 됐는데……", "어째서 이렇게 외로워 보이는 걸까?", "……이젠 서로 떨어질 수도 없는 거겠지." },
        },
        ["pope"] = new Entry
        {
            art = "성당",
            stage = "성당",
            intro = "죽음이 사라진 왕국에서도 기도는 멈추지 않았다. 이제 그들은 무엇을 구원이라 부르는가?",
            victory = new[] { "신은 정말 죽음이 없는 세상을 원했을까?", "……사신인 내가 할 말은 아니겠지만.", "적어도 난, 이게 축복이라고 생각하지 않아." },
        },
        ["noble"] = new Entry
        {
            art = "대저택",
            stage = "대저택",
            intro = "죽음마저 비켜 간 귀족들의 저택. 당신은 초대받지 아니하였다.",
            victory = new[] { "영원히 살 수 있다면 뭐든 할 수 있을 줄 알았는데.", "……막상 이 사람은 아무것도 하고 싶어 하지 않았어.", "대체 언제부터 이렇게 된 걸까?" },
        },
        ["king"] = new Entry
        {
            art = "알현실",
            stage = "알현실",
            intro = "영원을 약속한 왕이 기다리고 있다. 그 약속의 대가를 마주할 시간이다.",
        },
        ["death1"] = new Entry
        {
            art = "저승",
            stage = "저승",
            intro = "모든 것의 시작, 모든 것의 끝.",
        },
    };

    private static readonly Dictionary<string, UnityEngine.Sprite> ArtCache = new Dictionary<string, UnityEngine.Sprite>();

    /// <summary>보스가 있는 장소의 그림. 없으면 null.</summary>
    public static UnityEngine.Sprite Art(BossTemp boss)
    {
        var name = Find(boss)?.art;
        if (string.IsNullOrEmpty(name)) return null;
        if (ArtCache.TryGetValue(name, out var cached) && cached != null) return cached;
        var parts = UnityEngine.Resources.LoadAll<UnityEngine.Sprite>(name);
        var sprite = parts != null && parts.Length > 0 ? parts[0] : null;
        ArtCache[name] = sprite;
        return sprite;
    }

    public static Entry Find(BossTemp boss)
    {
        if (boss == null) return null;
        return Table.TryGetValue(boss.name.ToLowerInvariant(), out var e) ? e : null;
    }
}
