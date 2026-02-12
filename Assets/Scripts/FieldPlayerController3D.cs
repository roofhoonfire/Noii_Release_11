using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FieldPlayerController3D : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("움직임 속도 (유닛/초)")]
    public float moveSpeed = 5f;

    [Tooltip("물리 업데이트(FixedUpdate)에서 이동 처리할지 여부")]
    public bool useFixedUpdate = false;


    public Animator _animator;


    private Rigidbody _rb;
    private Vector3 _inputDir;
    private Vector3 _moveVelocity;


    private CharacterVisualController _visual;



    private bool _inputEnabled = true;

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;

        if (!enabled)
        {
            // 이동 벡터 즉시 0
            _moveVelocity = Vector3.zero;

            // 필요하다면 Rigidbody 있을 경우도 처리
            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            // 애니메이션 idle 강제
          /*  if (_animator != null)
            {
                _animator.SetFloat("MoveX", 0f);
                _animator.SetFloat("MoveY", 0f);
                _animator.SetBool("IsMoving", false);
            }*/
        }
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _visual = GetComponentInChildren<CharacterVisualController>();

        // 탑다운용 기본 세팅 보장
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation
                          | RigidbodyConstraints.FreezePositionY;
    }

    private void Update()
    {
        if (!_inputEnabled)
        {
            // 이동/회전 입력 무시
            return;
        }
        // 1) 입력 받기 (기본 Input Manager 기준 - Horizontal/Vertical)
        float h = Input.GetAxisRaw("Horizontal"); // A,D / ←,→
        float v = Input.GetAxisRaw("Vertical");   // W,S / ↑,↓

        // XZ 평면 기준 이동 방향
        _inputDir = new Vector3(h, 0f, v);

        // 대각 이동 시 속도 보정용
        if (_inputDir.sqrMagnitude > 1f)
        {
            _inputDir.Normalize();
        }

        _moveVelocity = _inputDir * moveSpeed;

        // 2) 비주얼에 방향 전달 (애니메이션용)
        if (_visual != null)
        {
            _visual.SetMoveDirection(_inputDir);
        }

        // useFixedUpdate == false일 땐 여기서 직접 이동
        if (!useFixedUpdate)
        {
            MoveCharacter(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            MoveCharacter(Time.fixedDeltaTime);
        }
    }

    private void MoveCharacter(float deltaTime)
    {
        if (_rb == null)
        {
            // 혹시라도 Rigidbody가 없으면 transform로 fallback
            transform.position += _moveVelocity * deltaTime;
            return;
        }

        // 위치 이동
        Vector3 newPos = _rb.position + _moveVelocity * deltaTime;
        _rb.MovePosition(newPos);

        // 이동중일 때만 몸통 방향 회전 (선택 사항)
        if (_inputDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_inputDir, Vector3.up);
            Quaternion newRot = Quaternion.Slerp(_rb.rotation, targetRot, 10f * deltaTime);
            _rb.MoveRotation(newRot);
        }
    }

    /// <summary>
    /// 외부에서 현재 입력 방향이 필요할 경우 사용 가능
    /// </summary>
    public Vector3 CurrentMoveDirection => _inputDir;
}
