using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardHoverScale : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Hover")]
    [SerializeField] private float hoverDelay = 0.1f;
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float tweenDuration = 0.12f;
    [SerializeField] private Ease hoverEase = Ease.OutBack;
    [SerializeField] private Ease exitEase = Ease.OutQuad;

    [Header("Y Offset")]
    [SerializeField] private float hoverYOffset = -30f;

    [Header("Preview")]
    [Tooltip("마우스가 이 시간 이상 머무르면 프리뷰 표시")]
    [SerializeField] private float previewHoldSeconds = 0.35f;

    [Header("Time")]
    [SerializeField] private bool ignoreTimeScale = true;

    private Vector3 baseScale;
    private Vector3 baseLocalPos;

    private Tween delayTween;
    private Tween scaleTween;
    private Tween posTween;

    private Tween previewTween;

    private bool inside;
    private bool previewShown;

    private EachCardInfo _eachCardInfo;

    private void Awake()
    {
        if (target == null)
            target = transform;

        baseScale = target.localScale;
        baseLocalPos = target.localPosition;

        // 프리팹에 EachCardInfo가 붙어있다고 했으니 캐싱
        _eachCardInfo = GetComponentInParent<EachCardInfo>();
        if (_eachCardInfo == null)
            _eachCardInfo = GetComponent<EachCardInfo>();
    }

    private void OnDisable()
    {
        KillTweens();
        HidePreviewSafe();

        target.localScale = baseScale;
        target.localPosition = baseLocalPos;
        inside = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CardDragHandler.AnyCardDragging)
            return;

        inside = true;
        KillTweens();
        previewShown = false;

        // 1) 스케일/포지션 (기존 로직 유지)
        delayTween = DOVirtual.DelayedCall(hoverDelay, () =>
        {
            if (!inside || CardDragHandler.AnyCardDragging)
                return;

            scaleTween = target
                .DOScale(baseScale * hoverScale, tweenDuration)
                .SetEase(hoverEase)
                .SetUpdate(ignoreTimeScale);

            posTween = target
                .DOLocalMoveY(baseLocalPos.y + hoverYOffset, tweenDuration)
                .SetEase(hoverEase)
                .SetUpdate(ignoreTimeScale);

        }).SetUpdate(ignoreTimeScale);

        // 2) 프리뷰: 일정 시간 이상 머무르면 ShowPreview
        previewTween = DOVirtual.DelayedCall(previewHoldSeconds, () =>
        {
            if (!inside || CardDragHandler.AnyCardDragging)
                return;

            ShowPreviewSafe();

        }).SetUpdate(ignoreTimeScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inside = false;
        KillTweens();

        // 스케일/포지션 원복
        scaleTween = target
            .DOScale(baseScale, tweenDuration)
            .SetEase(exitEase)
            .SetUpdate(ignoreTimeScale);

        posTween = target
            .DOLocalMoveY(baseLocalPos.y, tweenDuration)
            .SetEase(exitEase)
            .SetUpdate(ignoreTimeScale);

        // ✅ 마우스 떼면 즉시 프리뷰 숨김
        HidePreviewSafe();
    }

    private void ShowPreviewSafe()
    {
        if (previewShown) return;

        if (ActionCardHoverPreviewManager.Instance == null)
        {
            Debug.LogWarning("[CardHoverScale] ActionCardHoverPreviewManager.Instance 없음");
            return;
        }

        if (_eachCardInfo == null || _eachCardInfo.cardData == null)
        {
            Debug.LogWarning("[CardHoverScale] EachCardInfo/cardData 없음");
            return;
        }

        string code = _eachCardInfo.cardData.code;
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[CardHoverScale] cardData.code 비어있음");
            return;
        }

        previewShown = true;
        ActionCardHoverPreviewManager.Instance.ShowPreview(code);
    }

    private void HidePreviewSafe()
    {
        if (!previewShown) return;

        if (ActionCardHoverPreviewManager.Instance != null)
            ActionCardHoverPreviewManager.Instance.HidePreview();

        previewShown = false;
    }

    private void KillTweens()
    {
        if (delayTween != null && delayTween.IsActive()) delayTween.Kill();
        if (scaleTween != null && scaleTween.IsActive()) scaleTween.Kill();
        if (posTween != null && posTween.IsActive()) posTween.Kill();
        if (previewTween != null && previewTween.IsActive()) previewTween.Kill();

        delayTween = null;
        scaleTween = null;
        posTween = null;
        previewTween = null;
    }
}