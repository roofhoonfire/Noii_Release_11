using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueEventManager : MonoBehaviour
{
    public static DialogueEventManager Instance { get; private set; }

    [Serializable]
    public class DialogueEventDef
    {
        [Header("Key")]
        public string eventCode;

        [Header("Graph")]
        public DialogueGraphAsset graph;

        [Header("Optional")]
        [Tooltip("비워두면 씬에서 FieldPlayerController3D를 자동 탐색")]
        public FieldPlayerController3D playerController;
    }

    private class RuntimeState
    {
        public bool running;
        public DialogueGraphRunner runner;
        public FieldPlayerController3D playerController;
    }

    [Header("Definitions (per-field static data)")]
    [SerializeField] private List<DialogueEventDef> definitions = new();

    private readonly Dictionary<string, DialogueEventDef> _defMap = new();
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
                Debug.LogWarning($"[DialogueEventManager] eventCode 비어있음 (index {i})");
                continue;
            }

            if (_defMap.ContainsKey(def.eventCode))
            {
                Debug.LogWarning($"[DialogueEventManager] 중복 eventCode: {def.eventCode}");
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
            Debug.LogWarning($"[DialogueEventManager] 정의되지 않은 eventCode: {eventCode}");
            return;
        }

        if (DialogueGraphUIController.Instance == null)
        {
            Debug.LogWarning("[DialogueEventManager] DialogueGraphUIController가 씬에 없음!");
            return;
        }

        // 이미 대화 UI가 재생 중이면 무시
        if (DialogueGraphUIController.Instance.IsPlaying)
            return;

        if (def.graph == null)
        {
            Debug.LogWarning($"[DialogueEventManager] graph가 비어있음: {eventCode}");
            return;
        }

        var st = GetOrCreateState(eventCode);
        if (st.running) return;

        // playerController 확보
        var pc = def.playerController != null ? def.playerController : FindFirstObjectByType<FieldPlayerController3D>();
        if (pc == null)
        {
            Debug.LogWarning("[DialogueEventManager] FieldPlayerController3D를 찾지 못함!");
            return;
        }

        // FieldPlayerStatus 확보
        var ps = pc.GetComponent<FieldPlayerStatus>() ?? pc.GetComponentInChildren<FieldPlayerStatus>();
        if (ps == null)
        {
            Debug.LogWarning("[DialogueEventManager] FieldPlayerStatus를 찾지 못함!");
            return;
        }

        // 이동 잠금
        pc.SetInputEnabled(false);

        // CharacterState 가져오기
        var charState = GameStateManager.Instance.GetCharacterState(ps.characterId);

        // Runner 시작 (owner는 null: 이벤트 대화는 Destroy 대상이 없다고 가정)
        st.running = true;
        st.playerController = pc;
        st.runner = new DialogueGraphRunner(def.graph, charState, owner: null);

        st.runner.Start(onFinished: () =>
        {
            // 이동 다시 켜기
            if (st.playerController != null)
                st.playerController.SetInputEnabled(true);

            st.running = false;
            st.runner = null;
            st.playerController = null;
        });
    }

    public void StopEvent(string eventCode)
    {
        if (!_stateMap.TryGetValue(eventCode, out var st) || st == null) return;
        if (!st.running) return;

        // 강제 종료 + UI 닫기
        if (st.runner != null)
            st.runner.Abort();   //  아래 3)에서 Runner에 추가할 함수

        // 입력 잠금 해제(안전)
        if (st.playerController != null)
            st.playerController.SetInputEnabled(true);

        st.running = false;
        st.runner = null;
        st.playerController = null;
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
}