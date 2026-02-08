// EnemyHitEventReceiver.cs
using UnityEngine;

public class EnemyHitEventReceiver : MonoBehaviour
{
    /// <summary>
    /// Animation Event에서 호출할 함수.
    /// (애니메이션 클립 이벤트로 int 파라미터 전달 가능)
    /// </summary>
    public void EnemyHit(int hitStopIndex)
    {
        if (PlayerAttackSequenceDirector.Instance == null)
            return;

        PlayerAttackSequenceDirector.Instance.OnEnemyHitEvent(hitStopIndex);
    }
}
