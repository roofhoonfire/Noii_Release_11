using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundActivator : MonoBehaviour
{

    public static BoundActivator Instance { get; private set; }

    // Start is called before the first frame update
    public IEnumerator ActivateBoundEffects(Bound bound)
    {
        if (bound == null)
            yield break;

        if (CSVLoader.Instance == null)
        {
            Debug.LogWarning("[ActivateBoundEffects] CSVLoader.Instance가 없음");
            yield break;
        }

        switch (bound.boundType)
        {
            case BoundType.Force:

                // 1. 해당 Bound에 연결된 모든 Effect 불러오기
                var effects = CSVLoader.Instance.GetBoundEffectsByCode(bound.code);

                if (effects == null || effects.Count == 0)
                {
                    Debug.Log($"[ActivateBoundEffects] Bound {bound.code}에 연결된 Effect 없음");
                    yield break;
                }

                // 2. 모든 Effect 순회
                foreach (var effect in effects)
                {
                    if (effect == null)
                        continue;

                    switch (effect.boundEffectType)
                    {
                        case BoundEffectType.UseLeft:
                            // TODO: UseLeft 로직
                            Debug.LogWarning($"뿡뿡: {effect.boundEffectType}");

                            break;

                        case BoundEffectType.Add_Damage:
                            // TODO: Add_Damage 로직
                            BattleManager.Instance.apData.tempDam -= effect.amount;
                            break;

                        case BoundEffectType.Add_Cast:
                            BattleManager.Instance.apData.tempCast -= effect.amount;
                            // TODO: Add_Cast 로직
                            break;

                        default:
                            Debug.LogWarning($"[ActivateBoundEffects] 알 수 없는 BoundEffectType: {effect.boundEffectType}");
                            break;
                    }
                }

                break;

            case BoundType.Condition:
                // TODO: Condition 타입 처리
                break;

            default:
                Debug.LogWarning($"[ActivateBoundEffects] 알 수 없는 BoundType: {bound.boundType}");
                break;
        }

        yield break;
    }
}
