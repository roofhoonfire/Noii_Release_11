using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public static class ItemMetaDatabase // 이름은 데이터 베이스지만 실제 메타 데이터는 SO형태로 CardCSVLoader의 SO 변수에 들어감
{
    public static string csvResourceName = "itemsMeta";

    public static ItemSO LoadMetaFromCSV()
    {
        var csvAsset = Resources.Load<TextAsset>(csvResourceName);
        if (csvAsset == null)
        {
            Debug.LogError($"[ItemCSVLoader] Resources/{csvResourceName}.csv 를 찾을 수 없습니다.");
            return null;
        }

        var lines = csvAsset.text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Debug.LogError("[ItemCSVLoader] CSV에 데이터가 없습니다.");
            return null;
        }

        var cards = lines
            .Skip(1)
            .Select((line, idx) => ParseLine(line, idx + 2))
            .Where(c => c != null)
            .ToArray();

        var so = ScriptableObject.CreateInstance<ItemSO>();
        so.items = cards;
        return so;
    }

    private static Item ParseLine(string line, int lineNumber)
    {
        var cols = line.Split(',');
        if (cols.Length < 3) // damage 추가로 총 14개 필요
        {
            Debug.LogWarning($"[ItemCSVLoader] {lineNumber}번째 줄 열 부족: {cols.Length}개");
            return null;
        }

        try
        {
          //  string cardTextRaw = cols.Length > 14 ? string.Join(",", cols.Skip(14)) : string.Empty;
            return new Item
            {
                code = cols[0].Trim(),
                itemType= TryParseInt(cols[1], 0),
                name = cols[2].Trim(),
                sprite = LoadSprite(cols[3]),
                itemText = cols[4].Trim(),  // 원문 템플릿 그대로 저장(개행은 표시 시 변환)
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[ItemCSVLoader] {lineNumber}번째 줄 파싱 실패: {e.Message}");
            return null;
        }
    }

    public static Sprite LoadSprite(string spritePath)
    {
        if (string.IsNullOrWhiteSpace(spritePath))
            return null;

        var sprite = Resources.Load<Sprite>(spritePath.Trim());
        if (sprite == null)
            Debug.LogWarning($"[ItemCSVLoader] Sprite 로드 실패: Resources/{spritePath}");

        return sprite;
    }

    
    private static int TryParseInt(string s, int def) =>
        int.TryParse(s.Trim(), out var v) ? v : def;
}
