using UnityEngine;
using UnityEngine.EventSystems;

public class WhichCardSelected : MonoBehaviour, IPointerClickHandler
{
    private EachCardInfo _cardInfo;

    private void Awake()
    {
        _cardInfo = GetComponent<EachCardInfo>();
        if (_cardInfo == null)
            Debug.LogWarning("[WhichCardSelected] EachCardInfo가 같은 오브젝트에 없습니다.");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_cardInfo == null || _cardInfo.cardData == null)
            return;

        if (SelectionBuffer.Instance == null)
        {
            Debug.LogWarning("[WhichCardSelected] SelectionBuffer.Instance가 없습니다.");
            return;
        }

        string code = _cardInfo.cardData.code;
        SelectionBuffer.Instance.SetResult(code);

        Debug.Log($"[WhichCardSelected] 카드 선택됨 : {code}");
    }
}
