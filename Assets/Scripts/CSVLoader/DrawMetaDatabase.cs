using System;
using System.Linq;
using UnityEngine;

public static class DrawCardMetaDatabase
{
    // Resources 폴더 기준 파일명 (확장자 제외)
    public static string csvResourceName = "drawMeta";

    public static DrawCardSO LoadMetaFromCSV()
    {
        var csvAsset = Resources.Load<TextAsset>(csvResourceName);
        if (csvAsset == null)
        {
            Debug.LogError($"[DrawCardMetaDatabase] Resources/{csvResourceName}.csv 를 찾을 수 없습니다.");
            return null;
        }

        var lines = csvAsset.text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Debug.LogError("[DrawCardMetaDatabase] CSV에 데이터가 없습니다.");
            return null;
        }

        var drawCards = lines
            .Skip(1) // 헤더 스킵
            .Select((line, idx) => ParseLine(line, idx + 2))
            .Where(dc => dc != null)
            .ToArray();

        var so = ScriptableObject.CreateInstance<DrawCardSO>();
        so.drawcards = drawCards;
        return so;
    }

    private static DrawCard ParseLine(string line, int lineNumber)
    {
        var cols = line.Split(',');

        // code,name,sprite,knowledgeType,amount,cardText  => 최소 6개
        if (cols.Length < 6)
        {
            Debug.LogWarning($"[DrawCardMetaDatabase] {lineNumber}번째 줄 열 부족: {cols.Length}개");
            return null;
        }

        try
        {
            string cardTextRaw = cols.Length > 5 ? string.Join(",", cols.Skip(5)) : string.Empty;

            return new DrawCard
            {
                code = cols[0].Trim(),
                name = cols[1].Trim(),
                sprite = LoadSprite(cols[2]),
                kt = TryParseKnowledgeType(cols[3], KnowledgeType.sword),
                amount = TryParseInt(cols[4], 0),
                cardText = cardTextRaw.Trim(),
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[DrawCardMetaDatabase] {lineNumber}번째 줄 파싱 실패: {e.Message}");
            return null;
        }
    }

    private static Sprite LoadSprite(string spritePath)
    {
        if (string.IsNullOrWhiteSpace(spritePath))
            return null;

        var sprite = Resources.Load<Sprite>(spritePath.Trim());
        if (sprite == null)
            Debug.LogWarning($"[DrawCardMetaDatabase] Sprite 로드 실패: Resources/{spritePath}");

        return sprite;
    }

    private static KnowledgeType TryParseKnowledgeType(string s, KnowledgeType def)
    {
        if (string.IsNullOrWhiteSpace(s)) return def;

        // 대소문자 무시 파싱
        if (Enum.TryParse<KnowledgeType>(s.Trim(), ignoreCase: true, out var v))
            return v;

        Debug.LogWarning($"[DrawCardMetaDatabase] KnowledgeType 파싱 실패: '{s}' (default={def})");
        return def;
    }

    private static int TryParseInt(string s, int def) =>
        int.TryParse(s.Trim(), out var v) ? v : def;
}