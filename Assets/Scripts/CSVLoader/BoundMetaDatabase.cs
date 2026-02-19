// BoundMetaDatabase.cs
using System;
using System.Linq;
using UnityEngine;

public static class BoundMetaDatabase
{
    public static string csvResourceName = "boundsMeta";

    public static BoundSO LoadMetaFromCSV()
    {
        var csvAsset = Resources.Load<TextAsset>(csvResourceName);
        if (csvAsset == null)
        {
            Debug.LogError($"[BoundMetaDatabase] Resources/{csvResourceName}.csv 를 찾을 수 없습니다.");
            return null;
        }

        var lines = csvAsset.text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Debug.LogError("[BoundMetaDatabase] CSV에 데이터가 없습니다.");
            return null;
        }

        var bounds = lines
            .Skip(1)
            .Select((line, idx) => ParseLine(line, idx + 2))
            .Where(x => x != null)
            .ToArray();

        var so = ScriptableObject.CreateInstance<BoundSO>();
        so.bounds = bounds;
        return so;
    }

    private static Bound ParseLine(string line, int lineNumber)
    {
        // #code, name, sprite, boundPoint, boundType, text
        var cols = line.Split(',');
        if (cols.Length < 5)
        {
            Debug.LogWarning($"[BoundMetaDatabase] {lineNumber}번째 줄 열 부족: {cols.Length}개");
            return null;
        }

        try
        {
            // text는 쉼표가 포함될 수 있으니 6번째(인덱스 5)부터 join
            string textRaw = cols.Length > 5 ? string.Join(",", cols.Skip(5)) : string.Empty;

            return new Bound
            {
                code = cols[0].Trim(),
                name = cols[1].Trim(),
                sprite = LoadSprite(cols[2]),
                boundPoint = TryParseInt(cols[3], 0),
                boundType = TryParseBoundType(cols[4]),
                text = textRaw.Trim(),
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[BoundMetaDatabase] {lineNumber}번째 줄 파싱 실패: {e.Message}");
            return null;
        }
    }

    private static BoundType TryParseBoundType(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return BoundType.Force;

        var t = s.Trim();

        // "force" / "condition" 대응 (대소문자 무시)
        if (Enum.TryParse<BoundType>(t, true, out var parsed))
            return parsed;

        // 혹시 숫자로 들어오면 0=Force, 1=Condition 정도로 처리
        if (int.TryParse(t, out var v))
            return v == 1 ? BoundType.Condition : BoundType.Force;

        Debug.LogWarning($"[BoundMetaDatabase] BoundType 파싱 실패: '{t}' (기본 Force로 처리)");
        return BoundType.Force;
    }

    private static Sprite LoadSprite(string spritePath)
    {
        if (string.IsNullOrWhiteSpace(spritePath))
            return null;

        var sprite = Resources.Load<Sprite>(spritePath.Trim());
        if (sprite == null)
            Debug.LogWarning($"[BoundMetaDatabase] Sprite 로드 실패: Resources/{spritePath}");

        return sprite;
    }

    private static int TryParseInt(string s, int def) =>
        int.TryParse((s ?? string.Empty).Trim(), out var v) ? v : def;
}