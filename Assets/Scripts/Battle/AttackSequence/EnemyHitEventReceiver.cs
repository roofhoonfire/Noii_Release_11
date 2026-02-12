using UnityEngine;

public class EnemyHitEventReceiver : MonoBehaviour
{
    public void EnemyHit(int hitStopIndex)
    {
        EnemyHitEventHub.Instance?.EnemyHit(hitStopIndex);
    }
}
