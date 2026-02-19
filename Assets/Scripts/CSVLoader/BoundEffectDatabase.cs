// BoundEffectDatabase.cs
using System;
using System.Linq;
using UnityEngine;

public static class BoundEffectDatabase
{
    // 필요하면 파일명만 바꿔 끼우면 됨
    public static string csvResourceName = "boundsEffect";

    public static BoundEffectSO LoadEffectFromCSV()
    {
        var csvAsset = Resources.Load<TextAsset>(csvResourceName);
        if (csvAsset == null)
        {
            Debug.LogError($"[BoundEffectDatabase] Resources/{csvResourceName}.csv 를 찾을 수 없습니다.");
            return null;
        }

        var lines = csvAsset.text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Debug.LogError("[BoundEffectDatabase] CSV에 데이터가 없습니다.");
            return null;
        }

        var effects = lines
            .Skip(1)
            .Select((line, idx) => ParseLine(line, idx + 2))
            .Where(x => x != null)
            .ToArray();

        var so = ScriptableObject.CreateInstance<BoundEffectSO>();
        so.effects = effects;
        return so;
    }

    private static BoundEffect ParseLine(string line, int lineNumber)
    {
        // code, boundEffectType, amount, extraString, extraString2
        var cols = line.Split(',');
        if (cols.Length < 3)
        {
            Debug.LogWarning($"[BoundEffectDatabase] {lineNumber}번째 줄 열 부족: {cols.Length}개");
            return null;
        }

        try
        {
            // 4번째, 5번째는 없을 수도 있으니 안전 처리
            string extra1 = cols.Length > 3 ? cols[3].Trim() : string.Empty;
            string extra2 = cols.Length > 4 ? cols[4].Trim() : string.Empty;

            return new BoundEffect
            {
                code = (cols[0] ?? string.Empty).Trim(),
                boundEffectType = TryParseBoundEffectType(cols[1]),
                amount = TryParseInt(cols[2], 0),
                extraString = extra1,
                extraString2 = extra2,
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[BoundEffectDatabase] {lineNumber}번째 줄 파싱 실패: {e.Message}");
            return null;
        }
    }

    private static BoundEffectType TryParseBoundEffectType(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return BoundEffectType.UseLeft;

        var t = s.Trim();

        // "useLeft" 같은 문자열 대응 (대소문자 무시)
        if (Enum.TryParse<BoundEffectType>(t, true, out var parsed))
            return parsed;

        // 혹시 숫자면 0=UseLeft
        if (int.TryParse(t, out _))
            return BoundEffectType.UseLeft;

        Debug.LogWarning($"[BoundEffectDatabase] BoundEffectType 파싱 실패: '{t}' (기본 UseLeft로 처리)");
        return BoundEffectType.UseLeft;
    }

    private static int TryParseInt(string s, int def) =>
        int.TryParse((s ?? string.Empty).Trim(), out var v) ? v : def;
}