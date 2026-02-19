// JujuMetaDatabase.cs
using System;
using System.Linq;
using UnityEngine;

public static class JujuMetaDatabase
{
    public static string csvResourceName = "jujusMeta"; // 필요하면 파일명 맞춰 바꿔라. (요구사항에 없어서 기본값 둠)

    public static JujuSO LoadMetaFromCSV()
    {
        var csvAsset = Resources.Load<TextAsset>(csvResourceName);
        if (csvAsset == null)
        {
            Debug.LogError($"[JujuMetaDatabase] Resources/{csvResourceName}.csv 를 찾을 수 없습니다.");
            return null;
        }

        var lines = csvAsset.text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Debug.LogError("[JujuMetaDatabase] CSV에 데이터가 없습니다.");
            return null;
        }

        var jujus = lines
            .Skip(1)
            .Select((line, idx) => ParseLine(line, idx + 2))
            .Where(x => x != null)
            .ToArray();

        var so = ScriptableObject.CreateInstance<JujuSO>();
        so.jujus = jujus;
        return so;
    }

    private static Juju ParseLine(string line, int lineNumber)
    {
        // #code, name, sprite, jujuPoint, text
        var cols = line.Split(',');
        if (cols.Length < 4)
        {
            Debug.LogWarning($"[JujuMetaDatabase] {lineNumber}번째 줄 열 부족: {cols.Length}개");
            return null;
        }

        try
        {
            // text는 쉼표가 포함될 수 있으니 5번째(인덱스 4)부터 join
            string textRaw = cols.Length > 4 ? string.Join(",", cols.Skip(4)) : string.Empty;

            return new Juju
            {
                code = cols[0].Trim(),
                name = cols[1].Trim(),
                sprite = LoadSprite(cols[2]),
                jujuPoint = (cols[3] ?? string.Empty).Trim(),
                text = textRaw.Trim(),
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[JujuMetaDatabase] {lineNumber}번째 줄 파싱 실패: {e.Message}");
            return null;
        }
    }

    private static Sprite LoadSprite(string spritePath)
    {
        if (string.IsNullOrWhiteSpace(spritePath))
            return null;

        var sprite = Resources.Load<Sprite>(spritePath.Trim());
        if (sprite == null)
            Debug.LogWarning($"[JujuMetaDatabase] Sprite 로드 실패: Resources/{spritePath}");

        return sprite;
    }

    private static int TryParseInt(string s, int def) =>
        int.TryParse((s ?? string.Empty).Trim(), out var v) ? v : def;
}