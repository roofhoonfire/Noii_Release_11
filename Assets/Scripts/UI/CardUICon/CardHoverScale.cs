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

    [Header("Z Offset")]
    [SerializeField] private float hoverYOffset = -30f;

    [Header("Time")]
    [SerializeField] private bool ignoreTimeScale = true;

    private Vector3 baseScale;
    private Vector3 baseLocalPos;

    private Tween delayTween;
    private Tween scaleTween;
    private Tween posTween;

    private bool inside;

    private void Awake()
    {
        if (target == null)
            target = transform;

        baseScale = target.localScale;
        baseLocalPos = target.localPosition;
    }

    private void OnDisable()
    {
        KillTweens();
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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inside = false;
        KillTweens();

        scaleTween = target
            .DOScale(baseScale, tweenDuration)
            .SetEase(exitEase)
            .SetUpdate(ignoreTimeScale);

        posTween = target
            .DOLocalMoveY(baseLocalPos.y, tweenDuration)
            .SetEase(exitEase)
            .SetUpdate(ignoreTimeScale);
    }

    private void KillTweens()
    {
        if (delayTween != null && delayTween.IsActive()) delayTween.Kill();
        if (scaleTween != null && scaleTween.IsActive()) scaleTween.Kill();
        if (posTween != null && posTween.IsActive()) posTween.Kill();

        delayTween = null;
        scaleTween = null;
        posTween = null;
    }
}
