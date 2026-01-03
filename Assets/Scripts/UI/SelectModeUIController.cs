using UnityEngine;

/// <summary>
/// SelectMode 버튼 3개의 활성/퇴장 트리거만 담당.
/// - EnterSelectMode(): 버튼 3개 SetActive(true)만 수행
/// - ExitSelectMode(): 버튼 3개 Animator에 "Out" 트리거만 쏨
///
/// 비활성화는 각 버튼 Animator 쪽에서:
/// Init -> Idle 반복 -> Out -> Empty 전이
/// Empty State에 DisableOnEnter(StateMachineBehaviour)로 처리한다.
/// </summary>
public class SelectModeUIController : MonoBehaviour
{
    [Header("Select Mode Objects (3)")]
    [SerializeField] private GameObject cardModeObj;     // 예: BTN_Select_Card
    [SerializeField] private GameObject itemModeObj;     // 예: BTN_Select_Item
    [SerializeField] private GameObject releaseModeObj;  // 예: BTN_Select_Release

    [Header("Animator Trigger")]
    [SerializeField] private string outTrigger = "Out";

    private Animator _cardAnim;
    private Animator _itemAnim;
    private Animator _releaseAnim;

    private void Awake()
    {
        _cardAnim = cardModeObj != null ? cardModeObj.GetComponent<Animator>() : null;
        _itemAnim = itemModeObj != null ? itemModeObj.GetComponent<Animator>() : null;
        _releaseAnim = releaseModeObj != null ? releaseModeObj.GetComponent<Animator>() : null;

        // 시작은 꺼진 상태로
        SetAllActive(false);
    }

    public void EnterSelectMode()
    {
        SetAllActive(true);
        // 활성화되면 Animator가 기본 State(Init)로 자동 진입 → Idle로 넘어가는 구조
    }

    public void ExitSelectMode()
    {
        Debug.Log("dd");
        // "Out" 트리거만 쏜다. 비활성화는 각 버튼의 DisableOnEnter가 처리.
        _cardAnim?.SetTrigger(outTrigger);
        _itemAnim?.SetTrigger(outTrigger);
        _releaseAnim?.SetTrigger(outTrigger);
    }

    private void SetAllActive(bool active)
    {
        if (cardModeObj != null) cardModeObj.SetActive(active);
        if (itemModeObj != null) itemModeObj.SetActive(active);
        if (releaseModeObj != null) releaseModeObj.SetActive(active);
    }
}
