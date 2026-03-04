using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueFlag
{
    // 여기 네가 직접 늘려라
    Charm,
    Learned,
    Meet,
    LanguageLev,
}

public enum CompareOp
{
    Equal,
    GreaterOrEqual,
    LessOrEqual,
}

public enum CallableFunc
{
    // 네가 직접 늘려라
    AddStat,     // stats[flag] += value
    SetStat,     // stats[flag] = value
    UnlockChoice, // unlockedChoiceKeys.Add(stringKey)  (아래 Call에 stringKey도 지원)
AddCard,
AddItem,
MatChange,
Destroy,
Temp_Load_Battle1,
            CallEvent, //  추가
AddIntel,

}

[CreateAssetMenu(menuName = "Noii/Dialogue/Dialogue Graph Asset", fileName = "DialogueGraphAsset")]
public class DialogueGraphAsset : ScriptableObject
{
    // “단일 entry guid” 방식은 버린다.
    public List<DialogueNodeData> nodes = new();

    [NonSerialized] private Dictionary<string, DialogueNodeData> _map;

    public void BuildCache()
    {
        _map = new Dictionary<string, DialogueNodeData>(nodes.Count);
        foreach (var n in nodes)
        {
            if (!string.IsNullOrEmpty(n.guid))
                _map[n.guid] = n;
        }
    }

    public DialogueNodeData GetNode(string guid)
    {
        if (_map == null) BuildCache();
        if (string.IsNullOrEmpty(guid)) return null;
        _map.TryGetValue(guid, out var node);
        return node;
    }
}

[Serializable]
public class DialogueNodeData
{
    public string guid;

    public string speaker;
    [TextArea(3, 10)] public string text;

    // ─────────────────────────────
    // Choices (player 선택)
    // ─────────────────────────────
    public List<DialogueChoiceData> choices = new();

    // ─────────────────────────────
    // Auto Next (flag 기반 자동 분기)
    // ─────────────────────────────
    public string defaultNextGuid; // Next 포트(기본)
    public List<ConditionalLinkData> nextLinks = new(); // 멀티 분기

    // ─────────────────────────────
    // Entry Candidate (복수 entry)
    // ─────────────────────────────
    public bool isEntryCandidate;
    public EntryRuleData entryRule = new();
    public int entryPriority;
    public bool isDefaultEntry;

    // ─────────────────────────────
    // Calls (SetFlag 대신)
    // ─────────────────────────────
    public List<CallFuncCall> calls = new();

    // Graph layout
    public Vector2 position;
}

[Serializable]
public class DialogueChoiceData
{
    // 포트 연결용 안정 guid (unlockKey랑 분리)
    public string choiceGuid;

    // UI 표시 텍스트
    public string text;

    // 잠금/해금
    public bool lockedAtFirst;
    public string unlockKey;

    // 선택 후 이동 (기본/멀티)
    public string defaultNextGuid;
    public List<ConditionalLinkData> nextLinks = new();

    // 선택 시 호출
    public List<CallFuncCall> calls = new();
}

[Serializable]
public class EntryRuleData
{
    public DialogueFlag flag;
    public CompareOp op;
    public int value;
}

[Serializable]
public class ConditionalLinkData
{
    // 포트 연결용 안정 guid
    public string linkGuid;

    // 조건
    public DialogueFlag flag;
    public CompareOp op;
    public int value;

    // 우선도 및 디폴트 브랜치
    public int priority;
    public bool isDefaultBranch;

    // 연결 대상
    public string nextGuid;
}

[Serializable]
public class CallFuncCall
{
    public CallableFunc func;

    // 숫자형 타겟 (stats)
    public DialogueFlag flag;
    public int value;

    // 문자열 타겟 (unlockKey 같은)
    public string stringKey;


    //  CallEvent 전용 (다른 func에서는 무시)
    public FieldEventManager.EventType eventType;
    public string eventCode;
}
