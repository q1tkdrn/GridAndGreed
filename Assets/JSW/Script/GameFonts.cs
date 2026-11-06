using TMPro;
using UnityEngine;

/// <summary>
/// 게임 글꼴 한곳 모음 (2026-11-06). 코드에서 글자를 만들 때는 여기서 가져온다.
/// - Body: 고운바탕 Regular — 본문·설명
/// - Bold: 고운바탕 Bold — 버튼·이름·숫자 (TMP 기본 글꼴이기도 함)
/// - Display: 연성 — 제목·배너 같은 짧고 큰 글자
/// 글꼴 파일: Assets/JSW/Fonts/Source (OFL, 상업 이용 가능). 글꼴 자산: Assets/JSW/Resources/Fonts
/// 이전 글꼴(부크크 명조)은 빠진 글자를 채우는 대체 글꼴로만 남아 있다.
/// </summary>
public static class GameFonts
{
    private static TMP_FontAsset _body, _bold, _display;

    public static TMP_FontAsset Body => _body != null ? _body : _body = Resources.Load<TMP_FontAsset>("Fonts/GowunBatang-Regular SDF");
    public static TMP_FontAsset Bold => _bold != null ? _bold : _bold = Resources.Load<TMP_FontAsset>("Fonts/GowunBatang-Bold SDF");
    public static TMP_FontAsset Display => _display != null ? _display : _display = Resources.Load<TMP_FontAsset>("Fonts/YeonSung-Regular SDF");
}
