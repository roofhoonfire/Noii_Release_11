using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DialogueGraphRunner
{
    private readonly DialogueGraphAsset _graph;
    private readonly CharacterState _state;
    private readonly GameObject _owner; //  대화를 시작한 상호작용 대상(파괴할 대상)

    private string _currentGuid;

    private Action _onFinished;

    public DialogueGraphRunner(DialogueGraphAsset graph, CharacterState state, GameObject owner)
    {
        _graph = graph;
        _state = state;
        _owner = owner;

        _graph.BuildCache();
    }

    public void Start(Action onFinished = null)
    {
        _onFinished = onFinished;

        _currentGuid = ChooseEntryGuid();
        if (string.IsNullOrEmpty(_currentGuid))
        {
            Finish();
            return;
        }

        ShowCurrentNode();
    }

    // ─────────────────────────────
    // Core loop
    // ─────────────────────────────

    private void ShowCurrentNode()
    {
        var node = _graph.GetNode(_currentGuid);
        if (node == null)
        {
            Finish();
            return;
        }

        // 1) Node calls 실행
        ExecuteCalls(node.calls);

        // 2) 선택지 필터
        var visibleChoices = new List<(DialogueChoiceData choice, string text)>();
        if (node.choices != null)
        {
            foreach (var c in node.choices)
            {
                if (IsChoiceVisible(c))
                    visibleChoices.Add((c, c.text));
            }
        }

        // 3) UI에 보여주기
        var ui = DialogueGraphUIController.Instance;
        if (ui == null)
        {
            Debug.LogWarning("[DialogueGraphRunner] DialogueGraphUIController가 씬에 없음!");
            Finish();
            return;
        }

        if (visibleChoices.Count > 0)
        {
            var uiChoices = new List<(string text, Action onClick)>();
            foreach (var pair in visibleChoices)
            {
                var captured = pair.choice; // 클로저 캡쳐 안전
                uiChoices.Add((pair.text, () => OnChoiceSelected(node, captured)));
            }

            ui.ShowNode(node.speaker, node.text, uiChoices, onContinueIfNoChoices: null);
        }
        else
        {
            // 선택지 없음 → "continue" 입력 기다렸다가 자동 next 판단
            ui.ShowNode(node.speaker, node.text, choices: null, onContinueIfNoChoices: () => OnContinue(node));
        }
    }

    private void OnChoiceSelected(DialogueNodeData node, DialogueChoiceData choice)
    {
        // choice calls 실행
        ExecuteCalls(choice.calls);

        // 다음 guid 결정 (choice의 멀티 nextLinks → defaultBranch → defaultNextGuid)
        var next = ChooseNextGuid(choice.defaultNextGuid, choice.nextLinks);
        if (string.IsNullOrEmpty(next))
        {
            Finish();
            return;
        }

        _currentGuid = next;
        ShowCurrentNode();
    }

    private void OnContinue(DialogueNodeData node)
    {
        //  네가 확정해준 자동 분기 규칙:
        // 1) 선택지가 없고
        // 2) nextLinks에 "널이 아닌 브랜치"가 있을 때만 조건 분기 수행
        //
        // 여기서는 "브랜치가 있다"의 의미를
        // - nextLinks.Count > 0 이고
        // - nextGuid가 비어있지 않은 링크가 하나라도 있음
        // 으로 해석한다.

        bool hasBranch = node.nextLinks != null && node.nextLinks.Any(l => !string.IsNullOrEmpty(l.nextGuid));

        string next;
        if (hasBranch)
        {
            // 조건부 nextLinks → defaultBranch → defaultNextGuid
            next = ChooseNextGuid(node.defaultNextGuid, node.nextLinks);
        }
        else
        {
            // 브랜치가 없으면 그냥 defaultNextGuid로
            next = node.defaultNextGuid;
        }

        if (string.IsNullOrEmpty(next))
        {
            Finish();
            return;
        }

        _currentGuid = next;
        ShowCurrentNode();
    }

    private void Finish()
    {
        var ui = DialogueGraphUIController.Instance;
        if (ui != null) ui.Close();

        _onFinished?.Invoke();
        _onFinished = null;
    }

    // ─────────────────────────────
    // Rules
    // ─────────────────────────────

    private string ChooseEntryGuid()
    {
        // 후보: isEntryCandidate && entryRule 만족
        var candidates = _graph.nodes
            .Where(n => n.isEntryCandidate)
            .Where(n => Eval(_state.GetStat(n.entryRule.flag), n.entryRule.op, n.entryRule.value))
            .OrderByDescending(n => n.entryPriority)
            .ToList();

        if (candidates.Count > 0)
            return candidates[0].guid;

        // 후보 없으면 default entry
        var def = _graph.nodes
            .Where(n => n.isDefaultEntry)
            .OrderByDescending(n => n.entryPriority)
            .FirstOrDefault();

        return def != null ? def.guid : (_graph.nodes.Count > 0 ? _graph.nodes[0].guid : null);
    }

    private string ChooseNextGuid(string defaultNext, List<ConditionalLinkData> links)
    {
        if (links != null && links.Count > 0)
        {
            var ok = links
                .Where(l => !string.IsNullOrEmpty(l.nextGuid))
                .Where(l => Eval(_state.GetStat(l.flag), l.op, l.value))
                .OrderByDescending(l => l.priority)
                .ToList();

            if (ok.Count > 0) return ok[0].nextGuid;

            var def = links
                .Where(l => l.isDefaultBranch && !string.IsNullOrEmpty(l.nextGuid))
                .OrderByDescending(l => l.priority)
                .FirstOrDefault();

            if (def != null) return def.nextGuid;
        }

        return defaultNext;
    }

    private bool IsChoiceVisible(DialogueChoiceData c)
    {
        if (c == null) return false;
        if (!c.lockedAtFirst) return true;
        if (string.IsNullOrEmpty(c.unlockKey)) return false;
        return _state.HasUnlockedChoice(c.unlockKey);
    }

    private static bool Eval(int stat, CompareOp op, int value)
    {
        return op switch
        {
            CompareOp.Equal => stat == value,
            CompareOp.GreaterOrEqual => stat >= value,
            CompareOp.LessOrEqual => stat <= value,
            _ => false
        };
    }
    public void Abort()
    {
        Finish();
    }
    private void ExecuteCalls(List<CallFuncCall> calls)
    {
        if (calls == null) return;

        foreach (var c in calls)
        {
            if (c == null) continue;

            switch (c.func)
            {
                case CallableFunc.AddStat:
                    _state.AddStat(c.flag, c.value);
                    break;

                case CallableFunc.SetStat:
                    _state.SetStat(c.flag, c.value);
                    break;

                case CallableFunc.UnlockChoice:
                    _state.UnlockChoice(c.stringKey);
                    break;

                case CallableFunc.AddCard:
                    break;

                case CallableFunc.AddItem:
                    _state.itemIds.Add(c.stringKey);
                    NarrationOverlay.Instance.Show($"{c.stringKey}를 획득했다");

                    break;


                case CallableFunc.MatChange:
                    SpriteMaterialSwapper.Instance.Apply();
                    break;
                case CallableFunc.Destroy:
                    {
                        if (_owner != null)
                            UnityEngine.Object.Destroy(_owner);
                        else
                            Debug.LogWarning("[DialogueGraphRunner] Destroy 호출됐지만 owner가 null임!");
                        break;
                    }
                case CallableFunc.Temp_Load_Battle1:
                    {
                        SceneManager.LoadScene("Battle_1 Real");
                        break;
                    }

                    //나중에 전체 스탯으로 바꾸셈
                case CallableFunc.AddIntel:
                    {
                        _state.intel += 2;
                        NarrationOverlay.Instance.Show("지혜가 2 상승했습니다");
                        break;
                    }
                case CallableFunc.CallEvent:
                    {

                        //플래그 에 따라 이벤트 발생 여부는 나중에 

                        var fem = FieldEventManager.Instance;
                        if (fem == null)
                        {
                            Debug.LogWarning("[DialogueGraphRunner] CallEvent: FieldEventManager.Instance가 없음 (필드에 1개 있어야 함)");
                            break;
                        }

                        if (string.IsNullOrWhiteSpace(c.eventCode))
                        {
                            Debug.LogWarning("[DialogueGraphRunner] CallEvent: eventCode가 비어있음");
                            break;
                        }

                        if (!fem.HasEvent(c.eventType, c.eventCode))
                        {
                            Debug.LogWarning($"[DialogueGraphRunner] CallEvent: 필드에 등록되지 않은 이벤트 호출 시도: {c.eventType}/{c.eventCode}");
                            break;
                        }

                        fem.StartEvent(c.eventType, c.eventCode);
                        break;
                    }
                default:
                    Debug.LogWarning($"[DialogueGraphRunner] Unhandled func: {c.func}");
                    break;

               
            
            }
        }
    }
}
