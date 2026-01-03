using UnityEngine;

public class EnemyAttackEventReceiver : MonoBehaviour
{
    [SerializeField] private BattleManager battle;

    public void ParryOpen(int hitIndex) => battle?.OnParryOpen(hitIndex);
    public void ParryClose(int hitIndex) => battle?.OnParryClose(hitIndex);
}
