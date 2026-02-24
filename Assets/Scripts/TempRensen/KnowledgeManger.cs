using System.Collections.Generic;
using UnityEngine;
public enum KnowledgeType
{
    sword,
    defend,
    heist,
}

public class KnowledgeManger : MonoBehaviour
{
    public static KnowledgeManger Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Transform spawnRoot;           // VerticalLayoutGroup 달린 오브젝트 추천
    [SerializeField] private EachKnowledgeInfo knowledgePrefab;

    // 내부적으로 타입별 UI를 관리(중복 허용 위해 리스트)
    private readonly List<EachKnowledgeInfo> _spawned = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
      //  Rebuild(); // 초기 1회
    }

    /// <summary>
    /// Init + Refresh 통합: tempKnowledges 기준으로 UI를 완전히 다시 만든다.
    /// </summary>
    public void Rebuild()
    {
        if (BattleManager.Instance == null)
        {
            Debug.LogError("[KnowledgeManger] TempManager.Instance 없음");
            return;
        }
        if (spawnRoot == null || knowledgePrefab == null)
        {
            Debug.LogError("[KnowledgeManger] spawnRoot / knowledgePrefab 참조 비어있음");
            return;
        }

        // 1) 기존 UI 제거
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] != null)
                Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();

        // TempManager가 생성된 UI 리스트를 들고있어야 한다고 했으니 같이 정리
        BattleManager.Instance.spawnedKnowledgeUIs.Clear();

        // 2) 현재 tempKnowledges 기반으로 새로 생성
        var list = BattleManager.Instance.tempKnowledges;
        if (list == null) return;

        foreach (var kt in list)
        {
            var ui = Instantiate(knowledgePrefab, spawnRoot);
            ui.ApplyData(kt);

            _spawned.Add(ui);
            BattleManager.Instance.spawnedKnowledgeUIs.Add(ui);
        }
    }
}