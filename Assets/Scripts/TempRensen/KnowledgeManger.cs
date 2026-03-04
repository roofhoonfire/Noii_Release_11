using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
            Debug.LogError("[KnowledgeManger] BattleManager.Instance 없음");
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

        BattleManager.Instance.spawnedKnowledgeUIs.Clear();

        // 2) 새로 생성
        var list = BattleManager.Instance.currentKnowledges;
        if (list == null) return;

        foreach (var kt in list)
        {
            var ui = Instantiate(knowledgePrefab, spawnRoot);
            ui.ApplyData(kt);

            // 🔥 여기 추가된 부분
            ApplyKnowledgeColor(ui, kt);

            _spawned.Add(ui);
            BattleManager.Instance.spawnedKnowledgeUIs.Add(ui);
        }
    }

    private void ApplyKnowledgeColor(EachKnowledgeInfo ui, KnowledgeType kt)
    {
        Image img = ui.GetComponentInChildren<Image>();
        if (img == null) return;

        Color pastelColor = Color.white;

        switch (kt)
        {
            case KnowledgeType.sword:
                pastelColor = new Color(0.95f, 0.55f, 0.55f); // 파스텔 레드
                break;

            case KnowledgeType.defend:
                pastelColor = new Color(0.55f, 0.85f, 0.60f); // 파스텔 그린
                break;

            case KnowledgeType.heist:
                pastelColor = new Color(0.55f, 0.65f, 0.95f); // 파스텔 블루
                break;
        }

        img.color = pastelColor;
    }
}