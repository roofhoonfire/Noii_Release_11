using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class FusionRecipe
{
    [Tooltip("레시피 고유 ID (예: FUS_001, slime_sword_v1 등)")]
    public string recipeId;

    [Tooltip("결과 카드 코드")]
    public string outputCode;

    [Tooltip("재료 카드 코드들 (2~3개). 순서는 무시되며, 저장 시 자동 정렬됨.")]
    public List<string> inputCodes = new List<string>(3);

    [Header("옵션 메타")]
    public int cost = 0;
    [Range(0f, 1f)] public float successRate = 1f;
    [TextArea] public string note;

    public void Normalize()
    {
        recipeId = recipeId?.Trim();
        outputCode = outputCode?.Trim();

        if (inputCodes == null) inputCodes = new List<string>(3);

        inputCodes = inputCodes
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        inputCodes.Sort(StringComparer.Ordinal);
    }

    public bool IsValid(out string reason)
    {
        reason = null;

        if (string.IsNullOrWhiteSpace(recipeId))
        {
            reason = "recipeId가 비어있음";
            return false;
        }

        if (string.IsNullOrWhiteSpace(outputCode))
        {
            reason = "outputCode가 비어있음";
            return false;
        }

        if (inputCodes == null)
        {
            reason = "inputCodes가 null";
            return false;
        }

        int n = inputCodes.Count(s => !string.IsNullOrWhiteSpace(s));
        if (n < 2 || n > 3)
        {
            reason = $"inputCodes 개수가 2~3이 아님 (현재 {n})";
            return false;
        }

        var normalized = inputCodes.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        if (normalized.Count != normalized.Distinct(StringComparer.Ordinal).Count())
        {
            reason = "inputCodes에 중복이 있음";
            return false;
        }

        if (normalized.Contains(outputCode.Trim(), StringComparer.Ordinal))
        {
            reason = "outputCode가 inputCodes에 포함됨";
            return false;
        }

        return true;
    }

    public string GetKey()
    {
        Normalize();
        return FusionKeyUtil.MakeKey(inputCodes);
    }
}


public static class FusionKeyUtil
{
    /// <summary>
    /// 입력 코드(2~3개)를 정렬/정리해서 "A|B" 또는 "A|B|C" 키로 만든다.
    /// </summary>
    public static string MakeKey(IEnumerable<string> inputs)
    {
        if (inputs == null) return string.Empty;

        var list = inputs
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        list.Sort(StringComparer.Ordinal);

        if (list.Count < 2 || list.Count > 3) return string.Empty;

        return string.Join("|", list);
    }

    public static string MakeKey(string a, string b)
        => MakeKey(new[] { a, b });

    public static string MakeKey(string a, string b, string c)
        => MakeKey(new[] { a, b, c });
}
