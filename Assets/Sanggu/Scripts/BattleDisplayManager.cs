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
#if UNITY_EDITOR
    public enum DebugStartStage { Normal, AfterlifePhase1, AfterlifePhase2, King }
    [Header("Editor Play Test")]
    [Tooltip("BattleTemp 씬에서 Play할 때 지정한 보스로 바로 진입합니다. 실행 중에는 Debug의 보스별 버튼으로도 시작할 수 있습니다.")]
    public DebugStartStage debugStartStage;

    private void Start()
    {
        if (debugStartStage == DebugStartStage.AfterlifePhase1) DebugEnterAfterlife(1);
        else if (debugStartStage == DebugStartStage.AfterlifePhase2) DebugEnterAfterlife(2);
        else if (debugStartStage == DebugStartStage.King) DebugEnterKing();
    }

    public void DebugEnterAfterlife(int phaseNumber)
    {
        if (!Application.isPlaying) return;
        var targetBoss = phaseNumber == 2 ? bossDeath2 : bossDeath1;
        DebugEnterBoss(targetBoss);
    }

    public void DebugEnterKing()
    {
        if (!Application.isPlaying) return;
        DebugEnterBoss(bossKing);
    }

    public void DebugEnterBoss(BossTemp targetBoss)
    {
        if (!Application.isPlaying) return;
        if (targetBoss == null || currentUnits.Length != 3 || currentUnits.Any(unit => unit == null))
        {
            Debug.LogError("테스트할 보스와 기본 캐릭터 3명의 참조를 확인하세요.", this);
            return;
        }

        // 이전 전투/결과/컷신의 코루틴과 화면을 닫고 독립적인 테스트를 시작한다.
        boardPanel.gameObject.SetActive(false);
        cutScenePanel.gameObject.SetActive(false);
        entrancePanel.gameObject.SetActive(false);
        unitBuildingPanel.SetActive(false);
        itemBuildingPanel.gameObject.SetActive(false);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        waysPanel.gameObject.SetActive(false);
        arrow.SetActive(false);
        _decidedStage = -1;
        appearedBoss.Clear();
        appearedBoss.Add(targetBoss);
        boardPanel.boss = targetBoss;
        boardPanel.reaperCurrentHp = boardPanel.reaperMaxHp;
        // 전투 씬만 실행한 경우 인벤토리 없이 기본 3인으로 테스트한다.
        if (InventoryManager.Instance == null) currentItems = new ItemData[3];
        boardPanel.gameObject.SetActive(true);
        boardPanel.ShowCutScene();
    }
#endif

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
    public AudioSource bgmSourceLoop;
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
        RecordAchievement("ACH-7", 100);
        
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
                RecordAchievement("ACH-18", 1);
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
        if (boss == null) return;
        RecordAchievement("ACH-2", 1);
        RecordAchievement("ACH-8", 1);
        RecordAchievement("ACH-9", 1);

        switch (boss.bossId)
        {
            case "king":
                PlayerPrefs.SetInt("IsEnding", 1);
                RecordAchievement("ACH-19", 1);
                RecordAchievement("ACH-20", 1);
                ShowCutScene("Ending1");
                if (PlayerPrefs.GetInt("IsEnding") == 0) PlayerPrefs.SetInt("IsEnding", 1);
                waysPanel.gameObject.SetActive(false);
                break;
            case "death2":
                Debug.Log("b");
                PlayerPrefs.SetInt("IsEnding", 2);
                RecordAchievement("ACH-28", 1);
                RecordAchievement("ACH-29", 1);
                ShowCutScene("Ending2");

                waysPanel.gameObject.SetActive(false);
                break;
            case "pope":
                RecordAchievement("ACH-11", 1);
                break;
            case "noble":
                RecordAchievement("ACH-12", 1);
                break;
            case "instructor":
                RecordAchievement("ACH-13", 1);
                break;
            case "subject":
                RecordAchievement("ACH-14", 1);
                break;
            case "secretary":
                RecordAchievement("ACH-15", 1);
                break;
            case "fusion":
                RecordAchievement("ACH-16", 1);
                break;
            case "door":
                RecordAchievement("ACH-17", 1);
                break;
        }

        PlayerPrefs.Save();
    }

    private bool _warnedMissingAchievements;

    private void RecordAchievement(string id, int amount)
    {
        // 전투 씬을 직접 실행한 테스트에서도 결과 화면은 정상적으로 진행한다.
        var achievements = AchievementManager.Instance;
        if (achievements != null)
        {
            achievements.AddProgress(id, amount);
        }
        else if (!_warnedMissingAchievements)
        {
            _warnedMissingAchievements = true;
            Debug.LogWarning("업적 관리자가 없어 이번 전투 테스트의 업적 기록을 건너뜁니다. 업적 검증은 시작 씬부터 실행하세요.", this);
        }
    }

    [DebugButton]
    public void BackToVillage()
    {
        SceneManager.LoadScene("Main");
    }

    public void PlayBGM(AudioClip loopBgm, AudioClip introBgm = null)
    {
        bgmSourceLoop.loop = false;
        bgmSourceLoop.Stop();
        bgmSource.loop = false;
        bgmSource.Stop();
        
        if (introBgm != null)
        {
            var startTime = AudioSettings.dspTime + 0.1;
            bgmSource.clip = introBgm;
            bgmSource.loop = false;
            bgmSource.PlayScheduled(startTime);
        
            var loopStartTime = startTime + introBgm.length;
        
            bgmSourceLoop.clip = loopBgm;
            bgmSourceLoop.loop = true;
            bgmSourceLoop.PlayScheduled(loopStartTime);
        }
        else
        {
            bgmSource.clip = loopBgm;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }
}
