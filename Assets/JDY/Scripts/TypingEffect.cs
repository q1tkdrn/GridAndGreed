using UnityEngine;
using TMPro;
using System.Collections;
public class TypingEffect : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text text;
    [Header("Test")]
    [SerializeField] private float typingSpeed = 0.04f;
    public void StartTyping(string message, System.Action onComplete = null)
    {
        StartCoroutine(TypeText(message, onComplete));
    }
    IEnumerator TypeText(string message, System.Action onComplete)
    {
        text.text = "";

        foreach (char c in message)
        {
            text.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        onComplete?.Invoke();
    }
}