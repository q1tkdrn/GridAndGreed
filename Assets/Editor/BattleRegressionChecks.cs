using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Run with Unity -batchmode -executeMethod BattleRegressionChecks.Run -quit.
// The scene is never saved; temporary ownership keys are restored in finally.
public static class BattleRegressionChecks
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, Fields).SetValue(target, value);
    private static object Get(object target, string name) => target.GetType().GetField(name, Fields).GetValue(target);
    private static void Call(object target, string name) => target.GetType().GetMethod(name, Fields).Invoke(target, null);
    private static T Asset<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batchmode editor.");
        EditorSceneManager.OpenScene("Assets/Sanggu/Scenes/BattleTemp.unity");
        var display = UnityEngine.Object.FindFirstObjectByType<BattleDisplayManager>(FindObjectsInactive.Include);
        var board = display.boardPanel;
        Check(board != null, "Battle scene must bind its board");
        CheckTurnEnd(board, display);
        CheckBossPassives(board);
        CheckVisualReset(board);
        CheckSkins();
        CheckAssetsAndRoute(display);
        Debug.Log($"PASS: {checks} battle regression assertions");
    }

    private static void Prepare(BoardPanel board, BossTemp boss, int hp, int will, ItemBattleState items = null)
    {
        board.boss = boss;
        board.bossMaxHp = 60;
        board.bossCurrentHp = 60;
        board.reaperMaxHp = 15;
        board.reaperCurrentHp = hp;
        board.willPower = will;
        Set(board, "_isWin", false);
        Set(board, "_isLose", false);
        Set(board, "_isPrinting", true); // Queue logs without starting edit-mode coroutines.
        Set(board, "<ItemEffects>k__BackingField", items ?? new ItemBattleState(null));
        Set(board, "_bossPassives", new BossPassiveState(boss.bossId));
    }

    private static void CheckBossPassives(BoardPanel board)
    {
        Prepare(board, Asset<BossTemp>("Assets/Sanggu/ScriptableObjects/Boss/Fusion.asset"), 15, 5);
        board.JudgeBoss(4);
        board.JudgeBoss(5);
        Check(board.willPower == 7 && board.bossCurrentHp == 51, "Judgment events must apply fusion will gain as well as damage");
        Prepare(board, Asset<BossTemp>("Assets/Sanggu/ScriptableObjects/Boss/Subject.asset"), 15, 5);
        board.OnAllyDeath();
        board.OnAllyDeath();
        Check(board.willPower == 7, "Every actual death event increments subject will power");

        Prepare(board, Asset<BossTemp>("Assets/Sanggu/ScriptableObjects/Boss/instructor.asset"), 15, 5);
        board.AutomaticAttackBoss(9);
        Check(board.bossCurrentHp == 59 && board.reaperCurrentHp == 12, "First strength attack is reduced and countered");
        board.AutomaticAttackBoss(9);
        Check(board.bossCurrentHp == 58 && board.reaperCurrentHp == 12, "Additional attacks do not counter again");
        board.JudgeBoss(6);
        Check(board.bossCurrentHp == 46 && board.reaperCurrentHp == 12, "Judgment doubles without countering");

        Prepare(board, Asset<BossTemp>("Assets/Sanggu/ScriptableObjects/Boss/Secretary.asset"), 15, 6);
        board.AttackBoss(10);
        board.JudgeBoss(10);
        board.AutomaticAttackBoss(10);
        Check(board.bossCurrentHp == 57, "All three damage entry points use odd-turn reduction");
        ((BossPassiveState)Get(board, "_bossPassives")).StartTurn(2, (min, max) => min);
        board.AttackBoss(10);
        Check(board.bossCurrentHp == 47, "Even-turn item damage is restored");

        var battle = (BattleManagerTemp)Get(board, "battleManagerTemp");
        var units = (Array)Get(battle, "units");
        var original = units.GetValue(0);
        var originalSecond = units.GetValue(1);
        var unit = units.GetValue(0);
        var unitType = unit.GetType();
        unitType.GetField("isPlaced").SetValue(unit, true);
        unitType.GetField("reviveRemainTurn").SetValue(unit, 0);
        unitType.GetField("pos").SetValue(unit, new Vector2Int(0, 0));
        units.SetValue(unit, 0);
        try
        {
            Prepare(board, Asset<BossTemp>("Assets/Sanggu/ScriptableObjects/Boss/Pope.asset"), 15, 5);
            ((BossPassiveState)Get(board, "_bossPassives")).StartTurn(1, (min, max) => min);
            board.JudgeBossFromUnit(9, 0);
            board.AutomaticAttackBossFromUnit(9, 0);
            board.AttackBossFromUnit(9, 0);
            Check(board.bossCurrentHp == 60, "Sanctuary blocks every character damage entry point");
            board.AttackBoss(3);
            Check(board.bossCurrentHp == 57, "Independent item damage is not attributed to a sanctuary unit");
            unitType.GetField("pos").SetValue(unit, new Vector2Int(8, 8));
            units.SetValue(unit, 0);
            board.JudgeBossFromUnit(9, 0);
            Check(board.bossCurrentHp == 48, "Damage checks current logical position after movement");

            Prepare(board, Asset<BossTemp>("Assets/Sanggu/ScriptableObjects/Boss/Door.asset"), 15, 5);
            var blanks = (System.Collections.Generic.Dictionary<Vector2Int, GameObject>)Get(battle, "_blankPos");
            for (int y = 0; y < 9; y++)
                for (int x = 0; x < 9; x++)
                {
                    var cell = new GameObject("RegressionCell", typeof(RectTransform));
                    cell.GetComponent<RectTransform>().anchoredPosition = new Vector2(x * 82, y * 82);
                    blanks.Add(new Vector2Int(x, y), cell);
                }
            try
            {
                var second = units.GetValue(1);
                unitType.GetField("isPlaced").SetValue(second, true);
                unitType.GetField("pos").SetValue(second, new Vector2Int(4, 4));
                units.SetValue(second, 1);
                board.JudgeBossFromUnit(4, 0);
                var moved = units.GetValue(0);
                var destination = (Vector2Int)unitType.GetField("pos").GetValue(moved);
                var image = (Image)unitType.GetField("unit").GetValue(moved);
                Check(destination != new Vector2Int(8, 8) && destination != new Vector2Int(4, 4),
                    "Door relocates the caster without keeping the old cell or overlapping another unit");
                Check(image.rectTransform.anchoredPosition == blanks[destination].GetComponent<RectTransform>().anchoredPosition,
                    "Door relocation updates the visible and logical positions together");
                Check(board.bossCurrentHp == 56 && board.reaperCurrentHp == 15, "Forced movement causes no extra passive damage/healing");
            }
            finally
            {
                foreach (var cell in blanks.Values) UnityEngine.Object.DestroyImmediate(cell);
                blanks.Clear();
            }
        }
        finally
        {
            units.SetValue(original, 0);
            units.SetValue(originalSecond, 1);
        }
    }

    private static void CheckTurnEnd(BoardPanel board, BattleDisplayManager display)
    {
        Prepare(board, display.bossKing, 15, 5);
        Call(board, "EndBossTurn");
        Check(board.reaperCurrentHp == 10, "Will power must damage the player without a pattern hit");
        Call(board, "EndBossTurn");
        Check(board.reaperCurrentHp == 5, "Each subsequent turn applies will power once");
        Prepare(board, display.bossKing, 15, 0);
        Call(board, "EndBossTurn");
        Check(board.reaperCurrentHp == 15, "Zero will power must not damage HP");
        Prepare(board, display.bossKing, 4, 5);
        Call(board, "EndBossTurn");
        Check(board.reaperCurrentHp == 0 && board.IsBattleOver, "Lethal will power must lose the battle");
        Prepare(board, display.bossKing, 4, 5, new ItemBattleState(new[] { "23" }));
        Call(board, "EndBossTurn");
        Check(board.reaperCurrentHp == 10 && !board.IsBattleOver, "The contract rescues lethal will power");
        board.reaperCurrentHp = 4;
        Call(board, "EndBossTurn");
        Check(board.IsBattleOver, "The contract cannot rescue twice in the same battle");
        Prepare(board, display.bossDeath2, 4, 7);
        board.bossCurrentHp = 1;
        Call(board, "EndBossTurn");
        Check(board.bossCurrentHp == 0 && board.reaperCurrentHp == 4, "Victory stops further will power damage");
    }

    private static void CheckVisualReset(BoardPanel board)
    {
        var units = (Array)Get(board, "units");
        var materials = new Material[units.Length];
        var sizes = new Vector2[units.Length];
        Set(board, "_unitMaterials", materials);
        Set(board, "_unitSizes", sizes);
        Set(board, "_moveAnimations", new Coroutine[units.Length]);
        Set(board, "_hitAnimations", new Coroutine[units.Length]);
        try
        {
            for (int i = 0; i < units.Length; i++)
            {
                var unit = units.GetValue(i);
                var image = (Image)unit.GetType().GetField("unit").GetValue(unit);
                materials[i] = new Material(image.material);
                image.material = materials[i];
                sizes[i] = image.rectTransform.sizeDelta;
                image.rectTransform.sizeDelta *= 2;
                image.material.SetFloat("_EnableGlitch", 1);
                unit.GetType().GetField("isAnimPlaying").SetValue(unit, true);
                units.SetValue(unit, i);
            }
            Call(board, "ResetUnitVisuals");
            for (int i = 0; i < units.Length; i++)
            {
                var unit = units.GetValue(i);
                var image = (Image)unit.GetType().GetField("unit").GetValue(unit);
                Check(image.material.GetFloat("_EnableGlitch") == 0, "Next battle clears death noise");
                Check(image.rectTransform.sizeDelta == sizes[i], "Next battle restores interrupted animation size");
                Check(!(bool)unit.GetType().GetField("isAnimPlaying").GetValue(unit), "Next battle clears animation lock");
            }
        }
        finally
        {
            foreach (var material in materials) UnityEngine.Object.DestroyImmediate(material);
            Set(board, "_unitMaterials", null);
        }
    }

    private static void CheckSkins()
    {
        var previous = InventoryManager.Instance;
        var go = new GameObject("RegressionInventory");
        go.SetActive(false);
        InventoryManager.Instance = go.AddComponent<InventoryManager>();
        var unit = UnityEngine.Object.Instantiate(Asset<UnitTemp>("Assets/Sanggu/ScriptableObjects/Units/1.Knight.asset"));
        unit.id = 910001;
        var kinds = new[] { Skin.InsectSkin, Skin.BossSkin, Skin.NpcSkin };
        var keys = kinds.Select(s => $"Skin_{unit.id}_{s}").ToArray();
        var existed = keys.Select(PlayerPrefs.HasKey).ToArray();
        var values = keys.Select(k => PlayerPrefs.GetInt(k)).ToArray();
        try
        {
            foreach (var key in keys) PlayerPrefs.DeleteKey(key);
            unit.isSkin1Unlocked = unit.isSkin2Unlocked = unit.isSkin3Unlocked = true;
            for (int i = 0; i < keys.Length; i++)
            {
                Check(!unit.IsSkinUnlocked(i + 1), "Serialized debug flags must not unlock unpaid skins");
                PlayerPrefs.SetInt(keys[i], 1);
                Check(unit.IsSkinUnlocked(i + 1), "Purchased skin must unlock the matching formation slot");
                PlayerPrefs.DeleteKey(keys[i]);
            }
            unit.skin1 = null;
            PlayerPrefs.SetInt(keys[0], 1);
            Check(!unit.IsSkinUnlocked(1), "Missing skins cannot be equipped even with an ownership key");
        }
        finally
        {
            for (int i = 0; i < keys.Length; i++)
                if (existed[i]) PlayerPrefs.SetInt(keys[i], values[i]); else PlayerPrefs.DeleteKey(keys[i]);
            InventoryManager.Instance = previous;
            UnityEngine.Object.DestroyImmediate(unit);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static void CheckAssetsAndRoute(BattleDisplayManager display)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:UnitTemp"))
        {
            var unit = Asset<UnitTemp>(AssetDatabase.GUIDToAssetPath(guid));
            if (unit.id == 9) continue;
            int expected = unit.id == 8 ? 2 : 3;
            Check(unit.reviveCool == expected, "All revivable units gain one cooldown turn");
            Check(new ItemBattleState(new[] { "9" }).ReviveTurns(unit.reviveCool) == expected - 1,
                "The watch still reduces the new cooldown by one");
        }
        for (int i = 1; i <= 8; i++)
            Check(Asset<CharacterData>($"Assets/JDY/Data/Characters/Character_{i}.asset").price == 150, "Unit price is 150");
        foreach (var guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            var item = Asset<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            Check(item.price == -1 || item.price == 100, "Shop items cost 100; reward items stay unlisted");
        }
        Check(SkinSlot.Price == 100, "Skin price is independent of unit price");
        var scythe = Asset<ItemData>("Assets/JDY/Data/Items/Item_22.asset");
        var contract = Asset<ItemData>("Assets/JDY/Data/Items/Item_23.asset");
        var reaper = Asset<UnitTemp>("Assets/Sanggu/ScriptableObjects/Units/9.Reaper.asset");
        display.currentItems = new[] { scythe, contract, null };
        display.currentUnits = new[] { reaper, null, null };
        Check(display.GetFinalBoss() == display.bossDeath1 && display.bossDeath1 != null, "Complete equipment opens death phase one");
        display.currentItems[1] = null;
        Check(display.GetFinalBoss() == display.bossKing, "Missing contract selects the king");
        display.currentItems[1] = contract;
        display.currentUnits[0] = null;
        Check(display.GetFinalBoss() == display.bossKing, "Missing reaper selects the king");
    }
}
