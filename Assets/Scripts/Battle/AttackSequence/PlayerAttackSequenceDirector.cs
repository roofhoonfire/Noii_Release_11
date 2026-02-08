using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

/// <summary>
/// Back -> Cam2(CM_Attack) 블렌드 완료 후:
/// - 루트 비주얼 OFF + 플레이어 빌보드 연출 ON
/// - 오버레이(배경/캐릭터컷인/컷라인) 연출
/// - Cam2 Dolly 이동 중 triggerPos 도달 순간:
///     * 플레이어 Freeze
///     * (옵션) Dolly 정지
///     * 컷라인 ON
///     * 컷씬 유지시간(cutInHoldSeconds) 홀드
/// - Cam3 -> EnemyHit + HitStop -> Back 복귀
///
/// IMPORTANT:
/// - seq.cutInSprite는 "캐릭터 컷인 이미지"로 사용한다.
/// - CutInUIController는 더 이상 사용하지 않는다.
/// </summary>
public class PlayerAttackSequenceDirector : MonoBehaviour
{
    public static PlayerAttackSequenceDirector Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private BattleCameraDirector cameraDirector;
    [SerializeField] private PlayerSequenceActor playerSequenceActor;

    [Header("Cam2 (CM_Attack) Dolly Ref")]
    [SerializeField] private CinemachineSplineDolly cam2SplineDolly;

    [Header("Root Player Visual (hide during sequence)")]
    [SerializeField] private GameObject rootPlayerVisual;

    [Header("Overlay UI")]
    [Tooltip("Canvas에 붙은 AttackOverlayUIController. 비워두면 Instance 사용.")]
    [SerializeField] private AttackOverlayUIController overlayUI;
    // PlayerAttackSequenceDirector.cs 상단에 추가

    [Header("Enemy Hit Effect Object (SpriteRenderer + Animator)")]
    [SerializeField] private GameObject enemyHitEffectObject;
    [SerializeField] private Animator enemyHitEffectAnimator;
    // ─────────────────────────────
    // Event-driven HitStop state
    // ─────────────────────────────
    private PlayerAttackSequenceSO _currentSeq;
    private ActionData _currentAction;
    private readonly HashSet<int> _firedHitStopIndices = new();
    private void Awake()
    {
        Instance = this;

        if (enemyHitEffectObject != null)
            enemyHitEffectObject.SetActive(false);
    }

    public IEnumerator Play(PlayerAttackSequenceSO seq, ActionData action)
    {
        if (seq == null)
        {
            Debug.LogError("[PlayerAttackSequenceDirector] seq is null");
            yield break;
        }

        if (cameraDirector == null)
            cameraDirector = BattleCameraDirector.Instance;

        if (overlayUI == null)
            overlayUI = AttackOverlayUIController.Instance;

        bool prevRootActive = rootPlayerVisual != null && rootPlayerVisual.activeSelf;




        // 0) 다음 시퀀스 번쩍임 방지:
        // Cam2가 Live 되기 전에 Dolly Position을 start로 박아둔다
        if (seq.useCam2Dolly && cam2SplineDolly != null)
            SetCam2DollyPos(seq.cam2DollyStart);

        // 1) Back 위에 Cam2 푸시 (블렌드 시작)
        if (cameraDirector != null)
            cameraDirector.PushShot(seq.cam2Shot);

        // 1.5) Cam2 블렌드 완료까지 대기
        if (cameraDirector != null)
            yield return cameraDirector.WaitForBlendComplete();

        // 2) 블렌드 완료 후: 루트 비주얼 OFF + 빌보드 ON
        if (rootPlayerVisual != null)
            rootPlayerVisual.SetActive(false);

        if (playerSequenceActor != null)
        {
            playerSequenceActor.gameObject.SetActive(true);
            playerSequenceActor.Play(seq.playerSequenceClip);
        }

        // 2.1) 오버레이 준비 (배경 + 캐릭터 컷인 스케줄)
        // - 배경: DollyDuration 동안 0->1
        // - 캐릭터 컷인: (DollyDuration - fadeSeconds) 뒤에 활성화(알파0) -> fadeSeconds 동안 0->1
        if (overlayUI != null)
        {
            overlayUI.HardReset();

            if (seq.useCam2Dolly)
            {
                /*이거 배경 먼저 생기고 그담에 컷씬
                 * 
                 * overlayUI.BeginBackgroundFadeIn(seq.cam2DollyDuration);

                float fade = Mathf.Clamp(seq.characterCutInFadeSeconds, 0f, seq.cam2DollyDuration);
                float delay = Mathf.Max(0f, seq.cam2DollyDuration - fade);

                // ★ seq.cutInSprite를 "캐릭터 컷인 이미지"로 사용
                overlayUI.PrepareAndScheduleCharacterCutIn(seq.cutInSprite, delay, fade);
           */

                float fade = Mathf.Clamp(seq.characterCutInFadeSeconds, 0f, seq.cam2DollyDuration);
                float delay = Mathf.Max(0f, seq.cam2DollyDuration - fade);

                // 배경도 캐릭터 컷인과 "동시에" 켜지고 같이 페이드
                overlayUI.PrepareAndScheduleBackground(null, delay, fade);

                // 캐릭터 컷인도 동일
                overlayUI.PrepareAndScheduleCharacterCutIn(seq.cutInSprite, delay, fade);
            }
            else
            {
                // Dolly 안 쓰면 여기서 별도 룰을 정의해야 함.
                // 지금은 요구사항이 Dolly 기반이므로 생략.
            }
        }

        // 3) Cam2 Dolly 이동 + triggerPos에서 컷씬 홀드
        if (seq.useCam2Dolly && cam2SplineDolly != null)
        {
            yield return MoveCam2DollyAndHoldAtTrigger(seq);
        }
        else
        {
            // Dolly 안 쓰는 경우: 기존 시간기반으로 "컷씬 홀드"를 만들려면 여기서 처리
            // 지금은 Dolly 기반이 핵심이라 생략.
        }
        // ─────────────────────────────
        // 3.5) (추가) 컷인 홀드 종료 후:
        //      Cam2를 유지한 채 후반부 빌보드 애니 재생
        //      해당 애니가 "끝나면" Cam3로 블렌드
        // ─────────────────────────────
        if (playerSequenceActor != null && seq.playerSequenceClipAfterCutIn != null)
        {
            // ※ 주의:
            // 이전 단계에서 FreezeAt으로 animator.speed=0이 되었을 수 있음.
            // Play()가 speed=1로 올려주므로 그대로 호출해도 됨.
            playerSequenceActor.Play(seq.playerSequenceClipAfterCutIn);

            // 애니 1회 재생이 끝날 때까지 Cam2 유지
            yield return WaitForClipFinished(playerSequenceActor, seq.playerSequenceClipAfterCutIn);
        }
        // 4) Cam3로 넘어감
        if (cameraDirector != null)
            cameraDirector.PushShot(seq.cam3Shot);

        //  Cam3 블렌드 완료까지 대기 (필수)
        if (cameraDirector != null)
            yield return cameraDirector.WaitForBlendComplete();

        //  5~6) Cam3에서 피격 이펙트 연출(이펙트 애니 재생 중 Cam3 유지)
        if (seq.useEnemyHitEffect && enemyHitEffectObject != null && enemyHitEffectAnimator != null && seq.enemyHitEffectClip != null)
        {
            yield return PlayEnemyHitEffectCam3(seq, action);
        }


        // 7) 정리
        if (playerSequenceActor != null)
            playerSequenceActor.UnfreezeAndHide();

        if (cameraDirector != null)
            cameraDirector.ResetToBack();

        if (rootPlayerVisual != null)
            rootPlayerVisual.SetActive(prevRootActive);

        // 오버레이 종료 + 알파 리셋(다음 시퀀스 대비)
        if (overlayUI != null)
            overlayUI.HideAllAndResetAlpha();

        // Cam2 Dolly도 start로 복귀(안전)
        if (seq.useCam2Dolly && cam2SplineDolly != null)
            SetCam2DollyPos(seq.cam2DollyStart);
    }

    // ─────────────────────────────────────────────
    // Dolly 이동 + 트리거 도달 시 컷씬 홀드(= cutInHoldSeconds)
    // ─────────────────────────────────────────────
    private IEnumerator MoveCam2DollyAndHoldAtTrigger(PlayerAttackSequenceSO seq)
    {
        float start = Mathf.Clamp01(seq.cam2DollyStart);
        float end = Mathf.Clamp01(seq.cam2DollyEnd);
        float triggerPos = Mathf.Clamp01(seq.cam2CutInTriggerPos);
        float duration = Mathf.Max(0.0001f, seq.cam2DollyDuration);

        bool reachedTrigger = false;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            float pos = Mathf.Lerp(start, end, u);
            SetCam2DollyPos(pos);

            if (!reachedTrigger)
            {
                bool reached = (end >= start) ? (pos >= triggerPos) : (pos <= triggerPos);
                if (reached)
                {
                    reachedTrigger = true;

                    // 1) Dolly를 트리거 지점으로 "정확히" 박는다(선택)
                    if (seq.stopDollyAtCutInPoint)
                        SetCam2DollyPos(triggerPos);

                    // 2) 플레이어 애니를 지정 시점에서 Freeze
                    if (playerSequenceActor != null)
                    {
                        yield return playerSequenceActor.WaitUntilNormalizedTime(seq.playerFreezeNormalizedTime);
                        if (seq.freezePlayerAtTime)
                            playerSequenceActor.FreezeAt(seq.playerFreezeNormalizedTime);
                    }

                    // 3) 컷라인 ON (트리거 시점에)
                    if (overlayUI != null)
                        overlayUI.ShowCutLine();

                    // 4) 컷씬 유지시간 동안 "정지"
                    if (seq.cutInHoldSeconds > 0f)
                        yield return WaitUnscaledSeconds(seq.cutInHoldSeconds);

                    // 5) 컷씬 끝나면 오버레이 전부 끄고 알파 리셋 + (선택) 짧은 딜레이 후 Cam3로 넘어가게
                    if (overlayUI != null)
                        overlayUI.HideAllAndResetAlpha();

                    if (seq.afterCutInDelayBeforeCam3 > 0f)
                        yield return WaitUnscaledSeconds(seq.afterCutInDelayBeforeCam3);

                    // 여기서 바로 빠져나가서 Cam3로 전환
                    yield break;
                }
            }

            yield return null;
        }

        // duration 끝났는데도 trigger를 못 쐈으면 보정
        if (!reachedTrigger)
        {
            SetCam2DollyPos(end);

            if (playerSequenceActor != null)
            {
                yield return playerSequenceActor.WaitUntilNormalizedTime(seq.playerFreezeNormalizedTime);
                if (seq.freezePlayerAtTime)
                    playerSequenceActor.FreezeAt(seq.playerFreezeNormalizedTime);
            }

            if (overlayUI != null)
                overlayUI.ShowCutLine();

            if (seq.cutInHoldSeconds > 0f)
                yield return new WaitForSeconds(seq.cutInHoldSeconds);

            if (overlayUI != null)
                overlayUI.HideAllAndResetAlpha();

            if (seq.afterCutInDelayBeforeCam3 > 0f)
                yield return new WaitForSeconds(seq.afterCutInDelayBeforeCam3);
        }
    }

    // ─────────────────────────────────────────────
    // Dolly setter
    // ─────────────────────────────────────────────
    private void SetCam2DollyPos(float normalized01)
    {
        if (cam2SplineDolly == null) return;

        cam2SplineDolly.PositionUnits = PathIndexUnit.Normalized;
        cam2SplineDolly.CameraPosition = Mathf.Clamp01(normalized01);
    }

    // timeScale 영향 받지 않는 대기
    private static IEnumerator WaitUnscaledSeconds(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static IEnumerator WaitAnimatorNormalizedUnscaled(Animator anim, float targetNormalized)
    {
        targetNormalized = Mathf.Clamp01(targetNormalized);
        if (targetNormalized <= 0f) yield break;

        yield return null;

        while (true)
        {
            var st = anim.GetCurrentAnimatorStateInfo(0);
            if (st.normalizedTime >= targetNormalized)
                break;

            yield return null;
        }
    }

    private static IEnumerator WaitForClipFinished(PlayerSequenceActor actor, AnimationClip clip)
    {
        if (actor == null || clip == null) yield break;

        // PlayerSequenceActor가 animator를 private으로 숨기고 있으니,
        // 여기서는 actor에 Animator 접근용 프로퍼티를 추가하는 편이 가장 깔끔하다.
        //  아래처럼 PlayerSequenceActor에 프로퍼티 1줄 추가 추천:
        // public Animator Animator => animator;

        var anim = actor.Animator;
        if (anim == null) yield break;

        string stateName = clip.name;

        // 1) 해당 클립 상태로 "진입"할 때까지 잠깐 대기 (전환 프레임 보정)
        //    (animator.Play를 했어도, 어떤 경우엔 다음 프레임에 stateinfo가 갱신됨)
        for (int i = 0; i < 3; i++)
        {
            var st0 = anim.GetCurrentAnimatorStateInfo(0);
            if (st0.IsName(stateName)) break;
            yield return null;
        }

        // 2) 클립 재생 완료 대기
        while (true)
        {
            var st = anim.GetCurrentAnimatorStateInfo(0);

            // 다른 상태로 튀었으면(전이/인터럽트) "끝난 걸로" 보고 빠져나갈지,
            // 아니면 기다릴지 정책이 필요한데, 여기선 '끝난 것으로 간주'가 안전.
            if (!st.IsName(stateName))
                yield break;

            // normalizedTime은 루프면 1을 계속 넘는다.
            // 지금은 "한 번 끝날 때"를 원하니 1 이상이면 종료.
            if (st.normalizedTime >= 1f)
                yield break;

            yield return null;
        }
    }
    private IEnumerator PlayEnemyHitEffectCam3(PlayerAttackSequenceSO seq, ActionData action)
    {
        // 1) 오브젝트 ON
        enemyHitEffectObject.SetActive(true);

        // 2) 상태 세팅
        _currentSeq = seq;
        _currentAction = action;
        _firedHitStopIndices.Clear();

        // 3) 애니 재생 (clip.name == stateName 가정)
        enemyHitEffectAnimator.speed = 1f;
        enemyHitEffectAnimator.Play(seq.enemyHitEffectClip.name, 0, 0f);

        // 4) 애니 끝까지 대기 (Cam3 유지)
        yield return WaitAnimatorClipEnd(enemyHitEffectAnimator, seq.enemyHitEffectClip.name);

        // 5) 상태 정리
        _currentSeq = null;
        _currentAction = null;
        _firedHitStopIndices.Clear();

        // 6) 오브젝트 OFF
        enemyHitEffectObject.SetActive(false);
    }

    /// <summary>
    /// 애니 이벤트 리시버가 호출하는 진입점
    /// </summary>
    public void OnEnemyHitEvent(int hitStopIndex)
    {
        // 시퀀스 재생 중이 아니면 무시
        if (_currentSeq == null) return;

        // 같은 인덱스 중복 발동 방지 (원하면 제거 가능)
        if (_firedHitStopIndices.Contains(hitStopIndex))
            return;

        _firedHitStopIndices.Add(hitStopIndex);

        var info = _currentSeq.GetHitStopInfo(hitStopIndex);
        if (info == null)
        {
            Debug.LogWarning($"[PlayerAttackSequenceDirector] HitStopInfo not found for index={hitStopIndex} (cardCode={_currentSeq.cardCode})");
            return;
        }

        // 지금 요구사항: "히트스톱만" 처리
        StartCoroutine(DoHitStopFromInfo(info));
    }

    private IEnumerator DoHitStopFromInfo(PlayerAttackSequenceSO.HitStopInfo info)
    {
        if (HitStopController.Instance == null)
            yield break;


        BattleManager.Instance.HitStopRendering(_currentAction);
        
        yield return HitStopController.Instance.DoHitStop(
            info.hitStopSeconds,
            onEnterHitStop: () =>
            {
                if (cameraDirector != null)
                    cameraDirector.Shake(info.cameraShakeStrength);
            });


        // 추후 확장 포인트:
        // - info.enemyHitTrigger 기반으로 데미지/이펙트/HP 처리
        // - BattleManager.Instance.AttackSequencePhase(_currentAction) 같은 호출
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

            // 다른 상태로 튀면 끝난 걸로 간주(안전)
            if (!st.IsName(stateName))
                yield break;

            if (st.normalizedTime >= 1f)
                yield break;

            yield return null;
        }
    }

}
