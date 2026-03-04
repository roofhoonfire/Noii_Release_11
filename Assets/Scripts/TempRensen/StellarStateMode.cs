using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StellarStateMode : MonoBehaviour
{
    public static StellarStateMode Instance { get; private set; }

    [Header("Constellation DB")]
    [SerializeField] private StellarConstellationDBSO constellationDB;
    [SerializeField] private Transform constellationSpawnRoot;

    private GameObject _spawnedConstellation;

    [Header("Card UI")]
    [SerializeField] public GameObject cardPrefab;
    [SerializeField] public RectTransform cardContentArea;
    public List<GameObject> spawnedCards = new List<GameObject>();
    [Header("Preview Card UI")]
    [SerializeField] private GameObject previewCardPrefab;
    [SerializeField] private Transform previewCardSpawnPoint;

    private GameObject _spawnedPreviewCard;
    public bool isActive = false;
    private Coroutine _stellarCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 기존 호출 호환용(원하면 유지)
    public void SetActive(bool active)
    {
        SetActive(active, null);
    }

    // ✅ 새 버전: 별자리 키를 받는다
    public void SetActive(bool active, string constellationKey)
    {
        if (active == isActive) return;
        isActive = active;

        if (isActive)
        {
            SpawnConstellation(constellationKey);
            StartStellarLoop();
        }
        else
        {
            StopStellarLoop();
        }
    }

    private void SpawnConstellation(string key)
    {
        // 이전 것 제거(혹시 남아있다면)
        ClearConstellation();

        if (string.IsNullOrWhiteSpace(key))
            return; // 키 없으면 별자리 안 띄우고 그냥 모드만 켬

        if (constellationDB == null || constellationSpawnRoot == null)
        {
            Debug.LogWarning("[StellarStateMode] constellationDB / constellationSpawnRoot 비어있음");
            return;
        }

        if (!constellationDB.TryGetPrefab(key, out var prefab))
        {
            Debug.LogWarning($"[StellarStateMode] 별자리 키 못 찾음: {key}");
            return;
        }

        _spawnedConstellation = Instantiate(prefab, constellationSpawnRoot);
        // 필요하면 로컬 트랜스폼 초기화
        var rt = _spawnedConstellation.transform;
        rt.localPosition = Vector3.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }

    private void ClearConstellation()
    {
        if (_spawnedConstellation != null)
        {
            Destroy(_spawnedConstellation);
            _spawnedConstellation = null;
        }
    }

    private void StartStellarLoop()
    {
        if (_stellarCoroutine == null)
            _stellarCoroutine = StartCoroutine(FusionLoop());
    }

    private void StopStellarLoop()
    {
        if (_stellarCoroutine != null)
        {
            StopCoroutine(_stellarCoroutine);
            _stellarCoroutine = null;
        }

        isActive = false;
        StartCoroutine(CloseHandsThenReturn());
    }

    public void CancelAndReturn()
    {
        if (_stellarCoroutine != null)
        {
            StopCoroutine(_stellarCoroutine);
            _stellarCoroutine = null;
        }

        isActive = false;
        StartCoroutine(CloseHandsThenReturn());
    }

    private IEnumerator CloseHandsThenReturn()
    {
        ClearAllCards();
        ClearConstellation(); //  별자리도 같이 정리
        ClearPreviewCard(); // ✅ 추가

        yield return BattleCameraDirector.Instance.SetShotAndWait(BattleCameraDirector.ShotId.Back);
        BattleManager.Instance.AnotherModeDone();
    }

    private IEnumerator FusionLoop()
    {
        PopulateCards();

        while (isActive)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelAndReturn();
                yield break;
            }
            yield return null;
        }
    }

    // 이하 너 코드 그대로(카드 스폰/합성 등)…
    private void ClearAllCards()
    {
        if (cardContentArea != null)
        {
            for (int i = cardContentArea.childCount - 1; i >= 0; i--)
                Destroy(cardContentArea.GetChild(i).gameObject);
        }
        spawnedCards.Clear();
    }

    private void PopulateCards()
    {
        spawnedCards.Clear();

        foreach (string code in BattleManager.Instance.apData.hands)
        {
            var cardGO = Instantiate(cardPrefab, cardContentArea);
            spawnedCards.Add(cardGO);

            if (BattleManager.Instance.apData.isBlinded)
            {
                Transform visualRoot = cardGO.transform.Find("VisualRoot");
                if (visualRoot != null)
                {
                    Transform blood = visualRoot.Find("BloodShed");
                    if (blood != null) blood.gameObject.SetActive(true);
                }
            }

            var info = cardGO.GetComponent<EachCardInfo>();
            if (info == null) { Debug.LogWarning("EachCardInfo 없음"); continue; }

            info.cardData = CSVLoader.Instance.GetCardByCode(code);
            info.ApplyCardData();
        }
    }

    // TrySynthesize 등 나머지는 네 코드 유지하면 됨
    private void SpawnResultCardUI(string code) {
        if (string.IsNullOrWhiteSpace(code)) return; 
        code = code.Trim(); var cardGO = Instantiate(cardPrefab, cardContentArea); 
        spawnedCards.Add(cardGO);
        if (BattleManager.Instance.apData.isBlinded) { 
            Transform visualRoot = cardGO.transform.Find("VisualRoot");
            if (visualRoot != null) { Transform blood = visualRoot.Find("BloodShed"); 
                if (blood != null) blood.gameObject.SetActive(true); 
            }
        }
        var info = cardGO.GetComponent<EachCardInfo>();
        if (info == null) { 
            Debug.LogWarning("[Fusion] 결과 카드에 EachCardInfo 없음");
            return; 
        } 
        var cardData = CSVLoader.Instance.GetCardByCode(code);
        if (cardData == null) { 
            Debug.LogWarning($"[Fusion] 결과 카드 코드 못 찾음: {code}");
            return;
        }
        info.cardData = cardData; info.ApplyCardData();
    }


    /// <summary>
    /// 요구 재료(노드리스트/노울리지) 만족하면:
    /// - TempManager.Instance.tempHands 에서 요구 카드코드들 제거
    /// - TempManager.Instance.tempKnowledges 에서 요구 노울리지들 제거
    /// - rewardCardCode 를 tempHands에 추가
    /// </summary>
    /// 셋 액티브 될 때, 매개변수로 받은 프리팹 생성 . . . 
    public bool TrySynthesize(List<StellarNode> requiredNodes, List<KnowledgeType> requiredKnowledges, string rewardCardCode)
    {
        if (BattleManager.Instance == null)
        {
            Debug.LogError("[StellarOverallManager] TempManager.Instance 없음");
            return false;
        }

        var hands = BattleManager.Instance.apData.hands;              // List<string>
        var knowledges = BattleManager.Instance.currentKnowledges;    // List<KnowledgeType>

        // 1) 요구 카드코드 리스트로 변환 (requiredNodes가 null이면 = 요구 없음)
        List<string> requiredCardCodes = null;
        if (requiredNodes != null)
        {
            requiredCardCodes = new List<string>(requiredNodes.Count);
            foreach (var n in requiredNodes)
            {
                if (n == null) continue;
                if (!string.IsNullOrEmpty(n.rewarCardCode))
                    requiredCardCodes.Add(n.rewarCardCode);
            }
        }

        // 2) 보유 재산이 요구 재료를 전부 가지고 있는지 검사(중복 포함)
        bool cardsOk = (requiredCardCodes == null) || ContainsAllWithCounts(hands, requiredCardCodes);
        bool knowOk = (requiredKnowledges == null) || ContainsAllWithCounts(knowledges, requiredKnowledges);

        if (!cardsOk || !knowOk)
        {
            Debug.Log("[StellarOverallManager] 합성 실패: 재료 부족");
            return false;
        }

        // 3) 요구 재료 제거
        if (requiredCardCodes != null)
        {
            foreach (var code in requiredCardCodes)
                RemoveOne(hands, code);
            RemoveMaterialCardUI(requiredCardCodes);

        }

        if (requiredKnowledges != null)
        {
            foreach (var k in requiredKnowledges)
                RemoveOne(knowledges, k);
        }

        // 4) 보상 지급
        if (!string.IsNullOrEmpty(rewardCardCode))
            hands.Add(rewardCardCode);

        Debug.Log($"[StellarOverallManager] 합성 성공! reward={rewardCardCode}");

        SpawnResultCardUI(rewardCardCode);
        return true;
    }

    // ---------- helpers (중복 개수까지 고려) ----------

    private static bool ContainsAllWithCounts(List<string> owned, List<string> required)
    {
        if (required == null) return true;
        if (owned == null) return false;

        var counts = new Dictionary<string, int>();
        foreach (var s in owned)
        {
            if (string.IsNullOrEmpty(s)) continue;
            counts.TryGetValue(s, out int c);
            counts[s] = c + 1;
        }

        foreach (var r in required)
        {
            if (string.IsNullOrEmpty(r)) continue;

            if (!counts.TryGetValue(r, out int c) || c <= 0)
                return false;
            counts[r] = c - 1;
        }

        return true;
    }

    private static bool ContainsAllWithCounts(List<KnowledgeType> owned, List<KnowledgeType> required)
    {
        if (required == null) return true;
        if (owned == null) return false;

        var counts = new Dictionary<KnowledgeType, int>();
        foreach (var k in owned)
        {
            counts.TryGetValue(k, out int c);
            counts[k] = c + 1;
        }

        foreach (var r in required)
        {
            if (!counts.TryGetValue(r, out int c) || c <= 0)
                return false;
            counts[r] = c - 1;
        }

        return true;
    }

    private static void RemoveOne(List<string> list, string value)
    {
        if (list == null || string.IsNullOrEmpty(value)) return;
        int idx = list.IndexOf(value);
        if (idx >= 0) list.RemoveAt(idx);
    }

    private static void RemoveOne(List<KnowledgeType> list, KnowledgeType value)
    {
        if (list == null) return;

        int idx = list.IndexOf(value);
        if (idx >= 0)
        {
            KnowledgeType removed = list[idx];

            // 🔥 Trash로 이동
          /*  if (BattleManager.Instance != null && BattleManager.Instance.knowledgeTrash != null)
            {
                BattleManager.Instance.knowledgeTrash.Add(removed);
            }
          */// 이제 카드 뽑는 식이라 트래쉬로 갈 필요 없음
            list.RemoveAt(idx);
        }
    }
    private void RemoveMaterialCardUI(List<string> consumedCodes)
    {
        if (consumedCodes == null || consumedCodes.Count == 0)
            return;

        // 중복 카드 고려
        var need = new Dictionary<string, int>();
        foreach (var code in consumedCodes)
        {
            if (string.IsNullOrWhiteSpace(code)) continue;
            var trimmed = code.Trim();
            need.TryGetValue(trimmed, out int c);
            need[trimmed] = c + 1;
        }

        for (int i = spawnedCards.Count - 1; i >= 0; i--)
        {
            var go = spawnedCards[i];
            if (go == null) continue;

            var info = go.GetComponent<EachCardInfo>();
            if (info == null || info.cardData == null) continue;

            string code = info.cardData.code; // ⚠ 너 CSV 구조에 맞게 필드명 수정

            if (need.TryGetValue(code, out int count) && count > 0)
            {
                need[code] = count - 1;

                spawnedCards.RemoveAt(i);
                Destroy(go);
            }
        }
    }

    public void SpawnPreviewCard(string code)
    {
        if (previewCardPrefab == null || previewCardSpawnPoint == null)
        {
            Debug.LogWarning("[StellarStateMode] previewCardPrefab / previewCardSpawnPoint 비어있음");
            return;
        }

        // 기존 프리뷰 제거 (항상 1장만 유지)
        if (_spawnedPreviewCard != null)
        {
            Destroy(_spawnedPreviewCard);
            _spawnedPreviewCard = null;
        }

        if (string.IsNullOrWhiteSpace(code))
            return;

        code = code.Trim();

        // 스폰
        _spawnedPreviewCard = Instantiate(previewCardPrefab, previewCardSpawnPoint);

        // 로컬 트랜스폼 정리(프리팹이 UI든 3D든 안전하게)
        var t = _spawnedPreviewCard.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        // 카드 데이터 로드
        var cardData = CSVLoader.Instance.GetCardByCode(code);
        if (cardData == null)
        {
            Debug.LogWarning($"[StellarStateMode] 프리뷰 카드 코드 못 찾음: {code}");
            return;
        }

        // EachCardInfo에 반영
        var info = _spawnedPreviewCard.GetComponent<EachCardInfo>();
        if (info == null)
        {
            Debug.LogWarning("[StellarStateMode] previewCardPrefab에 EachCardInfo 없음");
            return;
        }

        info.cardData = cardData;
        info.ApplyCardData();
    }
    public void ClearPreviewCard()
    {
        if (_spawnedPreviewCard != null)
        {
            Destroy(_spawnedPreviewCard);
            _spawnedPreviewCard = null;
        }
    }
}