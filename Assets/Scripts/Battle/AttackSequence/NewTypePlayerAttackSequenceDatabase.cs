using System.Collections.Generic;
using UnityEngine;

public class NewTypePlayerAttackSequenceDatabase : MonoBehaviour
{
    public static NewTypePlayerAttackSequenceDatabase Instance { get; private set; }

    [SerializeField] private List<NewTypePlayerAttackSequenceSO> sequences = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public NewTypePlayerAttackSequenceSO Get(string code)
    {
        if (string.IsNullOrEmpty(code)) return null;

        for (int i = 0; i < sequences.Count; i++)
        {
            var so = sequences[i];
            if (so != null && so.cardCode == code)
                return so;
        }

        Debug.LogWarning($"[NewTypePlayerAttackSequenceDatabase] SO ¸øÃ£À½: cardCode={code}");
        return null;
    }
}
