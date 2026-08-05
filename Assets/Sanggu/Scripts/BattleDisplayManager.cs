using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class BattleDisplayManager : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject entrancePanel;
    public BoardPanel boardPanel;
    [SerializeField] private GameObject teamBuildingPanel;
    [Space]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private GameObject waysPanel;
    public UnitTemp[] currentUnits = new UnitTemp[3];

    
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
        entrancePanel.SetActive(false);
        teamBuildingPanel.SetActive(false);
        boardPanel.gameObject.SetActive(true);
    }
    
    public void OpenTeamBuilding()
    {
        entrancePanel.SetActive(false);
        teamBuildingPanel.SetActive(true);
        boardPanel.gameObject.SetActive(false);
    }
    
    public void OpenEntrancePanel()
    {
        entrancePanel.SetActive(true);
        teamBuildingPanel.SetActive(false);
        boardPanel.gameObject.SetActive(false);
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
        SceneChanger.GetInstance().LoadScene(3);
    }
}
