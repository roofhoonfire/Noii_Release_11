using UnityEngine;

public class FieldCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("따라갈 대상 (보통 PlayerRoot의 Transform)")]
    public Transform target;

    [Header("Position")]
    [Tooltip("타겟 기준 오프셋 (예: (0,10,-10) 정도)")]
    public Vector3 offset = new Vector3(0f, 10f, -10f);

    [Tooltip("카메라가 따라가는 속도 (0에 가까울수록 느리게)")]
    public float followSmoothTime = 0.15f;

    [Header("Rotation")]
    [Tooltip("회전을 고정할지, 항상 타겟을 바라볼지 선택")]
    public bool lockRotation = true;

    [Tooltip("lockRotation이 true일 때 사용할 세계 좌표 오일러 각도")]
    public Vector3 fixedEulerAngles = new Vector3(45f, 45f, 0f);

    private Vector3 _currentVelocity;

    private void LateUpdate()
    {
        if (target == null) return;

        // 1) 위치 따라가기
        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref _currentVelocity,
            followSmoothTime
        );

        // 2) 회전 제어
        if (lockRotation)
        {
            transform.rotation = Quaternion.Euler(fixedEulerAngles);
        }
        else
        {
            transform.LookAt(target.position);
        }
    }
}
