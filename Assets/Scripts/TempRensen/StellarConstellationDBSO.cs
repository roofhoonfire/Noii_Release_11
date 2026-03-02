using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stellar/Constellation DB")]
public class StellarConstellationDBSO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;                 // 예: "무명 기사의 자리"
        public GameObject constellationPrefab; // 별자리 프리팹
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, GameObject> _map;

    public bool TryGetPrefab(string key, out GameObject prefab)
    {
        if (_map == null) BuildMap();

        if (string.IsNullOrWhiteSpace(key))
        {
            prefab = null;
            return false;
        }

        return _map.TryGetValue(key.Trim(), out prefab) && prefab != null;
    }

    private void OnEnable()
    {
        // 에디터/런타임에서 값 바뀔 수 있으니 초기화
        _map = null;
    }

    private void BuildMap()
    {
        _map = new Dictionary<string, GameObject>(StringComparer.Ordinal);

        foreach (var e in entries)
        {
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.key)) continue;
            if (e.constellationPrefab == null) continue;

            var k = e.key.Trim();

            // 중복 키 방지: 뒤에 있는 값으로 덮어쓰기(원하면 경고만)
            _map[k] = e.constellationPrefab;
        }
    }
}