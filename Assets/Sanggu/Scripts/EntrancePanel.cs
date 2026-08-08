using TMPro;
using UnityEngine;

public class EntrancePanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI choiceLeft;
    [SerializeField] private TextMeshProUGUI choiceRight;
    public bool isBuilding = false;

    public void Init()
    {
        if (!isBuilding)
        {
            choiceLeft.text = "여정";
            choiceRight.text = "편성";
        }
        else
        {
            choiceLeft.text = "캐릭터";
            choiceRight.text = "아이템";
        }
    }

    public void OnLeftButtonClick()
    {
        if (!isBuilding)
        {
            BattleDisplayManager.GetInstance().OpenGameBoard();
        }
        else
        {
            BattleDisplayManager.GetInstance().OpenUnitBuilding();
        }
    }

    public void OnRightButtonClick()
    {
        if (!isBuilding)
        {
            isBuilding = true;
            Init();
        }
        else
        {
            BattleDisplayManager.GetInstance().OpenItemBuilding();
        }
    }
}
