using UnityEngine;
using DG.Tweening;

public class ActionCardHoverPreviewManager : MonoBehaviour
{
    public static ActionCardHoverPreviewManager Instance { get; private set; }

    [Header("Preview Settings")]
    [SerializeField] private Transform previewSpawnRoot;
    [SerializeField] private GameObject previewPrefab;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.12f;
    [SerializeField] private float fadeOutDuration = 0.10f;
    [SerializeField] private bool ignoreTimeScale = true;

    private GameObject _currentPreview;
    private CanvasGroup _currentGroup;
    private Tween _fadeTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowPreview(string code)
    {
        if (previewSpawnRoot == null || previewPrefab == null || string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[ActionCardHoverPreviewManager] 설정 누락");
            return;
        }

        if (CSVLoader.Instance == null)
        {
            Debug.LogWarning("[ActionCardHoverPreviewManager] CSVLoader.Instance 없음");
            return;
        }

        // 기존 제거(즉시)
        HidePreview(immediate: true);

        // 생성
        _currentPreview = Instantiate(previewPrefab, previewSpawnRoot);

        // 데이터 적용
        var info = _currentPreview.GetComponent<EachCardInfo>();
        if (info != null)
        {
            info.cardData = CSVLoader.Instance.GetCardByCode(code);
            info.ApplyCardData();
        }
        else
        {
            Debug.LogWarning("[ActionCardHoverPreviewManager] EachCardInfo 없음");
        }

        // ✅ 루트 CanvasGroup 잡아서 페이드 인
        _currentGroup = _currentPreview.GetComponent<CanvasGroup>();
        if (_currentGroup != null)
        {
            KillFade();
            _currentGroup.alpha = 0f;
            _fadeTween = _currentGroup
                .DOFade(1f, fadeInDuration)
                .SetUpdate(ignoreTimeScale);
        }
    }

    public void HidePreview() => HidePreview(immediate: false);

    private void HidePreview(bool immediate)
    {
        if (_currentPreview == null)
            return;

        // CanvasGroup 없으면 즉시 파괴
        if (_currentGroup == null || immediate)
        {
            KillFade();
            Destroy(_currentPreview);
            _currentPreview = null;
            _currentGroup = null;
            return;
        }

        // ✅ 페이드 아웃 후 파괴
        KillFade();
        _fadeTween = _currentGroup
            .DOFade(0f, fadeOutDuration)
            .SetUpdate(ignoreTimeScale)
            .OnComplete(() =>
            {
                if (_currentPreview != null)
                    Destroy(_currentPreview);

                _currentPreview = null;
                _currentGroup = null;
            });
    }

    private void KillFade()
    {
        if (_fadeTween != null && _fadeTween.IsActive())
            _fadeTween.Kill();

        _fadeTween = null;
    }
}