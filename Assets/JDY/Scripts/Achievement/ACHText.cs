using UnityEngine;

public class ACHText : MonoBehaviour
{
    public void Test()
    {
        AchievementManager.Instance.AddProgress("ACH-1", 1);
    }
}
