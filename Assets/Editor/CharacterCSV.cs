using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
public class CharacterCSVImporter
{
    [MenuItem("Tools/Import Characters From CSV")]
    public static void ImportItems()
    {
        string csvPath = "Assets/JDY/Data/CSV/Data - Characters.csv";
        string savePath = "Assets/JDY/Data/Characters/";

        if (!File.Exists(csvPath))
        {
            Debug.LogError("Characters.csv를 찾을 수 없습니다.");
            return;
        }

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        string csv = File.ReadAllText(csvPath, Encoding.UTF8);
        List<List<string>> rows = ParseCSV(csv);

        if (rows.Count <= 1)
        {
            Debug.LogError("데이터가 없음");
            return;
        }

        for (int i = 1; i < rows.Count; i++)
        {
            List<string> values = rows[i];

            if (values.Count == 0)
                continue;

            string id = values[0].Trim();
            string characterName = values[1].Trim();
            string STRText = values[2].Trim();
            string INTText = values[3].Trim();
            string priceText = values[4].Trim();
            string description = values[5];

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"{i + 1}번째 줄 ID 누락");
                continue;
            }
            if (!int.TryParse(STRText, out int STR))
            {
                Debug.LogError($"{i + 1}번째 줄 STR 오류");
                continue;
            }
            if (!int.TryParse(INTText, out int INT))
            {
                Debug.LogError($"{i + 1}번째 줄 INT 오류");
                continue;
            }
            if (!int.TryParse(priceText, out int price))
            {
                Debug.LogError($"{i + 1}번째 줄 price 오류");
                continue;
            }

            string assetPath = savePath + "Character_" + id + ".asset";

            CharacterData characterData = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);

            if (characterData == null)
            {
                characterData = ScriptableObject.CreateInstance<CharacterData>();
                AssetDatabase.CreateAsset(characterData, assetPath);
                Debug.Log("생성: " + id);
            }
            else
            {
                Debug.Log("수정: " + id);
            }

            characterData.id = id;
            characterData.characterName = characterName;
            characterData.STR = STR;
            characterData.INT = INT;
            characterData.price = price;
            characterData.description = description;

            EditorUtility.SetDirty(characterData);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("완료");
    }

    private static List<List<string>> ParseCSV(string csv)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> currentRow = new List<string>();
        string currentValue = "";

        bool insideQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];

            if (c == '"')
            {
                if (insideQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    currentValue += '"';
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (c == ',' && !insideQuotes)
            {
                currentRow.Add(currentValue);
                currentValue = "";
            }
            else if ((c == '\n' || c == '\r') && !insideQuotes)
            {
                if (c == '\r' &&
                    i + 1 < csv.Length &&
                    csv[i + 1] == '\n')
                {
                    i++;
                }

                currentRow.Add(currentValue);
                currentValue = "";

                rows.Add(currentRow);
                currentRow = new List<string>();
            }
            else
            {
                currentValue += c;
            }
        }
        if (currentValue.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentValue);
            rows.Add(currentRow);
        }
        return rows;
    }
}