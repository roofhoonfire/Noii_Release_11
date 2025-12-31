using UnityEngine;

public class SpriteBillboard : MonoBehaviour
{
    private Camera _cam;

    private void LateUpdate()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return;
        }

        // 카메라를 바라보도록 회전
        Vector3 dir = transform.position - _cam.transform.position;
        dir.y = 0f; // 위/아래 기울어짐을 조금 줄이고 싶으면 유지, 완전 카메라 정면이면 이 줄 지워도 됨
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
}
