#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueNodeView : Node
{
    public const string NextPortName = "Next";
    public const string NodeNextPrefix = "NEXT::";
    public const string ChoicePrefix = "CHOICE::";
    public const string ChoiceNextPrefix = "CHOICENEXT::";

    public static readonly Vector2 DefaultSize = new(360, 420);

    public DialogueNodeData Data { get; private set; }

    public Port InputPort { get; private set; }
    public Port NextPort { get; private set; }

    private TextField _speakerField;
    private TextField _textField;

    // UI containers
    private VisualElement _choicesContainer;
    private VisualElement _nodeNextContainer;
    private VisualElement _callsContainer;
    private VisualElement _entryContainer;

    public DialogueNodeView(DialogueNodeData data)
    {
        Data = data;

        // null-safe
        Data.choices ??= new System.Collections.Generic.List<DialogueChoiceData>();
        Data.nextLinks ??= new System.Collections.Generic.List<ConditionalLinkData>();
        Data.calls ??= new System.Collections.Generic.List<CallFuncCall>();
        Data.entryRule ??= new EntryRuleData();

        title = Data.speaker;

        // Input
        InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        InputPort.portName = "In";
        inputContainer.Add(InputPort);

        // Node default Next
        NextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        NextPort.portName = NextPortName;
        outputContainer.Add(NextPort);

        // Speaker/Text
        _speakerField = new TextField("Speaker") { value = Data.speaker };
        _speakerField.RegisterValueChangedCallback(e =>
        {
            Data.speaker = e.newValue;
            title = Data.speaker;
        });

        _textField = new TextField("Text") { value = Data.text, multiline = true };
        _textField.RegisterValueChangedCallback(e => Data.text = e.newValue);

        extensionContainer.Add(_speakerField);
        extensionContainer.Add(_textField);

        // ─────────────────────────────
        // Entry Candidate
        // ─────────────────────────────
        extensionContainer.Add(new Label("Entry Candidate"));
        _entryContainer = new VisualElement();
        _entryContainer.style.marginBottom = 6;
        extensionContainer.Add(_entryContainer);

        BuildEntryUI();

        // ─────────────────────────────
        // Node Auto Next Branches
        // ─────────────────────────────
        extensionContainer.Add(new Label("Auto Next Branches (Flag-based)"));
        _nodeNextContainer = new VisualElement();
        _nodeNextContainer.style.marginBottom = 6;
        extensionContainer.Add(_nodeNextContainer);

        var addNodeNextBtn = new Button(() =>
        {
            Data.nextLinks.Add(new ConditionalLinkData
            {
                linkGuid = Guid.NewGuid().ToString("N"),
                flag = DialogueFlag.Charm,
                op = CompareOp.GreaterOrEqual,
                value = 0,
                priority = 0,
                isDefaultBranch = false
            });
            RefreshNodeNextUI();
        })
        { text = "+ Add Next Branch" };
        extensionContainer.Add(addNodeNextBtn);

        // ─────────────────────────────
        // Choices
        // ─────────────────────────────
        extensionContainer.Add(new Label("Choices"));
        _choicesContainer = new VisualElement();
        _choicesContainer.style.marginBottom = 6;
        extensionContainer.Add(_choicesContainer);

        var addChoiceBtn = new Button(() =>
        {
            Data.choices.Add(new DialogueChoiceData
            {
                choiceGuid = Guid.NewGuid().ToString("N"),
                text = $"선택지 {Data.choices.Count + 1}",
                lockedAtFirst = false,
                unlockKey = "",
                nextLinks = new System.Collections.Generic.List<ConditionalLinkData>(),
                calls = new System.Collections.Generic.List<CallFuncCall>(),
            });
            RefreshChoicesUI();
        })
        { text = "+ Add Choice" };
        extensionContainer.Add(addChoiceBtn);

        // ─────────────────────────────
        // Calls
        // ─────────────────────────────
        extensionContainer.Add(new Label("CallFunc (Node)"));
        _callsContainer = new VisualElement();
        extensionContainer.Add(_callsContainer);

        var addCallBtn = new Button(() =>
        {
            Data.calls.Add(new CallFuncCall
            {
                func = CallableFunc.AddStat,
                flag = DialogueFlag.Charm,
                value = 1,
                stringKey = ""
            });
            RefreshCallsUI();
        })
        { text = "+ Add Call" };
        extensionContainer.Add(addCallBtn);

        // Build UI
        RefreshNodeNextUI();
        RefreshChoicesUI();
        RefreshCallsUI();

        RefreshExpandedState();
        RefreshPorts();
    }
    private Foldout MakeFoldout(string title, bool value = true)
    {
        var f = new Foldout { text = title, value = value };
        f.style.marginTop = 6;
        f.style.marginBottom = 4;
        f.style.paddingLeft = 4;
        return f;
    }

    private VisualElement MakeBox()
    {
        var box = new VisualElement();
        box.style.marginTop = 4;
        box.style.marginBottom = 6;
        box.style.paddingTop = 6;
        box.style.paddingBottom = 6;
        box.style.paddingLeft = 6;
        box.style.paddingRight = 6;

        box.style.borderTopWidth = 1;
        box.style.borderBottomWidth = 1;
        box.style.borderLeftWidth = 1;
        box.style.borderRightWidth = 1;

        // 색은 USS가 있으면 거기서 잡고, 없으면 기본값이라도 보이게
        box.style.borderTopColor = new Color(1, 1, 1, 0.12f);
        box.style.borderBottomColor = new Color(1, 1, 1, 0.12f);
        box.style.borderLeftColor = new Color(1, 1, 1, 0.12f);
        box.style.borderRightColor = new Color(1, 1, 1, 0.12f);

        box.style.backgroundColor = new Color(1, 1, 1, 0.03f);
        box.style.borderTopLeftRadius = 4;
        box.style.borderTopRightRadius = 4;
        box.style.borderBottomLeftRadius = 4;
        box.style.borderBottomRightRadius = 4;

        return box;
    }

    public void SyncViewToData()
    {
        Data.position = GetPosition().position;
        Data.speaker = _speakerField.value;
        Data.text = _textField.value;
    }

    // ─────────────────────────────
    // Ports lookup
    // ─────────────────────────────
    public Port GetNodeNextPort(string linkGuid)
        => outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == NodeNextPrefix + linkGuid);

    public Port GetChoicePort(string choiceGuid)
        => outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == ChoicePrefix + choiceGuid);

    public Port GetChoiceNextPort(string choiceGuid, string linkGuid)
        => outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == $"{ChoiceNextPrefix}{choiceGuid}::{linkGuid}");

    private Port CreateOutPort(string portName)
    {
        var p = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        p.portName = portName;
        outputContainer.Add(p);
        return p;
    }

    // ─────────────────────────────
    // Entry UI
    // ─────────────────────────────
    private void BuildEntryUI()
    {
        _entryContainer.Clear();

        // 1) 가장 위: Is Entry Candidate 체크박스 하나
        var isCandidateToggle = new Toggle("Is Entry Candidate")
        {
            value = Data.isEntryCandidate
        };
        _entryContainer.Add(isCandidateToggle);

        // 2) Entry Rule UI (Foldout처럼 인스펙터 느낌)
        var ruleFoldout = new Foldout
        {
            text = "Entry Rule",
            value = true
        };
        ruleFoldout.style.marginLeft = 12;   // 인스펙터처럼 들여쓰기
        _entryContainer.Add(ruleFoldout);

        // Rule 컨테이너
        var ruleRoot = new VisualElement();
        ruleRoot.style.marginTop = 4;
        ruleFoldout.Add(ruleRoot);

        // 3) Is Default Entry 체크박스 (이게 켜지면 아래 조건부 비활성화)
        var isDefaultToggle = new Toggle("Is Default Entry")
        {
            value = Data.isDefaultEntry
        };
        ruleRoot.Add(isDefaultToggle);

        // Entry Priority는 인스펙터처럼 rule 바깥(혹은 안쪽) 중 원하는데 두면 되는데,
        // 스샷 기준 인스펙터에서는 Rule 아래에 Priority가 독립 필드로 보임.
        var priorityField = new IntegerField("Entry Priority")
        {
            value = Data.entryPriority
        };
        ruleRoot.Add(priorityField);

        // 4) Flag / Op / Value를 "줄 별"로 (인스펙터처럼 레이블 + 필드)
        var condRoot = new VisualElement();
        condRoot.style.marginLeft = 12; // Default 체크박스 아래 들여쓰기 느낌
        condRoot.style.marginTop = 4;
        ruleRoot.Add(condRoot);

        var flagField = new EnumField("Flag", Data.entryRule.flag);
        condRoot.Add(flagField);

        var opField = new EnumField("Op", Data.entryRule.op);
        condRoot.Add(opField);

        var valueField = new IntegerField("Value")
        {
            value = Data.entryRule.value
        };
        condRoot.Add(valueField);

        // ─────────────────────────────
        // Enable/Disable 규칙 적용
        // ─────────────────────────────
        void ApplyEnabledState()
        {
            // 후보가 아니면 Rule 전체 비활성화
            bool candidate = Data.isEntryCandidate;
            ruleFoldout.SetEnabled(candidate);

            // 후보가 아니면 foldout을 접어두는 게 보기 좋음(원치 않으면 제거)
            if (!candidate) ruleFoldout.value = false;

            // Default면 조건(Flag/Op/Value)은 비활성화
            bool enableCond = candidate && !Data.isDefaultEntry;
            condRoot.SetEnabled(enableCond);
        }

        // ─────────────────────────────
        // Bind callbacks
        // ─────────────────────────────
        isCandidateToggle.RegisterValueChangedCallback(e =>
        {
            Data.isEntryCandidate = e.newValue;
            ApplyEnabledState();
        });

        isDefaultToggle.RegisterValueChangedCallback(e =>
        {
            Data.isDefaultEntry = e.newValue;
            ApplyEnabledState();
        });

        priorityField.RegisterValueChangedCallback(e =>
        {
            Data.entryPriority = e.newValue;
        });

        flagField.RegisterValueChangedCallback(e =>
        {
            Data.entryRule.flag = (DialogueFlag)e.newValue;
        });

        opField.RegisterValueChangedCallback(e =>
        {
            Data.entryRule.op = (CompareOp)e.newValue;
        });

        valueField.RegisterValueChangedCallback(e =>
        {
            Data.entryRule.value = e.newValue;
        });

        // 초기 적용
        ApplyEnabledState();
    }


    // ─────────────────────────────
    // Node Next branches UI (+ ports)
    // ─────────────────────────────
    private void RefreshNodeNextUI()
    {
        // 기존 NEXT:: 포트들 제거
        var ports = outputContainer.Children().OfType<Port>().ToList();
        foreach (var p in ports)
        {
            if (p.portName == NextPortName) continue;
            if (p.portName.StartsWith(NodeNextPrefix))
                outputContainer.Remove(p);
        }

        _nodeNextContainer.Clear();

        var fold = MakeFoldout("Auto Next Branches (Flag-based)", true);
        _nodeNextContainer.Add(fold);

        var boxWrap = MakeBox();
        fold.Add(boxWrap);

        if (Data.nextLinks.Count == 0)
        {
            boxWrap.Add(new Label("No branches. Add one below."));
        }

        foreach (var link in Data.nextLinks.ToList()) // ★ ToList로 스냅샷 (삭제 안정)
        {
            if (string.IsNullOrEmpty(link.linkGuid))
                link.linkGuid = Guid.NewGuid().ToString("N");

            // 포트 생성
            CreateOutPort(NodeNextPrefix + link.linkGuid);

            // branch box
            var b = MakeBox();
            b.style.backgroundColor = new Color(1, 1, 1, 0.02f);
            boxWrap.Add(b);

            // Row top: Default / Priority / Delete
            var top = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var def = new Toggle("Default") { value = link.isDefaultBranch };
            def.style.width = 120;

            var prio = new IntegerField("Priority") { value = link.priority };
            prio.style.flexGrow = 1;

            var del = new Button(() =>
            {
                // ★ 삭제 확정
                Data.nextLinks.Remove(link);
                RefreshNodeNextUI();
            })
            { text = "Delete" };
            del.style.width = 70;

            top.Add(def);
            top.Add(prio);
            top.Add(del);
            b.Add(top);

            // Row mid: Flag / Op / Value
            var mid = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };

            var flag = new EnumField("Flag", link.flag);
            flag.style.flexGrow = 1;

            var op = new EnumField("Op", link.op);
            op.style.width = 140;

            var val = new IntegerField("Value") { value = link.value };
            val.style.width = 160;

            mid.Add(flag);
            mid.Add(op);
            mid.Add(val);
            b.Add(mid);

            // bind
            def.RegisterValueChangedCallback(e => link.isDefaultBranch = e.newValue);
            prio.RegisterValueChangedCallback(e => link.priority = e.newValue);
            flag.RegisterValueChangedCallback(e => link.flag = (DialogueFlag)e.newValue);
            op.RegisterValueChangedCallback(e => link.op = (CompareOp)e.newValue);
            val.RegisterValueChangedCallback(e => link.value = e.newValue);
        }

        RefreshPorts();
        RefreshExpandedState();
    }


    // ─────────────────────────────
    // Choices UI (+ ports + choice conditional next)
    // ─────────────────────────────
    private void RefreshChoicesUI()
    {
        // 기존 CHOICE::, CHOICENEXT:: 포트 제거
        var ports = outputContainer.Children().OfType<Port>().ToList();
        foreach (var p in ports)
        {
            if (p.portName == NextPortName) continue;
            if (p.portName.StartsWith(ChoicePrefix) || p.portName.StartsWith(ChoiceNextPrefix))
                outputContainer.Remove(p);
        }

        _choicesContainer.Clear();

        foreach (var c in Data.choices)
        {
            if (string.IsNullOrEmpty(c.choiceGuid))
                c.choiceGuid = Guid.NewGuid().ToString("N");

            c.nextLinks ??= new System.Collections.Generic.List<ConditionalLinkData>();
            c.calls ??= new System.Collections.Generic.List<CallFuncCall>();

            // direct choice port (선택지 클릭 시 기본 이동)
            CreateOutPort(ChoicePrefix + c.choiceGuid);

            var box = new VisualElement();
            box.style.marginBottom = 8;
            box.style.borderBottomWidth = 1;

            // row: text + remove
            var row1 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var tf = new TextField { value = c.text };
            tf.style.flexGrow = 1;
            tf.RegisterValueChangedCallback(e => c.text = e.newValue);

            var rm = new Button(() =>
            {
                Data.choices.Remove(c);
                RefreshChoicesUI();
            })
            { text = "X" };
            rm.style.width = 24;

            row1.Add(tf);
            row1.Add(rm);
            box.Add(row1);

            // row: locked + unlockKey
            var row2 = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var locked = new Toggle("Locked") { value = c.lockedAtFirst };
            locked.style.width = 90;
            locked.RegisterValueChangedCallback(e => c.lockedAtFirst = e.newValue);

            var unlockKey = new TextField("UnlockKey") { value = c.unlockKey };
            unlockKey.style.flexGrow = 1;
            unlockKey.RegisterValueChangedCallback(e => c.unlockKey = e.newValue);

            row2.Add(locked);
            row2.Add(unlockKey);
            box.Add(row2);

            // choice conditional next links
            var label = new Label("Choice Next Branches");
            label.style.marginTop = 4;
            box.Add(label);

            var linksContainer = new VisualElement();
            box.Add(linksContainer);

            void RefreshChoiceLinks()
            {
                linksContainer.Clear();

                foreach (var link in c.nextLinks)
                {
                    if (string.IsNullOrEmpty(link.linkGuid))
                        link.linkGuid = Guid.NewGuid().ToString("N");

                    // conditional port
                    CreateOutPort($"{ChoiceNextPrefix}{c.choiceGuid}::{link.linkGuid}");

                    var line = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                    var def = new Toggle("D") { value = link.isDefaultBranch };
                    def.style.width = 40;
                    def.RegisterValueChangedCallback(e => link.isDefaultBranch = e.newValue);

                    var prio = new IntegerField("P") { value = link.priority };
                    prio.style.width = 70;
                    prio.RegisterValueChangedCallback(e => link.priority = e.newValue);

                    var flag = new EnumField(link.flag);
                    flag.style.width = 110;
                    flag.RegisterValueChangedCallback(e => link.flag = (DialogueFlag)e.newValue);

                    var op = new EnumField(link.op);
                    op.style.width = 110;
                    op.RegisterValueChangedCallback(e => link.op = (CompareOp)e.newValue);

                    var val = new IntegerField(link.value);
                    val.style.width = 70;
                    val.RegisterValueChangedCallback(e => link.value = e.newValue);

                    var del = new Button(() =>
                    {
                        c.nextLinks.Remove(link);
                        RefreshChoicesUI(); // 포트 재생성 필요
                    })
                    { text = "X" };
                    del.style.width = 24;

                    line.Add(def);
                    line.Add(prio);
                    line.Add(flag);
                    line.Add(op);
                    line.Add(val);
                    line.Add(del);

                    linksContainer.Add(line);
                }

                RefreshPorts();
                RefreshExpandedState();
            }

            var addLinkBtn = new Button(() =>
            {
                c.nextLinks.Add(new ConditionalLinkData
                {
                    linkGuid = Guid.NewGuid().ToString("N"),
                    flag = DialogueFlag.Charm,
                    op = CompareOp.GreaterOrEqual,
                    value = 0,
                    priority = 0,
                    isDefaultBranch = false
                });
                RefreshChoicesUI(); // 포트 포함 재생성
            })
            { text = "+ Add Choice Next Branch" };

            box.Add(addLinkBtn);

            RefreshChoiceLinks();

            _choicesContainer.Add(box);
        }

        RefreshPorts();
        RefreshExpandedState();
    }

    // ─────────────────────────────
    // CallFunc UI
    // ─────────────────────────────
    private void RefreshCallsUI()
    {
        _callsContainer.Clear();

        foreach (var call in Data.calls)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };

            var func = new EnumField(call.func);
            func.style.width = 120;
            func.RegisterValueChangedCallback(e => call.func = (CallableFunc)e.newValue);

            var flag = new EnumField(call.flag);
            flag.style.width = 110;
            flag.RegisterValueChangedCallback(e => call.flag = (DialogueFlag)e.newValue);

            var val = new IntegerField(call.value);
            val.style.width = 80;
            val.RegisterValueChangedCallback(e => call.value = e.newValue);

            var str = new TextField { value = call.stringKey };
            str.style.flexGrow = 1;
            str.RegisterValueChangedCallback(e => call.stringKey = e.newValue);

            var rm = new Button(() =>
            {
                Data.calls.Remove(call);
                RefreshCallsUI();
            })
            { text = "X" };
            rm.style.width = 24;

            row.Add(func);
            row.Add(flag);
            row.Add(val);
            row.Add(str);
            row.Add(rm);

            _callsContainer.Add(row);
        }

        RefreshExpandedState();
    }
}
#endif
