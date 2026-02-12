using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Battle/EnemyDefinition")]
public class EnemyDefinitionSO : ScriptableObject
{
    public string enemyId;
    public string enemyName;
    public int maxHp = 100;



    [Header("Animations")]
    public AnimationClip idleAnim;

    [Tooltip("이 몹이 사용할 수 있는 액션 템플릿들")]
    public List<EnemyActionTemplate> actions = new();
}
