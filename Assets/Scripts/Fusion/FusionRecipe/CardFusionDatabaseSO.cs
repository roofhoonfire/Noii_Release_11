using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Fusion Database", fileName = "CardFusionDatabase")]
public class CardFusionDatabaseSO : ScriptableObject
{
    [SerializeField] private List<FusionRecipe> recipes = new();

    // 런타임 인덱스
    private Dictionary<string, FusionRecipe> _index;
    private bool _indexBuilt;

    public IReadOnlyList<FusionRecipe> Recipes => recipes;

    /// <summary>
    /// 레시피들을 정규화하고(정렬/Trim) 인덱스를 다시 만든다.
    /// </summary>
    public void RebuildIndex(bool logErrors = false)
    {
        _indexBuilt = false;
        _index = null;

        _index = new Dictionary<string, FusionRecipe>(StringComparer.Ordinal);

        for (int i = 0; i < recipes.Count; i++)
        {
            var r = recipes[i];
            if (r == null) continue;

            r.Normalize();

            if (!r.IsValid(out var reason))
            {
                if (logErrors)
                    Debug.LogWarning($"[FusionDB] Invalid recipe at index {i}: {reason}", this);
                continue;
            }

            var key = r.GetKey();
            if (string.IsNullOrEmpty(key))
            {
                if (logErrors)
                    Debug.LogWarning($"[FusionDB] Invalid key at index {i}", this);
                continue;
            }

            if (_index.ContainsKey(key))
            {
                if (logErrors)
                {
                    var prev = _index[key];
                    Debug.LogWarning(
                        $"[FusionDB] Duplicate key '{key}'. " +
                        $"prev output='{prev.outputCode}', new output='{r.outputCode}' (index {i})",
                        this
                    );
                }
                // 중복이면 "첫 번째"를 유지 (원하면 덮어쓰게 바꿔도 됨)
                continue;
            }

            _index.Add(key, r);
        }

        _indexBuilt = true;
    }

    private void EnsureIndex()
    {
        if (_indexBuilt && _index != null) return;
        RebuildIndex(logErrors: false);
    }

    /// <summary>
    /// 입력 2~3개로 합성이 되는지 확인. 성공하면 outputCode 반환.
    /// </summary>
    public bool TryGetOutput(IList<string> inputs, out string outputCode)
    {
        outputCode = null;
        if (inputs == null) return false;

        EnsureIndex();

        var key = FusionKeyUtil.MakeKey(inputs);
        if (string.IsNullOrEmpty(key)) return false;

        if (_index.TryGetValue(key, out var recipe) && recipe != null)
        {
            outputCode = recipe.outputCode?.Trim();
            return !string.IsNullOrWhiteSpace(outputCode);
        }

        return false;
    }

    /// <summary>
    /// 디버그/에디터용: 입력 키로 레시피 자체를 얻는다.
    /// </summary>
    public bool TryGetRecipe(IList<string> inputs, out FusionRecipe recipe)
    {
        recipe = null;
        if (inputs == null) return false;

        EnsureIndex();

        var key = FusionKeyUtil.MakeKey(inputs);
        if (string.IsNullOrEmpty(key)) return false;

        return _index.TryGetValue(key, out recipe) && recipe != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 수정될 때 정규화만 해주고,
        // 인덱스는 필요할 때 재빌드.
        if (recipes == null) recipes = new List<FusionRecipe>();
        foreach (var r in recipes)
            r?.Normalize();
        _indexBuilt = false;
        _index = null;
    }
#endif
}
