using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class BattleDisplayManager : MonoBehaviour
{
    [Header("Panel")]
    public EntrancePanel entrancePanel;
    public BoardPanel boardPanel;
    [SerializeField] private GameObject unitBuildingPanel;
    public ItemBuildingPanel itemBuildingPanel;
    
    [Space]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private GameObject waysPanel;
    public UnitTemp[] currentUnits = new UnitTemp[3];
    public ItemData[] currentItems = new ItemData[3];
    
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
    
    public void OpenGameBoard()
    {
        entrancePanel.gameObject.SetActive(false);
        boardPanel.gameObject.SetActive(true);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(false);
    }
    
    public void OpenUnitBuilding()
    {
        entrancePanel.gameObject.SetActive(false);
        boardPanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(true);
        itemBuildingPanel.gameObject.SetActive(false);
    }
    
    public void OpenItemBuilding()
    {
        entrancePanel.gameObject.SetActive(false);
        boardPanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(true);
    }
    
    public void OpenEntrancePanel(bool isBuilding)
    {
        entrancePanel.gameObject.SetActive(true);
        boardPanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(false);
        entrancePanel.isBuilding = isBuilding;
        entrancePanel.Init();
    }
    
    public void ShowVictoryPanel()
    {
        victoryPanel.SetActive(true);
    }

    public void ShowDefeatPanel()
    {
        defeatPanel.SetActive(true);
    }

    public void ShowWaysPanel()
    {
        waysPanel.SetActive(true);
    }

    public void BackToVillage()
    {
        SceneChanger.GetInstance().LoadScene(6);
    }
}
