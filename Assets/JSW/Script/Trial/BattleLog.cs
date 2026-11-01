using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// v2 전투 로그 (에디터 전용). 프로젝트 폴더/BattleLogs 에 전투 1판당 파일 1개로 기록한다.
/// 좌표는 (x,y), y=0이 판 위쪽. 판 그림: ■ 공격 칸, G 욕심 칸, + 안전 힘 칸, 1·2·3 캐릭터.
/// </summary>
public static class BattleLog
{
#if UNITY_EDITOR
    private static string _path;
    private static float _lastTime;
#endif

    public static void Begin(string bossId, string bossName, int bossHp, int playerHp)
    {
#if UNITY_EDITOR
        try
        {
            string dir = Path.Combine(Application.dataPath, "..", "BattleLogs");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd_HHmmss}_{bossId}.txt");
            _lastTime = Time.realtimeSinceStartup;
            File.WriteAllText(_path, $"# 전투 로그 {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n보스 {bossName} ({bossId}) HP {bossHp}, 플레이어 HP {playerHp}\n", new UTF8Encoding(false));
        }
        catch (Exception e) { Debug.LogWarning("[BattleLog] " + e.Message); _path = null; }
#endif
    }

    public static void Line(string text)
    {
#if UNITY_EDITOR
        if (_path == null) return;
        try
        {
            float now = Time.realtimeSinceStartup;
            File.AppendAllText(_path, $"[+{now - _lastTime:0.0}s] {text}\n", new UTF8Encoding(false));
            _lastTime = now;
        }
        catch (Exception e) { Debug.LogWarning("[BattleLog] " + e.Message); }
#endif
    }

    public static void Raw(string text)
    {
#if UNITY_EDITOR
        if (_path == null) return;
        try { File.AppendAllText(_path, text + "\n", new UTF8Encoding(false)); }
        catch (Exception e) { Debug.LogWarning("[BattleLog] " + e.Message); }
#endif
    }

    public static void End(string result)
    {
#if UNITY_EDITOR
        if (_path == null) return;
        Line("결과: " + result);
        _path = null;
#endif
    }

    public static string Board(HashSet<Vector2Int> danger, IDictionary<Vector2Int, bool> power, Vector2Int?[] units)
    {
        var sb = new StringBuilder();
        for (int y = 0; y < 9; y++)
        {
            sb.Append("    ");
            for (int x = 0; x < 9; x++)
            {
                var c = new Vector2Int(x, y);
                char ch = danger != null && danger.Contains(c) ? '■' : '·';
                if (power != null && power.TryGetValue(c, out bool greed)) ch = greed ? 'G' : '+';
                for (int i = 0; i < units.Length; i++) if (units[i] == c) ch = (char)('1' + i);
                sb.Append(ch).Append(' ');
            }
            if (y < 8) sb.Append('\n');
        }
        return sb.ToString();
    }
}
