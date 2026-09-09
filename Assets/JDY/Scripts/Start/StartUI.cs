using UnityEngine;
using UnityEngine.UI;
public class StartUI : MonoBehaviour
{
    [SerializeField] private GameObject[] title;
    void Start()
    {
        bool isAllClear = AchievementManager.Instance.IsCompleted("ACH-31");
        title[0].SetActive(!isAllClear);
        title[1].SetActive(isAllClear);
    }
}
