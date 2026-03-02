using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombAvoidEventManager : MonoBehaviour
{
    public static BombAvoidEventManager Instance { get; private set; }

    [Serializable]
    public class BombAvoidEventDef
    {
        [Header("Key")]
        public string eventCode;

        [Header("Pattern")]
        public BombPatternSO pattern;

        [Header("Zones Source")]
        [Tooltip("비워두면 씬에서 BombZone을 자동 탐색한다.")]
        public List<BombZone> zones = new();

        [Header("Hit Output")]
        public bool debugLogOnHit = true;
        public string hitLog = "우웩";
    }

    private class RuntimeState
    {
        public bool running;
        public Coroutine routine;
        public readonly Dictionary<int, BombZone> zoneMap = new();
    }

    [Header("BombAvoid Event Definitions (per-field static data)")]
    [SerializeField] private List<BombAvoidEventDef> definitions = new();

    private readonly Dictionary<string, BombAvoidEventDef> _defMap = new();
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
                Debug.LogWarning($"[BombAvoidEventManager] eventCode 비어있음 (index {i})");
                continue;
            }

            if (_defMap.ContainsKey(def.eventCode))
            {
                Debug.LogWarning($"[BombAvoidEventManager] 중복 eventCode: {def.eventCode}");
                continue;
            }

            _defMap.Add(def.eventCode, def);
        }
    }

    public bool Has(string eventCode) => !string.IsNullOrWhiteSpace(eventCode) && _defMap.ContainsKey(eventCode);

    public bool IsRunning(string eventCode)
    {
        return _stateMap.TryGetValue(eventCode, out var st) && st.running;
    }

    public void StartEvent(string eventCode)
    {
        if (!_defMap.TryGetValue(eventCode, out var def) || def == null)
        {
            Debug.LogWarning($"[BombAvoidEventManager] 정의되지 않은 eventCode: {eventCode}");
            return;
        }

        if (def.pattern == null || def.pattern.steps == null || def.pattern.steps.Count == 0)
        {
            Debug.LogWarning($"[BombAvoidEventManager] Pattern 비어있음: {eventCode}");
            return;
        }

        var st = GetOrCreateState(eventCode);

        if (st.running) return;

        // zones 자동탐색 (def.zones가 비어있으면)
        if (def.zones == null || def.zones.Count == 0)
            def.zones = new List<BombZone>(FindObjectsOfType<BombZone>(true));

        BuildZoneMap(st.zoneMap, def.zones, eventCode);

        st.running = true;
        st.routine = StartCoroutine(RunRoutine(def, st));
    }

    public void StopEvent(string eventCode)
    {
        if (!_stateMap.TryGetValue(eventCode, out var st) || st == null)
            return;

        if (!st.running) return;

        st.running = false;

        if (st.routine != null)
        {
            StopCoroutine(st.routine);
            st.routine = null;
        }

        st.zoneMap.Clear();
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

    private void BuildZoneMap(Dictionary<int, BombZone> map, List<BombZone> zones, string eventCode)
    {
        map.Clear();

        for (int i = 0; i < zones.Count; i++)
        {
            var z = zones[i];
            if (z == null) continue;

            if (map.ContainsKey(z.zoneId))
            {
                Debug.LogWarning($"[BombAvoidEventManager] ({eventCode}) 중복 zoneId: {z.zoneId} (오브젝트: {z.name})");
                continue;
            }

            map.Add(z.zoneId, z);
        }
    }

    private IEnumerator RunRoutine(BombAvoidEventDef def, RuntimeState st)
    {
        var pattern = def.pattern;

        while (st.running)
        {
            for (int i = 0; i < pattern.steps.Count; i++)
            {
                if (!st.running) yield break;

                var step = pattern.steps[i];
                if (step == null) continue;

                // 0) 스텝 시작 순간: 애니 트리거
                for (int k = 0; k < step.zoneIds.Count; k++)
                {
                    if (st.zoneMap.TryGetValue(step.zoneIds[k], out var zone) && zone != null)
                        zone.PlayBombTimelineLike();
                }

                // 1) 애니가 터지는 프레임까지 기다림
                if (step.delayBeforeExplode > 0f)
                    yield return new WaitForSeconds(step.delayBeforeExplode);

                // 2) 폭발 판정(순간)
                bool playerHit = false;

                for (int k = 0; k < step.zoneIds.Count; k++)
                {
                    if (!st.zoneMap.TryGetValue(step.zoneIds[k], out var zone) || zone == null)
                        continue;

                    if (!playerHit && zone.CheckPlayerHitNow())
                        playerHit = true;
                }

                if (playerHit && def.debugLogOnHit)
                    Debug.Log(def.hitLog);
            }

            if (!pattern.loop) break;

            if (pattern.loopInterval > 0f)
                yield return new WaitForSeconds(pattern.loopInterval);
        }

        st.running = false;
        st.routine = null;
        st.zoneMap.Clear();
    }
}