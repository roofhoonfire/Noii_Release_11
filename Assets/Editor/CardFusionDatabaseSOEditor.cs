#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardFusionDatabaseSO))]
public class CardFusionDatabaseSOEditor : Editor
{
    private string _search = "";
    private bool _showOnlyInvalid = false;
    private bool _showOnlyDuplicates = false;

    // key -> indices
    private readonly Dictionary<string, List<int>> _keyToIndices = new(StringComparer.Ordinal);

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawToolbar();
        RefreshDuplicateMap();

        EditorGUILayout.Space(6);
        DrawFilteredList();

        EditorGUILayout.Space(8);
        DrawActions();

        serializedObject.ApplyModifiedProperties();
    }

    // ─────────────────────────────
    // UI: Toolbar
    // ─────────────────────────────
    private void DrawToolbar()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Filter / Search", EditorStyles.boldLabel);

            _search = EditorGUILayout.TextField("Search (code/note)", _search);

            using (new EditorGUILayout.HorizontalScope())
            {
                _showOnlyInvalid = GUILayout.Toggle(_showOnlyInvalid, "Only Invalid", "Button");
                _showOnlyDuplicates = GUILayout.Toggle(_showOnlyDuplicates, "Only Duplicates", "Button");
            }

            EditorGUILayout.HelpBox(
                "Search는 output/input/note/key에서 포함 여부로 필터합니다.\n" +
                "중복은 입력 키(A|B 또는 A|B|C)가 같은 레시피를 의미합니다.",
                MessageType.None
            );
        }
    }

    // ─────────────────────────────
    // UI: List (filtered)
    // ─────────────────────────────
    private void DrawFilteredList()
    {
        var recipesProp = serializedObject.FindProperty("recipes");

        int total = recipesProp.arraySize;
        int shown = 0;

        for (int i = 0; i < total; i++)
        {
            var element = recipesProp.GetArrayElementAtIndex(i);
            if (!PassFilter(element)) continue;

            shown++;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawRecipeElement_Layout(element, i);

                EditorGUILayout.Space(4);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(i <= 0))
                    {
                        if (GUILayout.Button("▲", GUILayout.Width(36)))
                            recipesProp.MoveArrayElement(i, i - 1);
                    }

                    using (new EditorGUI.DisabledScope(i >= total - 1))
                    {
                        if (GUILayout.Button("▼", GUILayout.Width(36)))
                            recipesProp.MoveArrayElement(i, i + 1);
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Remove", GUILayout.Width(90)))
                    {
                        recipesProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }
        }

        if (shown == 0)
            EditorGUILayout.HelpBox("필터 결과가 없습니다.", MessageType.Info);

        EditorGUILayout.LabelField($"Shown {shown} / Total {total}", EditorStyles.miniLabel);

        if (GUILayout.Button("Add Recipe"))
        {
            recipesProp.arraySize++;
            var el = recipesProp.GetArrayElementAtIndex(recipesProp.arraySize - 1);


            el.FindPropertyRelative("recipeId").stringValue = ""; //  추가

            el.FindPropertyRelative("outputCode").stringValue = "";

            var inputs = el.FindPropertyRelative("inputCodes");
            inputs.arraySize = 2;
            inputs.GetArrayElementAtIndex(0).stringValue = "";
            inputs.GetArrayElementAtIndex(1).stringValue = "";

            el.FindPropertyRelative("cost").intValue = 0;
            el.FindPropertyRelative("successRate").floatValue = 1f;
            el.FindPropertyRelative("note").stringValue = "";
        }
    }

    // ─────────────────────────────
    // UI: One element (Layout)
    // ─────────────────────────────
    private void DrawRecipeElement_Layout(SerializedProperty element, int index)
    {
        var idProp = element.FindPropertyRelative("recipeId");   // 추가

        var outputProp = element.FindPropertyRelative("outputCode");
        var inputsProp = element.FindPropertyRelative("inputCodes");
        var costProp = element.FindPropertyRelative("cost");
        var rateProp = element.FindPropertyRelative("successRate");
        var noteProp = element.FindPropertyRelative("note");

        string key = BuildKeyPreview(inputsProp);
        bool isDup = IsDuplicateKey(key);
        bool isValid = IsElementValid(outputProp, inputsProp, out var reason);

        string title = $"#{index}  key: {(string.IsNullOrEmpty(key) ? "(empty)" : key)}";
        if (!isValid) title += "   [INVALID]";
        else if (isDup) title += "   [DUPLICATE]";

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);


        if (idProp != null)
            EditorGUILayout.PropertyField(idProp, new GUIContent("Fusion ID"));


        EditorGUILayout.PropertyField(outputProp, new GUIContent("Output Code"));

        EditorGUILayout.LabelField("Inputs (2~3, order ignored)", EditorStyles.miniBoldLabel);
        DrawInputsInline_Layout(inputsProp);

        EditorGUILayout.Space(2);
        EditorGUILayout.PropertyField(costProp);
        EditorGUILayout.PropertyField(rateProp);
        EditorGUILayout.PropertyField(noteProp);

        if (!isValid)
            EditorGUILayout.HelpBox(reason, MessageType.Error);
        else if (isDup)
            EditorGUILayout.HelpBox("같은 입력 조합 키를 가진 레시피가 여러 개입니다. (중복)", MessageType.Warning);
    }

    private void DrawInputsInline_Layout(SerializedProperty inputsProp)
    {
        if (inputsProp == null || !inputsProp.isArray)
        {
            EditorGUILayout.HelpBox("inputCodes is not an array", MessageType.Error);
            return;
        }

        if (inputsProp.arraySize < 2) inputsProp.arraySize = 2;
        if (inputsProp.arraySize > 3) inputsProp.arraySize = 3;

        using (new EditorGUILayout.HorizontalScope())
        {
            int show = Mathf.Min(inputsProp.arraySize, 3);

            for (int i = 0; i < show; i++)
            {
                var p = inputsProp.GetArrayElementAtIndex(i);
                p.stringValue = EditorGUILayout.TextField(p.stringValue);
            }

            using (new EditorGUI.DisabledScope(inputsProp.arraySize <= 2))
            {
                if (GUILayout.Button("-", GUILayout.Width(28)))
                    inputsProp.arraySize = Mathf.Max(2, inputsProp.arraySize - 1);
            }

            using (new EditorGUI.DisabledScope(inputsProp.arraySize >= 3))
            {
                if (GUILayout.Button("+", GUILayout.Width(28)))
                    inputsProp.arraySize = Mathf.Min(3, inputsProp.arraySize + 1);
            }
        }
    }

    // ─────────────────────────────
    // UI: Actions
    // ─────────────────────────────
    private void DrawActions()
    {
        var db = (CardFusionDatabaseSO)target;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Normalize All (sort inputs/trim)"))
                {
                    NormalizeAll();
                }

                if (GUILayout.Button("Validate & Log"))
                {
                    db.RebuildIndex(logErrors: true);
                    RefreshDuplicateMap();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Show Only Invalid"))
                {
                    _showOnlyInvalid = true;
                    _showOnlyDuplicates = false;
                }
                if (GUILayout.Button("Show Only Duplicates"))
                {
                    _showOnlyInvalid = false;
                    _showOnlyDuplicates = true;
                }
                if (GUILayout.Button("Clear Filters"))
                {
                    _showOnlyInvalid = false;
                    _showOnlyDuplicates = false;
                    _search = "";
                }
            }
        }
    }

    // ─────────────────────────────
    // Filtering / Validation / Duplicates
    // ─────────────────────────────
    private bool PassFilter(SerializedProperty element)
    {
        var idProp = element.FindPropertyRelative("recipeId");   // 추가

        var outputProp = element.FindPropertyRelative("outputCode");
        var inputsProp = element.FindPropertyRelative("inputCodes");
        var noteProp = element.FindPropertyRelative("note");

        string key = BuildKeyPreview(inputsProp);
        bool isDup = IsDuplicateKey(key);
        bool isValid = IsElementValid(outputProp, inputsProp, out _);

        if (_showOnlyInvalid && isValid) return false;
        if (_showOnlyDuplicates && !isDup) return false;

        if (string.IsNullOrWhiteSpace(_search)) return true;

        string s = _search.Trim();

        if ((outputProp.stringValue ?? "").IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if ((noteProp.stringValue ?? "").IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (!string.IsNullOrEmpty(key) && key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) return true;

        if (inputsProp != null && inputsProp.isArray)
        {
            for (int i = 0; i < inputsProp.arraySize; i++)
            {
                var p = inputsProp.GetArrayElementAtIndex(i);
                if ((p.stringValue ?? "").IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
        }

        return false;
    }

    private void RefreshDuplicateMap()
    {
        _keyToIndices.Clear();

        var recipesProp = serializedObject.FindProperty("recipes");
        for (int i = 0; i < recipesProp.arraySize; i++)
        {
            var el = recipesProp.GetArrayElementAtIndex(i);
            var inputsProp = el.FindPropertyRelative("inputCodes");

            string key = BuildKeyPreview(inputsProp);
            if (string.IsNullOrEmpty(key)) continue;

            if (!_keyToIndices.TryGetValue(key, out var list))
            {
                list = new List<int>();
                _keyToIndices.Add(key, list);
            }
            list.Add(i);
        }
    }

    private bool IsDuplicateKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        return _keyToIndices.TryGetValue(key, out var list) && list.Count >= 2;
    }

    private string BuildKeyPreview(SerializedProperty inputsProp)
    {
        if (inputsProp == null || !inputsProp.isArray) return "";

        var list = new List<string>();
        for (int i = 0; i < inputsProp.arraySize; i++)
        {
            var p = inputsProp.GetArrayElementAtIndex(i);
            if (p.propertyType != SerializedPropertyType.String) continue;
            if (string.IsNullOrWhiteSpace(p.stringValue)) continue;
            list.Add(p.stringValue.Trim());
        }

        list.Sort(StringComparer.Ordinal);
        if (list.Count < 2 || list.Count > 3) return "";
        return string.Join("|", list);
    }

    private bool IsElementValid(SerializedProperty outputProp, SerializedProperty inputsProp, out string reason)
    {
        reason = null;

        string output = outputProp?.stringValue;
        if (string.IsNullOrWhiteSpace(output))
        {
            reason = "outputCode가 비어있음";
            return false;
        }

        if (inputsProp == null || !inputsProp.isArray)
        {
            reason = "inputCodes가 배열이 아님";
            return false;
        }

        var inputs = new List<string>();
        for (int i = 0; i < inputsProp.arraySize; i++)
        {
            var p = inputsProp.GetArrayElementAtIndex(i);
            if (p.propertyType != SerializedPropertyType.String) continue;
            if (string.IsNullOrWhiteSpace(p.stringValue)) continue;
            inputs.Add(p.stringValue.Trim());
        }

        if (inputs.Count < 2 || inputs.Count > 3)
        {
            reason = $"inputCodes 개수가 2~3이 아님 (현재 {inputs.Count})";
            return false;
        }

        if (inputs.Count != inputs.Distinct(StringComparer.Ordinal).Count())
        {
            reason = "inputCodes에 중복이 있음";
            return false;
        }

        if (inputs.Contains(output.Trim(), StringComparer.Ordinal))
        {
            reason = "outputCode가 inputCodes에 포함됨";
            return false;
        }

        return true;
    }

    private void NormalizeAll()
    {
        var recipesProp = serializedObject.FindProperty("recipes");
        for (int i = 0; i < recipesProp.arraySize; i++)
        {
            var el = recipesProp.GetArrayElementAtIndex(i);

            var outputProp = el.FindPropertyRelative("outputCode");
            if (outputProp != null && outputProp.propertyType == SerializedPropertyType.String && outputProp.stringValue != null)
                outputProp.stringValue = outputProp.stringValue.Trim();

            var inputsProp = el.FindPropertyRelative("inputCodes");
            if (inputsProp != null && inputsProp.isArray)
            {
                var list = new List<string>();
                for (int k = 0; k < inputsProp.arraySize; k++)
                {
                    var p = inputsProp.GetArrayElementAtIndex(k);
                    if (p.propertyType != SerializedPropertyType.String) continue;
                    if (string.IsNullOrWhiteSpace(p.stringValue)) continue;
                    list.Add(p.stringValue.Trim());
                }

                list.Sort(StringComparer.Ordinal);

                inputsProp.arraySize = Mathf.Clamp(list.Count, 2, 3);
                for (int k = 0; k < inputsProp.arraySize; k++)
                    inputsProp.GetArrayElementAtIndex(k).stringValue = list[k];
            }
        }

        serializedObject.ApplyModifiedProperties();
        RefreshDuplicateMap();
    }
}
#endif
