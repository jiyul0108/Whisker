using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 고양이 시야 (고양이 오브젝트에 붙임)
// 화면 전체를 어둡게 하고(전체 조명), 고양이 눈에서 바라보는 쪽으로 원뿔 모양 빛(자동차 라이트처럼)을 비춘다
// 고양이 둘레에도 작은 빛이 있어서 바로 옆(주인 등)은 보인다
// 연출 장면(철문 열림, 존재가 떨어짐)은 Reveal()로 그 자리를 잠깐 밝힌다
// 빛은 실행할 때 코드가 직접 만든다 (URP 2D Light 2D 사용, 그림 재질은 Sprite-Lit-Default여야 함)
[RequireComponent(typeof(CatController))]
public class CatVision : MonoBehaviour
{
    public static CatVision Instance { get; private set; }

    [Header("전체 어둠 (0 = 완전히 깜깜, 1 = 원래 밝기)")]
    [SerializeField, Range(0f, 1f)] private float ambientIntensity = 0.08f;
    [SerializeField] private Color ambientColor = new Color(0.55f, 0.6f, 0.75f);

    [Header("원뿔 시야 (눈에서 바라보는 쪽)")]
    // 눈 위치: 고양이 그림 안에서의 비율 (x: 가운데 0 ~ 앞쪽 끝 1, y: 발밑 0 ~ 머리 끝 1)
    // cat_meow_01 기준 눈은 가운데에서 앞쪽으로 58%, 발밑에서 69% 높이
    [SerializeField] private Vector2 eyeInSprite = new Vector2(0.58f, 0.69f);
    [SerializeField] private float coneRange = 7f;                            // 빛이 닿는 거리
    [SerializeField, Range(10f, 180f)] private float coneAngle = 70f;         // 원뿔 전체 각도
    [SerializeField, Range(0f, 1f)] private float coneSoftEdge = 0.5f;        // 가장자리 부드러움 (클수록 부드러움)
    [SerializeField] private float coneIntensity = 1.1f;
    [SerializeField] private Color coneColor = new Color(1f, 0.96f, 0.88f);

    [Header("고양이 둘레 빛")]
    [SerializeField] private float nearRadius = 1.7f;
    [SerializeField] private float nearIntensity = 0.55f;

    [Header("숨어 있을 때 (원뿔이 꺼지고 둘레 빛만 좁게)")]
    [SerializeField] private float hiddenNearRadius = 1.1f;
    [SerializeField] private float hiddenNearIntensity = 0.35f;
    [SerializeField] private float hideFadeSpeed = 4f;

    [Header("연출용 빛 (Reveal)")]
    [SerializeField] private float revealIntensity = 0.9f;

    private CatController cat;
    private SpriteRenderer body;   // 자식 Visual의 그림 (눈 위치 계산용)
    private Light2D cone, near, global;

    void Awake()
    {
        Instance = this;
        cat = GetComponent<CatController>();
        body = GetComponentInChildren<SpriteRenderer>();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        global = FindOrMakeGlobal();
        global.intensity = ambientIntensity;
        global.color = ambientColor;

        cone = MakeLight("CatVision_Cone", Light2D.LightType.Point, coneColor, coneIntensity);
        cone.pointLightInnerRadius = 0.4f;
        cone.pointLightOuterRadius = coneRange;
        cone.pointLightOuterAngle = coneAngle;
        cone.pointLightInnerAngle = coneAngle * (1f - coneSoftEdge);
        cone.falloffIntensity = 0.6f;

        near = MakeLight("CatVision_Near", Light2D.LightType.Point, coneColor, nearIntensity);
        near.pointLightInnerRadius = 0.2f;
        near.pointLightOuterRadius = nearRadius;
        near.falloffIntensity = 0.7f;

        Place();
    }

    void LateUpdate()
    {
        if (cone == null) return;
        Place();

        // 숨으면 원뿔 시야가 꺼지고, 둘레 빛도 좁고 어두워짐 (상자 틈으로 보는 느낌)
        bool hidden = HideController.IsHidden;
        float dt = hideFadeSpeed * Time.unscaledDeltaTime;
        cone.intensity = Mathf.MoveTowards(cone.intensity, hidden ? 0f : coneIntensity, coneIntensity * dt);
        near.pointLightOuterRadius = Mathf.MoveTowards(near.pointLightOuterRadius, hidden ? hiddenNearRadius : nearRadius, nearRadius * dt);
        near.intensity = Mathf.MoveTowards(near.intensity, hidden ? hiddenNearIntensity : nearIntensity, nearIntensity * dt);
    }

    // 눈 위치에 원뿔을 두고, 바라보는 쪽으로 바로 방향을 바꿈 (돌아볼 때 돌아가는 동작 없이 즉시 반전)
    // 빛은 기본으로 위(+Y)를 비춤 → 오른쪽은 -90도, 왼쪽은 +90도
    private void Place()
    {
        int facing = cat.Facing;
        Vector3 eye = EyePosition(facing);
        cone.transform.SetPositionAndRotation(eye, Quaternion.Euler(0f, 0f, facing > 0 ? -90f : 90f));
        near.transform.position = eye;
    }

    private Vector3 EyePosition(int facing)
    {
        if (body == null || body.sprite == null) return transform.position + Vector3.up * 0.6f;
        Bounds b = body.bounds;
        return new Vector3(b.center.x + facing * b.extents.x * eyeInSprite.x,
                           b.min.y + b.size.y * eyeInSprite.y, 0f);
    }

    // ---------- 연출용: 정해진 자리를 잠깐 밝힘 ----------
    public static void Reveal(Vector3 pos, float radius, float seconds)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.RevealRoutine(pos, radius, seconds));
    }

    private IEnumerator RevealRoutine(Vector3 pos, float radius, float seconds)
    {
        var l = MakeLight("CatVision_Reveal", Light2D.LightType.Point, coneColor, 0f);
        l.transform.SetParent(null);
        l.transform.position = pos;
        l.pointLightInnerRadius = radius * 0.3f;
        l.pointLightOuterRadius = radius;
        l.falloffIntensity = 0.6f;

        const float fade = 0.4f;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Min(1f, t / fade) * Mathf.Min(1f, (seconds - t) / fade);
            l.intensity = revealIntensity * Mathf.Clamp01(a);
            yield return null;
        }
        Destroy(l.gameObject);
    }

    // ---------- 빛 만들기 ----------
    // 다른 오브젝트에 붙는 동그란 빛 (예: 어둠 속에서도 존재가 희미하게 보이게). 처음엔 꺼져 있음(세기 0)
    public static Light2D AttachGlow(Transform parent, Vector3 localPos, Color color, float radius)
    {
        var l = MakeLight(parent, "Glow", Light2D.LightType.Point, color, 0f);
        l.transform.localPosition = localPos;
        l.pointLightInnerRadius = radius * 0.2f;
        l.pointLightOuterRadius = radius;
        l.falloffIntensity = 0.7f;
        return l;
    }

    private Light2D MakeLight(string name, Light2D.LightType type, Color color, float intensity)
        => MakeLight(transform, name, type, color, intensity);

    private static Light2D MakeLight(Transform parent, string name, Light2D.LightType type, Color color, float intensity)
    {
        var go = new GameObject(name);
        go.SetActive(false);   // 설정을 다 넣은 뒤 켬
        go.transform.SetParent(parent, false);
        var l = go.AddComponent<Light2D>();
        SetLightType(l, type);
        l.color = color;
        l.intensity = intensity;
        LightAllLayers(l);
        go.SetActive(true);
        return l;
    }

    // 꺼져 있는 빛에 lightType을 바로 넣으면 URP 내부에서 에러가 나서, 저장되는 값을 직접 바꿈 (켜질 때 적용됨)
    private static void SetLightType(Light2D l, Light2D.LightType type)
    {
        var f = typeof(Light2D).GetField("m_LightType", BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(l, type);
        else Debug.LogWarning("CatVision: 빛 종류를 설정하지 못했어요");
    }

    private Light2D FindOrMakeGlobal()
    {
        foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (l.lightType == Light2D.LightType.Global) return l;

        var go = new GameObject("Global Light 2D");
        go.SetActive(false);
        var g = go.AddComponent<Light2D>();
        SetLightType(g, Light2D.LightType.Global);
        LightAllLayers(g);
        go.SetActive(true);
        return g;
    }

    // 코드로 만든 빛도 모든 정렬 레이어(배경·캐릭터·소품)를 비추게
    private static void LightAllLayers(Light2D l)
    {
        var layers = SortingLayer.layers;
        var ids = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
        var f = typeof(Light2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(l, ids);
        else Debug.LogWarning("CatVision: 빛의 정렬 레이어를 설정하지 못했어요. Light 2D의 Target Sorting Layers를 확인해 주세요.");
    }
}
