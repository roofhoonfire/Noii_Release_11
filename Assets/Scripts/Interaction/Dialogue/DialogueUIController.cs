using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueGraphUIController : MonoBehaviour
{
    public static DialogueGraphUIController Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root; // 대화 UI 전체 루트(패널)

    [Header("Texts")]
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Choices UI")]
    [SerializeField] private List<Transform> choiceSpawnRoot;      // Canvas 아래(VerticalLayoutGroup 추천)
    [SerializeField] private DialogueChoiceButton choicePrefab; // 프리팹 드래그&드랍

    [Header("Continue")]
    [Tooltip("선택지가 없을 때 '클릭/스페이스'로 다음 진행 허용")]
    [SerializeField] private bool enableClickToContinue = true;


    [Header("Fade")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.25f;

    public bool IsPlaying { get; private set; }

    private Action _onContinue;
    private readonly List<GameObject> _spawned = new();

    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (root != null)
            root.SetActive(false);
    }
    private void Update()
    {
        if (!IsPlaying || !enableClickToContinue) return;

        // 선택지가 없는 경우에만 continue가 설정됨
        if (_onContinue != null)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                var cb = _onContinue;
                _onContinue = null;
                cb?.Invoke();
            }
        }
    }

    public void Open()
    {
        IsPlaying = true;

        if (root != null)
            root.SetActive(true);

        StartFade(1f);
    }


    public void Close()
    {
        IsPlaying = false;
        _onContinue = null;
        ClearChoices();

        StartFade(0f, onComplete: () =>
        {
            if (root != null)
                root.SetActive(false);
        });
    }

    private void StartFade(float targetAlpha, Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
    }

    private System.Collections.IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        bool visible = targetAlpha > 0.01f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;

        _fadeRoutine = null;
        onComplete?.Invoke();
    }

    public void ShowNode(
        string speaker,
        string text,
        List<(string text, Action onClick)> choices,
        Action onContinueIfNoChoices)
    {
        Open();

        if (speakerText != null) speakerText.text = speaker;
        if (bodyText != null) bodyText.text = text;

        ClearChoices();

        if (choices != null && choices.Count > 0)
        {
            // 선택지 생성
            for (int i = 0; i < choices.Count; i++)
            {
                var (ctext, onClick) = choices[i];
                var btn = Instantiate(choicePrefab, choiceSpawnRoot[i]);
                btn.Bind(ctext, onClick);
                _spawned.Add(btn.gameObject);
            }

            _onContinue = null; // 선택지가 있으면 continue 금지
        }
        else
        {
            // 선택지 없음 → continue 대기
            _onContinue = onContinueIfNoChoices;
        }
    }

    private void ClearChoices()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null) Destroy(_spawned[i]);
        }
        _spawned.Clear();
    }
}
