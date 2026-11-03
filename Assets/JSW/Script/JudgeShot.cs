using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 캐릭터(기본 스킨) 심판 연출: 6프레임 동작 → 무언가가 보스까지 날아가 닿음 → 그때 보스 피격.
// 그림은 Resources/Anim/<폴더>/ 에 있다. 캐릭터를 추가하려면 Profiles에 한 줄 넣으면 된다.
public static class JudgeShot
{
    public sealed class Profile
    {
        public int unitId;
        public int skin;                 // 0 기본, 1 곤충, 2 보스
        public string folder;            // Resources/Anim/ 아래 폴더
        public string framePrefix;       // 프레임 그림 이름 앞부분 (뒤에 _1 ~ _N)
        public float[] frameTimes;       // 프레임별 시간(초)
        public int releaseFrame;         // 0부터 센 번호. 이 그림이 나올 때 날아간다
        public string projectile;        // 날아가는 그림 이름 (끝이 오른쪽을 향함)
        public float canvasScale = 1f;   // 프레임 그림 크기 / 원래 캐릭터 그림 크기 (가운데 정렬)
        public Vector2 launchOffset;     // 출발점: 캐릭터 그림 가운데에서 폭·높이 비율만큼
        public float projectileWidth;    // 날아가는 그림 폭 (캐릭터 칸 폭 대비)
        public float flightTime;
        public float arcHeight;
        public bool stick;               // true: 박힌 채 머묾 (화살) / false: 닿으면 퍼지며 사라짐 (검기)
        public float shakeAmplitude = 14f;
        public float shakeDuration = 0.3f;
        public int projectileCount = 1;          // 한 번에 날리는 개수 (도적: 단검 2자루)
        public float projectileInterval = 0.07f; // 여러 개일 때 출발 간격
        public Vector2 projectileSpread;         // 여러 개일 때 출발점 차이 (폭·높이 비율)
        public float spin;                       // 날아가며 도는 속도(도/초). 0이면 날아가는 방향을 향함
        // 컷인(전방에 크게 보여주는 긴 동작). 그림이 없으면 컷인 없이 판 위 동작만 한다.
        public string cutPrefix;         // Resources/Anim/<폴더>/Cut/<이름>_1 ~ _N
        public float[] cutTimes;
        public int cutRelease;           // 0부터 센 번호. 이 그림에서 번쩍인다
    }

    public static readonly Profile[] Profiles =
    {
        // 궁수: 정확한 한 발. 끝까지 당긴 채 멈췄다가 짧게 놓는다.
        new Profile
        {
            unitId = 2, folder = "Archer", framePrefix = "archer_shoot",
            frameTimes = new[] { 0.05f, 0.08f, 0.2f, 0.04f, 0.08f, 0.1f }, releaseFrame = 3,
            projectile = "archer_arrow", launchOffset = new Vector2(0.24f, 0f), projectileWidth = 0.95f,
            flightTime = 0.18f, arcHeight = 40f, stick = true, shakeAmplitude = 14f, shakeDuration = 0.3f,
            // 컷인 11프레임 1.6초: 원래 자세 → 화살 꺼내기 → 당기기 → 조준(멈춤) → 떨림 → 놓음(번쩍) → 반동 → 내림
            cutPrefix = "archer_cut", cutRelease = 8,
            cutTimes = new[] { 0.12f, 0.11f, 0.11f, 0.12f, 0.10f, 0.12f, 0.28f, 0.20f, 0.08f, 0.16f, 0.20f },
        },
        // 기사: 무거운 한 방. 검을 높이 든 채 멈췄다가 내려베며 검기를 날린다.
        new Profile
        {
            unitId = 1, folder = "Knight", framePrefix = "knight_slash",
            frameTimes = new[] { 0.07f, 0.09f, 0.22f, 0.04f, 0.12f, 0.12f }, releaseFrame = 3,
            projectile = "knight_wave", canvasScale = 1.6f, launchOffset = new Vector2(0.3f, 0.05f), projectileWidth = 1.3f,
            flightTime = 0.22f, arcHeight = 0f, stick = false, shakeAmplitude = 24f, shakeDuration = 0.42f,
            // 컷인 10프레임 약 1.65초: 준비 → 양손 → 세워 듦 → 당김 → 들기 → 최대(멈춤) → 빛 → 베기(번쩍) → 숙임 → 복귀
            cutPrefix = "knight_cut", cutRelease = 7,
            cutTimes = new[] { 0.13f, 0.11f, 0.16f, 0.11f, 0.10f, 0.28f, 0.25f, 0.07f, 0.20f, 0.27f },
        },
        // 도적: 빠르고 가벼운 두 번. 단검 두 자루가 빙글빙글 돌며 날아가 "탁, 탁" 두 번 맞는다. (피해 계산은 한 번)
        new Profile
        {
            unitId = 3, folder = "Rogue", framePrefix = "rogue_throw",
            frameTimes = new[] { 0.04f, 0.06f, 0.12f, 0.03f, 0.07f, 0.08f }, releaseFrame = 4,
            projectile = "rogue_dagger", canvasScale = 1.28f, launchOffset = new Vector2(0.42f, 0.14f), projectileWidth = 0.75f,
            flightTime = 0.15f, arcHeight = 20f, stick = true, shakeAmplitude = 10f, shakeDuration = 0.2f,
            projectileCount = 2, projectileInterval = 0.07f, projectileSpread = new Vector2(0f, -0.14f), spin = 1800f,
            // 컷인 10프레임 약 1.6초: 노려봄 → 칼 던졌다 받기 → X자 → 웅크림 → 힘 모음(멈춤) → 눈 번뜩 → 던짐(번쩍) → 빈손 → 새 칼 뽑기 → 복귀
            cutPrefix = "rogue_cut", cutRelease = 6,
            // 던진 뒤 팔을 뻗은 모습을 충분히 보여 주도록 던짐 0.09초 + 팔 뻗음 0.32초, 대신 앞부분을 줄여 전체 약 1.6초 유지
            cutTimes = new[] { 0.13f, 0.15f, 0.10f, 0.10f, 0.20f, 0.15f, 0.09f, 0.32f, 0.14f, 0.25f },
        },
        // ── 아래는 그림이 아직 없는 연출 (2026-11-01 기획). 그림이 Resources/Anim/<폴더>/ 에 들어오면 자동으로 켜진다.
        // 파일 이름 규칙: <폴더 소문자>_act_1~6, <폴더 소문자>_shot, Cut/<폴더 소문자>_cut_1~10
        // 기사 곤충: 사슴벌레 뿔로 들이받기 → V자 충격파
        Heavy(1, 1, "Knight_Insect", flight: 0.2f),
        // 기사 보스(교관): 칼 끝으로 "교정" 찌르기 → 일직선 찌르기 충격
        Heavy(1, 2, "Knight_Boss", flight: 0.12f),
        // 궁수 곤충: 잠자리, 공중에서 침 발사
        Light(2, 1, "Archer_Insect"),
        // 궁수 보스(서기관): 책을 펼쳐 페이지 세 장이 화살처럼
        Light(2, 2, "Archer_Boss", count: 3, spin: 0f),
        // 도적 곤충: 사마귀, 앞발 낫 X자 → 낫 날 두 개 회전
        Light(3, 1, "Rogue_Insect", count: 2, spin: 1500f),
        // 도적 보스(실험체): 사슬 달린 단검 휘둘러 던지기
        Light(3, 2, "Rogue_Boss"),
        // 사제: 두 손 모아 기도 → 머리 위 빛 고리 → 십자 빛 구슬
        Magic(4, 0, "Bishop"),
        // 사제 곤충: 꿀벌, 날개 붕붕 → 꿀빛 구슬
        Magic(4, 1, "Bishop_Insect"),
        // 사제 보스(교황): 손을 들어 심판 → 빛의 창이 높게 날아가 내리꽂힘
        Heavy(4, 2, "Bishop_Boss", flight: 0.3f, arc: 140f, stick: true),
        // 무희: 빙글 회전 → 부채 두 개 던지기
        Light(5, 0, "Dancer", count: 2, spin: 1200f),
        // 무희 곤충: 나방, 날개 펄럭 → 나비 모양 빛
        Magic(5, 1, "Dancer_Insect"),
        // 무희 보스(융합체): 몸이 풀리며 붕대 리본이 뻗어 감김
        Light(5, 2, "Dancer_Boss", flight: 0.2f),
        // 스캐빈저: 갈고리를 머리 위로 돌리다 던지기
        Light(6, 0, "Scavenger", spin: 1000f),
        // 스캐빈저 곤충: 쇠똥구리, 공을 굴리다 높이 던지기
        Heavy(6, 1, "Scavenger_Insect", flight: 0.32f, arc: 120f, spin: 600f),
        // 스캐빈저 보스(영생귀족): 지팡이로 바닥을 치고 금화 세 닢 튕기기
        Light(6, 2, "Scavenger_Boss", count: 3, spin: 1400f),
        // 마법사: 지팡이 휘두르기 → 별 마법탄
        Magic(7, 0, "Wizard", spin: 720f),
        // 마법사 곤충: 더듬이에 번개를 모아 지그재그 번개
        Heavy(7, 1, "Wizard_Insect", flight: 0.1f),
        // 마법사 보스(문): 몸(문)이 열리고 안쪽 눈에서 빛
        Magic(7, 2, "Wizard_Boss"),
        // 광신도: 가슴 문양이 빛나며 떨어져 날아감
        Magic(8, 0, "Fanatic", spin: 240f),
        // 광신도 곤충: 등껍질을 펼쳐 문양 새긴 조각 세 개
        Light(8, 1, "Fanatic_Insect", count: 3, spin: 900f),
        // 광신도 보스(왕): 손가락을 튕기면 왕의 인장이 날아가 찍힘
        Heavy(8, 2, "Fanatic_Boss", flight: 0.26f, arc: 60f),
        // 꼬마 사신: 낫을 크게 돌려 베기 → 검은 반달 참격 (가장 크게)
        Heavy(9, 0, "Reaper", flight: 0.24f, width: 1.6f, shake: 28f),
    };

    // 새 연출 틀. 프레임 그림은 정리 도구가 같은 크기·같은 발 위치로 맞춰 주므로 canvasScale·launchOffset은 공통값에서 시작해 그림을 보고 조정한다.
    private static Profile Std(int unit, int skin, string folder, float[] times)
    {
        string key = folder.ToLowerInvariant();
        return new Profile
        {
            unitId = unit, skin = skin, folder = folder, framePrefix = key + "_act", frameTimes = times, releaseFrame = 3,
            projectile = key + "_shot", canvasScale = 1.4f, launchOffset = new Vector2(0.3f, 0.05f),
            // 컷인 10프레임 약 1.6초: 대기 → 준비 2 → 힘 모음 2 → 최대(멈춤) → 번뜩 → 발사(번쩍) → 여운 → 복귀
            cutPrefix = key + "_cut", cutRelease = 7,
            cutTimes = new[] { 0.13f, 0.11f, 0.12f, 0.11f, 0.12f, 0.28f, 0.20f, 0.08f, 0.22f, 0.25f },
        };
    }

    // 가볍고 빠름: 던지기·쏘기. 박힌 채 머묾.
    private static Profile Light(int unit, int skin, string folder, int count = 1, float spin = 0f, float flight = 0.16f)
    {
        var p = Std(unit, skin, folder, new[] { 0.05f, 0.07f, 0.16f, 0.04f, 0.08f, 0.10f });
        p.projectileWidth = 0.8f; p.flightTime = flight; p.arcHeight = 25f; p.stick = true;
        p.shakeAmplitude = 11f; p.shakeDuration = 0.22f; p.spin = spin;
        p.projectileCount = count; if (count > 1) p.projectileSpread = new Vector2(0f, -0.14f);
        return p;
    }

    // 마법: 모으는 시간이 길고 둥글게 날아가 퍼지며 사라짐.
    private static Profile Magic(int unit, int skin, string folder, float spin = 0f)
    {
        var p = Std(unit, skin, folder, new[] { 0.06f, 0.08f, 0.24f, 0.04f, 0.10f, 0.10f });
        p.projectileWidth = 0.9f; p.flightTime = 0.28f; p.arcHeight = 60f; p.stick = false;
        p.shakeAmplitude = 16f; p.shakeDuration = 0.3f; p.spin = spin;
        return p;
    }

    // 무거운 한 방: 크게 흔들림.
    private static Profile Heavy(int unit, int skin, string folder, float flight = 0.22f, float arc = 0f, bool stick = false, float spin = 0f, float width = 1.3f, float shake = 22f)
    {
        var p = Std(unit, skin, folder, new[] { 0.07f, 0.09f, 0.22f, 0.04f, 0.12f, 0.12f });
        p.projectileWidth = width; p.flightTime = flight; p.arcHeight = arc; p.stick = stick; p.spin = spin;
        p.shakeAmplitude = shake; p.shakeDuration = 0.4f;
        return p;
    }

    private const float StuckTime = 0.12f;
    private const float FadeTime = 0.15f;

    private static readonly Dictionary<Profile, Sprite[]> _frames = new Dictionary<Profile, Sprite[]>();
    private static readonly Dictionary<Profile, Sprite> _projectiles = new Dictionary<Profile, Sprite>();
    private static readonly Dictionary<Profile, Sprite[]> _cutFrames = new Dictionary<Profile, Sprite[]>();

    /// <summary>컷인 그림. 없으면 null.</summary>
    public static Sprite[] CutFrames(Profile p)
    {
        if (p == null || string.IsNullOrEmpty(p.cutPrefix) || p.cutTimes == null) return null;
        if (_cutFrames.TryGetValue(p, out var cached)) return cached;
        var frames = new Sprite[p.cutTimes.Length];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Resources.Load<Sprite>($"Anim/{p.folder}/Cut/{p.cutPrefix}_{i + 1}");
        bool ok = Array.TrueForAll(frames, f => f != null);
        if (!ok) Debug.LogWarning($"JudgeShot: Resources/Anim/{p.folder}/Cut 그림이 모자라서 {p.cutPrefix} 컷인을 건너뜀");
        return _cutFrames[p] = ok ? frames : null;
    }
    private static readonly List<GameObject> _live = new List<GameObject>();

    /// <summary>이 캐릭터·스킨에 심판 연출이 있으면 돌려준다. 그림이 하나라도 없으면 null. (skin: 0 기본, 1 곤충, 2 보스)</summary>
    public static Profile Find(int unitId, int skin = 0)
    {
        foreach (var p in Profiles)
            if (p.unitId == unitId && p.skin == skin) return Load(p) ? p : null;
        return null;
    }

    /// <summary>판 위 동작 프레임 (편성창 미리보기용). 그림이 모자라면 null.</summary>
    public static Sprite[] ActFrames(Profile p) => p != null && Load(p) ? _frames[p] : null;

    private static bool Load(Profile p)
    {
        if (_frames.TryGetValue(p, out var cached)) return cached != null;
        var frames = new Sprite[p.frameTimes.Length];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Resources.Load<Sprite>($"Anim/{p.folder}/{p.framePrefix}_{i + 1}");
        var projectile = Resources.Load<Sprite>($"Anim/{p.folder}/{p.projectile}");
        bool ok = Array.TrueForAll(frames, f => f != null) && projectile != null;
        _frames[p] = ok ? frames : null;
        _projectiles[p] = projectile;
        // 아직 그림을 하나도 넣지 않은 연출은 조용히 건너뛴다. 일부만 있으면 알려 준다.
        if (!ok && (frames[0] != null || projectile != null)) Debug.LogWarning($"JudgeShot: Resources/Anim/{p.folder} 그림이 모자라서 {p.framePrefix} 연출을 건너뜀");
        return ok;
    }

    /// <summary>전투 화면이 꺼질 때 날아가던 것을 지운다.</summary>
    public static void Cleanup()
    {
        foreach (var go in _live) if (go != null) UnityEngine.Object.Destroy(go);
        _live.Clear();
    }

    /// <summary>
    /// unit 그림을 프레임으로 바꿔 재생하고, 놓는 순간 target까지 날린다.
    /// 닿으면 onHit, 동작이 끝나면 onDone. 끝나면 원래 그림·크기로 되돌린다.
    /// </summary>
    public static IEnumerator Play(MonoBehaviour host, Profile p, Image unit, RectTransform target,
        Action onRelease, Action<int> onHit, Action onDone)
    {
        var frames = _frames[p];
        var original = unit.sprite;
        var rect = unit.rectTransform;
        var baseSize = rect.sizeDelta;
        // 프레임 그림이 원래보다 넓으면(검이 칸 밖으로 나가므로) 그만큼 키워서 원래 캐릭터 크기와 맞춘다.
        if (!Mathf.Approximately(p.canvasScale, 1f)) rect.sizeDelta = baseSize * p.canvasScale;
        try
        {
            for (int f = 0; f < frames.Length; f++)
            {
                unit.sprite = frames[f];
                if (f == p.releaseFrame)
                {
                    onRelease?.Invoke();
                    for (int j = 0; j < Mathf.Max(1, p.projectileCount); j++)
                    {
                        if (target != null) host.StartCoroutine(Fly(p, unit, baseSize, target, j, onHit));
                        else onHit?.Invoke(j);
                    }
                }
                yield return new WaitForSeconds(p.frameTimes[f]);
            }
        }
        finally
        {
            unit.sprite = original;
            rect.sizeDelta = baseSize;
            onDone?.Invoke();
        }
    }

    /// <param name="index">몇 번째로 날아가는지 (0부터). 두 번째부터는 조금 늦게 출발한다.</param>
    private static IEnumerator Fly(Profile p, Image unit, Vector2 unitSize, RectTransform target, int index, Action<int> onHit)
    {
        if (index > 0) yield return new WaitForSeconds(p.projectileInterval * index);
        var canvas = unit != null && unit.canvas != null ? unit.canvas.rootCanvas : null;
        if (canvas == null) { onHit?.Invoke(index); yield break; }
        var root = (RectTransform)canvas.transform;
        var sprite = _projectiles[p];

        var go = new GameObject(p.projectile, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = unit.gameObject.layer;
        _live.RemoveAll(x => x == null);
        _live.Add(go);
        var rect = (RectTransform)go.transform;
        rect.SetParent(root, false);
        rect.SetAsLastSibling();
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        // 화살은 촉 끝이 위치점(박힌 자리), 검기는 가운데가 위치점
        // 도는 것은 가운데를 중심으로 돌아야 자연스럽다
        rect.pivot = p.stick && p.spin == 0f ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 0.5f);
        float width = unitSize.x * p.projectileWidth;
        rect.sizeDelta = new Vector2(width, width * sprite.rect.height / sprite.rect.width);

        var unitRect = unit.rectTransform;
        Vector2 start = ToRoot(root, unitRect, unitRect.rect.center + Vector2.Scale(unitSize, p.launchOffset + p.projectileSpread * index));
        Vector2 end = ToRoot(root, target, target.rect.center + new Vector2(-target.rect.width * 0.12f, target.rect.height * (0.05f - 0.04f * index)));
        float arc = Mathf.Min(p.arcHeight, Vector2.Distance(start, end) * 0.08f);

        Vector2 prev = start;
        rect.anchoredPosition = start;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
        for (float t = 0f; t < p.flightTime; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float k = t / p.flightTime;
            Vector2 pos = Vector2.Lerp(start, end, k) + Vector2.up * (4f * arc * k * (1f - k));
            rect.anchoredPosition = pos;
            Vector2 dir = pos - prev;
            if (p.spin != 0f) rect.localRotation = Quaternion.Euler(0f, 0f, -p.spin * t);
            else if (dir.sqrMagnitude > 0.01f) rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            prev = pos;
            yield return null;
        }
        if (go == null) yield break;
        rect.anchoredPosition = end;
        // 돌던 것은 박히는 순간 날아온 방향으로 꽂힌다
        if (p.spin != 0f) rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);

        onHit?.Invoke(index);

        if (p.stick) yield return new WaitForSeconds(StuckTime);
        var size = rect.sizeDelta;
        for (float t = 0f; t < FadeTime && go != null; t += Time.deltaTime)
        {
            float k = t / FadeTime;
            var c = image.color;
            c.a = 1f - k;
            image.color = c;
            if (!p.stick) rect.sizeDelta = size * (1f + 0.4f * k); // 검기는 부딪히며 퍼진다
            yield return null;
        }
        if (go != null) UnityEngine.Object.Destroy(go);
    }

    private static Vector2 ToRoot(RectTransform root, RectTransform from, Vector2 localPoint)
    {
        Vector3 local = root.InverseTransformPoint(from.TransformPoint(localPoint));
        return new Vector2(local.x, local.y) - root.rect.center;
    }
}
