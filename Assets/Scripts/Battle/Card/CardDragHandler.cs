using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class CardDragHandler : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler
{
    public static bool AnyCardDragging { get; private set; }
    public static bool GlobalDragEnabled { get; private set; } = true;
    private bool dragEnabled = true; //특정 카드만 드래그 안되게 ㅎ

    [Header("FX")]
    [SerializeField] private GameObject inZoneFlame;

    [Header("Drag Rotation")]
    [SerializeField] private float dragRotZ = 0f;
    [SerializeField] private float dragRotDuration = 0.08f;
    [SerializeField] private Ease dragRotEase = Ease.OutQuad;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private RectTransform dropZone;

    private LayoutElement layoutElement;
    private Vector2 dragOffset;
    private Vector2 originalAnchoredPos;
    private Vector3 originalScale;
    private float originalRotZ;

    private Card thisCardData;

    private bool wasInside;
    private bool draggingThis;


    public void SetDragEnabled(bool enabled)
    {
        dragEnabled = enabled;
    }
    public static void SetGlobalDragEnabled(bool enabled)
    {
        GlobalDragEnabled = enabled;

        if (!enabled)
            AnyCardDragging = false;
    }
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        originalScale = transform.localScale;

        layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = false;

        var zone = GameObject.FindWithTag("dropzone");
        if (zone != null)
            dropZone = zone.GetComponent<RectTransform>();

        if (inZoneFlame) inZoneFlame.SetActive(false);
    }

    private void OnDisable()
    {
        if (draggingThis)
        {
            draggingThis = false;
            AnyCardDragging = false;
        }

        if (inZoneFlame) inZoneFlame.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!dragEnabled || !GlobalDragEnabled)
            return;
        if (CardModeState.Instance == null ||
            !CardModeState.Instance.isActive ||
            !CardModeState.Instance.isInteractiveSession)
            return;

        canvas = GetComponentInParent<Canvas>();
        thisCardData = GetComponent<EachCardInfo>().cardData;

        canvasGroup.blocksRaycasts = false;

        originalAnchoredPos = rectTransform.anchoredPosition;
        originalRotZ = rectTransform.localEulerAngles.z;

        layoutElement.ignoreLayout = true;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        dragOffset = localPoint - originalAnchoredPos;

        wasInside = false;
        if (inZoneFlame) inZoneFlame.SetActive(false);

        AnyCardDragging = true;
        draggingThis = true;

        rectTransform.DOKill();
        if (dragRotDuration <= 0f)
        {
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, dragRotZ);
        }
        else
        {
            rectTransform
                .DOLocalRotate(new Vector3(0f, 0f, dragRotZ), dragRotDuration)
                .SetEase(dragRotEase)
                .SetUpdate(true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!draggingThis) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        rectTransform.anchoredPosition = localPoint - dragOffset;

        bool inside = IsInsideDropZone(eventData.position);
        HandleZoneFX(inside);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!draggingThis) return;

        draggingThis = false;
        AnyCardDragging = false;

        canvasGroup.blocksRaycasts = true;
        layoutElement.ignoreLayout = false;

        bool success = IsInsideDropZone(eventData.position);

        if (!success)
        {
            rectTransform
                .DOAnchorPos(originalAnchoredPos, 0.25f)
                .SetEase(Ease.OutQuad);

            transform
                .DOScale(originalScale, 0.25f)
                .SetEase(Ease.OutQuad);

            rectTransform
                .DOLocalRotate(new Vector3(0f, 0f, originalRotZ), 0.2f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            if (inZoneFlame) inZoneFlame.SetActive(false);
        }
        else
        {
            var apData = BattleManager.Instance.apData;

            if (thisCardData.cardType == 0)
            {
                CardModeState.Instance.curAction.actionId = 1;
                CardModeState.Instance.curAction.defense =
                    Mathf.Max(0, thisCardData.defense + apData.tempDef + apData.permDef);
                CardModeState.Instance.curAction.rumblePoint =
                    thisCardData.rumblePoint + apData.tempRum;
                CardModeState.Instance.curAction.cardcode = thisCardData.code;
                CardModeState.Instance.curAction.animations = thisCardData.animations;
                CardModeState.Instance.curAction.actionClock =
                    Mathf.Max(apData.CastingMinumum,
                        thisCardData.actionClock + apData.permCast + apData.tempCast);
                CardModeState.Instance.curAction.cardname = thisCardData.name;
                CardModeState.Instance.curAction.zoneIndex = thisCardData.zoneIndex;
                CardModeState.Instance.curAction.seqType = thisCardData.seqType;
                CardModeState.Instance.curAction.effects
                    .AddRange(CSVLoader.Instance.GetCardEffectsByCode(thisCardData.code));
                CardModeState.Instance.curAction.damage =
                    Mathf.Max(0, thisCardData.damage + apData.tempDam + apData.permDam);

                RemoveUsedFromHands(thisCardData.code);
                apData.trash.Add(thisCardData.code);

                CardModeState.Instance.StopSelectCardLoop(
                    CardModeState.Instance.curAction);
            }
        }
    }

    private static void RemoveUsedFromHands(string used)
    {
        if (BattleManager.Instance.apData?.hands == null) return;

        int idx = BattleManager.Instance.apData.hands.IndexOf(used);
        if (idx >= 0)
            BattleManager.Instance.apData.hands.RemoveAt(idx);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        transform.DOKill();
        transform
            .DOScale(originalScale * 1.1f, 0.08f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
                transform.DOScale(originalScale, 0.08f).SetEase(Ease.InQuad));
    }

    private void HandleZoneFX(bool inside)
    {
        if (inside == wasInside) return;
        wasInside = inside;
        if (inZoneFlame) inZoneFlame.SetActive(inside);
    }

    private bool IsInsideDropZone(Vector2 screenPos)
    {
        return dropZone != null &&
               RectTransformUtility.RectangleContainsScreenPoint(dropZone, screenPos);
    }
}
