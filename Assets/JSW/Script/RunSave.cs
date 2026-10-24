using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 진행 중인 여정(런)의 체크포인트 저장.
/// 보스를 고른 순간과 보스를 처치한 순간에만 기록하며, 전투 도중 상태는 저장하지 않는다.
/// 이어하면 "고른 보스의 등장 장면" 또는 "승리 보상/다음 경로 선택" 지점에서 다시 시작한다.
/// 메타 진행(소울, 구매, 업적)은 기존 PlayerPrefs 키를 그대로 사용한다.
/// </summary>
public static class RunSave
{
    private const string Prefix = "Run_v1_";
    private const string ActiveKey = Prefix + "Active";
    private const string ClearedKey = Prefix + "Cleared";          // 처치한 보스 id (순서대로, 쉼표 구분)
    private const string ClearedNamesKey = Prefix + "ClearedNames"; // 마을 안내용 이름
    private const string CurrentKey = Prefix + "Current";          // 고른 뒤 아직 못 깬 보스 id
    private const string CurrentNameKey = Prefix + "CurrentName";
    private const string ChoicesKey = Prefix + "Choices";          // 다음 경로 후보 3개 (리롤 방지)
    private const string RewardPendingKey = Prefix + "RewardPending";
    private const string UnitsKey = Prefix + "Units";              // 여정 시작 시 고정된 편성
    private const string ItemsKey = Prefix + "Items";

    private const char Separator = ',';

    /// <summary>마을에서 "여정 이어가기"를 눌렀을 때 전투 씬이 이어하기로 시작하도록 전달한다.</summary>
    private static bool _resumeRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _resumeRequested = false;

    public static bool HasRun => PlayerPrefs.GetInt(ActiveKey, 0) == 1;

    /// <summary>보스를 골라 놓고 아직 처치하지 못한 상태(= 보스전 다시 시작 가능).</summary>
    public static bool HasCurrentBattle => HasRun && !string.IsNullOrEmpty(CurrentBossId);

    public static string CurrentBossId => PlayerPrefs.GetString(CurrentKey, "");
    public static string CurrentBossName => PlayerPrefs.GetString(CurrentNameKey, "");
    public static bool RewardPending => PlayerPrefs.GetInt(RewardPendingKey, 0) == 1;
    public static IReadOnlyList<string> ClearedBossIds => Split(PlayerPrefs.GetString(ClearedKey, ""));
    public static IReadOnlyList<string> ClearedBossNames => Split(PlayerPrefs.GetString(ClearedNamesKey, ""));
    public static IReadOnlyList<string> ChoiceIds => Split(PlayerPrefs.GetString(ChoicesKey, ""));
    public static IReadOnlyList<int> UnitIds => Split(PlayerPrefs.GetString(UnitsKey, ""))
        .Select(s => int.TryParse(s, out var id) ? id : 0).ToArray();
    public static IReadOnlyList<string> ItemIds => PlayerPrefs.GetString(ItemsKey, "").Split(Separator);

    public static void RequestResume() => _resumeRequested = true;

    public static bool ConsumeResumeRequest()
    {
        bool requested = _resumeRequested;
        _resumeRequested = false;
        return requested && HasRun;
    }

    /// <summary>첫 보스를 고른 순간 여정을 시작하고 편성을 고정한다.</summary>
    public static void Begin(UnitTemp[] units, ItemData[] items)
    {
        Delete(save: false);
        PlayerPrefs.SetInt(ActiveKey, 1);
        PlayerPrefs.SetString(UnitsKey, string.Join(Separator.ToString(), units.Select(u => u != null ? u.id : 0)));
        // 빈 아이템 칸도 자리를 유지해야 하므로 빈 문자열을 그대로 둔다.
        PlayerPrefs.SetString(ItemsKey, string.Join(Separator.ToString(), items.Select(i => i != null ? i.id : "")));
        PlayerPrefs.Save();
    }

    public static void SetCurrentBoss(BossTemp boss)
    {
        if (!HasRun || boss == null) return;
        PlayerPrefs.SetString(CurrentKey, boss.bossId);
        PlayerPrefs.SetString(CurrentNameKey, boss.bossName ?? "");
        PlayerPrefs.DeleteKey(ChoicesKey);
        PlayerPrefs.SetInt(RewardPendingKey, 0);
        PlayerPrefs.Save();
    }

    /// <summary>현재 보스를 처치 목록으로 옮긴다. 보상을 받기 전까지는 승리 화면부터 이어한다.</summary>
    public static void MarkCurrentCleared()
    {
        if (!HasCurrentBattle) return;
        PlayerPrefs.SetString(ClearedKey, Append(PlayerPrefs.GetString(ClearedKey, ""), CurrentBossId));
        PlayerPrefs.SetString(ClearedNamesKey, Append(PlayerPrefs.GetString(ClearedNamesKey, ""), CurrentBossName));
        PlayerPrefs.DeleteKey(CurrentKey);
        PlayerPrefs.DeleteKey(CurrentNameKey);
        PlayerPrefs.SetInt(RewardPendingKey, 1);
        PlayerPrefs.Save();
    }

    public static void ClearRewardPending()
    {
        if (!HasRun || !RewardPending) return;
        PlayerPrefs.SetInt(RewardPendingKey, 0);
        PlayerPrefs.Save();
    }

    public static void SaveChoices(IEnumerable<BossTemp> choices)
    {
        if (!HasRun) return;
        PlayerPrefs.SetString(ChoicesKey, string.Join(Separator.ToString(), choices.Where(b => b != null).Select(b => b.bossId)));
        PlayerPrefs.Save();
    }

    /// <summary>패배, 엔딩, 여정 포기, 새로하기 시 호출한다.</summary>
    public static void Delete(bool save = true)
    {
        foreach (var key in new[] { ActiveKey, ClearedKey, ClearedNamesKey, CurrentKey, CurrentNameKey,
                     ChoicesKey, RewardPendingKey, UnitsKey, ItemsKey })
            PlayerPrefs.DeleteKey(key);
        if (save) PlayerPrefs.Save();
    }

    private static string Append(string list, string value)
    {
        value = (value ?? "").Replace(Separator, ' ');
        return string.IsNullOrEmpty(list) ? value : list + Separator + value;
    }

    private static string[] Split(string list) =>
        string.IsNullOrEmpty(list) ? Array.Empty<string>() : list.Split(Separator);
}
