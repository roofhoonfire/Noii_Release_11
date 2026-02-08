using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CurvedHandLayoutTween : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("카드들의 부모(Hands 컨테이너). 보통 이 오브젝트의 RectTransform")]
    [SerializeField] private RectTransform handRoot;

    [Header("Spawn (생성 위치)")]
    [Tooltip("카드가 처음 생성될 위치(로컬 anchoredPosition). 덱 위치 같은 곳")]
    [SerializeField] private Vector2 spawnAnchoredPos = new Vector2(0f, -450f);

    [Tooltip("카드가 처음 생성될 때의 회전(Z)")]
    [SerializeField] private float spawnRotZ = 0f;

    [Tooltip("생성 시 스케일")]
    [SerializeField] private float spawnScale = 0.95f;

    [Header("Hand (최종 위치 / 커브)")]
    [Tooltip("손패 전체를 화면 어디에 둘지 (최종 위치 오프셋)")]
    [SerializeField] private Vector2 handOffset = new Vector2(0f, -220f);

    [Tooltip("부채꼴 원 중심(Hands 로컬). 보통 화면 아래쪽(손패보다 한참 아래)")]
    [SerializeField] private Vector2 curvePivot = new Vector2(0f, -700f);

    [Tooltip("부채꼴 반지름(클수록 덜 휨)")]
    [SerializeField] private float radius = 600f;

    [Tooltip("최대 펼침 각도(도)")]
    [SerializeField] private float maxSpreadAngle = 40f;

    [Header("Tween")]
    [SerializeField] private float moveDuration = 0.28f;
    [SerializeField] private float rotDuration = 0.28f;
    [SerializeField] private float scaleDuration = 0.22f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;
    [SerializeField] private Ease rotEase = Ease.OutCubic;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Tooltip("카드별 딜레이(0이면 동시에)")]
    [SerializeField] private float perCardStagger = 0.03f;

    [Tooltip("TimeScale 0에서도 UI 트윈 진행")]
    [SerializeField] private bool ignoreTimeScale = true;

    [Tooltip("배치할 때 기존 트윈을 죽임")]
    [SerializeField] private bool killExistingTweens = true;

    [Header("Render Order (겹침 순서)")]
    [Tooltip("하스스톤 스타일: 오른쪽(최근 카드)이 위로 오게")]
    [SerializeField] private bool rightOnTop = true;

    private void Reset()
    {
        handRoot = GetComponent<RectTransform>();
    }
    public Vector2 GetSpawnAnchoredPos() => spawnAnchoredPos;

    /// <summary>
    /// 1) 방금 생성된 카드에 '생성 위치' 세팅 (덱 위치)
    /// </summary>
    public void SnapAllToSpawn(IReadOnlyList<GameObject> cards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            var go = cards[i];
            if (!go) continue;

            var rt = go.GetComponent<RectTransform>();
            if (!rt) continue;

            if (killExistingTweens) rt.DOKill();

            rt.anchoredPosition = spawnAnchoredPos;
            rt.localRotation = Quaternion.Euler(0f, 0f, spawnRotZ);
            rt.localScale = Vector3.one * spawnScale;
        }
    }

    /// <summary>
    /// 2) 손패(부채꼴) 최종 위치로 트윈 이동/회전/스케일
    /// </summary>
    public void TweenToHand(IReadOnlyList<GameObject> cards)
    {
        int n = cards.Count;
        if (n <= 0) return;

        float spread = Mathf.Min(maxSpreadAngle, 10f + (n - 1) * 6f);
        if (n == 1) spread = 0f;

        float start = -spread * 0.5f;
        float step = (n == 1) ? 0f : spread / (n - 1);

        for (int i = 0; i < n; i++)
        {
            var go = cards[i];
            if (!go) continue;

            var rt = go.GetComponent<RectTransform>();
            if (!rt) continue;

            float angleDeg = start + step * i;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // 부채꼴 원호 위 점
            Vector2 dir = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
            Vector2 targetPos = curvePivot + dir * radius + handOffset;

            // 부채꼴 따라 기울기
            Vector3 targetRot = new Vector3(0f, 0f, -angleDeg);

            float delay = perCardStagger * i;

            if (killExistingTweens) rt.DOKill();

            rt.DOAnchorPos(targetPos, moveDuration)
              .SetEase(moveEase)
              .SetDelay(delay)
              .SetUpdate(ignoreTimeScale);

            rt.DOLocalRotate(targetRot, rotDuration)
              .SetEase(rotEase)
              .SetDelay(delay)
              .SetUpdate(ignoreTimeScale);

            rt.DOScale(1f, scaleDuration)
              .SetEase(scaleEase)
              .SetDelay(delay)
              .SetUpdate(ignoreTimeScale);
        }

        if (rightOnTop) SortSiblingsRightOnTop(cards);
        else SortSiblingsCenterOnTop(cards);
    }

    /// <summary>
    /// 한 번에: 생성 위치로 스냅 → 손패로 트윈
    /// </summary>
    public void SpawnThenTweenToHand(IReadOnlyList<GameObject> cards)
    {
        SnapAllToSpawn(cards);
        TweenToHand(cards);
    }

    // ====== 겹침 순서 ======

    private void SortSiblingsRightOnTop(IReadOnlyList<GameObject> cards)
    {
        // 왼쪽부터 위로 쌓기 => 오른쪽(최근)이 최상단
        for (int i = 0; i < cards.Count; i++)
            cards[i].transform.SetAsLastSibling();
    }

    private void SortSiblingsCenterOnTop(IReadOnlyList<GameObject> cards)
    {
        int n = cards.Count;
        int mid = (n - 1) / 2;

        for (int i = 0; i < n; i++)
            cards[i].transform.SetAsFirstSibling();

        for (int offset = 0; offset < n; offset++)
        {
            int left = mid - offset;
            int right = mid + offset;

            if (left >= 0 && left < n) cards[left].transform.SetAsLastSibling();
            if (right != left && right >= 0 && right < n) cards[right].transform.SetAsLastSibling();
        }
    }
}
