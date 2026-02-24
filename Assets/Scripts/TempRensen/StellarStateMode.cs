// StellarOverallManager.cs
using System.Collections.Generic;
using UnityEngine;

public class StellarStateMode : MonoBehaviour
{
    public static StellarStateMode Instance { get; private set; }

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
    /// 요구 재료(노드리스트/노울리지) 만족하면:
    /// - TempManager.Instance.tempHands 에서 요구 카드코드들 제거
    /// - TempManager.Instance.tempKnowledges 에서 요구 노울리지들 제거
    /// - rewardCardCode 를 tempHands에 추가
    /// </summary>
    public bool TrySynthesize(List<StellarNode> requiredNodes, List<KnowledgeType> requiredKnowledges, string rewardCardCode)
    {
        if (BattleManager.Instance == null)
        {
            Debug.LogError("[StellarOverallManager] TempManager.Instance 없음");
            return false;
        }

        var hands = BattleManager.Instance.apData.hands;              // List<string>
        var knowledges = BattleManager.Instance.tempKnowledges;    // List<KnowledgeType>

        // 1) 요구 카드코드 리스트로 변환 (requiredNodes가 null이면 = 요구 없음)
        List<string> requiredCardCodes = null;
        if (requiredNodes != null)
        {
            requiredCardCodes = new List<string>(requiredNodes.Count);
            foreach (var n in requiredNodes)
            {
                if (n == null) continue;
                if (!string.IsNullOrEmpty(n.rewarCardCode))
                    requiredCardCodes.Add(n.rewarCardCode);
            }
        }

        // 2) 보유 재산이 요구 재료를 전부 가지고 있는지 검사(중복 포함)
        bool cardsOk = (requiredCardCodes == null) || ContainsAllWithCounts(hands, requiredCardCodes);
        bool knowOk = (requiredKnowledges == null) || ContainsAllWithCounts(knowledges, requiredKnowledges);

        if (!cardsOk || !knowOk)
        {
            Debug.Log("[StellarOverallManager] 합성 실패: 재료 부족");
            return false;
        }

        // 3) 요구 재료 제거
        if (requiredCardCodes != null)
        {
            foreach (var code in requiredCardCodes)
                RemoveOne(hands, code);
        }

        if (requiredKnowledges != null)
        {
            foreach (var k in requiredKnowledges)
                RemoveOne(knowledges, k);
        }

        // 4) 보상 지급
        if (!string.IsNullOrEmpty(rewardCardCode))
            hands.Add(rewardCardCode);

        Debug.Log($"[StellarOverallManager] 합성 성공! reward={rewardCardCode}");
        return true;
    }

    // ---------- helpers (중복 개수까지 고려) ----------

    private static bool ContainsAllWithCounts(List<string> owned, List<string> required)
    {
        if (required == null) return true;
        if (owned == null) return false;

        var counts = new Dictionary<string, int>();
        foreach (var s in owned)
        {
            if (string.IsNullOrEmpty(s)) continue;
            counts.TryGetValue(s, out int c);
            counts[s] = c + 1;
        }

        foreach (var r in required)
        {
            if (string.IsNullOrEmpty(r)) continue;

            if (!counts.TryGetValue(r, out int c) || c <= 0)
                return false;
            counts[r] = c - 1;
        }

        return true;
    }

    private static bool ContainsAllWithCounts(List<KnowledgeType> owned, List<KnowledgeType> required)
    {
        if (required == null) return true;
        if (owned == null) return false;

        var counts = new Dictionary<KnowledgeType, int>();
        foreach (var k in owned)
        {
            counts.TryGetValue(k, out int c);
            counts[k] = c + 1;
        }

        foreach (var r in required)
        {
            if (!counts.TryGetValue(r, out int c) || c <= 0)
                return false;
            counts[r] = c - 1;
        }

        return true;
    }

    private static void RemoveOne(List<string> list, string value)
    {
        if (list == null || string.IsNullOrEmpty(value)) return;
        int idx = list.IndexOf(value);
        if (idx >= 0) list.RemoveAt(idx);
    }

    private static void RemoveOne(List<KnowledgeType> list, KnowledgeType value)
    {
        if (list == null) return;
        int idx = list.IndexOf(value);
        if (idx >= 0) list.RemoveAt(idx);
    }
}