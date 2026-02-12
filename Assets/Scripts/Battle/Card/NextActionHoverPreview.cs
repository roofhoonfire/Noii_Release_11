using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class NextActionHoverPreview : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    public enum OwnerType { Player, Enemy }

    [Header("Owner")]
    [SerializeField] private OwnerType ownerType = OwnerType.Player;

    [Header("Hover")]
    [SerializeField] private float hoverDelay = 0.35f;
    [SerializeField] private float fadeDuration = 0.15f;

    [Header("Spawn")]
    [SerializeField] private GameObject previewCardPrefab;
    [SerializeField] private Transform spawnRoot;

    private Coroutine hoverRoutine;
    private Coroutine fadeRoutine;

    private GameObject spawnedCard;
    private CanvasGroup spawnedGroup;

    private bool isHovering;

    // =========================
    // Hover Events
    // =========================

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;

        if (hoverRoutine != null)
            StopCoroutine(hoverRoutine);

        hoverRoutine = StartCoroutine(HoverDelayRoutine());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        if (hoverRoutine != null)
            StopCoroutine(hoverRoutine);

        HidePreview();
    }

    private IEnumerator HoverDelayRoutine()
    {
        yield return new WaitForSecondsRealtime(hoverDelay);

        if (!isHovering) yield break;

        SpawnPreview();
    }

    // =========================
    // Spawn Logic
    // =========================

    private void SpawnPreview()
    {
        if (spawnedCard != null) return;

        string code = GetCardCodeFromMonitor();
        if (string.IsNullOrEmpty(code)) return;

        if (previewCardPrefab == null || spawnRoot == null)
        {
            Debug.LogWarning("previewCardPrefab / spawnRoot 비어있음");
            return;
        }

        spawnedCard = Instantiate(previewCardPrefab, spawnRoot);
        spawnedCard.transform.localPosition = Vector3.zero;
        spawnedCard.transform.localRotation = Quaternion.identity;
        spawnedCard.transform.localScale = Vector3.one;

        spawnedGroup = spawnedCard.GetComponent<CanvasGroup>();
        if (spawnedGroup == null)
            spawnedGroup = spawnedCard.AddComponent<CanvasGroup>();

        spawnedGroup.alpha = 0f;

        // 카드 데이터 주입
        var info = spawnedCard.GetComponent<EachCardInfo>();
        if (info != null)
        {
            info.cardData = CSVLoader.Instance.GetCardByCode(code);
            info.ApplyCardData();
        }
        else
        {
            Debug.LogWarning("EachCardInfo 없음");
        }

        FadeTo(1f);
    }

    private string GetCardCodeFromMonitor()
    {
        var mon = NextActionMonitor.Instance;
        if (mon == null) return null;

        ActionData action =
            ownerType == OwnerType.Player
            ? mon.myNextAction
            : mon.opNextAction;

        if (action == null) return null;

        return action.cardcode;
    }

    // =========================
    // Fade Logic
    // =========================

    private void HidePreview()
    {
        if (spawnedCard == null) return;

        FadeTo(0f, true);
    }

    private void FadeTo(float targetAlpha, bool destroyOnEnd = false)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, destroyOnEnd));
    }

    private IEnumerator FadeRoutine(float targetAlpha, bool destroyOnEnd)
    {
        float start = spawnedGroup.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            spawnedGroup.alpha = Mathf.Lerp(start, targetAlpha, t / fadeDuration);
            yield return null;
        }

        spawnedGroup.alpha = targetAlpha;

        if (destroyOnEnd)
        {
            Destroy(spawnedCard);
            spawnedCard = null;
            spawnedGroup = null;
        }

        fadeRoutine = null;
    }
}
