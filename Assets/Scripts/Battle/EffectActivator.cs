using System;
using System.Collections;
using System.Drawing.Imaging;
using Unity.VisualScripting;
using UnityEngine;

public class EffectActivator : MonoBehaviour
{
    public static EffectActivator Instance { get; private set; }

    [Header("For Chant")]
    [SerializeField] private Transform chantSpawnA;
    [SerializeField] private Transform chantSpawnB;
    [SerializeField] private GameObject chantSelectPrefab;


    //임시긴함
    [Header("Effects")]
    [SerializeField] private GameObject Buff;
    [SerializeField] private GameObject Debuff;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 특정 Phase에 해당하는 이펙트들을 순차적으로 발동 (Coroutine)
    /// </summary>
    public IEnumerator ActivateEffects(ActionData actionData, Phase phase)
    {
        Debug.LogWarning($"{phase} 페이즈 , 시작");

        if (actionData == null || actionData.effects == null)
            yield break;

        foreach (var effect in actionData.effects)
        {
            if (effect == null)
                continue;

            if (effect.phase != phase)
                continue;

            switch (effect.effectType)
            {
                case EffectType.add_Damage:

                    NextActionMonitor.Instance.myNextAction.damage += effect.amount;
                    Buff.SetActive(true);
                    // TODO
                    break;

                case EffectType.SuddenAttack_On:
                    // TODO
                    break;

                case EffectType.Chant:
                    yield return ActivateChant(effect);
                    break;
                case EffectType.add_Chakra:
                    var BIap = BattleManager.Instance.apData;
                    if (BIap.currentChakra < BIap.maxChakraPoint)
                    {
                        BIap.currentChakra++;
                        // 연출
                        yield return BoundManager.Instance.RefreshChakraRoutine();

                    }
                    break;
                case EffectType.ifPrevWasTemp:
                    if (NextActionMonitor.Instance.prevExecuted != null && NextActionMonitor.Instance.prevExecuted.cardcode == effect.extraString)
                    {
                        actionData.damage += effect.amount;
                        Debuff.SetActive(true);

                    }
                    break;
                default:
                    Debug.LogWarning($"[EffectActivator] 처리되지 않은 EffectType: {effect.effectType}");
                    break;
            }

            yield return null; // (원하면) 한 프레임 쉬기
        }
    }

    // ─────────────────────────────
    // Chant (Coroutine)
    // ─────────────────────────────
    private IEnumerator ActivateChant(CardEffect effect)
    {
        if (effect == null) yield break;

        if (chantSelectPrefab == null || chantSpawnA == null || chantSpawnB == null)
        {
            Debug.LogWarning("[EffectActivator] Chant 세팅 누락(프리팹/스폰포인트)!");
            yield break;
        }

        if (CSVLoader.Instance == null)
        {
            Debug.LogWarning("[EffectActivator] CSVLoader.Instance가 없습니다.");
            yield break;
        }

        if (SelectionBuffer.Instance == null)
        {
            Debug.LogWarning("[EffectActivator] SelectionBuffer.Instance가 없습니다.");
            yield break;
        }

        SelectionBuffer.Instance.BeginSelection();

        string raw = effect.extraString ?? string.Empty;
        var codes = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        if (codes.Length == 0)
        {
            Debug.LogWarning("[EffectActivator] Chant extraString이 비어있음");
            yield break;
        }

        // 스폰된 프리팹 참조 보관
        GameObject spawnedA = SpawnChantSelect(chantSpawnA, codes, 0);
        GameObject spawnedB = SpawnChantSelect(chantSpawnB, codes, 1);

        while (!SelectionBuffer.Instance.isSelectingDone)
            yield return null;

        string selected = SelectionBuffer.Instance.selectedResultCode;
        Debug.Log($"[EffectActivator] Chant 선택 결과: {selected}");

        var thisCardData = CSVLoader.Instance.GetCardByCode(selected);
        var apData = BattleManager.Instance.apData;

        ActionData chantedAction = new ActionData();
        chantedAction.actionId = 1;
        chantedAction.defense = Mathf.Max(0, thisCardData.defense + apData.tempDef + apData.permDef);
        chantedAction.rumblePoint = thisCardData.rumblePoint + apData.tempRum;
        chantedAction.cardcode = thisCardData.code;
        chantedAction.animations = thisCardData.animations;
        chantedAction.actionClock = Mathf.Max(apData.CastingMinumum, thisCardData.actionClock + apData.permCast + apData.tempCast);
        chantedAction.cardname = thisCardData.name;
        chantedAction.zoneIndex = thisCardData.zoneIndex;
        chantedAction.seqType = thisCardData.seqType;

        // effects 리스트가 null일 가능성 있으면 여기서 new 해줘야 함 (네 ActionData 구현에 따라)
        chantedAction.effects.AddRange(CSVLoader.Instance.GetCardEffectsByCode(thisCardData.code));

        chantedAction.damage = Mathf.Max(0, thisCardData.damage + apData.tempDam + apData.permDam);

        BattleManager.Instance.SubmitPlayerAction_NotRegular(chantedAction, chantedAction.actionClock);

        //  스폰된 두 프리팹 파괴
        if (spawnedA != null) Destroy(spawnedA);
        if (spawnedB != null) Destroy(spawnedB);
    }


    private GameObject SpawnChantSelect(Transform spawnPoint, string[] codes, int index)
    {
        if (index >= codes.Length) return null;

        string code = codes[index].Trim();
        if (string.IsNullOrEmpty(code)) return null;

        var go = Instantiate(chantSelectPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);

        var each = go.GetComponent<EachCardInfo>();
        if (each == null)
        {
            Debug.LogWarning("[EffectActivator] ChantSelect 프리팹에 EachCardInfo가 없습니다.");
            Destroy(go);
            return null;
        }

        each.cardData = CSVLoader.Instance.GetCardByCode(code);
        if (each.cardData == null)
        {
            Debug.LogWarning($"[EffectActivator] 카드 코드 '{code}' 를 찾지 못함");
            Destroy(go);
            return null;
        }

        each.ApplyCardData();
        return go;
    }

}
