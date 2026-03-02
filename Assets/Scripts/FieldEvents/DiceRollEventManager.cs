using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DiceRollEventManager : MonoBehaviour
{
    public static DiceRollEventManager Instance { get; private set; }

    public enum Stat
    {
        str, dex, def, intel
    }

    [Serializable]
    public enum EventAction
    {
        Start,
        Stop
    }

    [Serializable]
    public class EventCall
    {
        public FieldEventManager.EventType type;
        public string eventCode;
        public EventAction action; //  추가
    }

    [Serializable]
    public class DiceRollEventDef
    {
        [Header("Key")]
        public string eventCode;

        [Header("Rule")]
        public int targetNumber = 7;
        public Stat stat = Stat.str;

        [Header("On Success Calls")]
        public List<EventCall> successCalls = new();

        [Header("On Fail Calls")]
        public List<EventCall> failCalls = new();
    }

    [Serializable]
    public class DiceAnimatorBinding
    {
        [Header("Animator")]
        public Animator animator;

        [Tooltip("Animator Controller 안에 존재하는, 주사위가 '굴러서 결과를 보여주는' 베이스 상태 이름")]
        public string baseStateName = "DiceBaseState";

        [Tooltip("Animator Controller에서 baseState가 사용하고 있는 '원본 클립'(Override 대상)")]
        public AnimationClip baseClip;

        [Header("Face Clips (1~6)")]
        public AnimationClip face1;
        public AnimationClip face2;
        public AnimationClip face3;
        public AnimationClip face4;
        public AnimationClip face5;
        public AnimationClip face6;

        public AnimationClip GetFaceClip(int face)
        {
            return face switch
            {
                1 => face1,
                2 => face2,
                3 => face3,
                4 => face4,
                5 => face5,
                6 => face6,
                _ => null
            };
        }
    }

    private class RuntimeState
    {
        public bool running;
        public Coroutine routine;
    }

    [Header("Definitions (per-field static data)")]
    [SerializeField] private List<DiceRollEventDef> definitions = new();

    [Header("UI Root")]
    [SerializeField] private GameObject uiRoot;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text statText;         // "어떤 스탯"
    [SerializeField] private TMP_Text targetText;       // "목표 숫자"
    [SerializeField] private TMP_Text resultText;       // "성공/실패"

    [Header("Dice Objects (2)")]
    [SerializeField] private DiceAnimatorBinding dice1;
    [SerializeField] private DiceAnimatorBinding dice2;

    [Header("Timing")]
    [SerializeField] private float afterResultHoldSeconds = 1.0f;

    private readonly Dictionary<string, DiceRollEventDef> _defMap = new();
    private readonly Dictionary<string, RuntimeState> _stateMap = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildDefMap();
    }

    private void BuildDefMap()
    {
        _defMap.Clear();

        for (int i = 0; i < definitions.Count; i++)
        {
            var def = definitions[i];
            if (def == null) continue;

            if (string.IsNullOrWhiteSpace(def.eventCode))
            {
                Debug.LogWarning($"[DiceRollEventManager] eventCode 비어있음 (index {i})");
                continue;
            }

            if (_defMap.ContainsKey(def.eventCode))
            {
                Debug.LogWarning($"[DiceRollEventManager] 중복 eventCode: {def.eventCode}");
                continue;
            }

            _defMap.Add(def.eventCode, def);
        }
    }

    public bool IsRunning(string eventCode)
    {
        return _stateMap.TryGetValue(eventCode, out var st) && st.running;
    }

    public void StartEvent(string eventCode)
    {
        if (!_defMap.TryGetValue(eventCode, out var def) || def == null)
        {
            Debug.LogWarning($"[DiceRollEventManager] 정의되지 않은 eventCode: {eventCode}");
            return;
        }

        if (uiRoot == null || statText == null || targetText == null || resultText == null)
        {
            Debug.LogWarning("[DiceRollEventManager] UI 참조가 비어있음 (uiRoot/statText/targetText/resultText)");
            return;
        }

        if (dice1.animator == null || dice2.animator == null)
        {
            Debug.LogWarning("[DiceRollEventManager] dice Animator 참조가 비어있음");
            return;
        }

        var st = GetOrCreateState(eventCode);
        if (st.running) return;

        st.running = true;
        st.routine = StartCoroutine(RunRoutine(def, st));
    }

    public void StopEvent(string eventCode)
    {
        if (!_stateMap.TryGetValue(eventCode, out var st) || st == null) return;
        if (!st.running) return;

        st.running = false;

        if (st.routine != null)
        {
            StopCoroutine(st.routine);
            st.routine = null;
        }

        // UI는 즉시 닫아줌
        if (uiRoot != null) uiRoot.SetActive(false);
    }

    private RuntimeState GetOrCreateState(string eventCode)
    {
        if (!_stateMap.TryGetValue(eventCode, out var st) || st == null)
        {
            st = new RuntimeState();
            _stateMap[eventCode] = st;
        }
        return st;
    }

    private IEnumerator RunRoutine(DiceRollEventDef def, RuntimeState st)
    {
        // 0) UI ON + 메타 반영
        uiRoot.SetActive(true);

        statText.text = def.stat.ToString();                  // "str" 이런 식으로 표시됨
        targetText.text = def.targetNumber.ToString();
        resultText.text = "";                                 // 아직 결과 전

        // 1) 1~6 랜덤 2번
        int a = UnityEngine.Random.Range(1, 7);
        int b = UnityEngine.Random.Range(1, 7);

        // 2) 첫 번째 주사위 애니
        float len1 = PlayDiceFace(dice1, a);
        if (len1 > 0f) yield return new WaitForSeconds(len1);
        else yield return null;

        // 3) 두 번째 주사위 애니
        float len2 = PlayDiceFace(dice2, b);
        if (len2 > 0f) yield return new WaitForSeconds(len2);
        else yield return null;

        // 4) 성공/실패 판정
        int sum = a + b;
        bool success = (sum >= def.targetNumber);

        resultText.text = success ? "성공" : "실패";

        // 5) 분기 이벤트 호출
        var fem = FieldEventManager.Instance;
        var calls = success ? def.successCalls : def.failCalls;

        if (fem != null && calls != null)
        {
            for (int i = 0; i < calls.Count; i++)
            {
                var c = calls[i];
                if (c == null) continue;

                if (string.IsNullOrWhiteSpace(c.eventCode)) continue;

                // (선택) 필드에 등록된 이벤트인지 먼저 확인해도 안전함
                if (!fem.HasEvent(c.type, c.eventCode))
                {
                    Debug.LogWarning($"[DiceRollEventManager] 분기 호출 이벤트가 필드에 등록되지 않음: {c.type}/{c.eventCode}");
                    continue;
                }

                if (c.action == EventAction.Stop)
                    fem.StopEvent(c.type, c.eventCode);
                else
                    fem.StartEvent(c.type, c.eventCode);
            }
        }
        else if (fem == null)
        {
            Debug.LogWarning("[DiceRollEventManager] FieldEventManager.Instance가 없음");
        }

        // 6) 1초 홀드 후 UI OFF
        if (afterResultHoldSeconds > 0f)
            yield return new WaitForSeconds(afterResultHoldSeconds);

        uiRoot.SetActive(false);

        st.running = false;
        st.routine = null;
    }

    private float PlayDiceFace(DiceAnimatorBinding binding, int face)
    {
        if (binding.animator == null) return 0f;

        var faceClip = binding.GetFaceClip(face);
        if (faceClip == null)
        {
            Debug.LogWarning($"[DiceRollEventManager] faceClip 비어있음: {face}");
            return 0f;
        }

        if (binding.baseClip == null)
        {
            Debug.LogWarning("[DiceRollEventManager] baseClip이 비어있음 (AnimatorOverrideController로 강제 재생하려면 필요)");
            // 그래도 시도: state만 플레이
            binding.animator.Play(binding.baseStateName, 0, 0f);
            return 0f;
        }

        // OverrideController 구성(매 호출마다 새로 만들어도 OK / 비용 작음)
        var original = binding.animator.runtimeAnimatorController;
        if (original == null)
        {
            Debug.LogWarning("[DiceRollEventManager] runtimeAnimatorController가 없음");
            return 0f;
        }

        var overrideController = new AnimatorOverrideController(original);
        overrideController[binding.baseClip] = faceClip;
        binding.animator.runtimeAnimatorController = overrideController;

        // 강제 재생
        binding.animator.Play(binding.baseStateName, 0, 0f);
        return faceClip.length;
    }
}