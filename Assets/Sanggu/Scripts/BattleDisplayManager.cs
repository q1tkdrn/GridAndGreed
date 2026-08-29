using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct Stage
{
    public BossTemp bossTemp;
    public TextMeshProUGUI stageName;
    public Image stageImage;
}

public class BattleDisplayManager : MonoBehaviour
{
    [Header("Panel")]
    public EntrancePanel entrancePanel;
    public BoardPanel boardPanel;
    [SerializeField] private GameObject unitBuildingPanel;
    public ItemBuildingPanel itemBuildingPanel;
    public CutScenePanel cutScenePanel;
    
    [Space]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private Image waysPanel;
    public UnitTemp[] currentUnits = new UnitTemp[3];
    public ItemData[] currentItems = new ItemData[3];
    
    public List<BossTemp> remainBoss = new List<BossTemp>();
    public List<BossTemp> appearedBoss = new List<BossTemp>();
    public Stage[] stages = new Stage[3];
    [SerializeField] private GameObject arrow;
    private int _decidedStage = -1;
    
    
    //싱글톤-------------------------------------------------------------------------------
    private static BattleDisplayManager _instance;

    public static BattleDisplayManager GetInstance()
    {
        Init();
        return _instance;
    }

    static void Init()
    {
        if (_instance == null)
        {
            GameObject go = GameObject.Find("BattleDisplayManager");
            if (go == null)
            {
                go = new GameObject { name = "BattleDisplayManager" };
                go.AddComponent<BattleDisplayManager>();
            }

            _instance = go.GetComponent<BattleDisplayManager>();
        }
    }
    //-------------------------------------------------------------------------------싱글톤
    
    [DebugButton]
    public void OpenGameBoard()
    {
        entrancePanel.gameObject.SetActive(false);
        boardPanel.gameObject.SetActive(true);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(false);
        ShowWaysPanel();
    }
    
    [DebugButton]
    public void OpenUnitBuilding()
    {
        entrancePanel.gameObject.SetActive(false);
        boardPanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(true);
        itemBuildingPanel.gameObject.SetActive(false);
    }
    
    [DebugButton]
    public void OpenItemBuilding()
    {
        entrancePanel.gameObject.SetActive(false);
        boardPanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(true);
    }
    
    [DebugButton]
    public void OpenEntrancePanel(bool isBuilding)
    {
        entrancePanel.gameObject.SetActive(true);
        boardPanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(false);
        entrancePanel.isBuilding = isBuilding;
        entrancePanel.Init();
    }
    
    [DebugButton]
    public void ShowVictoryPanel()
    {
        victoryPanel.SetActive(true);
    }

    [DebugButton]
    public void ShowDefeatPanel()
    {
        defeatPanel.SetActive(true);
    }

    [DebugButton]
    public void ShowWaysPanel()
    {
        victoryPanel.SetActive(false);
        waysPanel.gameObject.SetActive(true);
        remainBoss.Shuffle();
        for (int i = 0; i < 3; i++)
        {
            var temp = remainBoss[i];
            if (remainBoss.Count < i + 1)
            {
                temp = remainBoss[0];
            }
            stages[i].bossTemp = temp;
            stages[i].stageImage.sprite = temp.stageSprite;
            stages[i].stageName.text = temp.stageName;
        }
    }

    [DebugButton]
    public void ShowCutScene(string cutsceneName)
    {
        cutScenePanel.gameObject.SetActive(true);
        cutScenePanel.SetCutScene(cutsceneName);
    }

    public void OnClickStage(int i)
    {
        if (_decidedStage == i)
        {
            var boss = stages[i].bossTemp;
            remainBoss.Remove(boss);
            appearedBoss.Add(boss);
            boardPanel.boss = boss;
            arrow.SetActive(false);
            waysPanel.color = new Color(255, 255, 255, 250);
            waysPanel.gameObject.SetActive(false);
            _decidedStage = -1;
            boardPanel.ShowCutScene();
            return;
        }
        
        _decidedStage = i;
        arrow.SetActive(true);
        var vector3 = arrow.transform.position;
        vector3.x = stages[i].stageImage.transform.position.x;
        arrow.transform.position = vector3;
    }
    

    [DebugButton]
    public void BackToVillage()
    {
        SceneChanger.GetInstance().LoadScene(6);
    }
}
