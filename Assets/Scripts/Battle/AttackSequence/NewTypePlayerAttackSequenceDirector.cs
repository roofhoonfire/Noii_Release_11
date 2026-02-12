using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewTypePlayerAttackSequenceDirector : MonoBehaviour ,IEnemyHitEventHandler
{
    public static NewTypePlayerAttackSequenceDirector Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private BattleCameraDirector cameraDirector;

    [Header("Root Player (anim plays directly here)")]
    [SerializeField] private Animator rootPlayerAnimator;

    [Header("Small Cutscene UI Object (Animator + GameObject)")]
    [SerializeField] private GameObject smallCutsceneObject;
    [SerializeField] private Animator smallCutsceneAnimator;

    [Header("Enemy Hit Effect Object (SpriteRenderer + Animator)")]
    [SerializeField] private GameObject enemyHitEffectObject;
    [SerializeField] private Animator enemyHitEffectAnimator;

    // 상태
    private NewTypePlayerAttackSequenceSO _currentSeq;
    private ActionData _currentAction;
    private readonly HashSet<int> _firedHitStopIndices = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (smallCutsceneObject != null) smallCutsceneObject.SetActive(false);
        if (enemyHitEffectObject != null) enemyHitEffectObject.SetActive(false);
    }

    public IEnumerator Play(NewTypePlayerAttackSequenceSO seq, ActionData action)
    {
        if (seq == null)
        {
            Debug.LogError("[NewTypePlayerAttackSequenceDirector] seq is null");
            yield break;
        }

        if (cameraDirector == null)
            cameraDirector = BattleCameraDirector.Instance;

        if (rootPlayerAnimator == null)
        {
            Debug.LogError("[NewTypePlayerAttackSequenceDirector] rootPlayerAnimator is null");
            yield break;
        }

        // 0) 노티스
        if (!string.IsNullOrEmpty(seq.actionNotice) && SimpleTypewriterUI.Instance != null)
            yield return SimpleTypewriterUI.Instance.TypeAndCloseRoutine(seq.actionNotice);

        // 1) 와이드샷
        if (cameraDirector != null)
        {
            cameraDirector.PushShot(seq.wideShot);
            yield return cameraDirector.WaitForBlendComplete();
        }

        // 2) 루트 플레이어: 준비 애니
        if (seq.playerPrepareClip != null)
        {
            rootPlayerAnimator.speed = 1f;
            rootPlayerAnimator.Play(seq.playerPrepareClip.name, 0, 0f);
            yield return WaitAnimatorClipEnd(rootPlayerAnimator, seq.playerPrepareClip.name);
        }

        // 3) 공격발동 애니 시작 + 스몰컷씬 ON + 스몰컷씬 애니 시작(동시에)
        if (seq.playerAttackClip != null)
        {
            rootPlayerAnimator.speed = 1f;
            rootPlayerAnimator.Play(seq.playerAttackClip.name, 0, 0f);

            // 스몰컷씬 동시 재생
            if (smallCutsceneObject != null && smallCutsceneAnimator != null && seq.smallCutsceneClip != null)
            {
                smallCutsceneObject.SetActive(true);
                smallCutsceneAnimator.speed = 1f;
                smallCutsceneAnimator.Play(seq.smallCutsceneClip.name, 0, 0f);
            }

            // 공격발동 애니 끝까지 대기
            yield return WaitAnimatorClipEnd(rootPlayerAnimator, seq.playerAttackClip.name);
        }

        // 스몰컷씬은 공격발동 끝나면 OFF (요구사항)
        if (smallCutsceneObject != null)
            smallCutsceneObject.SetActive(false);

        // 4) 공격발동 끝나는 순간 -> 에너미 샷 전환
        if (cameraDirector != null)
        {
            cameraDirector.PushShot(seq.enemyShot);
            yield return cameraDirector.WaitForBlendComplete();
        }

        // 5) Cam3에서 피격 이펙트 + 히트스톱(기존 방식 동일)
        if (seq.useEnemyHitEffect && enemyHitEffectObject != null && enemyHitEffectAnimator != null && seq.enemyHitEffectClip != null)
        {
            yield return PlayEnemyHitEffectAndHitStop(seq, action);
        }

        // 6) 정리: (카메라 Back 복귀는 기존 시스템이 하는 방식이 있으니 여기서는 ResetToBack만)
        if (cameraDirector != null)
            cameraDirector.ResetToBack();
    }

    private IEnumerator PlayEnemyHitEffectAndHitStop(NewTypePlayerAttackSequenceSO seq, ActionData action)
    {
        //허브 등장~!!!!
        EnemyHitEventHub.Instance?.SetActive(this);

        enemyHitEffectObject.SetActive(true);

        _currentSeq = seq;
        _currentAction = action;
        _firedHitStopIndices.Clear();
        try
        {
            enemyHitEffectAnimator.speed = 1f;
            enemyHitEffectAnimator.Play(seq.enemyHitEffectClip.name, 0, 0f);

            yield return WaitAnimatorClipEnd(enemyHitEffectAnimator, seq.enemyHitEffectClip.name);
        }
        finally
        {
            // 1) 상태 정리
            _currentSeq = null;
            _currentAction = null;
            _firedHitStopIndices.Clear();

            enemyHitEffectObject.SetActive(false);

            // 2) Hub 해제 (무조건!)
            EnemyHitEventHub.Instance?.ClearActive(this);
        }
    }

    /// <summary>
    /// EnemyHitEffect 애니메이션 이벤트에서 int 파라미터로 호출할 것
    /// </summary>
    public void OnEnemyHitEvent(int hitStopIndex)
    {
        if (_currentSeq == null) return;

        if (_firedHitStopIndices.Contains(hitStopIndex))
            return;

        _firedHitStopIndices.Add(hitStopIndex);

        var info = _currentSeq.GetHitStopInfo(hitStopIndex);
        if (info == null)
        {
            Debug.LogWarning($"[NewTypePlayerAttackSequenceDirector] HitStopInfo not found index={hitStopIndex} cardCode={_currentSeq.cardCode}");
            return;
        }

        StartCoroutine(DoHitStopFromInfo(info));
    }

    private IEnumerator DoHitStopFromInfo(NewTypePlayerAttackSequenceSO.HitStopInfo info)
    {
        if (HitStopController.Instance == null)
            yield break;

        // 네 기존 처리 그대로
        if (BattleManager.Instance != null)
            BattleManager.Instance.HitStopRendering(_currentAction);

        yield return HitStopController.Instance.DoHitStop(
            info.hitStopSeconds,
            onEnterHitStop: () =>
            {
                if (cameraDirector != null)
                    cameraDirector.Shake(info.cameraShakeStrength);
            });
    }

    private static IEnumerator WaitAnimatorClipEnd(Animator anim, string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName)) yield break;

        // state 진입 보정
        for (int i = 0; i < 3; i++)
        {
            var st0 = anim.GetCurrentAnimatorStateInfo(0);
            if (st0.IsName(stateName)) break;
            yield return null;
        }

        while (true)
        {
            var st = anim.GetCurrentAnimatorStateInfo(0);

            // 다른 상태로 튀면 끝난 걸로 간주
            if (!st.IsName(stateName))
                yield break;

            if (st.normalizedTime >= 1f)
                yield break;

            yield return null;
        }
    }
}
