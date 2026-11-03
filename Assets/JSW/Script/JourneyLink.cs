using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 마을의 여정 지도(JourneyMap)와 전투 장면(BattleDisplayManager) 사이의 전달.
public static class JourneyLink
{
    /// <summary>지도에서 고른 보스. 전투 장면이 열리면 이 보스와 바로 싸운다.</summary>
    public static string PendingBossId;
    /// <summary>지도에서 꼬마 사신의 편성 버튼을 눌렀다. 전투 장면에서 편성 화면만 열고, 나가면 지도로 돌아온다.</summary>
    public static bool FormationRequested;
    /// <summary>마을이 열리면 바로 지도를 띄운다 (전투·편성에서 돌아올 때).</summary>
    public static bool OpenMapOnVillage;
    /// <summary>방금 이기고 돌아왔다 (지도에서 안개가 걷히는 연출).</summary>
    public static bool JustCleared;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        PendingBossId = null;
        FormationRequested = OpenMapOnVillage = JustCleared = false;
    }

    public static bool ConsumeOpenMap()
    {
        bool open = OpenMapOnVillage;
        OpenMapOnVillage = false;
        return open;
    }

    /// <summary>전투 장면에서 마을의 지도로 돌아간다.</summary>
    public static void ReturnToMap(bool justCleared)
    {
        OpenMapOnVillage = true;
        JustCleared = justCleared;
        SceneFade.LoadScene("Main", Color.white, 0.8f, 0.6f);
    }
}

// 여정 중 편성 = 정비. 보스를 이길 때마다 1회(캐릭터 1명 또는 아이템 1칸) 바꿀 수 있고,
// 저승 조건인 꼬마 사신·사신의 낫·계약서는 넣지도 빼지도 못한다. 여정 시작 전에는 제한 없음.
public static class JourneyRefit
{
    public const int ReaperId = 9;
    private static readonly string[] LockedItems = { "22", "23" };

    private static int[] _startUnits;
    private static string[] _startItems;
    private static TextMeshProUGUI _banner;
    private static float _noticeUntil;
    private static bool _showingNotice;
    private static UnitTemp[] _lastUnits;
    private static ItemData[] _lastItems;

    /// <summary>지도에서 연 편성 화면인지 (나가면 지도로 돌아간다).</summary>
    public static bool FromMap { get; private set; }
    /// <summary>정비 제한이 걸려 있는지 (여정 중).</summary>
    public static bool Limited => FromMap && RunSave.HasRun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        FromMap = false;
        _startUnits = null;
        _startItems = null;
        _banner = null;
    }

    public static void Begin(UnitTemp[] units, ItemData[] items)
    {
        FromMap = true;
        _startUnits = units.Select(u => u != null ? u.id : 0).ToArray();
        _startItems = items.Select(i => i != null ? i.id : "").ToArray();
        RefreshBanner(units, items);
    }

    /// <summary>편성 화면을 나갈 때. 바꾼 만큼 정비 횟수를 쓰고 여정 편성으로 저장한다.</summary>
    public static void Finish(UnitTemp[] units, ItemData[] items)
    {
        if (Limited)
        {
            RunSave.UseRefit(Changes(units, items));
            RunSave.SetFormation(units, items);
        }
        FromMap = false;
        if (_banner != null) Object.Destroy(_banner.canvas.gameObject);
        _banner = null;
    }

    /// <summary>여정 중 정비에서 이 캐릭터를 바꿀 수 없는지 (편성창 안내 문구용)</summary>
    public static bool IsUnitLocked(UnitTemp unit) => Limited && unit != null && unit.id == ReaperId;

    public static bool CanSetUnit(UnitTemp[] units, ItemData[] items, int slot, UnitTemp next)
    {
        if (!Limited || next == null) return true;
        int before = _startUnits[slot];
        if ((before == ReaperId || next.id == ReaperId) && before != next.id)
        {
            Notice("꼬마 사신은 여정 중에 바꿀 수 없어요");
            return false;
        }
        var after = units.ToArray();
        after[slot] = next;
        return CheckCount(after, items);
    }

    public static bool CanSetItem(UnitTemp[] units, ItemData[] items, int slot, ItemData next)
    {
        if (!Limited) return true;
        string before = items[slot] != null ? items[slot].id : "";
        string nextId = next != null ? next.id : "";
        if (LockedItems.Contains(before) || LockedItems.Contains(nextId) || LockedItems.Contains(_startItems[slot]) && nextId != _startItems[slot])
        {
            Notice("사신의 낫과 계약서는 여정 중에 바꿀 수 없어요");
            return false;
        }
        var after = items.ToArray();
        after[slot] = next;
        return CheckCount(units, after);
    }

    public static void Refresh(UnitTemp[] units, ItemData[] items) => RefreshBanner(units, items);

    private static bool CheckCount(UnitTemp[] units, ItemData[] items)
    {
        if (Changes(units, items) <= RunSave.RefitTokens) return true;
        Notice(RunSave.RefitTokens == 0 ? "정비 횟수가 없어요. 보스를 이기면 1회 얻어요" : "정비 횟수를 모두 썼어요");
        return false;
    }

    private static int Changes(UnitTemp[] units, ItemData[] items)
    {
        if (_startUnits == null) return 0;
        int count = 0;
        for (int i = 0; i < units.Length && i < _startUnits.Length; i++)
            if ((units[i] != null ? units[i].id : 0) != _startUnits[i]) count++;
        for (int i = 0; i < items.Length && i < _startItems.Length; i++)
            if ((items[i] != null ? items[i].id : "") != _startItems[i]) count++;
        return count;
    }

    // 화면 위쪽 안내: 남은 정비 횟수 / 막혔을 때 이유
    private static void RefreshBanner(UnitTemp[] units, ItemData[] items)
    {
        if (!Limited) return;
        _lastUnits = units;
        _lastItems = items;
        EnsureBanner();
        if (Time.unscaledTime < _noticeUntil) return;
        _showingNotice = false;
        int left = Mathf.Max(0, RunSave.RefitTokens - Changes(units, items));
        _banner.color = new Color(0.92f, 0.91f, 0.87f);
        _banner.text = $"정비  ·  남은 교체 {left}회\n<size=70%>꼬마 사신 · 사신의 낫 · 계약서는 바꿀 수 없어요</size>";
    }

    private static void Notice(string text)
    {
        EnsureBanner();
        _noticeUntil = Time.unscaledTime + 1.6f;
        _showingNotice = true;
        _banner.color = new Color(1f, 0.55f, 0.5f);
        _banner.text = text;
    }

    private static void EnsureBanner()
    {
        if (_banner != null) return;
        var go = new GameObject("Refit Banner", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var back = new GameObject("Back", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        var br = back.rectTransform;
        br.SetParent(go.transform, false);
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f);
        br.pivot = new Vector2(0.5f, 1f);
        br.anchoredPosition = new Vector2(0f, -64f);
        br.sizeDelta = new Vector2(620f, 92f);   // 아이템 화면 제목(왼쪽 위, x 636까지)과 겹치지 않게 (760 → 620)
        back.sprite = FxSprites.FadeBar;           // 좌우 끝이 흐려지는 띠 (검은 상자 → 부드럽게)
        back.color = new Color(0f, 0f, 0f, 0.9f);
        back.raycastTarget = false;
        _banner = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var tr = _banner.rectTransform;
        tr.SetParent(br, false);
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(16f, 0f); tr.offsetMax = new Vector2(-16f, 0f);   // 긴 경고는 두 줄로 꺾인다
        var font = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).FirstOrDefault(t => t.font != null && t != _banner)?.font;
        if (font != null) _banner.font = font;
        _banner.fontSize = 34f;
        _banner.alignment = TextAlignmentOptions.Center;
        _banner.raycastTarget = false;
        go.AddComponent<BannerTicker>();
    }

    // 막혔다는 안내가 끝나면 다시 남은 횟수를 보여 준다
    private sealed class BannerTicker : MonoBehaviour
    {
        private void Update()
        {
            if (_showingNotice && Time.unscaledTime >= _noticeUntil && _lastUnits != null) RefreshBanner(_lastUnits, _lastItems);
        }
    }
}
