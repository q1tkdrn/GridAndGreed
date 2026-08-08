
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleDisplayManager))]
public class DisplayDebug: Editor
{
    private static bool _debugFoldOut;
    private static bool _panelFoldOut;
    private static bool _boardFoldOut;
    private static bool _inventoryFoldOut;
    
    private int _inputReaperHp;
    private int _inputBossHp;
    private int _inputActionPoint;
    private string _inputText;
    private string _inputItemId;
    private int _inputSoul;
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        _debugFoldOut = EditorGUILayout.BeginFoldoutHeaderGroup(_debugFoldOut, "Debug");

        if (_debugFoldOut)
        {
            BattleDisplayManager manager = (BattleDisplayManager)target;
            EditorGUI.indentLevel++;
            _panelFoldOut = EditorGUILayout.Foldout(_panelFoldOut, "Panel");
            if (_panelFoldOut)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (GUILayout.Button("Open Entrance Panel"))
                {
                    manager.OpenEntrancePanel(false);
                }
                if (GUILayout.Button("Open Board Panel"))
                {
                    manager.OpenGameBoard();
                }
                if (GUILayout.Button("Open UnitBuilding Panel"))
                {
                    manager.OpenUnitBuilding();
                }
                if (GUILayout.Button("Open ItemBuilding Panel"))
                {
                    manager.OpenItemBuilding();
                }
                if (GUILayout.Button("Show Victory Panel"))
                {
                    manager.ShowVictoryPanel();
                }
                if (GUILayout.Button("Show Defeat Panel"))
                {
                    manager.ShowDefeatPanel();
                }
                if (GUILayout.Button("Show Ways Panel"))
                {
                    manager.ShowWaysPanel();
                }
                EditorGUILayout.EndVertical();
            }
            _inventoryFoldOut = EditorGUILayout.Foldout(_inventoryFoldOut, "Inventory");
            if (_inventoryFoldOut)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                using (new  EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Soul");
                    _inputSoul = EditorGUILayout.IntField(_inputSoul, GUILayout.Width(40));
                    if (GUILayout.Button("Add"))
                    {
                        InventoryManager.Instance.AddSoul(_inputSoul);
                    }

                    if (GUILayout.Button("Remove"))
                    {
                        InventoryManager.Instance.RemoveSoul(_inputSoul);
                    }
                }
                using (new  EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Item");
                    _inputItemId = EditorGUILayout.TextField(_inputItemId, GUILayout.Width(40));
                    if (GUILayout.Button("Add"))
                    {
                        InventoryManager.Instance.AddItem(_inputItemId);
                        manager.itemBuildingPanel.Init();
                    }

                    if (GUILayout.Button("Remove"))
                    {
                        InventoryManager.Instance.RemoveItem(_inputItemId);
                        manager.itemBuildingPanel.Init();
                    }
                }
                EditorGUILayout.EndVertical();
            }
            
            _boardFoldOut = EditorGUILayout.Foldout(_boardFoldOut, "Board");
            if (_boardFoldOut)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (GUILayout.Button("Next Turn"))
                {
                    manager.boardPanel.NextTurn();
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Text");
                    _inputText = EditorGUILayout.TextArea(_inputText, GUILayout.ExpandHeight(true));
                }
                if(GUILayout.Button("AppendText"))
                    manager.boardPanel.PrintText(_inputText);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Reaper Hp");
                    _inputReaperHp = EditorGUILayout.IntField(_inputReaperHp, GUILayout.Width(40));
                    if(GUILayout.Button("UpdateReaperHp", GUILayout.Width(150)))
                    {
                        manager.boardPanel.UpdateReaperHp(_inputReaperHp);
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Boss Hp");
                    _inputBossHp = EditorGUILayout.IntField(_inputBossHp, GUILayout.Width(40));
                    if(GUILayout.Button("UpdateBossHp", GUILayout.Width(150)))
                    {
                        manager.boardPanel.UpdateBossHp(_inputBossHp);
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Action Point");
                    _inputActionPoint = EditorGUILayout.IntField(_inputActionPoint, GUILayout.Width(40));
                    if (GUILayout.Button("UpdateActionPoint", GUILayout.Width(150)))
                    {
                        manager.boardPanel.UpdateActionPoint(_inputActionPoint);
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        serializedObject.ApplyModifiedProperties();
    }
}