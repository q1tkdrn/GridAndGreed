using System;
using System.Collections.Generic;

// In-memory persistence and inventory: these checks never touch the player's save.
namespace UnityEngine
{
    public class MonoBehaviour
    {
        public object gameObject = new object();
        protected static void DontDestroyOnLoad(object value) { }
        protected static void Destroy(object value) { }
    }
    public static class Debug { public static void Log(object value) { } }
    public static class PlayerPrefs
    {
        public static readonly Dictionary<string, int> Values = new Dictionary<string, int>();
        public static int GetInt(string key, int fallback = 0) => Values.TryGetValue(key, out var value) ? value : fallback;
        public static void SetInt(string key, int value) => Values[key] = value;
    }
}

public class AchievementData
{
    public string id;
    public int targetValue;
    public AchievementReward[] rewards;
}
public class InventoryManager
{
    public static InventoryManager Instance;
    public int Grants;
    public string LastItem;
    public int LastAmount;
    public void AddSoul(int value) { Grants++; }
    public void AddItem(string id, int amount) { Grants++; LastItem = id; LastAmount = amount; }
    public void UnlockMemorial(string id) { Grants++; }
    public void UnlockCharacter(string id) { Grants++; }
}

public static class CreditAchievementTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }

    private static AchievementManager Manager() => new AchievementManager
    {
        achievements = new[] { new AchievementData
        {
            id = "ACH-10", targetValue = 1,
            rewards = new[] { new AchievementReward { type = RewardType.Item, rewardID = "10", amount = 1 } }
        } }
    };

    public static void Main()
    {
        UnityEngine.PlayerPrefs.Values.Clear();
        InventoryManager.Instance = new InventoryManager();
        var manager = Manager();
        manager.GetReward("ACH-10");
        Check(InventoryManager.Instance.Grants == 0, "Unviewed credits cannot grant a reward");
        manager.AddProgress("ACH-10", 1);
        Check(manager.IsCompleted("ACH-10") && manager.IsRewarded("ACH-10"), "Viewing credits completes and rewards the achievement");
        Check(InventoryManager.Instance.LastItem == "10" && InventoryManager.Instance.LastAmount == 1, "Existing item reward is preserved");
        manager.AddProgress("ACH-10", 1);
        manager.GetReward("ACH-10");
        Check(InventoryManager.Instance.Grants == 1, "Reopening credits cannot grant duplicates");
        Check(UnityEngine.PlayerPrefs.GetInt("ACH-10_Current") == 1, "Completed progress does not keep increasing");

        UnityEngine.PlayerPrefs.Values.Clear();
        InventoryManager.Instance = null;
        manager = Manager();
        manager.AddProgress("ACH-10", 1);
        Check(manager.IsCompleted("ACH-10"), "Start-screen viewing records completion without inventory");
        Check(!manager.IsRewarded("ACH-10"), "Unavailable inventory leaves the reward pending");
        manager.AddProgress("ACH-10", 1);
        InventoryManager.Instance = new InventoryManager();
        manager = Manager(); // Scene reload retains saved completion.
        foreach (var achievement in manager.achievements) manager.GetReward(achievement.id);
        Check(manager.IsRewarded("ACH-10") && InventoryManager.Instance.Grants == 1, "Loading inventory delivers the pending reward");
        manager.GetReward("ACH-10");
        Check(InventoryManager.Instance.Grants == 1, "Loading inventory again does not duplicate rewards");
        Console.WriteLine($"PASS: {checks} credit achievement assertions");
    }
}
