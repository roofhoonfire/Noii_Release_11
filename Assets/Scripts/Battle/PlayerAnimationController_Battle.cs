using System;
using System.Collections.Generic;
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
    // ─────────────────────────────
    // Singleton
    // ─────────────────────────────
    public static PlayerAnimationController Instance { get; private set; }

    [Header("Singleton")]
    [Tooltip("중복 생성 시, 이 오브젝트를 살아남길지 여부. (기본: 기존 인스턴스 유지)")]
    [SerializeField] private bool keepNewestInstance = false;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // ─────────────────────────────
    // Animator Trigger Names
    // ─────────────────────────────
    private static readonly int IdleBack = Animator.StringToHash("Idle_Back");
    private static readonly int IdleFront = Animator.StringToHash("Idle_Front");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int Parry = Animator.StringToHash("Parry");

    // ─────────────────────────────
    // Force Play Clips
    // ─────────────────────────────
    [Header("Force Play Clips (Drag & Drop)")]
    [SerializeField] private List<AnimationClip> forcePlayClips = new();

    private readonly Dictionary<string, AnimationClip> _clipMap = new(StringComparer.Ordinal);

    private void Awake()
    {
        // Singleton bootstrap
        if (Instance != null && Instance != this)
        {
            if (keepNewestInstance)
            {
                // 새로 들어온 놈을 살리고, 기존을 제거
                Destroy(Instance.gameObject);
                Instance = this;
            }
            else
            {
                // 기존을 유지하고, 새로 들어온 놈 제거
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            Instance = this;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        BuildClipMap();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        BuildClipMap();
    }

    private void BuildClipMap()
    {
        _clipMap.Clear();
        if (forcePlayClips == null) return;

        for (int i = 0; i < forcePlayClips.Count; i++)
        {
            var clip = forcePlayClips[i];
            if (clip == null) continue;
            _clipMap[clip.name] = clip; // 중복 이름이면 마지막이 우선
        }
    }

    // ─────────────────────────────
    // Camera / Facing
    // ─────────────────────────────

    public void PlayIdleBack()
    {
        ResetAllTriggers();

        if (spriteRenderer != null)
            spriteRenderer.flipX = false;

        animator.SetTrigger(IdleBack);
    }

    public void PlayIdleFront()
    {
        ResetAllTriggers();

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
    // Force Play
    // ─────────────────────────────

    /// <summary>
    /// forcePlayClips 리스트에서 clipName과 동일한 이름의 AnimationClip을 찾아,
    /// 트리거/전이를 무시하고 즉시 해당 상태를 재생한다.
    /// (Animator에 State 이름 == Clip 이름이 존재해야 안전)
    /// </summary>
    public bool ForcePlayByClipName(string clipName, int layer = 0, float normalizedTime = 0f)
    {
        if (string.IsNullOrWhiteSpace(clipName)) return false;
        if (animator == null) return false;

        if (!_clipMap.TryGetValue(clipName, out var clip) || clip == null)
            return false;

        ResetAllTriggers();

        animator.Play(clip.name, layer, normalizedTime);
        animator.Update(0f);
        return true;
    }

    // ─────────────────────────────
    // Utils
    // ─────────────────────────────

    private void ResetAllTriggers()
    {
        if (animator == null) return;

        animator.ResetTrigger(IdleBack);
        animator.ResetTrigger(IdleFront);
        animator.ResetTrigger(Attack);
        animator.ResetTrigger(Hit);
        animator.ResetTrigger(Parry);
    }
}
