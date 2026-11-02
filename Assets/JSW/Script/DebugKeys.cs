#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Reflection;
using UnityEngine;

/// <summary>
/// 테스트용 단축키. 에디터와 Development Build에서만 들어가고, 정식 빌드에는 포함되지 않는다.
/// 씬에 따로 붙일 필요 없이 게임 시작 시 자동으로 생성된다.
///
/// F4  : 단축키 목록 켜기/끄기
/// F5  : 소울 +10,000
/// F6  : 캐릭터 전부 해금
/// F7  : 아이템 전부 해금
/// F8  : 스킨 + 추억 전부 해금
/// Shift + F6/F7/F8 : 다시 잠금 (기본 지급 캐릭터 1~3, 아이템 1~3 은 남김)
/// F9  : 정비 횟수 +1 (여정 중에만 의미 있음)
/// F10 : 전투 중 보스 즉시 처치
/// F11 : 전투 중 플레이어 HP 가득 + 행동력 +1
///
/// 해금은 PlayerPrefs 에 직접 쓰고 인벤토리를 다시 읽는다.
/// InventoryManager.Unlock* 를 거치지 않으므로 "전부 모으기" 업적(ACH-31)이 테스트로 달성되지 않는다.
/// 상점 화면이 열려 있으면 버튼 상태는 화면을 다시 열어야 갱신된다.
/// </summary>
public class DebugKeys : MonoBehaviour
{
    // RunSave.RefitKey 와 같은 값. RunSave 를 건드리지 않으려고 여기서 직접 쓴다.
    private const string RefitKey = "Run_v1_Refit";

    // InventoryManager 의 characterCount / memorialCount 와 같은 값.
    private const int CharacterCount = 9;   // 1~9 (9 = 사신)
    private const int SkinCharacterCount = 8; // 스킨은 1~8 만 있음
    private const int MemorialCount = 8;
    private const int SoulAmount = 10000;
    private const int DefaultOwnedCount = 3; // MainManager 가 새 게임에 주는 캐릭터 1~3, 아이템 1~3

    private static readonly FieldInfo BossHpGateField =
        typeof(BoardPanel).GetField("_bossHpGate", BindingFlags.Instance | BindingFlags.NonPublic);

    private string _message;
    private float _messageUntil;
    private bool _showHelp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var go = new GameObject("DebugKeys");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugKeys>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4)) _showHelp = !_showHelp;
        if (Input.GetKeyDown(KeyCode.F5)) AddSoul();
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (Input.GetKeyDown(KeyCode.F6)) { if (shift) LockCharacters(); else UnlockAllCharacters(); }
        if (Input.GetKeyDown(KeyCode.F7)) { if (shift) LockItems(); else UnlockAllItems(); }
        if (Input.GetKeyDown(KeyCode.F8)) { if (shift) LockSkinsAndMemorials(); else UnlockAllSkinsAndMemorials(); }
        if (Input.GetKeyDown(KeyCode.F9)) AddRefit();
        if (Input.GetKeyDown(KeyCode.F10)) KillBoss();
        if (Input.GetKeyDown(KeyCode.F11)) RestoreReaper();
    }

    // ───────── 메타 진행 ─────────

    private void AddSoul()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) { Show("[테스트] 인벤토리가 아직 없어요 (마을에서 사용)"); return; }
        inv.AddSoul(SoulAmount);
        Show($"[테스트] 소울 +{SoulAmount:N0}  →  {inv.GetSoul():N0}");
    }

    private void UnlockAllCharacters()
    {
        for (int i = 1; i <= CharacterCount; i++) PlayerPrefs.SetInt("Character_" + i, 1);
        Reload();
        Show($"[테스트] 캐릭터 {CharacterCount}명 전부 해금");
    }

    private void UnlockAllItems()
    {
        var items = ItemManager.Instance != null ? ItemManager.Instance.items : null;
        if (items == null) { Show("[테스트] 아이템 목록이 아직 없어요 (마을에서 사용)"); return; }
        foreach (var item in items)
            if (item != null && PlayerPrefs.GetInt("Item_" + item.id, 0) <= 0)
                PlayerPrefs.SetInt("Item_" + item.id, 1);
        Reload();
        Show($"[테스트] 아이템 {items.Length}개 전부 해금");
    }

    private void UnlockAllSkinsAndMemorials()
    {
        for (int i = 1; i <= SkinCharacterCount; i++)
            foreach (Skin skin in System.Enum.GetValues(typeof(Skin)))
                PlayerPrefs.SetInt($"Skin_{i}_{skin}", 1);
        for (int i = 1; i <= MemorialCount; i++) PlayerPrefs.SetInt("Memorial_" + i, 1);
        Reload();
        Show("[테스트] 스킨 + 추억 전부 해금");
    }

    // ───────── 다시 잠금 ─────────

    private void LockCharacters()
    {
        for (int i = DefaultOwnedCount + 1; i <= CharacterCount; i++) PlayerPrefs.DeleteKey("Character_" + i);
        Reload();
        Show($"[테스트] 캐릭터 잠금 (기본 1~{DefaultOwnedCount}번만 남김)");
    }

    private void LockItems()
    {
        var items = ItemManager.Instance != null ? ItemManager.Instance.items : null;
        if (items == null) { Show("[테스트] 아이템 목록이 아직 없어요 (마을에서 사용)"); return; }
        foreach (var item in items)
            if (item != null && !(int.TryParse(item.id, out int id) && id <= DefaultOwnedCount))
                PlayerPrefs.DeleteKey("Item_" + item.id);
        Reload();
        Show($"[테스트] 아이템 잠금 (기본 1~{DefaultOwnedCount}번만 남김)");
    }

    private void LockSkinsAndMemorials()
    {
        for (int i = 1; i <= SkinCharacterCount; i++)
            foreach (Skin skin in System.Enum.GetValues(typeof(Skin)))
                PlayerPrefs.DeleteKey($"Skin_{i}_{skin}");
        for (int i = 1; i <= MemorialCount; i++) PlayerPrefs.DeleteKey("Memorial_" + i);
        Reload();
        Show("[테스트] 스킨 + 추억 잠금");
    }

    private static void Reload()
    {
        PlayerPrefs.Save();
        // 캐시된 목록을 다시 읽어야 HasCharacter/HasItem 이 바로 true 가 된다.
        if (InventoryManager.Instance != null) InventoryManager.Instance.ReloadFromSave();
    }

    private void AddRefit()
    {
        if (!RunSave.HasRun)
        {
            Show("[테스트] 여정 중이 아니라서 정비 제한이 없어요");
            return;
        }
        PlayerPrefs.SetInt(RefitKey, RunSave.RefitTokens + 1);
        PlayerPrefs.Save();
        Show($"[테스트] 정비 +1  →  남은 정비 {RunSave.RefitTokens}회");
    }

    // ───────── 전투 ─────────

    private BoardPanel FindBattle()
    {
        var board = FindAnyObjectByType<BoardPanel>();
        if (board == null || !board.isActiveAndEnabled || board.boss == null)
        {
            Show("[테스트] 전투 중이 아니에요");
            return null;
        }
        if (board.IsBattleOver)
        {
            Show("[테스트] 이미 전투가 끝났어요");
            return null;
        }
        return board;
    }

    private void KillBoss()
    {
        var board = FindBattle();
        if (board == null) return;
        // 패턴 HP 게이트(일정 HP 아래로 안 내려가는 구간)를 무시하고 바로 0 으로 만든다.
        BossHpGateField?.SetValue(board, null);
        board.UpdateBossHp(0);
        Show("[테스트] 보스 즉시 처치");
    }

    private void RestoreReaper()
    {
        var board = FindBattle();
        if (board == null) return;
        board.UpdateReaperHp(board.reaperMaxHp);
        board.UpdateActionPoint(board.actionPoint + 1);
        Show($"[테스트] HP 가득 ({board.reaperMaxHp}) + 행동력 +1  →  {board.actionPoint}");
    }

    // ───────── 표시 ─────────

    private void Show(string text)
    {
        _message = text;
        _messageUntil = Time.unscaledTime + 2f;
        Debug.Log(text);
    }

    private const string HelpText =
        "F4  단축키 목록\n" +
        "F5  소울 +10,000\n" +
        "F6  캐릭터 전부 해금\n" +
        "F7  아이템 전부 해금\n" +
        "F8  스킨 + 추억 전부 해금\n" +
        "Shift+F6~F8  다시 잠금\n" +
        "F9  정비 +1 (여정 중)\n" +
        "F10 보스 즉시 처치 (전투 중)\n" +
        "F11 HP 가득 + 행동력 +1 (전투 중)";

    private void OnGUI()
    {
        if (_showHelp)
        {
            var help = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.UpperLeft, padding = new RectOffset(16, 16, 12, 12) };
            GUI.Box(new Rect(20f, 120f, 420f, 280f), HelpText, help);
        }

        if (Time.unscaledTime > _messageUntil) return;
        var style = new GUIStyle(GUI.skin.box) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
        GUI.Box(new Rect(Screen.width * 0.5f - 320f, 40f, 640f, 60f), _message, style);
    }
}
#endif
