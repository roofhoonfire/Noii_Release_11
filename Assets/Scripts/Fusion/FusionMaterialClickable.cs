using UnityEngine;
using UnityEngine.EventSystems;

public class FusionMaterialClickable : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs")]
    [SerializeField] private EachCardInfo eachCardInfo;

    [Header("Options")]
    [SerializeField] private bool logClick = false;

    private void Reset()
    {
        if (eachCardInfo == null)
            eachCardInfo = GetComponent<EachCardInfo>();
    }

    private void Awake()
    {
        if (eachCardInfo == null)
            eachCardInfo = GetComponent<EachCardInfo>();
    }

    // UI 클릭(캔버스 위 카드라면 이쪽이 들어옴)
    public void OnPointerClick(PointerEventData eventData)
    {
        TrySelect();
    }

    // 혹시 월드 오브젝트로도 쓸 경우 대비
  /*  private void OnMouseDown()
    {
        // UI 위에서 OnMouseDown도 같이 터질 수 있으니
        // EventSystem이 있고 포인터가 UI를 가리키는 중이면 스킵
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        TrySelect();
    }
  */
    private void TrySelect()
    {
        if (CardFusionState.Instance == null)
        {
            Debug.LogWarning("[FusionMaterialClickable] CardFusionState.Instance가 없음");
            return;
        }

        if (eachCardInfo == null)
        {
            Debug.LogWarning("[FusionMaterialClickable] EachCardInfo 참조가 없음");
            return;
        }

        if (eachCardInfo.cardData == null)
        {
            Debug.LogWarning("[FusionMaterialClickable] eachCardInfo.cardData가 null (ApplyCardData 전에 클릭됐나?)");
            return;
        }

        //  여기서 "카드 코드"를 꺼내는 필드명은 네 cardData 타입에 따라 다름
        // 흔한 케이스: cardData.code 또는 cardData.cardCode
        // 우선 둘 다 시도하도록 안전하게 처리
        string code = GetCodeFromCardData(eachCardInfo.cardData);
        if (string.IsNullOrWhiteSpace(code))
        {
            Debug.LogWarning("[FusionMaterialClickable] cardData에서 카드 코드를 추출 못함 (필드명 확인 필요)");
            return;
        }

        if (logClick)
            Debug.Log($"[Fusion] material clicked: {code}");

        CardFusionState.Instance.SelectMaterial(code);
    }

    private string GetCodeFromCardData(Card cardData)
    {
        // 리플렉션으로 안전하게 뽑는다(필드명이 확정이면 이 함수 없이 직접 접근하는 게 더 깔끔함)
        // 성능 이슈는 클릭 시 1회라 무시 가능.

        return cardData.code;
       /* var p1 = t.GetProperty("code");
        if (p1 != null)
        {
            var v = p1.GetValue(cardData) as string;
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }

        var p2 = t.GetProperty("cardCode");
        if (p2 != null)
        {
            var v = p2.GetValue(cardData) as string;
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }

        var f1 = t.GetField("code");
        if (f1 != null)
        {
            var v = f1.GetValue(cardData) as string;
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }

        var f2 = t.GetField("cardCode");
        if (f2 != null)
        {
            var v = f2.GetValue(cardData) as string;
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }
       */
        return null;
    }
}
