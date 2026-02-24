// StellarNodeClickProxy.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class StellarNodeClickProxy : MonoBehaviour, IPointerClickHandler
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
}