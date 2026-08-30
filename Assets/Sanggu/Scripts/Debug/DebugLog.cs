using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugLog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI log;
    [SerializeField] private ScrollRect scrollRect;
    
    private static DebugLog _instance;

    public static DebugLog GetInstance()
    {
        return _instance;
    }

    private void Init()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Awake()
    {
        Init();
        log.text = "";
    }

    public void Log(string text)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(log.text);
        var newLog = $"[{DateTime.Now:HH:mm:ss}] {text}";
        stringBuilder.Append(newLog+"\n");
        log.text = stringBuilder.ToString();
        Debug.Log(text);
        Canvas.ForceUpdateCanvases();
        StartCoroutine(ScrollToBottom());
    }
    
    private IEnumerator ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        yield return new WaitForSeconds(0.1f);

        Canvas.ForceUpdateCanvases();

        scrollRect.verticalNormalizedPosition = 0f;
    }
}
