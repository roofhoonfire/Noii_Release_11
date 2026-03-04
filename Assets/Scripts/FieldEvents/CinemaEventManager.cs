using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class CinemaEventManager : MonoBehaviour
{
    public static CinemaEventManager Instance { get; private set; }

    [Serializable]
    public class CinemaEventDef
    {
        [Header("Key")]
        public string eventCode;

        [Header("Timeline")]
        public PlayableDirector timeline;
    }

    private class RuntimeState
    {
        public bool running;
        public Coroutine routine;
    }

    [Header("Definitions (per-field static data)")]
    [SerializeField] private List<CinemaEventDef> definitions = new();

    [Header("Player Controller")]
    [SerializeField] private FieldPlayerController3D playerController;

    private readonly Dictionary<string, CinemaEventDef> _defMap = new();
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
                Debug.LogWarning($"[CinemaEventManager] eventCode 비어있음 (index {i})");
                continue;
            }

            if (_defMap.ContainsKey(def.eventCode))
            {
                Debug.LogWarning($"[CinemaEventManager] 중복 eventCode: {def.eventCode}");
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
            Debug.LogWarning($"[CinemaEventManager] 정의되지 않은 eventCode: {eventCode}");
            return;
        }

        if (def.timeline == null)
        {
            Debug.LogWarning($"[CinemaEventManager] Timeline 없음: {eventCode}");
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

        if (_defMap.TryGetValue(eventCode, out var def) && def.timeline != null)
        {
            def.timeline.Stop();
        }

        if (playerController != null)
            playerController.SetInputEnabled(true);
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

    private IEnumerator RunRoutine(CinemaEventDef def, RuntimeState st)
    {
        // 1) 플레이어 입력 봉쇄
        if (playerController != null)
            playerController.SetInputEnabled(false);

        // 2) 타임라인 재생
        def.timeline.Play();

        // 3) 타임라인 끝날 때까지 대기
        yield return new WaitForSeconds((float)def.timeline.duration);

        // 4) 입력 해제
        if (playerController != null)
            playerController.SetInputEnabled(true);

        st.running = false;
        st.routine = null;
    }
}