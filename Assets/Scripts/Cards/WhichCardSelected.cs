using UnityEngine;
using UnityEngine.EventSystems;

public class WhichCardSelected : MonoBehaviour, IPointerClickHandler
{
    public enum SelectKind
    {
        Card,
        Bound,
        Juju
    }

    [Header("Select Kind")]
    [SerializeField] private SelectKind kind = SelectKind.Card;

    private EachCardInfo _cardInfo;
    private EachBoundInfo _boundInfo;
    private EachJujuInfo _jujuInfo; // 나중에

    private void Awake()
    {
        _cardInfo = GetComponent<EachCardInfo>();
        _boundInfo = GetComponent<EachBoundInfo>();
        _jujuInfo = GetComponent<EachJujuInfo>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (SelectionBuffer.Instance == null)
        {
            Debug.LogWarning("[WhichCardSelected] SelectionBuffer.Instance가 없습니다.");
            return;
        }

        string code = null;

        switch (kind)
        {
            case SelectKind.Card:
                if (_cardInfo == null || _cardInfo.cardData == null) return;
                code = _cardInfo.cardData.code;
                break;

            case SelectKind.Bound:
                if (_boundInfo == null || _boundInfo.boundData == null) return;
                code = _boundInfo.boundData.code; //  string 그대로
                break;

            case SelectKind.Juju:
                if (_jujuInfo == null || _jujuInfo.jujuData == null) return;
                code = _jujuInfo.jujuData.code; //  string 그대로
                return;
        }

        SelectionBuffer.Instance.SetResult(code);
        Debug.Log($"[WhichCardSelected] 선택됨 ({kind}) : {code}");
    }

    public void SetKind(SelectKind k) => kind = k;
}