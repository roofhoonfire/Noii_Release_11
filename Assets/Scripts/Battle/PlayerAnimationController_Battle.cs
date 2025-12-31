using UnityEngine;

/// <summary>
/// 플레이어 애니메이션을 단일 진입점으로 제어하는 컨트롤러
/// - 카메라 연출
/// - 배틀 로직
/// - 피격 / 패링 / 공격
/// 전부 여기로만 들어오게 만든다
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // ─────────────────────────────
    // Animator Trigger Names
    // (문자열 하드코딩 방지)
    // ─────────────────────────────
    private static readonly int IdleBack = Animator.StringToHash("Idle_Back");
    private static readonly int IdleFront = Animator.StringToHash("Idle_Front");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int Parry = Animator.StringToHash("Parry");

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    // ─────────────────────────────
    // Camera / Facing 관련
    // ─────────────────────────────

    public void PlayIdleBack()
    {
        ResetAllTriggers();

        // 뒷모습: 보통 flipX 원상복귀
        if (spriteRenderer != null)
            spriteRenderer.flipX = false;

        animator.SetTrigger(IdleBack);
    }

    public void PlayIdleFront()
    {
        ResetAllTriggers();

        // 정면: 카메라 기준 좌우 맞추기
        if (spriteRenderer != null)
            spriteRenderer.flipX = true;

        animator.SetTrigger(IdleFront);
    }


    // ─────────────────────────────
    // Battle Actions
    // ─────────────────────────────

    public void PlayAttack()
    {
        ResetAllTriggers();
        animator.SetTrigger(Attack);
    }

    public void PlayHit()
    {
        ResetAllTriggers();
        animator.SetTrigger(Hit);
    }

    public void PlayParry()
    {
        ResetAllTriggers();
        animator.SetTrigger(Parry);
    }

    // ─────────────────────────────
    // 내부 유틸
    // ─────────────────────────────

    /// <summary>
    /// 트리거 중첩 방지
    /// (카메라 연출 중 공격 들어오는 상황 대비)
    /// </summary>
    private void ResetAllTriggers()
    {
        animator.ResetTrigger(IdleBack);
        animator.ResetTrigger(IdleFront);
        animator.ResetTrigger(Attack);
        animator.ResetTrigger(Hit);
        animator.ResetTrigger(Parry);
    }
}
