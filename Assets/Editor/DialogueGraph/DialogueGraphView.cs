#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    private readonly EditorWindow _window;

    public DialogueGraphView(EditorWindow window)
    {
        _window = window;

        var ss = Resources.Load<StyleSheet>("DialogueGraphStyle");
        if (ss != null) styleSheets.Add(ss);

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        nodeCreationRequest = context =>
        {
            var pos = context.screenMousePosition;
            var world = _window.rootVisualElement.ChangeCoordinatesTo(_window.rootVisualElement.parent, pos - _window.position.position);
            var local = contentViewContainer.WorldToLocal(world);
            CreateNode("NPC", "새 대사", local);
        };
    }
    public void CreateNodeAtMouse(string speaker, string text)
    {
        // 그래프뷰 로컬 좌표로 적당히 생성 (현재 뷰의 중앙 근처)
        Vector2 localPos = contentViewContainer.WorldToLocal(_window.position.center);
        CreateNode(speaker, text, localPos);
    }

    public DialogueNodeView CreateNode(string speaker, string text, Vector2 position)
    {
        var data = new DialogueNodeData
        {
            guid = Guid.NewGuid().ToString("N"),
            speaker = speaker,
            text = text,
            position = position,
            choices = new List<DialogueChoiceData>(),
            nextLinks = new List<ConditionalLinkData>(),
            calls = new List<CallFuncCall>(),
        };

        var view = new DialogueNodeView(data);
        view.SetPosition(new Rect(position, DialogueNodeView.DefaultSize));
        AddElement(view);
        return view;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(p =>
            p != startPort &&
            p.node != startPort.node &&
            p.direction != startPort.direction
        ).ToList();
    }

    // ─────────────────────────────
    // Save / Load
    // ─────────────────────────────

    public void SaveToAsset(DialogueGraphAsset asset)
    {
        if (asset == null)
        {
            EditorUtility.DisplayDialog("Save Failed", "Asset is null", "OK");
            return;
        }

        var nodeViews = nodes.ToList().OfType<DialogueNodeView>().ToList();

        asset.nodes.Clear();
        foreach (var v in nodeViews)
        {
            v.SyncViewToData();

            // 연결값 초기화
            v.Data.defaultNextGuid = null;
            foreach (var link in v.Data.nextLinks) link.nextGuid = null;

            foreach (var c in v.Data.choices)
            {
                c.defaultNextGuid = null;
                foreach (var link in c.nextLinks) link.nextGuid = null;
            }

            asset.nodes.Add(v.Data);
        }

        // 엣지 분석
        foreach (var e in edges.ToList())
        {
            if (e.output?.node is not DialogueNodeView from) continue;
            if (e.input?.node is not DialogueNodeView to) continue;

            var outName = e.output.portName;

            // 1) Node default next
            if (outName == DialogueNodeView.NextPortName)
            {
                from.Data.defaultNextGuid = to.Data.guid;
                continue;
            }

            // 2) Node conditional next: NEXT::<linkGuid>
            if (outName.StartsWith(DialogueNodeView.NodeNextPrefix))
            {
                var linkGuid = outName.Substring(DialogueNodeView.NodeNextPrefix.Length);
                var link = from.Data.nextLinks.FirstOrDefault(x => x.linkGuid == linkGuid);
                if (link != null) link.nextGuid = to.Data.guid;
                continue;
            }

            // 3) Choice direct: CHOICE::<choiceGuid>
            if (outName.StartsWith(DialogueNodeView.ChoicePrefix))
            {
                var choiceGuid = outName.Substring(DialogueNodeView.ChoicePrefix.Length);
                var choice = from.Data.choices.FirstOrDefault(x => x.choiceGuid == choiceGuid);
                if (choice != null) choice.defaultNextGuid = to.Data.guid;
                continue;
            }

            // 4) Choice conditional: CHOICENEXT::<choiceGuid>::<linkGuid>
            if (outName.StartsWith(DialogueNodeView.ChoiceNextPrefix))
            {
                var rest = outName.Substring(DialogueNodeView.ChoiceNextPrefix.Length);
                var parts = rest.Split(new[] { "::" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var choiceGuid = parts[0];
                    var linkGuid = parts[1];
                    var choice = from.Data.choices.FirstOrDefault(x => x.choiceGuid == choiceGuid);
                    var link = choice?.nextLinks.FirstOrDefault(x => x.linkGuid == linkGuid);
                    if (link != null) link.nextGuid = to.Data.guid;
                }
                continue;
            }
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public void LoadFromAsset(DialogueGraphAsset asset)
    {
        DeleteElements(graphElements.ToList());
        if (asset == null) return;

        var guidToView = new Dictionary<string, DialogueNodeView>();

        // 노드 생성
        foreach (var n in asset.nodes)
        {
            if (string.IsNullOrEmpty(n.guid))
                n.guid = Guid.NewGuid().ToString("N");

            var v = new DialogueNodeView(n);
            v.SetPosition(new Rect(n.position, DialogueNodeView.DefaultSize));
            AddElement(v);
            guidToView[n.guid] = v;
        }

        // 엣지 연결
        foreach (var n in asset.nodes)
        {
            if (!guidToView.TryGetValue(n.guid, out var from)) continue;

            // Node default next
            if (!string.IsNullOrEmpty(n.defaultNextGuid) && guidToView.TryGetValue(n.defaultNextGuid, out var toNext))
            {
                AddElement(from.NextPort.ConnectTo(toNext.InputPort));
            }

            // Node conditional next
            foreach (var link in n.nextLinks)
            {
                if (string.IsNullOrEmpty(link.linkGuid)) link.linkGuid = Guid.NewGuid().ToString("N");
                if (string.IsNullOrEmpty(link.nextGuid)) continue;
                if (!guidToView.TryGetValue(link.nextGuid, out var to)) continue;

                var outPort = from.GetNodeNextPort(link.linkGuid);
                if (outPort == null) continue;
                AddElement(outPort.ConnectTo(to.InputPort));
            }

            // Choices
            foreach (var c in n.choices)
            {
                if (string.IsNullOrEmpty(c.choiceGuid)) c.choiceGuid = Guid.NewGuid().ToString("N");

                // choice direct next
                if (!string.IsNullOrEmpty(c.defaultNextGuid) && guidToView.TryGetValue(c.defaultNextGuid, out var toChoice))
                {
                    var outPort = from.GetChoicePort(c.choiceGuid);
                    if (outPort != null)
                        AddElement(outPort.ConnectTo(toChoice.InputPort));
                }

                // choice conditional next
                foreach (var link in c.nextLinks)
                {
                    if (string.IsNullOrEmpty(link.linkGuid)) link.linkGuid = Guid.NewGuid().ToString("N");
                    if (string.IsNullOrEmpty(link.nextGuid)) continue;
                    if (!guidToView.TryGetValue(link.nextGuid, out var to)) continue;

                    var outPort = from.GetChoiceNextPort(c.choiceGuid, link.linkGuid);
                    if (outPort != null)
                        AddElement(outPort.ConnectTo(to.InputPort));
                }
            }
        }
    }
}
#endif
