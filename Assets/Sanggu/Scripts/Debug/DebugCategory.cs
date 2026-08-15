using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugCategory : MonoBehaviour
{
    public TextMeshProUGUI categoryName;
    public GameObject arrow;
    public List<GameObject> contents;
    public bool foldout = false;

    public void OnClick()
    {
        foldout = !foldout;
        foreach (GameObject go in contents)
        {
            go.SetActive(foldout);
        }
        var rotation = foldout ? 0f : 90f;
        arrow.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }
}
