using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    public BossTemp bossKing;
    public BossTemp bossDeath1;
    public BossTemp bossDeath2;
    public Stage[] stages = new Stage[3];
    [SerializeField] private GameObject arrow;
    private int _decidedStage = -1;
    
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    public AudioClip buildingClip;
    public AudioClip endingClip;
    
    
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

    public void OnEnable()
    {
        PlayBGM(buildingClip);
    }
    
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
        
        if(bgmSource.clip != buildingClip) PlayBGM(buildingClip);
    }
    
    [DebugButton]
    public void ShowVictoryPanel()
    {
        victoryPanel.SetActive(true);
        if(appearedBoss.Count > 0) ClearBoss(appearedBoss[^1]);
    }

    [DebugButton]
    public void ShowDefeatPanel()
    {
        defeatPanel.SetActive(true);
        AchievementManager.Instance.AddProgress("ACH-7", 100);
        
    }

    [DebugButton]
    public void ShowWaysPanel()
    {
        victoryPanel.SetActive(false);
        if (appearedBoss.Count == 4)
        {
            var check = 0;
            if (currentItems.Count(i => i?.id == "22") > 0) check++;
            if (currentItems.Count(i => i?.id == "23") > 0) check++;
            if (currentUnits.Count(i => i?.id == 9) > 0) check++;
            var boss = check == 3 ? bossDeath1 : bossKing;
            if (check != 3)
            {
                AchievementManager.Instance.AddProgress("ACH-18", 1);
            } 
            appearedBoss.Add(boss);
            boardPanel.boss = boss;
            waysPanel.gameObject.SetActive(false);
            _decidedStage = -1;
            boardPanel.ShowCutScene();
            
            PlayerPrefs.SetInt("currentPhase", appearedBoss.Count);
            PlayerPrefs.Save();
            return;
        }
        
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
        PlayBGM(endingClip);
    }

    public void OnClickStage(int i)
    {
        if (_decidedStage == i)
        {
            var boss = stages[i].bossTemp;
            remainBoss.Remove(boss);
            appearedBoss.Add(boss);
            PlayerPrefs.SetInt("currentPhase", appearedBoss.Count);
            PlayerPrefs.Save();
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

    public void ClearBoss(BossTemp boss)
    {
        AchievementManager.Instance.AddProgress("ACH-2", 1);
        AchievementManager.Instance.AddProgress("ACH-8", 1);
        AchievementManager.Instance.AddProgress("ACH-9", 1);

        switch (boss.bossId)
        {
            case "king":
                PlayerPrefs.SetInt("IsEnding", 1);
                AchievementManager.Instance.AddProgress("ACH-19", 1);
                AchievementManager.Instance.AddProgress("ACH-20", 1);
                ShowCutScene("Ending1");
                if (PlayerPrefs.GetInt("IsEnding") == 0) PlayerPrefs.SetInt("IsEnding", 1);
                waysPanel.gameObject.SetActive(false);
                break;
            case "death2":
                Debug.Log("b");
                PlayerPrefs.SetInt("IsEnding", 2);
                AchievementManager.Instance.AddProgress("ACH-28", 1);
                AchievementManager.Instance.AddProgress("ACH-29", 1);
                ShowCutScene("Ending2");

                waysPanel.gameObject.SetActive(false);
                break;
            case "pope":
                AchievementManager.Instance.AddProgress("ACH-11", 1);
                break;
            case "noble":
                AchievementManager.Instance.AddProgress("ACH-12", 1);
                break;
            case "instructor":
                AchievementManager.Instance.AddProgress("ACH-13", 1);
                break;
            case "subject":
                AchievementManager.Instance.AddProgress("ACH-14", 1);
                break;
            case "secretary":
                AchievementManager.Instance.AddProgress("ACH-15", 1);
                break;
            case "fusion":
                AchievementManager.Instance.AddProgress("ACH-16", 1);
                break;
            case "door":
                AchievementManager.Instance.AddProgress("ACH-17", 1);
                break;
        }

        PlayerPrefs.Save();
    }

    [DebugButton]
    public void BackToVillage()
    {
        SceneManager.LoadScene("Main");
    }

    public void PlayBGM(AudioClip loopBgm, AudioClip introBgm = null)
    {
        if (introBgm != null)
        {
            StartCoroutine(PlayBGMEnumerator(loopBgm, introBgm));
        }
        else
        {
            bgmSource.clip = loopBgm;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    IEnumerator PlayBGMEnumerator(AudioClip loopBgm, AudioClip introBgm)
    {
        bgmSource.clip = introBgm;
        bgmSource.loop = false;
        bgmSource.Play();

        while (bgmSource.isPlaying)
        {
            yield return null;
        }
        
        bgmSource.clip = loopBgm;
        bgmSource.loop = true;
        bgmSource.Play();
    }
}
