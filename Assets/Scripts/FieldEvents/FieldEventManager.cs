using System;
using System.Collections.Generic;
using UnityEngine;

public class FieldEventManager : MonoBehaviour
{
    public static FieldEventManager Instance { get; private set; }

    public enum EventType
    {
        BombAvoid,
        DiceRoll,
        Dialogue,          //  추가

        Cinema,
        // LaserAvoid, FallingRocks, etc...
    }

    [Serializable]
    public class EventEntry
    {
        public EventType type;
        public string eventCode; // 필드 내에서의 개별 이벤트 식별 코드
    }

    [Header("Field Event Registry (Type + Code)")]
    [SerializeField] private List<EventEntry> fieldEvents = new();

    // type -> set(codes)
    private readonly Dictionary<EventType, HashSet<string>> _registry = new();

    private void Awake()
    {
        // 필드마다 1개만 존재한다는 전제지만, 안전장치
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildRegistry();
    }

    private void BuildRegistry()
    {
        _registry.Clear();

        for (int i = 0; i < fieldEvents.Count; i++)
        {
            var e = fieldEvents[i];
            if (e == null) continue;

            if (string.IsNullOrWhiteSpace(e.eventCode))
            {
                Debug.LogWarning($"[FieldEventManager] eventCode 비어있음 (index {i})");
                continue;
            }

            if (!_registry.TryGetValue(e.type, out var set))
            {
                set = new HashSet<string>();
                _registry.Add(e.type, set);
            }

            if (!set.Add(e.eventCode))
                Debug.LogWarning($"[FieldEventManager] 중복 등록: {e.type} / {e.eventCode}");
        }
    }

    public bool HasEvent(EventType type, string eventCode)
    {
        if (string.IsNullOrWhiteSpace(eventCode)) return false;
        return _registry.TryGetValue(type, out var set) && set.Contains(eventCode);
    }

    public void StartEvent(EventType type, string eventCode)
    {
        if (!HasEvent(type, eventCode))
        {
            Debug.LogWarning($"[FieldEventManager] 이 필드에 등록되지 않은 이벤트: {type} / {eventCode}");
            return;
        }

        switch (type)
        {
            case EventType.BombAvoid:
                if (BombAvoidEventManager.Instance != null)
                    BombAvoidEventManager.Instance.StartEvent(eventCode);
                else
                    Debug.LogWarning("[FieldEventManager] BombAvoidEventManager Instance가 없음");
                break;
            case EventType.DiceRoll:
                if (DiceRollEventManager.Instance != null)
                    DiceRollEventManager.Instance.StartEvent(eventCode);
                else
                    Debug.LogWarning("[FieldEventManager] DiceRollEventManager Instance가 없음");
                break;

            case EventType.Dialogue:
                if (DialogueEventManager.Instance != null)
                    DialogueEventManager.Instance.StartEvent(eventCode);
                else
                    Debug.LogWarning("[FieldEventManager] DialogueEventManager Instance가 없음");
                break;
            case EventType.Cinema:
                if (CinemaEventManager.Instance != null)
                    CinemaEventManager.Instance.StartEvent(eventCode);
                else
                    Debug.LogWarning("[FieldEventManager] CinemaEventManager Instance가 없음");
                break;

            default:
                Debug.LogWarning($"[FieldEventManager] 미지원 타입: {type}");
                break;
        }
    }

    public void StopEvent(EventType type, string eventCode)
    {
        if (!HasEvent(type, eventCode))
        {
            Debug.LogWarning($"[FieldEventManager] 이 필드에 등록되지 않은 이벤트: {type} / {eventCode}");
            return;
        }

        switch (type)
        {
            case EventType.BombAvoid:
                if (BombAvoidEventManager.Instance != null)
                    BombAvoidEventManager.Instance.StopEvent(eventCode);
                break;
            case EventType.DiceRoll:
                if (DiceRollEventManager.Instance != null)
                    DiceRollEventManager.Instance.StopEvent(eventCode);
                break;

            case EventType.Dialogue:
                if (DialogueEventManager.Instance != null)
                    DialogueEventManager.Instance.StopEvent(eventCode);
                break;
            case EventType.Cinema:
                if (CinemaEventManager.Instance != null)
                    CinemaEventManager.Instance.StopEvent(eventCode);
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 디버그용 키 입력은 요구대로 무시(주석 처리)
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("BombAvoid Start");
            StartEvent(EventType.BombAvoid, "Bomb_1");
        }

    
    }
    
}