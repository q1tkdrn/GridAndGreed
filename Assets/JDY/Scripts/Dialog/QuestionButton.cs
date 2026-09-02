using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestionButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button button;

    private DialogData questionData;
    private QuestionUI questionUI;
    
    public void SetQuestion(DialogData data, QuestionUI ui)//2
    {
        questionData = data;
        questionUI = ui;

        questionText.text = data.target;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()//3
    {
        questionUI.SelectQuestion(questionData);
    }
}