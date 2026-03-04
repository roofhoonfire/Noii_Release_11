using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterState
{
  
    public string characterId;

    public int maxHP = 100;
    public int currentHP = 100;

    public List<string> statusIds = new List<string>();
    public List<string> itemIds = new List<string>();
    public List<string> cardIds = new List<string>();

    public int str = 5;
    public int dex = 5;
    public int def = 5;
    public int intel = 5;
    
    // ─────────────────────────────
    // Dialogue / Graph State
    // (JsonUtility friendly)
    // ─────────────────────────────

    [Serializable]
    public class StatEntry
    {
        public DialogueFlag key;
        public int value;
    }

    // enum 키 → int 값 (매력도, 호감도, 돈 등)
    public List<StatEntry> stats = new List<StatEntry>();

    // 해금된 선택지 키들 (whenhappy 같은 문자열)
    //나중에 Enum으로 바꿔주기
    public List<string> unlockedChoiceKeys = new List<string>();

    // ─────────────────────────────
    // Helpers
    // ─────────────────────────────

    public int GetStat(DialogueFlag key, int defaultValue = 0)
    {
        for (int i = 0; i < stats.Count; i++)
        {
            if (stats[i].key == key) return stats[i].value;
        }
        return defaultValue;
    }

    public void SetStat(DialogueFlag key, int value)
    {
        for (int i = 0; i < stats.Count; i++)
        {
            if (stats[i].key == key)
            {
                stats[i].value = value;
                return;
            }
        }
        stats.Add(new StatEntry { key = key, value = value });
    }

    public void AddStat(DialogueFlag key, int delta)
    {
        SetStat(key, GetStat(key) + delta);
    }





    public bool HasUnlockedChoice(string unlockKey)
    {
        if (string.IsNullOrEmpty(unlockKey)) return false;
        return unlockedChoiceKeys.Contains(unlockKey);
    }

    public void UnlockChoice(string unlockKey)
    {
        if (string.IsNullOrEmpty(unlockKey)) return;
        if (!unlockedChoiceKeys.Contains(unlockKey))
            unlockedChoiceKeys.Add(unlockKey);
    }

 
}
