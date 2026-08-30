using System;
using UnityEngine;

[Serializable]
public struct Cut
{
    public Sprite image;
    public string[] texts;
}

[CreateAssetMenu(fileName = "CutScene", menuName = "CutScene")]
public class CutScene : ScriptableObject
{
    public string cutSceneName;
    public Cut[] cuts;
    public bool skippable = true;
}
