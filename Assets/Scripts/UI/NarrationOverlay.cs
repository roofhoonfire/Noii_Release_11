using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 나레이션 오버레이
/// - 싱글톤
/// - ShowRoutine(string text) 호출 → 텍스트 표시 → 유지 → 사라짐
/// </summary>
public class NarrationOverlay : MonoBehaviour
{
    public static NarrationOverlay Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text bodyText;

    [Header("Timing")]
    [SerializeField] private float holdSeconds = 2f;
    [SerializeField] private float fadeInSeconds = 0.2f;
    [SerializeField] private float fadeOutSeconds = 0.2f;

    private CanvasGroup cg;
    private Coroutine runningRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        cg = root.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = root.AddComponent<CanvasGroup>();

        root.SetActive(false);
        cg.alpha = 0f;
    }

    /// <summary>
    /// 외부에서 바로 호출 가능
    /// </summary>
    public void Show(string text)
    {
        if (runningRoutine != null)
            StopCoroutine(runningRoutine);

        runningRoutine = StartCoroutine(ShowRoutine(text));
    }

    /// <summary>
    /// 실제 코루틴
    /// </summary>
    public IEnumerator ShowRoutine(string text)
    {
        if (runningRoutine != null)
            StopCoroutine(runningRoutine);

        runningRoutine = StartCoroutine(Routine(text));
        yield return runningRoutine;
    }

    private IEnumerator Routine(string text)
    {
        bodyText.text = text;

        root.SetActive(true);

        // Fade In
        yield return Fade(0f, 1f, fadeInSeconds);

        // Hold
        yield return new WaitForSeconds(holdSeconds);

        // Fade Out
        yield return Fade(1f, 0f, fadeOutSeconds);

        root.SetActive(false);

        runningRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        cg.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
    }
}