using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class BattleDisplayManager : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject entrancePanel;
    [SerializeField] private GameObject boardPanel;
    [SerializeField] private GameObject teamBuildingPanel;
    [Space]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private GameObject waysPanel;
    
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI textBox;
    [SerializeField] private float delay = 0.1f;
    [Space(1)]
    
    [Header("Reaper")]
    public int reaperMaxHp;
    public int reaperCurrentHp;
    [SerializeField] private TextMeshProUGUI reaperHp;
    
    [Header("Boss")]
    public int bossMaxHp;
    public int bossCurrentHp;
    [SerializeField] private TextMeshProUGUI bossHp;
    
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
        boardPanel.SetActive(true);
    }
    
    public void OpenTeamBuilding()
    {
        entrancePanel.SetActive(false);
        teamBuildingPanel.SetActive(true);
        boardPanel.SetActive(false);
    }
    
    public void OpenEntrancePanel()
    {
        entrancePanel.SetActive(true);
        teamBuildingPanel.SetActive(false);
        boardPanel.SetActive(false);
    }
    
    
    public void PrintText(string text)
    {
        StartCoroutine(TypeEffect(text));
    }

    IEnumerator TypeEffect(string text)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(textBox.text+"\n");
        for (int i = 0; i < text.Length; i++)
        {
            stringBuilder.Append(text[i]);
            textBox.text = stringBuilder.ToString();
            yield return new WaitForSeconds(delay);
        }
    }

    public void UpdateReaperHp(int hp)
    {
        reaperCurrentHp = hp;
        reaperHp.text = $"{reaperCurrentHp} / {reaperMaxHp}";
    }
    
    public void UpdateBossHp(int hp)
    {
        bossCurrentHp = hp;
        bossHp.text = $"{bossCurrentHp} / {bossMaxHp}";
    }
    
    public void ShowVictoryPanel()
    {
        victoryPanel.SetActive(true);
        StartCoroutine(ClosePanel(victoryPanel));
    }

    public void ShowDefeatPanel()
    {
        defeatPanel.SetActive(true);
        StartCoroutine(ClosePanel(defeatPanel));
    }
    public void ShowRewardPanel()
    {
        rewardPanel.SetActive(true);
        StartCoroutine(ClosePanel(rewardPanel));
    }

    public void ShowWaysPanel()
    {
        waysPanel.SetActive(true);
        StartCoroutine(ClosePanel(waysPanel));
    }

    IEnumerator ClosePanel(GameObject panel)
    {
        yield return new WaitForSeconds(1f);
        panel.SetActive(false);
    }
}
