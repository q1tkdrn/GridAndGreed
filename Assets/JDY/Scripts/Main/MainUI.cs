using UnityEngine;
public class MainUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject[] buttonPanel;
    [Header("Test")]
    [SerializeField] private int currentRound;
    void Start()
    {
        for (int i = 0; i < buttonPanel.Length; i++)
        {
            buttonPanel[i].SetActive(false);
        }
        currentRound = PlayerPrefs.GetInt("currentRound", 1);
        if (currentRound == 1)
            buttonPanel[0].SetActive(true);
        
        if (currentRound == 2)
            buttonPanel[1].SetActive(true);
    }
}
