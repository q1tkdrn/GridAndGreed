using System.Text.RegularExpressions;

/// <summary>
/// 음악 파일 이름 → 마리골드 음악 화면에 보이는 제목 (JSW, 2026-11-04).
/// 앞의 분류어('마을', '캐릭터 상점 OST 2 -' 등)와 '(Loopable)'을 뗀다. 같은 곡의 다른 버전 표시('(Guitar Version)')는 남긴다.
/// 대사(Dialog.csv의 Target)도 이 제목을 열쇠로 쓴다. 파일 이름은 바꾸지 않는다.
/// </summary>
public static class MusicTitle
{
    public static string Of(UnityEngine.AudioClip clip) => clip == null ? "" : Of(clip.name);

    public static string Of(string fileName)
    {
        string s = fileName ?? "";
        s = Regex.Replace(s, @"\s*\(Loopable\)", "");
        int ost = s.IndexOf("OST ");
        if (ost >= 0)
        {
            int dash = s.IndexOf(" - ", ost);
            if (dash >= 0) s = s.Substring(dash + 3);
        }
        s = Regex.Replace(s, @"^마을\s+", "");
        return s.Trim();
    }
}
