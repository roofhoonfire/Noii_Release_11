// CardEffectDatabase.cs
using System;
using System.Linq;
using UnityEngine;

public static class CardEffectDatabase
{
    // Resources/cardsEffect (TextAsset) 로부터 읽는다. (확장자 .csv는 Resources에서 생략)
    public static string csvResourceName = "cardsEffect";

    public static CardEffectSO LoadEffectsFromCSV()
    {
        var csvAsset = Resources.Load<TextAsset>(csvResourceName);
        if (csvAsset == null)
        {
            Debug.LogError($"[CardEffectDatabase] Resources/{csvResourceName}.csv 를 찾을 수 없습니다.");
            return null;
        }

        var lines = csvAsset.text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Debug.LogError("[CardEffectDatabase] CSV에 데이터가 없습니다.");
            return null;
        }

        var effects = lines
            .Skip(1) // 헤더 스킵
            .Select((line, idx) => ParseLine(line, idx + 2))
            .Where(e => e != null)
            .ToArray();

        var so = ScriptableObject.CreateInstance<CardEffectSO>();
        so.cardeffects = effects;

        return so;
    }

    private static CardEffect ParseLine(string line, int lineNumber)
    {
        // 포맷: code, Phase, effectType, amount, flag
        var cols = line.Split(',');
        if (cols.Length < 3)
        {
            Debug.LogWarning($"[CardEffectDatabase] {lineNumber}번째 줄 열 부족: {cols.Length}개 (최소 3개 필요: code, phase, effectType)");
            return null;
        }

        string code = cols[0].Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            Debug.LogWarning($"[CardEffectDatabase] {lineNumber}번째 줄 code가 비어있음");
            return null;
        }

        if (!TryParseEnum(cols[1], out Phase phase))
        {
            Debug.LogWarning($"[CardEffectDatabase] {lineNumber}번째 줄 Phase 파싱 실패: '{cols[1]}'");
            return null;
        }

        if (!TryParseEnum(cols[2], out EffectType effectType))
        {
            Debug.LogWarning($"[CardEffectDatabase] {lineNumber}번째 줄 EffectType 파싱 실패: '{cols[2]}'");
            return null;
        }

        int amount = (cols.Length >= 4) ? TryParseInt(cols[3], 0) : 0;
        int flag = (cols.Length >= 5) ? TryParseInt(cols[4], 0) : 0;

        return new CardEffect
        {
            code = code,
            phase = phase,
            effectType = effectType,
            amount = amount,
            flag = flag
        };
    }

    private static bool TryParseEnum<TEnum>(string s, out TEnum value) where TEnum : struct
    {
        s = (s ?? string.Empty).Trim();
        return Enum.TryParse<TEnum>(s, ignoreCase: true, out value);
    }

    private static int TryParseInt(string s, int def) =>
        int.TryParse((s ?? string.Empty).Trim(), out var v) ? v : def;
}
