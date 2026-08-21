using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
public class ItemCSVImporter
{
    [MenuItem("Tools/Import Items From CSV")]
    public static void ImportItems()
    {
        string csvPath = "Assets/JDY/Data/CSV/Data - Items.csv";
        string savePath = "Assets/JDY/Data/Items/";

        if (!File.Exists(csvPath))
        {
            Debug.LogError("Items.csv를 찾을 수 없습니다.");
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
            Debug.LogError("데이터 없음");
            return;
        }

        for (int i = 1; i < rows.Count; i++)
        {
            List<string> values = rows[i];

            if (values.Count == 0)
                continue;

            string id = values[0].Trim();
            string itemName = values[1].Trim();
            string priceText = values[2].Trim();
            string description = values[3];

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"{i + 1}번째 줄 ID 누락");
                continue;
            }

            if (!int.TryParse(priceText, out int price))
            {
                Debug.LogError($"{i + 1}번째 줄 price 오류");
                continue;
            }

            string assetPath = savePath + "Item_" + id + ".asset";

            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

            if (itemData == null)
            {
                itemData = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(itemData, assetPath);
                Debug.Log("생성: " + id);
            }
            else
            {
                Debug.Log("수정: " + id);
            }

            itemData.id = id;
            itemData.itemName = itemName;
            itemData.price = price;
            itemData.description = description;

            EditorUtility.SetDirty(itemData);
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