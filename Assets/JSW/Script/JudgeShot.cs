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
    };

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

    /// <summary>이 캐릭터(기본 스킨)에 심판 연출이 있으면 돌려준다. 그림이 하나라도 없으면 null.</summary>
    public static Profile Find(int unitId)
    {
        foreach (var p in Profiles)
            if (p.unitId == unitId) return Load(p) ? p : null;
        return null;
    }

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
        if (!ok) Debug.LogWarning($"JudgeShot: Resources/Anim/{p.folder} 그림이 모자라서 {p.framePrefix} 연출을 건너뜀");
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
