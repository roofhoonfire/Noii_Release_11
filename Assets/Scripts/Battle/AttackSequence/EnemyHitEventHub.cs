using UnityEngine;

public class EnemyHitEventHub : MonoBehaviour
{
    public static EnemyHitEventHub Instance { get; private set; }

    private IEnemyHitEventHandler _activeHandler;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 필요 없으면 제거
    }

    public void SetActive(IEnemyHitEventHandler handler)
    {
        _activeHandler = handler;
    }

    public void ClearActive(IEnemyHitEventHandler handler)
    {
        // 같은 애가 해제할 때만 null 처리
        if (_activeHandler == handler)
            _activeHandler = null;
    }

    // Animation Event Receiver가 부르는 함수
    public void EnemyHit(int hitStopIndex)
    {
        _activeHandler?.OnEnemyHitEvent(hitStopIndex);
    }
}
