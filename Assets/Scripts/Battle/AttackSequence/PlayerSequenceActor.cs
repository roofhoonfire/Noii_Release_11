using System.Collections;
using UnityEngine;

public class PlayerSequenceActor : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public Animator Animator => animator; // ★ 추가

    [Tooltip("빌보드용: 활성 시 항상 카메라 바라보게 할지")]
    [SerializeField] private bool billboardToMainCamera = true;

    private Camera _cam;

    private void OnEnable()
    {
        _cam = Camera.main;
        if (animator != null) animator.speed = 1f;
    }

    private void LateUpdate()
    {
        if (!billboardToMainCamera) return;
        if (_cam == null) return;

        // 카메라를 "향해서" 보게 한다 (진짜 빌보드)
        Vector3 dir = (_cam.transform.position - transform.position);
        if (dir.sqrMagnitude > 0.0001f)
            transform.forward = dir.normalized;
    }


    public void Play(AnimationClip clip)
    {
        if (animator == null || clip == null) return;

        animator.speed = 1f;
        // clip.name과 동일한 state가 있어야 가장 쉬움
        animator.Play(clip.name, 0, 0f);
    }

    public IEnumerator WaitUntilNormalizedTime(float normalized)
    {
        if (animator == null) yield break;

        while (true)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            if (st.normalizedTime >= normalized)
                break;

            yield return null;
        }
    }

    public void FreezeAt(float normalized)
    {
        if (animator == null) return;

        var st = animator.GetCurrentAnimatorStateInfo(0);
        animator.Play(st.fullPathHash, 0, normalized);
        animator.speed = 0f;
    }

    public void UnfreezeAndHide()
    {
        if (animator != null) animator.speed = 1f;
        gameObject.SetActive(false);
    }
}
