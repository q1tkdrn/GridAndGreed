using UnityEngine;

/// <summary>
/// 저장 데이터 전체에 대한 판정. 개별 데이터는 기존 PlayerPrefs 키를 그대로 쓴다.
/// </summary>
public static class SaveData
{
    private const string StartedKey = "Save_Started";

    /// <summary>
    /// 이어할 데이터가 있는지. 이 키가 생기기 전 버전의 저장 데이터도 인정하기 위해
    /// 마을에 도착할 때마다 기록되던 mainCurrentIndex도 확인한다.
    /// </summary>
    public static bool HasProgress => PlayerPrefs.HasKey(StartedKey) || PlayerPrefs.HasKey("mainCurrentIndex");

    public static void MarkStarted() => PlayerPrefs.SetInt(StartedKey, 1);
}
