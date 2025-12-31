using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIController : MonoBehaviour
{
    public static DialogueUIController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private GameObject nextHintRoot;

    [Header("Input")]
    [SerializeField] private KeyCode nextKey = KeyCode.Z;

    private DialogueAsset _currentAsset;
    private int _currentIndex;
    private bool _isPlaying;
    private bool _isWaitingForInput;

    private Action _onFinished;
    private Action<int> _onLineChanged; // ★ 추가: 현재 라인 인덱스 콜백

    public bool IsPlaying => _isPlaying;
    public int CurrentLineIndex => _currentIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        SetVisible(false);
    }

    private void Update()
    {
        if (!_isPlaying) return;

        if (_isWaitingForInput && Input.GetKeyDown(nextKey))
        {
            ShowNextLine();
        }
    }

    /// <summary>
    /// 대화 시작.
    /// onFinished : 대화가 완전히 끝났을 때 호출.
    /// onLineChanged : 각 라인이 표시될 때마다 현재 라인 인덱스를 전달.
    /// </summary>
    public void StartDialogue(DialogueAsset asset, Action onFinished = null, Action<int> onLineChanged = null)
    {
        if (asset == null || asset.lines == null || asset.lines.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        _currentAsset = asset;
        _currentIndex = -1;
        _onFinished = onFinished;
        _onLineChanged = onLineChanged;

        _isPlaying = true;
        _isWaitingForInput = false;

        SetVisible(true);
        ShowNextLine(); // 첫 라인 표시
    }

    private void ShowNextLine()
    {
        _isWaitingForInput = false;
        _currentIndex++;

        if (_currentAsset == null || _currentIndex >= _currentAsset.lines.Count)
        {
            EndDialogue();
            return;
        }

        var line = _currentAsset.lines[_currentIndex];

        if (nameText != null)
            nameText.text = line.speakerName;

        if (bodyText != null)
            bodyText.text = line.text;

        if (portraitImage != null)
        {
            if (line.portrait != null)
            {
                portraitImage.gameObject.SetActive(true);
                portraitImage.sprite = line.portrait;
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        if (nextHintRoot != null)
            nextHintRoot.SetActive(true);

        _isWaitingForInput = true;

        // ★ 현재 라인 인덱스 콜백 호출
        _onLineChanged?.Invoke(_currentIndex);
    }

    private void EndDialogue()
    {
        _isPlaying = false;
        _isWaitingForInput = false;
        SetVisible(false);

        var finishedCb = _onFinished;
        var lineChangedCb = _onLineChanged;

        _onFinished = null;
        _onLineChanged = null;
        _currentAsset = null;

        finishedCb?.Invoke();
        // lineChangedCb는 여기선 호출하지 않음 (-1 처리는 DialogueInteractable에서)
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}
