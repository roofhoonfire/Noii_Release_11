// JujuEffectDatabase.cs
using System;
using System.Linq;
using UnityEngine;

public static class JujuEffectDatabase
{
    public static string csvResourceName = "jujusEffect";

    public static JujuEffectSO LoadEffectFromCSV()
    {
        var csvAsset = Resources.Load<TextAsset>(csvResourceName);
        if (csvAsset == null)
        {
            Debug.LogError($"[JujuEffectDatabase] Resources/{csvResourceName}.csv 를 찾을 수 없습니다.");
            return null;
        }

        var lines = csvAsset.text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Debug.LogError("[JujuEffectDatabase] CSV에 데이터가 없습니다.");
            return null;
        }

        var effects = lines
            .Skip(1)
            .Select((line, idx) => ParseLine(line, idx + 2))
            .Where(x => x != null)
            .ToArray();

        var so = ScriptableObject.CreateInstance<JujuEffectSO>();
        so.effects = effects;
        return so;
    }

    private static JujuEffect ParseLine(string line, int lineNumber)
    {
        // code, jujuEffectType, amount, extraString, extraString2
        var cols = line.Split(',');
        if (cols.Length < 3)
        {
            Debug.LogWarning($"[JujuEffectDatabase] {lineNumber}번째 줄 열 부족: {cols.Length}개");
            return null;
        }

        try
        {
            string extra1 = cols.Length > 3 ? cols[3].Trim() : string.Empty;
            string extra2 = cols.Length > 4 ? cols[4].Trim() : string.Empty;

            return new JujuEffect
            {
                code = (cols[0] ?? string.Empty).Trim(),
                jujuEffectType = TryParseJujuEffectType(cols[1]),
                amount = TryParseInt(cols[2], 0),
                extraString = extra1,
                extraString2 = extra2,
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[JujuEffectDatabase] {lineNumber}번째 줄 파싱 실패: {e.Message}");
            return null;
        }
    }

    private static JujuEffectType TryParseJujuEffectType(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return JujuEffectType.AddDamage;

        var t = s.Trim();

        // "add damage" 처럼 공백 들어오는 케이스도 흡수
        t = t.Replace(" ", "").Replace("_", "");

        // enum 이름은 AddDamage라서 "adddamage"로 맞춰주기
        if (t.Equals("adddamage", StringComparison.OrdinalIgnoreCase))
            return JujuEffectType.AddDamage;

        // 정석 파싱도 시도
        if (Enum.TryParse<JujuEffectType>((s ?? string.Empty).Trim(), true, out var parsed))
            return parsed;

        // 숫자면 0=AddDamage
        if (int.TryParse((s ?? string.Empty).Trim(), out _))
            return JujuEffectType.AddDamage;

        Debug.LogWarning($"[JujuEffectDatabase] JujuEffectType 파싱 실패: '{s}' (기본 AddDamage로 처리)");
        return JujuEffectType.AddDamage;
    }

    private static int TryParseInt(string s, int def) =>
        int.TryParse((s ?? string.Empty).Trim(), out var v) ? v : def;
}