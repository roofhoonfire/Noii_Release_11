// StellarNodeClickProxy.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class StellarNodeClickProxy : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private StellarNode owner;

    public void Bind(StellarNode node) => owner = node;

    public void OnPointerClick(PointerEventData eventData)
    {
        // Unity UI는 PointerEventData.clickCount가 더블클릭을 잡아준다.
        if (eventData != null && eventData.clickCount >= 2)
        {
            owner?.OnDoubleClicked();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner == null) return;

        // 노드 보상 카드 코드 기준으로 프리뷰 스폰
        StellarStateMode.Instance.SpawnPreviewCard(owner.rewarCardCode);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 호버 종료 → 프리뷰 제거
        StellarStateMode.Instance.ClearPreviewCard();
    }
}