using UnityEngine;
using UnityEngine.UI;
[CreateAssetMenu(fileName = "AchievementData", menuName = "Scriptable Objects/AchievementData")]
public class AchievementData : ScriptableObject
{
    public string id;
    public string title;
    public string description;

    public int targetValue;

    public AchievementReward[] rewards;
}
