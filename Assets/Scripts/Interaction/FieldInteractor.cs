using UnityEngine;

[RequireComponent(typeof(FieldPlayerController3D))]
public class FieldInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("앞으로 쏠 상호작용 거리")]
    [SerializeField] private float interactDistance = 2.0f;

    [Tooltip("스피어캐스트 반지름 (살짝 여유를 두는 용도)")]
    [SerializeField] private float rayRadius = 0.3f;

    [Tooltip("상호작용 대상으로 인식할 레이어")]
    [SerializeField] private LayerMask interactableLayer;

    [Tooltip("상호작용 키")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Ray Origin Offset")]
    [Tooltip("플레이어 위치에서 얼마나 위에서 쏠지 (발이 아니라 몸통/가슴 높이 느낌)")]
    [SerializeField] private float rayOriginHeight = 0.8f;

    // 현재 가장 가까운(= 레이에 실제로 맞은) 상호작용 대상
    public IInteractable CurrentTarget { get; private set; }
    public Transform CurrentTargetTransform { get; private set; }

    // 마지막으로 사용한 forward (정지 상태에서도 방향 유지용)
    private Vector3 _lastForward = Vector3.forward;

    private void Update()
    {
        UpdateCurrentTargetRay();

        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    /// <summary>
    /// 플레이어가 바라보는 방향으로 스피어캐스트해서 상호작용 대상 찾기
    /// </summary>
    private void UpdateCurrentTargetRay()
    {
        // 1) origin / dir 계산
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;

        // 탑다운 기준 forward에서 y 날리기 (XZ 평면 위의 방향만 사용)
        Vector3 dir = transform.forward;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            // 거의 0이라면, 이전 방향 유지 (정지 상태에서 방향 정보 유지)
            dir = _lastForward;
        }
        else
        {
            dir.Normalize();
            _lastForward = dir;
        }

        // 2) 스피어캐스트
        RaycastHit hit;

        bool hasHit = Physics.SphereCast(
            origin,
            rayRadius,
            dir,
            out hit,
            interactDistance,
            interactableLayer
        );

        if (!hasHit)
        {
            CurrentTarget = null;
            CurrentTargetTransform = null;
            return;
        }

        // 3) 맞은 콜라이더에서 IInteractable 찾기
        var interactable = hit.collider.GetComponent<IInteractable>();
        if (interactable == null)
        {
            CurrentTarget = null;
            CurrentTargetTransform = null;
            return;
        }

        CurrentTarget = interactable;
        CurrentTargetTransform = hit.collider.transform;
    }

    /// <summary>
    /// 상호작용 키 입력 시 호출
    /// </summary>
    private void TryInteract()
    {
        if (CurrentTarget == null) return;

        CurrentTarget.Interact(gameObject); // interactor = player
    }

    /// <summary>
    /// 에디터에서 디버그용으로 레이 뷰 그리기
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        Vector3 dir = transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = Vector3.forward;
        }
        else
        {
            dir.Normalize();
        }

        // 레이 라인
        Gizmos.DrawLine(origin, origin + dir * interactDistance);
        // 끝 지점 구체
        Gizmos.DrawWireSphere(origin + dir * interactDistance, rayRadius);
    }
}
