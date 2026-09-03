using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
public class AchievementCSVImporter
{
    [MenuItem("Tools/Import Achievements From CSV")]
    public static void ImportAchievements()
    {
        string csvPath = "Assets/JDY/Data/CSV/Data - Achievements.csv";
        string savePath = "Assets/JDY/Data/Achievements/";

        if (!File.Exists(csvPath))
        {
            Debug.LogError("Achievements.csv 파일을 찾을 수 없습니다.");
            return;
        }

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length <= 1)
        {
            Debug.LogWarning("데이터 없음");
            return;
        }

        Dictionary<string, AchievementData> achievementDict = new Dictionary<string, AchievementData>();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] columns = ParseCSVLine(lines[i]);

            string achievementID = columns[0].Trim();
            string title = columns[1].Trim();
            string description = columns[2].Trim();
            if (!int.TryParse(columns[3].Trim(), out int targetValue))
            {
                Debug.LogError($"{i + 1}: TargetValue 오류");
                continue;
            }
            string rewardTypeText = columns[4].Trim();
            string rewardID = columns[5].Trim();
            if (!int.TryParse(columns[6].Trim(), out int amount))
            {
                Debug.LogError($"{i + 1}: Amount 오류");
                continue;
            }
            /*
            if (!int.TryParse(columns[7].Trim(), out int fromPhase))
            {
                Debug.LogError($"{i + 1}: FromPhase 오류");
                continue;
            }
            */
            if (!achievementDict.ContainsKey(achievementID))
            {
                AchievementData data = ScriptableObject.CreateInstance<AchievementData>();

                data.id = achievementID;
                data.title = title;
                data.description = description;
                data.targetValue = targetValue;

                string iconNumber = achievementID.Replace("ACH-", "");
                string iconPath = $"Assets/JDY/Data/Achievements/Icons/업적{iconNumber}.png";

                data.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

                if (data.icon == null)
                {
                    Debug.LogWarning($"{achievementID}: 아이콘 없음");
                }

                data.rewards = new AchievementReward[0];

                achievementDict.Add(achievementID, data);
            }

            AchievementData achievement = achievementDict[achievementID];

            if (!Enum.TryParse(rewardTypeText, true, out RewardType rewardType))
            {
                Debug.LogError($"{i + 1}: RewardType 오류");
                continue;
            }
            AchievementReward reward = new AchievementReward();

            reward.type = rewardType;
            reward.rewardID = rewardID;
            reward.amount = amount;

            List<AchievementReward> rewardList = new List<AchievementReward>(achievement.rewards);
            rewardList.Add(reward);

            achievement.rewards = rewardList.ToArray();
        }

        foreach (var pair in achievementDict)
        {
            AchievementData data = pair.Value;

            string assetPath = savePath + data.id + ".asset";

            AchievementData oldData = AssetDatabase.LoadAssetAtPath<AchievementData>(assetPath);

            if (oldData != null)
            {
                oldData.id = data.id;
                oldData.title = data.title;
                oldData.description = data.description;
                oldData.targetValue = data.targetValue;
                oldData.icon = data.icon;
                oldData.rewards = data.rewards;

                EditorUtility.SetDirty(oldData);
            }
            else
            {
                AssetDatabase.CreateAsset(data, assetPath);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("완료");
    }
    private static string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();

        bool insideQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (c == ',' && !insideQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);

        return result.ToArray();
    }
}