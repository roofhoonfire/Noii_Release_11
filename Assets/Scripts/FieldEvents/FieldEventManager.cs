using UnityEngine;

public class FieldEventManager : MonoBehaviour
{
    public enum EventType
    {
        BombAvoid,
        TimelineCinematic, // 미래 확장용
        // LaserAvoid, FallingRocks, etc...
    }

    [Header("Events (Scene References)")]
    [SerializeField] private BombAvoidEvent bombAvoidEvent;

    // ─────────────────────────────────────────────────────────────

    public void StartEvent(EventType type)
    {
        switch (type)
        {
            case EventType.BombAvoid:
                if (bombAvoidEvent != null) bombAvoidEvent.StartEvent();
                else Debug.LogWarning("[FieldEventManager] BombAvoidEvent 참조가 비어있음");
                break;

            case EventType.TimelineCinematic:
                Debug.Log("[FieldEventManager] TimelineCinematic은 추후 구현");
                break;
        }
    }

    public void StopEvent(EventType type)
    {
        switch (type)
        {
            case EventType.BombAvoid:
                if (bombAvoidEvent != null) bombAvoidEvent.StopEvent();
                break;

            case EventType.TimelineCinematic:
                Debug.Log("[FieldEventManager] TimelineCinematic은 추후 구현");
                break;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("BombAvoid Start");
            StartEvent(EventType.BombAvoid);
        }

        // 키보드 상단 숫자 2
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("BombAvoid Stop");
            StopEvent(EventType.BombAvoid);
        }
    }
}