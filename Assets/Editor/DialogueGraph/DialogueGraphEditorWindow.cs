#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphEditorWindow : EditorWindow
{
    private DialogueGraphView _view;
    private DialogueGraphAsset _asset;

    [MenuItem("Tools/Noii/Dialogue Graph Editor")]
    public static void Open()
    {
        var w = GetWindow<DialogueGraphEditorWindow>();
        w.titleContent = new GUIContent("Dialogue Graph");
        w.Show();
    }

    private void OnEnable()
    {
        _view = new DialogueGraphView(this);
        _view.StretchToParentSize();
        rootVisualElement.Add(_view);

        var toolbar = new Toolbar();

        var objField = new ObjectField("Asset")
        {
            objectType = typeof(DialogueGraphAsset),
            allowSceneObjects = false
        };
        objField.RegisterValueChangedCallback(evt =>
        {
            _asset = evt.newValue as DialogueGraphAsset;
            _view.LoadFromAsset(_asset);
        });

        var btnNewNode = new ToolbarButton(() => _view.CreateNodeAtMouse("NPC", "»õ ´ë»ç"))
        {
            text = "New Node"
        };

        var btnSave = new ToolbarButton(() => _view.SaveToAsset(_asset))
        {
            text = "Save"
        };

        var btnReload = new ToolbarButton(() => _view.LoadFromAsset(_asset))
        {
            text = "Reload"
        };

        toolbar.Add(objField);
        toolbar.Add(btnNewNode);
        toolbar.Add(btnSave);
        toolbar.Add(btnReload);

        rootVisualElement.Add(toolbar);
    }

    private void OnDisable()
    {
        if (_view != null)
            rootVisualElement.Remove(_view);
    }
}
#endif
