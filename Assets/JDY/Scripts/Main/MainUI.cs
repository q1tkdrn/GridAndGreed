using UnityEngine;
public class MainUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject[] buttonPanel;
    [Header("Test")]
    [SerializeField] private int isEnding;
    void Start()
    {
        for (int i = 0; i < buttonPanel.Length; i++)
        {
            buttonPanel[i].SetActive(false);
        }
        int isEnding = PlayerPrefs.GetInt("IsEnding", 0);
        if (isEnding == 2)
        {
            buttonPanel[1].SetActive(true);
        }
        else
        {
            buttonPanel[0].SetActive(true);
        }
    }
}
