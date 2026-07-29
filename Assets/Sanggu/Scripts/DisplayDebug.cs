
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleDisplayManager))]
public class DisplayDebug: Editor
{
    private bool _debugFoldOut;
    private bool _panelFoldOut;
    
    private int _inputReaperHp;
    private int _inputBossHp;
    private string _inputText;
    
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
                    manager.OpenEntrancePanel();
                }
                if (GUILayout.Button("Open Board Panel"))
                {
                    manager.OpenGameBoard();
                }
                if (GUILayout.Button("Open TeamBuilding Panel"))
                {
                    manager.OpenTeamBuilding();
                }
                if (GUILayout.Button("Show Victory Panel"))
                {
                    manager.ShowVictoryPanel();
                }
                if (GUILayout.Button("Show Defeat Panel"))
                {
                    manager.ShowDefeatPanel();
                }
                if (GUILayout.Button("Show Reward Panel"))
                {
                    manager.ShowRewardPanel();
                }
                if (GUILayout.Button("Show Ways Panel"))
                {
                    manager.ShowWaysPanel();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Text");
                _inputText = EditorGUILayout.TextArea(_inputText, GUILayout.ExpandHeight(true), GUILayout.Width(600));
            }
            if(GUILayout.Button("AppendText"))
                manager.PrintText(_inputText);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Reaper Hp");
                _inputReaperHp = EditorGUILayout.IntField(_inputReaperHp, GUILayout.Width(40));
                if(GUILayout.Button("UpdateReaperHp", GUILayout.Width(150)))
                {
                    manager.UpdateReaperHp(_inputReaperHp);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Boss Hp");
                _inputBossHp = EditorGUILayout.IntField(_inputBossHp, GUILayout.Width(40));
                if(GUILayout.Button("UpdateBossHp", GUILayout.Width(150)))
                {
                    manager.UpdateBossHp(_inputBossHp);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        serializedObject.ApplyModifiedProperties();
    }
}