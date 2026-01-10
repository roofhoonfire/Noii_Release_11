using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배틀 씬에서 카드 합성(레시피 기반) 미리보기/생성 관리.
/// - Singleton
/// - 재료 선택 -> 재료 리스트에 추가
/// - 재료 2개 이상이면 가능한 합성 결과(들) 프리뷰 생성
/// - 합성 실행(현재는 Debug.Log만)
/// </summary>
public class CardFusionState : MonoBehaviour
{
    public static CardFusionState Instance { get; private set; }

    [Header("DB")]
    [SerializeField] private CardFusionDatabaseSO fusionDB;

    [Header("Fusion Result Preview")]
    [SerializeField] private Transform previewSpawnRoot;      // Canvas 하위에 지정 (FusionPreviewRoot)
    [SerializeField] private GameObject previewCardPrefab;     // EachCardInfo가 붙은 프리뷰용 카드 프리팹
    [SerializeField] private bool clearPreviewOnNoResult = true;
    
    [Header("Card UI")]
    [Tooltip("재료로 쓰일 카드들 인포")]
    [SerializeField] public GameObject cardPrefab;
    [SerializeField] public RectTransform cardContentArea;
    public List<GameObject> spawnedCards = new List<GameObject>();

    [Header("Debug")]
    [SerializeField] private bool logSelection = true;

    // 재료들(카드 코드)
    [SerializeField] private List<string> materials = new List<string>(3);

    // 현재 떠있는 프리뷰 인스턴스들
    private readonly List<GameObject> _previewInstances = new List<GameObject>(16);

    // 현재 계산된 "가능한 결과 코드들"
    private readonly List<string> _currentResultCodes = new List<string>(8);


    //그,외
    public bool isActive = false;
    private Coroutine _fusionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 재료 선택 함수
    /// - code를 materials에 추가
    /// - materials.Count >= 2면 결과 프리뷰 갱신
    /// </summary>
    /// 

    public void SetActive(bool active)
    {
        // 기존 호출 호환: ChooseLoop 경로는 인터랙티브로 취급

        if (active == isActive) return;
        isActive = active;

        if (isActive)
        {
            //아직은 여기 코루틴으로 할 필요를 못느껴서 카드 포퓰레이트만 할예정 추후
            //필요하다 싶으면 코루틴으로 바꿔주기
            StartFusionLoop();
        }
        else StopFusionLoop();
    }
    private void StopFusionLoop()
    {
        if (_fusionCoroutine != null)
        {
            StopCoroutine(_fusionCoroutine);
            _fusionCoroutine = null;
        }
        isActive = false;

        // ★ 인터랙티브일 때만 Think_Done 트리거
      

       StartCoroutine(CloseHandsThenReturn());
    }

    private void StartFusionLoop()
    {
        // ★ 인터랙티브일 때만 생각 모션 트리거

        //    var anim = meGo.GetComponentInChildren<Animator>();
        //    if (anim) anim.SetTrigger("Trig_Think");


        if (_fusionCoroutine == null)
            _fusionCoroutine = StartCoroutine(FusionLoop());
    }

    private IEnumerator FusionLoop()
    {
        PopulateCards();
        materials.Clear();
        while (isActive)
        {
           
            // ESC 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelAndReturn();     // ← 필드로 체크
                yield break;
            }

            yield return null;
        }
    }
    public void CancelAndReturn()
    {
       
        if (_fusionCoroutine != null)
        {
            StopCoroutine(_fusionCoroutine);
            _fusionCoroutine = null;
        }
        isActive = false;

        // 카드 제거 → 손 사라짐 애니 → ChooseLoop 복귀
        StartCoroutine(CloseHandsThenReturn());
    }

    private IEnumerator CloseHandsThenReturn()
    {
     
        ClearAllCards();
     
        yield return null;
    }
    private void ClearAllCards()
    {
        for (int i = cardContentArea.childCount - 1; i >= 0; i--)
            Destroy(cardContentArea.GetChild(i).gameObject);

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

            /*     if (anim != null)
                 {
                     var deferrer = cardGO.GetComponent<CardApplyDeferrer>();
                     if (deferrer == null) deferrer = cardGO.AddComponent<CardApplyDeferrer>();
                     deferrer.Bind(
                         info, anim,
                         idleStateName: "Card_Actioin_Idle",
                         layerIndex: 0,
                         requireInitStateName: "Card_Actioin_Init"
                     );
                 }*/
            //else
            //{
            info.ApplyCardData(); // 애니 없으면 즉시 적용
            //}
        }

        // ★ 여기 추가: 이번 생성 라운드 버전 고유값 부여 후, 모두 적용될 때까지 기다렸다가 업그레이드 호출
        //  StartCoroutine(WaitCardsAppliedThenUpgrade(apData, myVersion));

        Debug.Log("합성재료 생성완료");
    }
    public void SelectMaterial(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return;

        code = code.Trim();

        // 재료 최대 3개. 초과하면 무시(원하면 FIFO로 바꿔도 됨)
        if (materials.Count >= 3)
        {
            Debug.LogWarning("[Fusion] 재료는 최대 3개입니다.");
            return;
        }

        materials.Add(code);

        if (logSelection)
            Debug.Log($"[Fusion] SelectMaterial: {code} (count={materials.Count})");

        RefreshPreview();
    }

    /// <summary>
    /// 재료 전체 초기화
    /// </summary>
    public void ClearMaterials()
    {
        materials.Clear();
        RefreshPreview();
    }

    /// <summary>
    /// 마지막 재료 하나 취소
    /// </summary>
    public void UndoLastMaterial()
    {
        if (materials.Count == 0) return;
        materials.RemoveAt(materials.Count - 1);
        RefreshPreview();
    }

    /// <summary>
    /// 합성될 카드 생성 함수 (현재는 로그만)
    /// </summary>
    public void CreateFusedCard(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;
        Debug.Log($"{code.Trim()} 카드 생성!");
    }

    // ─────────────────────────────────────────────
    // Preview
    // ─────────────────────────────────────────────
    private void RefreshPreview()
    {
        if (fusionDB == null)
        {
            Debug.LogWarning("[Fusion] fusionDB가 연결되지 않음");
            ClearPreview();
            return;
        }

        if (previewSpawnRoot == null || previewCardPrefab == null)
        {
            Debug.LogWarning("[Fusion] previewSpawnRoot / previewCardPrefab이 연결되지 않음");
            ClearPreview();
            return;
        }

        // 재료 2개 미만이면 프리뷰 없음
        if (materials.Count < 2)
        {
            ClearPreview();
            return;
        }

        // 가능한 결과 코드들 계산
        _currentResultCodes.Clear();
        CollectPossibleOutputs(materials, _currentResultCodes);

        if (_currentResultCodes.Count == 0)
        {
            if (clearPreviewOnNoResult) ClearPreview();
            return;
        }

        // 프리뷰 재생성(간단하고 확실)
        RebuildPreviewObjects(_currentResultCodes);
    }

    /// <summary>
    /// 현재 materials 상태에서 가능한 output들을 모은다.
    /// 규칙:
    /// - 재료가 2개면: 그 2개 조합 결과 + (추가 재료 1개로 확장 가능한 3재료 레시피의 "후보"도 보여줄지?) -> 여기선 "현재 재료로 완성되는 것만" 표시
    /// - 재료가 3개면: 3개 조합 결과
    ///
    /// 중요: 지금은 "현재 재료로 완성되는 레시피"만 프리뷰한다.
    /// </summary>
    private void CollectPossibleOutputs(List<string> currentMaterials, List<string> outputs)
    {
        // 현재 재료 그대로 완성되는 결과만 체크
        if (fusionDB.TryGetOutput(currentMaterials, out var output))
        {
            outputs.Add(output);
        }

        // 만약 "한 재료가 여러 카드(동일 키 중복)"를 허용하고 싶으면
        // DB 구조를 key->List로 바꿔야 하는데, 우리는 중복을 경고로 막는 구조라 여기선 1개만 나옴.
        // 향후 "복수 결과"가 필요하면 DB를 확장해줄게.
    }

    private void RebuildPreviewObjects(List<string> resultCodes)
    {
        ClearPreview();

        for (int i = 0; i < resultCodes.Count; i++)
        {
            string code = resultCodes[i];
            if (string.IsNullOrWhiteSpace(code)) continue;

            var cardGO = Instantiate(previewCardPrefab, previewSpawnRoot);

            // EachCardInfo 적용
            var info = cardGO.GetComponent<EachCardInfo>();
            if (info == null)
            {
                Debug.LogWarning("[Fusion] EachCardInfo 없음");
                Destroy(cardGO);
                continue;
            }

            var cardData = CSVLoader.Instance.GetCardByCode(code);
            if (cardData == null)
            {
                Debug.LogWarning($"[Fusion] CSVLoader에서 카드 코드 못 찾음: {code}");
                Destroy(cardGO);
                continue;
            }

            info.cardData = cardData;
            info.ApplyCardData();

            _previewInstances.Add(cardGO);
        }
    }

    private void ClearPreview()
    {
        for (int i = 0; i < _previewInstances.Count; i++)
        {
            if (_previewInstances[i] != null)
                Destroy(_previewInstances[i]);
        }
        _previewInstances.Clear();
    }

    // ─────────────────────────────────────────────
    // (선택) 외부에서 현재 결과를 쓰고 싶을 때
    // ─────────────────────────────────────────────
    public IReadOnlyList<string> GetCurrentMaterials() => materials;
    public IReadOnlyList<string> GetCurrentResultCodes() => _currentResultCodes;
}
