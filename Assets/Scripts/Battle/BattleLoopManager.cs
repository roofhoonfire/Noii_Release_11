using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using UnityEngine;
using SRandom = System.Random;

/// <summary>
/// PVE 배틀 메인 루프 담당
/// UI / 이펙트는 신경 안 쓰고,
/// 액션큐 / 시간 / 쇼다운 / 노말 액션만 관리.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("참조")]
    [SerializeField] private EnemyDefinitionSO enemyDefinition;

    [Header("디버그 시작값")]
    [SerializeField] private int playerStartHp = 100;
    [SerializeField] private int enemyStartHp = 100;
    [SerializeField] public List<string> deckList;


    // --- 액터들 ---
    public BattleActor Player { get; private set; }
    public BattleActor Enemy { get; private set; }
    public Animator EnemyAnimator;

    [Header("Select Mode UI")]
    [SerializeField] private SelectModeUIController selectModeUI;
    // --- 액션 큐 ---
    private struct QueuedAction
    {
        public int actorId;
        public ActionData action;
        public int remainingCost;
    }

    private readonly List<QueuedAction> _actionQueue = new();

    // 전역 액션 순번 (동일 코스트일 때 우선순위용)
    private int _globalActionCounter = 0;

    // 플레이어 액션 선택 대기 플래그
    private bool _waitingForPlayerAction = false;
    private ActionData _pendingPlayerAction;
    private int _pendingPlayerCost;

    // 전투 종료 플래그
    private bool _battleEnded = false;

    // 랜덤
    private SRandom _rng = new SRandom();

    // === 셀렉트 모드 관련 ===
    private Coroutine _playerSelectRoutine;
    private bool _isSelectModeActive = false;

    // ====== 유니티 라이프사이클 ======


    //패리
    private bool _parryWindowActive;
    private bool _parrySucceededThisWindow;
    private int _currentHitIndex;
    [Header("Parry Effect")]
    [SerializeField] GameObject parryEffects;
    [SerializeField] Transform parryTransform;

    [Header("Damage Popup")]
    [SerializeField] private DamageTextPopup popupPrefab;
    [SerializeField] private Transform popupSpawnPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitActors();
        StartCoroutine(BattleLoop());
        HPBarManager.Instance.HPBar_Init(100);
    }

    // ====== 초기화 ======

    private void InitActors()
    {
        Player = new BattleActor
        {
            actorId = 0,
            name = "Player",
            isPlayer = true,
            maxHp = playerStartHp,
            hp = playerStartHp,
            curPos = 0,
            energy = 3
        };

        Enemy = new BattleActor
        {
            actorId = 1,
            name = enemyDefinition != null ? enemyDefinition.enemyName : "Enemy",
            isPlayer = false,
            maxHp = enemyStartHp,
            hp = enemyStartHp,
            curPos = 0,
            energy = 3
        };
    }

    // ====== 외부에서 플레이어 액션 넘겨주는 함수 ======
    // (UI에서 버튼 누르거나 카드 선택 끝났을 때 이걸 호출하게 만들면 됨)

    /// <summary>
    /// 플레이어가 액션 선택을 완료했을 때 호출.
    /// </summary>
    public void SubmitPlayerAction(ActionData action, int cost)
    {
        if (!_waitingForPlayerAction)
        {
            Debug.LogWarning("지금은 플레이어 액션을 받을 타이밍이 아니다.");
            return;
        }

        _pendingPlayerAction = action;
        _pendingPlayerCost = cost;
        _waitingForPlayerAction = false;
    }

    // ====== 메인 배틀 루프 ======

    private IEnumerator BattleLoop()
    {
        // 메인 전투 루프
        while (!_battleEnded)
        {
            // 1) 양쪽이 최소 1개씩 액션을 준비하게 보장
            yield return EnsureEnemyHasQueuedAction();
            
            if (RemainingCostRenderer.Instance != null)
                yield return RemainingCostRenderer.Instance.ActionAddedRenderingRoutine(Enemy.actorId);

            yield return EnsurePlayerHasQueuedAction();
           
            if (RemainingCostRenderer.Instance != null)
                yield return RemainingCostRenderer.Instance.ActionAddedRenderingRoutine(Player.actorId);


            // 2) 큐가 비어있으면 의미 있는 일이 없음 → 다시 루프
            if (_actionQueue.Count == 0)
            {
                yield return null;
                continue;
            }

            // 3) 코스트/순번 기준 정렬
            SortActionQueue();

            // 4) 실제 시간 흐름 + 액션 처리 루프
            yield return ProcessQueueTick();

            // 5) 전투 종료 체크
            if (IsBattleOver())
            {
                _battleEnded = true;
            }
        }

        Debug.Log("배틀 종료!");
        // 여기서 결과 처리 (승리/패배) 호출하면 됨.
    }

    // ====== 액션 준비 단계 ======

    /// <summary>
    /// 몹 쪽에 남은 액션이 없으면, 몹 SO에서 하나 랜덤으로 뽑아서 큐에 넣는다.
    /// </summary>
    private IEnumerator EnsureEnemyHasQueuedAction()
    {
        if (HasAnyActionForActor(Enemy.actorId))
            yield break;

        if (enemyDefinition == null || enemyDefinition.actions.Count == 0)
        {
            Debug.LogError("EnemyDefinition 또는 액션 템플릿이 비어있다.");
            yield break;
        }

        // 랜덤으로 하나 픽
        var templates = enemyDefinition.actions;
        int index = _rng.Next(0, templates.Count);
        var tpl = templates[index];

        var action = new ActionData
        {
            actionId = 1,
            cardcode = tpl.cardCode,
            cardname = tpl.cardCode,
            damage = tpl.baseDamage,
            defense = tpl.baseDefense,
            rumblePoint = tpl.baseRumble,
            actionClock = tpl.baseActionClock,
            isDot = false,
            nthaction = 0,
            actionNotice = tpl.ActionNotice,
            isEnemyAction = true,
            parryable = tpl.parryable,
            animations = (tpl.attackAnim != null)
                    ? new List<AnimationClip> { tpl.attackAnim }
                    : null
        };
        if (tpl.parryWindows != null && tpl.parryWindows.Count > 0)
        {
            action.parryWindows = new List<ParryWindow>(tpl.parryWindows.Count);
            foreach (var w in tpl.parryWindows)
            {
                action.parryWindows.Add(new ParryWindow
                {
                    startTime = w.startTime,
                    endTime = w.endTime,
                    hitIndex = w.hitIndex
                });
            }
        }
        else
        {
            action.parryWindows = new List<ParryWindow>();
        }

        InitActionBeforeInsert(action, Enemy.actorId);

        _actionQueue.Add(new QueuedAction
        {
            actorId = Enemy.actorId,
            action = action,
            remainingCost = action.actionClock
        });


        yield break;
    }

    /// <summary>
    /// 플레이어 쪽에 남은 액션이 없으면, 
    /// 플레이어에게 "액션 골라라" 요청하고 SubmitPlayerAction 기다린다.
    /// </summary>
    private IEnumerator EnsurePlayerHasQueuedAction()
    {
        if (HasAnyActionForActor(Player.actorId))
            yield break;


        yield return SimpleTypewriterUI.Instance.TypeAndCloseRoutine("자신의 차례");

        Debug.Log("플레이어 액션 선택 대기 시작 (Enemy 의도는 이미 공개된 상태라고 가정)");

        _waitingForPlayerAction = true;
        _pendingPlayerAction = null;
        _pendingPlayerCost = 0;

        // === 셀렉트 모드 루프 시작 ===
        if (_playerSelectRoutine != null)
        {
            StopCoroutine(_playerSelectRoutine);
        }
        _playerSelectRoutine = StartCoroutine(PlayerSelectLoop());

        // 플레이어가 SubmitPlayerAction 호출할 때까지 대기
        yield return new WaitUntil(() => _waitingForPlayerAction == false);

        // 셀렉트 모드 종료
        _playerSelectRoutine = null;

        if (_pendingPlayerAction == null)
        {
            Debug.LogError("플레이어 액션이 null이다. 디폴트 이동이라도 넣어야 할 수 있음.");
            yield break;
        }

        InitActionBeforeInsert(_pendingPlayerAction, Player.actorId);

        _actionQueue.Add(new QueuedAction
        {
            actorId = Player.actorId,
            action = _pendingPlayerAction,
            remainingCost = _pendingPlayerCost
        });

        _pendingPlayerAction = null;
        _pendingPlayerCost = 0;
    }

    /// <summary>
    /// 셀렉트 모드:
    /// C → CardMode 예정 (현재는 로그만)
    /// I → ItemMode 예정 (현재는 로그만)
    /// ESC → 나중에 Card/Item 모드에서 셀렉트로 돌아올 때 쓸 예정, 지금은 그냥 로그
    /// </summary>
    private IEnumerator PlayerSelectLoop()
    {
       yield return BattleCameraDirector.Instance.SetShotAndWait(BattleCameraDirector.ShotId.Front);
        _isSelectModeActive = true;
        Debug.Log("=== SelectMode 진입: C=Card, I=Item (지금은 모드 없이 로그만) ===");
        selectModeUI.EnterSelectMode();


        while (_waitingForPlayerAction)
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                BattleCameraDirector.Instance.ExitSelectShot();

                CardModeState.Instance.SetActive(true);
                Debug.Log("[SelectMode] C 입력 → 여기서 CardMode 진입 예정 (현재는 로그만)");
                selectModeUI.ExitSelectMode();

                // 나중에: 여기서 CardModeState.Instance.SetActive(...) 같은 걸 호출하고,
                // 카드 선택 완료 시 SubmitPlayerAction(...) 호출하는 구조로 확장
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                Debug.Log("[SelectMode] I 입력 → 여기서 ItemMode 진입 예정 (현재는 로그만)");
                // 나중에: ItemModeState.Instance.SetActive(...) → 사용 확정 시 SubmitPlayerAction(...)
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[SelectMode] ESC 입력 → 현재는 아무 것도 안 하고 대기 유지");
                // 나중에: Card/Item 모드 안에서 ESC → SelectMode 복귀 용도로 사용
            }

            yield return null;
        }
        selectModeUI.ExitSelectMode();

        _isSelectModeActive = false;
        Debug.Log("SelectMode 종료 (_waitingForPlayerAction == false)");
    }

    private bool HasAnyActionForActor(int actorId)
    {
        return _actionQueue.Any(q => q.actorId == actorId && !q.action.isDot);
    }

    private void InitActionBeforeInsert(ActionData action, int actorId)
    {
        _globalActionCounter++;
        action.nthaction = _globalActionCounter;
        action.hasOtherExecutedSinceInsertion = false;

        // 이펙트 초기화가 필요하다면 여기에
        // action.InitializeEffects();
    }

    // ====== 큐 처리 ======

    private void SortActionQueue()
    {
        _actionQueue.Sort((a, b) =>
        {
            int costCmp = a.remainingCost.CompareTo(b.remainingCost);
            if (costCmp != 0) return costCmp;
            return a.action.nthaction.CompareTo(b.action.nthaction);
        });
    }

    /// <summary>
    /// 한 틱 동안 큐를 처리하는 코루틴.
    /// (여기서는 "틱"을 한 번의 while 루프로 보고, 안에서 TimeGoesOn + 발동 처리까지 한다)
    /// </summary>
    private IEnumerator ProcessQueueTick()
    {
        // 도중에 배틀이 끝날 수도 있으니까 방어
        if (_actionQueue.Count == 0)
            yield break;

        // 1) 시간 흐르게 (맨 앞놈 기준)
        TimeGoesOn();
        if (RemainingCostRenderer.Instance != null)
            yield return RemainingCostRenderer.Instance.PlayRemainingCostUpdate();
        // 2) 도트 액션 먼저 처리
        yield return ProcessDotActions();

        // 3) 남은 액션 중, remainingCost == 0 이면서 isDot == false 인 것들 체크
        var ready = _actionQueue
            .Where(q => q.remainingCost == 0 && !q.action.isDot)
            .ToList();

        if (ready.Count == 0)
        {
            // 이번 틱에서 발동할 액션 없음 → 다음 루프에서 다시 시간 흐르게
            yield break;
        }

        if (ready.Count >= 2)
        {
            // 쇼다운 – 여기선 가장 먼저 온 두 개만 처리한다고 가정 (PVE 기준)
            var q1 = ready[0];
            var q2 = ready[1];

            yield return ProcessShowdown(q1, q2);

            // 큐에서 제거
            RemoveFromQueue(q1);
            RemoveFromQueue(q2);
        }
        else
        {
            // 단일 액션 발동
            var q = ready[0];
            yield return ProcessSingleAction(q);
            RemoveFromQueue(q);
        }

        yield break;
    }

    private void TimeGoesOn()
    {
        if (_actionQueue.Count == 0)
            return;

        int minCost = _actionQueue.Min(q => q.remainingCost);
        if (minCost <= 0)
            return;

        for (int i = 0; i < _actionQueue.Count; i++)
        {
            var q = _actionQueue[i];
            q.remainingCost -= minCost;
            _actionQueue[i] = q;
        }
    }

    private IEnumerator ProcessDotActions()
    {
        // remainingCost == 0 && isDot == true 인 것들 전부 처리
        int idx = 0;
        while (idx < _actionQueue.Count)
        {
            var q = _actionQueue[idx];

            if (q.remainingCost != 0 || !q.action.isDot)
            {
                idx++;
                continue;
            }

            // 도트 훅은 일단 주석 처리 (훅 시스템 이후에 다시 연결)
            /*
            yield return q.action.Norm_ProcessHook(
                HookType.Dot,
                q.action.Dot_to,
                GetOtherActorId(q.action.Dot_to),
                q.action,
                null,
                null);
            */

            _actionQueue.RemoveAt(idx);
        }

        yield break;
    }

    private void RemoveFromQueue(QueuedAction target)
    {
        int index = _actionQueue.FindIndex(q =>
            q.actorId == target.actorId &&
            q.action == target.action &&
            q.remainingCost == target.remainingCost);

        if (index >= 0)
            _actionQueue.RemoveAt(index);
    }

    // ====== 단일 액션 발동 (노말 루트) ======

    private IEnumerator ProcessSingleAction(QueuedAction q)
    {
        Debug.Log($"{q.actorId}의 발동이다 크하하");
        int actorId = q.actorId;
        int opId = GetOtherActorId(actorId);

        var myAction = q.action;
        var opMainAction = FindMainActionOfActor(opId);
        var myMainAction = FindMainActionOfActor(actorId);

        // --- 몹 공격 + 패링 가능일 때 ---
        if (actorId == Enemy.actorId && myAction.parryable &&
            myAction.parryWindows != null && myAction.parryWindows.Count > 0)


        {
            BattleCameraDirector.Instance.SetShot(BattleCameraDirector.ShotId.Cam4);

            yield return SimpleTypewriterUI.Instance.TypeAndCloseRoutine(q.action.actionNotice);
            //   BattleCameraDirector.Instance.SetShot(BattleCameraDirector.ShotId.Back);
            //카메라 블렌드 이후 공격 연출
            yield return BattleCameraDirector.Instance.SetShotAndWait(BattleCameraDirector.ShotId.E_Attack_Cam1);

            yield return StartCoroutine(EnemyAttackWithParryWindows(q));


            yield return BattleCameraDirector.Instance.SetShotAndWait(BattleCameraDirector.ShotId.Back);

            // TODO: 나중에 parrySuccessCount / wasParried 보고 데미지/카운터 처리
            yield break;
        }
        if (actorId == Player.actorId)
        {
            // (예시) "공격 카드"라는 전제. 너의 게임 규칙에 맞춰 조건을 더 엄격히 하면 됨.
            // myAction.cardcode로 SO를 찾는다.
            var seq = PlayerAttackSequenceDatabase.Instance.Get(myAction.cardcode);
            if (seq != null)
            {

                //노티스 띄우기

                yield return SimpleTypewriterUI.Instance.TypeAndCloseRoutine(seq.actionNotice);

                // 여기서 “연출 전체”를 기다린다.
                yield return PlayerAttackSequenceDirector.Instance.Play(seq, myAction);

                // 연출이 끝난 뒤에 실제 데미지/상태이상 적용을 할지,
                // 아니면 연출 도중 특정 타이밍에 적용할지는 네가 선택.
                // (일단 지금은 연출 끝나고 적용하는 형태로 두자.)
               // ApplyDamageToEnemy(myAction.damage);

                yield break;
            }
            else
            {
                Debug.LogWarning($"[Battle] No PlayerAttackSequenceSO for cardcode={myAction.cardcode}");
                // 시퀀스 없으면 최소한의 처리라도
               // ApplyDamageToEnemy(myAction.damage);
                yield break;
            }
        }



        // TODO: 여기 나중에 플레이어 공격, 이동, 몹 비패링 공격 등 집어넣기

        yield break;
    }


    // ====== 쇼다운 (동시 발동) ======

    private IEnumerator ProcessShowdown(QueuedAction q1, QueuedAction q2)
    {
        // 훅/럼블 비교는 나중에 다시 살릴 예정이니, 일단 빈 상태로 둔다.
        yield break;
    }

    private ActionData FindMainActionOfActor(int actorId)
    {
        // asCombo == false 인 첫번째 액션을 메인으로 본다.
        // (없으면 null)
        var q = _actionQueue.FirstOrDefault(
            qa => qa.actorId == actorId && qa.action != null && !qa.action.asCombo);
        return q.action;
    }

    private void TurnEndForActor(int actorId)
    {
        var actor = GetActor(actorId);
        if (actor == null) return;

        // 턴 끝 처리 (temp 스탯 리셋 등) 나중에 추가
    }

    // ====== 유틸 ======

    private BattleActor GetActor(int actorId)
    {
        if (actorId == Player.actorId) return Player;
        if (actorId == Enemy.actorId) return Enemy;
        return null;
    }

    private int GetOtherActorId(int actorId)
    {
        if (actorId == Player.actorId) return Enemy.actorId;
        if (actorId == Enemy.actorId) return Player.actorId;
        return -1;
    }

    private bool IsBattleOver()
    {
        if (Player.hp <= 0) return true;
        if (Enemy.hp <= 0) return true;
        return false;
    }

    private IEnumerator EnemyAttackWithParryWindows(QueuedAction q)
    {
        var action = q.action;

        if (action.animations == null || action.animations.Count == 0)
        {
            Debug.LogWarning("[EnemyAttack] animations가 비어있다.");
            yield break;
        }

        var clip = action.animations[0];
        string stateName = clip.name;

        // 패리 입력 상태 초기화
        _parryWindowActive = false;
        _parrySucceededThisWindow = false;
        _currentHitIndex = -1;

        // 애니 재생
        EnemyAnimator.Play(stateName, 0, 0f);

        // Play 직후 상태가 아직 전환 중일 수 있어서 1~2프레임 보정
        yield return null;

        // “이 상태가 끝날 때까지” 루프
        while (true)
        {
            // 1) 패리 윈도우가 열려있는 동안만 입력 감시
            if (_parryWindowActive && !_parrySucceededThisWindow)
            {
                if (Input.GetKeyDown(KeyCode.A))
                {
                    _parrySucceededThisWindow = true;
                    Debug.Log($"[Parry] Input success at hitIndex={_currentHitIndex}");

                    // (옵션) 여기서 작은 피드백(사운드/짧은 이펙트)만 주고 싶으면 여기서.
                    // 큰 히트스탑/쉐이크는 Close에서 1번만.
                }
            }

            // 2) 애니가 끝났는지 체크
            // 현재 재생 상태가 우리가 킨 stateName인지 확인 + normalizedTime >= 1
            var st = EnemyAnimator.GetCurrentAnimatorStateInfo(0);

            // 주의: stateName이 “클립명 == 스테이트명”이라는 전제.
            // 만약 Animator State 이름이 다르면, 해시로 맞추는 방식으로 바꿔야 함.
            if (st.IsName(stateName) && st.normalizedTime >= 1f && !EnemyAnimator.IsInTransition(0))
                break;

            yield return null;
        }
        //  공격 끝 → Idle로 복귀
        if (enemyDefinition != null && enemyDefinition.idleAnim != null && EnemyAnimator != null)
        {
            EnemyAnimator.Play(enemyDefinition.idleAnim.name, 0, 0f);
            // (선택) yield return null;  // 바로 다음 프레임에 안정적으로 반영시키고 싶으면
        }
        // 공격 애니가 끝남. (Close 이벤트는 애니 프레임에 박혀있으니 이미 호출됐을 것)
        yield break;
    }



    /// <summary>
    /// 현재 액션 큐에서 해당 actorId의 "메인 액션"(isDot == false)의 남은 코스트를 가져온다.
    /// 성공 시 true, 못 찾으면 false 리턴.
    /// </summary>
    public bool TryGetRemainingCost(int actorId, out int remainingCost)
    {
        remainingCost = 0;

        if (_actionQueue == null || _actionQueue.Count == 0)
            return false;

        // isDot == false 인 액션 중 하나를 찾는다 (여기선 첫 번째)
        var q = _actionQueue.FirstOrDefault(
            qa => qa.actorId == actorId && qa.action != null && !qa.action.isDot);

        // 기본값(struct)과 같으면 못 찾은 것
        if (EqualityComparer<QueuedAction>.Default.Equals(q, default))
            return false;

        remainingCost = q.remainingCost;
        return true;
    }
    public void OnParryOpen(int hitIndex)
    {
        _parryWindowActive = true;
        _parrySucceededThisWindow = false;
        _currentHitIndex = hitIndex;

        Debug.Log($"[Parry] OPEN hitIndex={hitIndex}");
        // 사운드도 여기서 (지금은 로그)
    }
    public void OnParryClose(int hitIndex)
    {
        if (_currentHitIndex != hitIndex) return; // hitIndex 불일치 방지(선택)

        bool success = _parrySucceededThisWindow;

        _parryWindowActive = false;

        StartCoroutine(HitStopController.Instance.DoHitStop(0.06f, () =>
        {
            BattleCameraDirector.Instance.Shake(0.21f);
            Debug.Log(success ? "PARRY!" : "MISS!");


            if (success)
            {
                GameObject instance = Instantiate(parryEffects, parryTransform.position, Quaternion.identity);
                Destroy(instance, 2f); // Destroy in 2 seconds
                if (popupPrefab != null && popupSpawnPoint != null)
                {
                    var pop = Instantiate(popupPrefab, popupSpawnPoint.position, Quaternion.identity);
                    pop.Setup("PARRY!");
                }
            }
            if (!success)
            {
                HPBarManager.Instance.DamageMe();
                if (popupPrefab != null && popupSpawnPoint != null)
                {
                    var pop = Instantiate(popupPrefab, popupSpawnPoint.position, Quaternion.identity);
                    pop.Setup(10.ToString());
                }
            }
            
        }));
    }
  
}
