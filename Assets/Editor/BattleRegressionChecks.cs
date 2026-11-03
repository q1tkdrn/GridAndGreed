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
        CheckPlacementSelection(board);
        CheckTurnEnd(board, display);
        CheckVisualReset(board);
        CheckSkins();
        CheckAssetsAndRoute(display);
        var bellkeeper = Asset<BossTemp>("Assets/Sanggu/ScriptableObjects/Boss/Bellkeeper.asset");
        Check(bellkeeper != null && bellkeeper.bossId == "bellkeeper", "Bellkeeper asset imports");
        Check(bellkeeper.bossSprite != null && bellkeeper.stageSprite != null, "Bellkeeper sprite references resolve");
        Check(display.remainBoss.Contains(bellkeeper), "Bellkeeper is available in the journey pool");
        Check(bellkeeper.bgmLoop != null, "Bellkeeper has battle music");
        Check(BellkeeperTrial.IsTrialBoss(bellkeeper), "Bellkeeper uses the battle rules");
        Debug.Log($"PASS: {checks} battle regression assertions");
    }

    private static void CheckPlacementSelection(BoardPanel board)
    {
        var battle = (BattleManagerTemp)Get(board, "battleManagerTemp");
        var originalUnits = (Array)Get(battle, "units");
        var testUnits = (Array)originalUnits.Clone();
        var originalTurn = board.turn;
        var originalWin = Get(board, "_isWin");
        var originalLose = Get(board, "_isLose");
        var originalSelection = Get(battle, "_currentMoveIndex");
        try
        {
            Set(battle, "units", testUnits);
            Set(board, "_isWin", false);
            Set(board, "_isLose", false);
            board.turn = BoardPanel.ETurn.Place;
            for (int i = 0; i < testUnits.Length; i++)
            {
                var unit = testUnits.GetValue(i);
                unit.GetType().GetField("isPlaced").SetValue(unit, false);
                unit.GetType().GetField("reviveRemainTurn").SetValue(unit, 0);
                testUnits.SetValue(unit, i);
            }

            battle.OnUnitInReadyClicked(0);
            battle.OnUnitInReadyClicked(1);
            battle.OnUnitInReadyClicked(0);
            Check((int)Get(battle, "_currentMoveIndex") == 0,
                "Switching placement selection must allow selecting the first unit again");
            var selected = testUnits.GetValue(0);
            Check((int)selected.GetType().GetField("reviveRemainTurn").GetValue(selected) == 0,
                "Selecting a waiting unit must not consume its placement eligibility");
            battle.OnUnitInReadyClicked(-1);
            battle.OnUnitInReadyClicked(testUnits.Length);
            Check((int)Get(battle, "_currentMoveIndex") == 0, "Invalid selection leaves the current unit intact");

            board.turn = BoardPanel.ETurn.Player;
            battle.OnUnitInReadyClicked(1);
            Check((int)Get(battle, "_currentMoveIndex") == 0, "Waiting icons cannot hijack movement outside placement");
            board.turn = BoardPanel.ETurn.Place;
            Set(board, "_isWin", true);
            battle.OnUnitInReadyClicked(1);
            Check((int)Get(battle, "_currentMoveIndex") == 0, "Finished battles cannot change placement selection");
        }
        finally
        {
            Set(battle, "units", originalUnits);
            board.turn = originalTurn;
            Set(board, "_isWin", originalWin);
            Set(board, "_isLose", originalLose);
            Set(battle, "_currentMoveIndex", originalSelection);
        }
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
        Set(board, "_isBossPrinting", true);
        Set(board, "<ItemEffects>k__BackingField", items ?? new ItemBattleState(null));
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
        for (int i = 1; i <= 8; i++)
            Check(Asset<CharacterData>($"Assets/JDY/Data/Characters/Character_{i}.asset").price == 500, "Unit price is 500");
        foreach (var guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            var item = Asset<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            Check(item.price == -1 || item.price == 300, "Shop items cost 300; reward items stay unlisted");
        }
        Check(SkinSlot.Price == 400, "Skin price is independent of unit price");
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
