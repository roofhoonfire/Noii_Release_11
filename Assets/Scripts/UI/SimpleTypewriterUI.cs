using System.Collections;
using UnityEngine;
using TMPro;

public class SimpleTypewriterUI : MonoBehaviour
{
    public static SimpleTypewriterUI Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject panelRoot;   // ▶ 오브젝트 A (전체 말풍선 패널)
    [SerializeField] private TMP_Text messageLabel;  // ▶ 오브젝트 B (TMP 텍스트)
    [SerializeField] private Animator panelAnimator; // ▶ A에 달린 Animator (done 트리거용)

    [Header("Typing")]
    [Tooltip("글자당 대기 시간(초)")]
    [SerializeField] private float secondsPerChar = 0.03f;
    [Tooltip("TimeScale 무시하고 싶으면 체크")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("After Typing")]
    [Tooltip("타이핑 완료 후, done 애니메이션 시작 전에 기다릴 시간(초)")]
    [SerializeField] private float waitBeforeDoneAnim = 1.5f;

    [Header("Done Animation")]
    [Tooltip("타이핑이 끝난 후 쏠 트리거 이름")]
    [SerializeField] private string doneTriggerName = "done";
    [Tooltip("done 애니메이션 재생 시간(초). 끝나고 패널 비활성화됨")]
    [SerializeField] private float doneAnimDuration = 0.4f;

    private Coroutine _currentRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Play(string message)
    {
        if (_currentRoutine != null)
            StopCoroutine(_currentRoutine);

        _currentRoutine = StartCoroutine(TypeAndCloseRoutine(message));
    }

    public IEnumerator TypeAndCloseRoutine(string message)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (messageLabel != null)
            messageLabel.text = string.Empty;

        float delay = Mathf.Max(0.001f, secondsPerChar);

        // --- 타이핑 ---
        foreach (char c in message)
        {
            if (messageLabel != null)
                messageLabel.text += c;

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(delay);
            else
                yield return new WaitForSeconds(delay);
        }

        // --- 타이핑 끝난 뒤 잠깐 대기 (연출 텀) ---
        if (waitBeforeDoneAnim > 0f)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(waitBeforeDoneAnim);
            else
                yield return new WaitForSeconds(waitBeforeDoneAnim);
        }

        // --- done 트리거 쏘기 ---
        if (panelAnimator != null && !string.IsNullOrEmpty(doneTriggerName))
        {
            panelAnimator.ResetTrigger(doneTriggerName);
            panelAnimator.SetTrigger(doneTriggerName);
        }

        // --- done 애니 끝날 때까지 대기 ---
        if (doneAnimDuration > 0f)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(doneAnimDuration);
            else
                yield return new WaitForSeconds(doneAnimDuration);
        }

        // --- 패널 비활성화 ---
        if (panelRoot != null)
            panelRoot.SetActive(false);

        _currentRoutine = null;
    }
}
