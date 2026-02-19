using System.Collections;
using UnityEngine;
using Unity.Cinemachine;   // ✅ 조건부 제거하고 고정 사용

public class FinaleController : MonoBehaviour
{
    public static FinaleController Instance { get; private set; }

    [Header("Slow Motion")]
    [Range(0.01f, 1f)]
    [SerializeField] private float slowMoScale = 0.12f;

    [Header("KO UI (CanvasGroup Fade)")]
    [SerializeField] private GameObject koUIRoot;
    [SerializeField] private CanvasGroup koUICanvasGroup;
    [SerializeField] private float koFadeInSeconds = 0.25f;

    [Header("Texts Root (inactive -> active)")]
    [SerializeField] private GameObject textRoot;
    [SerializeField] private CanvasGroup textGroup1;
    [SerializeField] private CanvasGroup textGroup2;
    [SerializeField] private float textStartDelay = 1.0f;
    [SerializeField] private float textFadeInSeconds = 0.35f;

    [Header("Camera Zoom Out")]
    [SerializeField] private CinemachineCamera zoomTargetCamera; // 수동 지정도 가능
    [SerializeField] private Camera fallbackCamera;              // 혹시 모를 대비
    [SerializeField] private float targetFov = 40f;
    [SerializeField] private float zoomSeconds = 1.2f;

    public bool IsFinaleActive { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (koUIRoot != null) koUIRoot.SetActive(false);
        if (textRoot != null) textRoot.SetActive(false);

        InitGroup(koUICanvasGroup, 0f);
        InitGroup(textGroup1, 0f);
        InitGroup(textGroup2, 0f);
    }

    private static void InitGroup(CanvasGroup g, float alpha)
    {
        if (g == null) return;
        g.alpha = alpha;
    }

    public IEnumerator PlayFinale(
        Animator enemyVisualAnimator,
        AnimationClip enemyFinaleClip,
        System.Action onBeforeFreeze = null
    )
    {
        if (IsFinaleActive) yield break;
        IsFinaleActive = true;

        float prevScale = Time.timeScale;
        float prevFixed = Time.fixedDeltaTime;

        // 1️⃣ 슬로모션
        Time.timeScale = slowMoScale;
        Time.fixedDeltaTime = prevFixed * slowMoScale;

        // 2️⃣ KO UI 페이드 인
        if (koUIRoot != null) koUIRoot.SetActive(true);
        if (koUICanvasGroup != null)
        {
            koUICanvasGroup.alpha = 0f;
            StartCoroutine(FadeCanvasGroupUnscaled(koUICanvasGroup, 0f, 1f, koFadeInSeconds));
        }

        // 텍스트 초기화
        if (textRoot != null) textRoot.SetActive(false);
        InitGroup(textGroup1, 0f);
        InitGroup(textGroup2, 0f);

        // 3️⃣ 적 피날레 애니 재생 + 텍스트 예약
        if (enemyVisualAnimator != null && enemyFinaleClip != null)
        {
            enemyVisualAnimator.speed = 1f;
            enemyVisualAnimator.Play(enemyFinaleClip.name, 0, 0f);

            StartCoroutine(ShowTextsAfterDelayUnscaled(textStartDelay));
        }

        // 4️⃣ 🔥 현재 라이브 카메라 자동 바인딩
        if (zoomTargetCamera == null && BattleCameraDirector.Instance != null)
        {
            zoomTargetCamera = BattleCameraDirector.Instance.GetCurrentLiveCamera();
        }

        // 5️⃣ 줌아웃
        onBeforeFreeze?.Invoke();
        yield return ZoomOutUnscaled(targetFov, zoomSeconds);

        // 6️⃣ 적 애니 멈춤
        if (enemyVisualAnimator != null)
            enemyVisualAnimator.speed = 0f;

        // 7️⃣ 완전 정지 + Space 대기
        Time.timeScale = 0f;

        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;

        Debug.Log("야호");

        // 8️⃣ 복구
        Time.timeScale = prevScale;
        Time.fixedDeltaTime = prevFixed;

        if (enemyVisualAnimator != null)
            enemyVisualAnimator.speed = 1f;

        if (koUIRoot != null) koUIRoot.SetActive(false);
        if (textRoot != null) textRoot.SetActive(false);

        InitGroup(koUICanvasGroup, 0f);
        InitGroup(textGroup1, 0f);
        InitGroup(textGroup2, 0f);

        IsFinaleActive = false;
    }

    private IEnumerator ShowTextsAfterDelayUnscaled(float delay)
    {
        if (delay > 0f)
            yield return WaitUnscaled(delay);

        if (textRoot != null)
            textRoot.SetActive(true);

        if (textGroup1 != null)
        {
            textGroup1.alpha = 0f;
            StartCoroutine(FadeCanvasGroupUnscaled(textGroup1, 0f, 1f, textFadeInSeconds));
        }

        if (textGroup2 != null)
        {
            textGroup2.alpha = 0f;
            StartCoroutine(FadeCanvasGroupUnscaled(textGroup2, 0f, 1f, textFadeInSeconds));
        }
    }

    private static IEnumerator FadeCanvasGroupUnscaled(CanvasGroup g, float from, float to, float seconds)
    {
        if (g == null) yield break;

        float dur = Mathf.Max(0.0001f, seconds);
        float t = 0f;

        g.alpha = from;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }

        g.alpha = to;
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator ZoomOutUnscaled(float toFov, float seconds)
    {
        float dur = Mathf.Max(0.0001f, seconds);

        // 🎯 CinemachineCamera가 라이브일 경우
        if (zoomTargetCamera != null)
        {
            float from = zoomTargetCamera.Lens.FieldOfView;
            float t = 0f;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                zoomTargetCamera.Lens.FieldOfView = Mathf.Lerp(from, toFov, t / dur);
                yield return null;
            }

            zoomTargetCamera.Lens.FieldOfView = toFov;
            yield break;
        }

        // 혹시 fallback
        if (fallbackCamera != null)
        {
            float from = fallbackCamera.fieldOfView;
            float t = 0f;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                fallbackCamera.fieldOfView = Mathf.Lerp(from, toFov, t / dur);
                yield return null;
            }

            fallbackCamera.fieldOfView = toFov;
        }
    }
}
