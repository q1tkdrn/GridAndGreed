using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class DialogueCSVImporter
{
    private const string csvPath ="Assets/JDY/Data/CSV/Dialog.csv";
    private const string savePath = "Assets/JDY/Data/Dialog/DialogDatabase.asset";

    [MenuItem("Tools/Import Dialog From CSV")]
    public static void ImportDialog()
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError("Dialog.csv를 찾을 수 없습니다.");
            return;
        }

        string csv = File.ReadAllText(csvPath, Encoding.UTF8);
        List<string[]> rows = ParseCSV(csv);

        if (rows.Count <= 1)
        {
            Debug.LogError("데이터 없음");
            return;
        }

        DialogDatabase database = AssetDatabase.LoadAssetAtPath<DialogDatabase>(savePath);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<DialogDatabase>();

            string directory = Path.GetDirectoryName(savePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(database, savePath);
        }

        database.dialogues.Clear();

        string previousType = "";
        string previousTarget = "";
        string previousNpc = "";

        int previousFromPhase = -1;
        int previousToPhase = -1;

        int currentGroupId = -1;

        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];

            string id = row[0].Trim();
            string npcName = row[1].Trim();

            string typeText = row[2].Trim();
            string target = row[3].Trim();

            string fromPhaseText = row[4].Trim();
            string toPhaseText = row[5].Trim();

            string text = row[6].Trim();

            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (string.IsNullOrWhiteSpace(typeText))
            {
                typeText = previousType;
            }

            if (string.IsNullOrWhiteSpace(typeText))
            {
                Debug.LogWarning($"{id}: Type 누락");
                continue;
            }

            if (!Enum.TryParse(typeText, true, out DialogType dialogueType))
            {
                Debug.LogWarning($"{id}: Type 오류");
                continue;
            }

            if (!int.TryParse(fromPhaseText, out int fromPhase))
            {
                Debug.LogWarning($"{id}: FromPhase 오류");
                continue;
            }

            if (!int.TryParse(toPhaseText, out int toPhase))
            {
                Debug.LogWarning($"{id}: ToPhase 오류");
                continue;
            }

            string groupTarget = target;
            if (string.IsNullOrWhiteSpace(groupTarget))
            {
                groupTarget = previousTarget;
            }

            bool sameGroup = npcName == previousNpc && typeText.Equals(previousType, StringComparison.OrdinalIgnoreCase) &&
                groupTarget == previousTarget && fromPhase == previousFromPhase && toPhase == previousToPhase;

            if (!sameGroup)
            {
                currentGroupId++;
            }
 
            DialogData data = new DialogData();

            data.groupId = currentGroupId;

            data.id = id;
            data.npcName = npcName;
            data.type = dialogueType;

            data.target = target;

            data.fromPhase = fromPhase;
            data.toPhase = toPhase;
            data.text = text;

            database.dialogues.Add(data);


            previousNpc = npcName;
            previousType = typeText;

            if (!string.IsNullOrWhiteSpace(target))
            {
                previousTarget = target;
            }

            previousFromPhase = fromPhase;
            previousToPhase = toPhase;
        }

        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("완료");
    }
    private static List<string[]> ParseCSV(string csv)
    {
        List<string[]> result = new List<string[]>();

        List<string> currentRow = new List<string>();

        StringBuilder currentValue = new StringBuilder();

        bool insideQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];

            if (c == '"')
            {
                // "" → "
                if (
                    insideQuotes &&
                    i + 1 < csv.Length &&
                    csv[i + 1] == '"'
                )
                {
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (
                c == ',' &&
                !insideQuotes
            )
            {
                currentRow.Add(
                    currentValue.ToString()
                );

                currentValue.Clear();
            }
            else if (
                (c == '\n' || c == '\r') &&
                !insideQuotes
            )
            {
                if (
                    c == '\r' &&
                    i + 1 < csv.Length &&
                    csv[i + 1] == '\n'
                )
                {
                    i++;
                }

                currentRow.Add(
                    currentValue.ToString()
                );

                currentValue.Clear();


                if (currentRow.Count > 0)
                {
                    result.Add(
                        currentRow.ToArray()
                    );
                }

                currentRow =
                    new List<string>();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        if (
            currentValue.Length > 0 ||
            currentRow.Count > 0
        )
        {
            currentRow.Add(currentValue.ToString());

            result.Add(currentRow.ToArray());
        }

        return result;
    }
}