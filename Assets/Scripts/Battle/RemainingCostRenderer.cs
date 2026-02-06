using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 액션 큐 remainingCost를 보여주는 UI 싱글톤.
/// - ActionAddedRenderingRoutine(int actorId)
///   : 액션이 큐에 들어간 직후 한 번 튀어오르는 연출 + 이전 값 갱신
/// - PlayRemainingCostUpdate()
///   : 저장된 이전 값 → 현재 큐 값까지 숫자가 1씩 변하는 연출
/// 전부 BattleManager 쪽에서 yield return 으로 기다리는 구조.
/// </summary>
public class RemainingCostRenderer : MonoBehaviour
{
    public static RemainingCostRenderer Instance { get; private set; }

    [Header("참조")]
    [SerializeField] private BattleManager battleManager;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI myCostText;    // actorId == 0 (플레이어)
    [SerializeField] private TextMeshProUGUI opCostText;    // actorId == 1 (적)

    [Header("Scale FX (액션 추가 시)")]
    [SerializeField] private float scaleUp = 1.08f;
    [SerializeField] private float scaleDuration = 5f;
    [SerializeField] private Ease scaleEase = Ease.OutQuad;
    [SerializeField] private bool useUnscaledTimeForScale = true;

    [Header("Number Tick (띠리리링)")]
    [SerializeField] private float tickInterval = 0.05f;
    [SerializeField] private bool useUnscaledTimeForTick = true;

    // 이전 remainingCost 기록 (다음 업데이트 연출의 시작값)
    private int myPreviousRemainingCost = 0;
    private int opPreviousRemainingCost = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (battleManager == null)
            battleManager = BattleManager.Instance;
    }

    // =========================================================
    // 1) 액션 추가 시: 남은 코스트 표시 + 스케일 튀는 연출
    // =========================================================

    /// <summary>
    /// BattleManager에서 액션을 _actionQueue에 Add한 직후
    /// yield return 으로 호출하는 코루틴.
    /// </summary>
    public IEnumerator ActionAddedRenderingRoutine(int actorId)
    {
        if (battleManager == null)
            battleManager = BattleManager.Instance;

        if (battleManager == null)
            yield break;

        if (!battleManager.TryGetRemainingCost(actorId, out int cost))
            yield break;

        if (actorId == 0)
        {
            myPreviousRemainingCost = cost;

            if (myCostText != null)
            {
                myCostText.text = cost.ToString();
                yield return PlayScaleFX(myCostText.rectTransform);
            }
        }
        else if (actorId == 1)
        {
            opPreviousRemainingCost = cost;

            if (opCostText != null)
            {
                opCostText.text = cost.ToString();
                yield return PlayScaleFX(opCostText.rectTransform);
            }
        }
    }

    private IEnumerator PlayScaleFX(RectTransform target)
    {
        if (target == null)
            yield break;

        Vector3 origin = Vector3.one;
        target.localScale = origin;

        float half = scaleDuration * 0.5f;

        // 1) 1 → scaleUp
        Tweener t1 = target.DOScale(scaleUp, half).SetEase(scaleEase);
        if (useUnscaledTimeForScale)
            t1.SetUpdate(true);

        yield return t1.WaitForCompletion();

        // 2) scaleUp → 1
        Tweener t2 = target.DOScale(origin, half).SetEase(scaleEase);
        if (useUnscaledTimeForScale)
            t2.SetUpdate(true);

        yield return t2.WaitForCompletion();
    }

    // =========================================================
    // 2) 이전 값 → 현재 큐 값까지 숫자 "띠리리링" 연출
    // =========================================================

    /// <summary>
    /// BattleManager에서 코스트가 실제로 바뀐 직후
    /// (예: TimeGoesOn() 호출 다음)에 yield return 으로 호출.
    /// myPrevious/opPrevious 에서 현재 큐 값까지 1씩 변화.
    /// </summary>
    public IEnumerator PlayRemainingCostUpdate()
    {
        if (battleManager == null)
            battleManager = BattleManager.Instance;

        if (battleManager == null)
            yield break;

        // 최신 큐 값 읽기 (없으면 0으로 처리)
        int newMy = 0;
        int newOp = 0;

        if (!battleManager.TryGetRemainingCost(0, out newMy))
            newMy = 0;

        if (!battleManager.TryGetRemainingCost(1, out newOp))
            newOp = 0;

        int curMy = myPreviousRemainingCost;
        int curOp = opPreviousRemainingCost;

        if (myCostText != null)
            myCostText.text = curMy.ToString();
        if (opCostText != null)
            opCostText.text = curOp.ToString();

        bool doneMy = curMy == newMy;
        bool doneOp = curOp == newOp;

        // 예: (6,3) → (3,0) 이면
        // my: 6→5→4→3, op: 3→2→1→0 순서로 하나씩 이동
        while (!doneMy || !doneOp)
        {
            if (!doneMy && myCostText != null)
            {
                if (curMy < newMy) curMy++;
                else if (curMy > newMy) curMy--;

                myCostText.text = curMy.ToString();
                doneMy = curMy == newMy;
            }

            if (!doneOp && opCostText != null)
            {
                if (curOp < newOp) curOp++;
                else if (curOp > newOp) curOp--;

                opCostText.text = curOp.ToString();
                doneOp = curOp == newOp;
            }

            if (useUnscaledTimeForTick)
                yield return new WaitForSecondsRealtime(tickInterval);
            else
                yield return new WaitForSeconds(tickInterval);
        }

        // 최종 값으로 previous 갱신
        myPreviousRemainingCost = newMy;
        opPreviousRemainingCost = newOp;
    }
}
