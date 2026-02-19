using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundManager : MonoBehaviour
{
    public static BoundManager Instance { get; private set; }
    [Header("UI")]
    [SerializeField] private Transform gridLayoutRoot;
    [SerializeField] private GameObject chakraPrefab;

    [Header("Animation")]
    [SerializeField] private float interval = 0.05f;

    [Header("Runtime")]
    [SerializeField] private List<GameObject> chakraObjects = new();



    [Header("for BoundSelect")]
    [SerializeField] private Transform SpawnA;
    [SerializeField] private Transform SpawnB;
    [SerializeField] private Transform SpawnC;
    
    [SerializeField] private GameObject boundSelectPrefab;
    [SerializeField] private GameObject JujuSelectPrefab;

    private Coroutine _refreshRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 외부에서: StartCoroutine(BoundManager.Instance.RefreshChakraRoutine());
    public IEnumerator RefreshChakraRoutine()
    {
        if (gridLayoutRoot == null || chakraPrefab == null)
        {
            Debug.LogError("[BoundManager] gridLayoutRoot / chakraPrefab 인스펙터 세팅 필요");
            yield break;
        }

        if (BattleManager.Instance == null || BattleManager.Instance.apData == null)
        {
            Debug.LogError("[BoundManager] BattleManager.Instance 또는 apdata가 null");
            yield break;
        }

        int target = Mathf.Max(0, BattleManager.Instance.apData.currentChakra);

        while (chakraObjects.Count < target)
        {
            var go = Instantiate(chakraPrefab, gridLayoutRoot);
            chakraObjects.Add(go);
            if (interval > 0f) yield return new WaitForSeconds(interval);
            else yield return null;
        }

        while (chakraObjects.Count > target)
        {
            int last = chakraObjects.Count - 1;
            var go = chakraObjects[last];
            chakraObjects.RemoveAt(last);
            if (go != null) Destroy(go);

            if (interval > 0f) yield return new WaitForSeconds(interval);
            else yield return null;
        }
    }

    // BoundManager가 스스로 돌리는 버전도 남겨둠 (원하면 안 써도 됨)
    public void RefreshChakra()
    {
        if (_refreshRoutine != null)
            StopCoroutine(_refreshRoutine);

        _refreshRoutine = StartCoroutine(RefreshChakraRoutine());
    }


    public IEnumerator ChooseBound()
    {
        if (boundSelectPrefab == null || SpawnA == null || SpawnB == null || SpawnC == null)
        {
            Debug.LogWarning("[ChooseBound] 바운드 선택 세팅 누락(프리팹/스폰포인트)!");
            yield break;
        }

        if (CSVLoader.Instance == null)
        {
            Debug.LogWarning("[ChooseBound] CSVLoader.Instance가 없습니다.");
            yield break;
        }

        if (SelectionBuffer.Instance == null)
        {
            Debug.LogWarning("[ChooseBound] SelectionBuffer.Instance가 없습니다.");
            yield break;
        }

        if (BattleManager.Instance == null)
        {
            Debug.LogWarning("[ChooseBound] BattleManager.Instance가 없습니다.");
            yield break;
        }

        // boundList가 private면 여기 접근 불가 → BattleManager에 public getter 필요
        var list = BattleManager.Instance.boundList;
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("[ChooseBound] boundList가 비어있음");
            yield break;
        }

        SelectionBuffer.Instance.BeginSelection();

        var picked = Pick3(list); // string code 3개

        GameObject a = SpawnBoundSelect(SpawnA, picked[0]);
        GameObject b = SpawnBoundSelect(SpawnB, picked[1]);
        GameObject c = SpawnBoundSelect(SpawnC, picked[2]);

        while (!SelectionBuffer.Instance.isSelectingDone)
            yield return null;

        string selected = SelectionBuffer.Instance.selectedResultCode;
        Debug.Log($"[ChooseBound] Bound 선택 결과: {selected}");

        var bound = CSVLoader.Instance.GetBoundByCode(selected);

        if (bound != null)
        {
            yield return BoundActivator.Instance.ActivateBoundEffects(bound);
            if (bound.boundType == BoundType.Force)
            {
                yield return JujuManager.Instance.ChooseJuju();
                //주주 호출
            }
        }
        if (a) Destroy(a);
        if (b) Destroy(b);
        if (c) Destroy(c);
    }

    private List<string> Pick3(List<string> src)
    {
        var result = new List<string>(3);

        if (src.Count >= 3)
        {
            var pool = new List<string>(src);
            for (int i = 0; i < 3; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
        }
        else
        {
            // 1~2개면 중복 허용해서 3개 맞춤
            for (int i = 0; i < 3; i++)
            {
                int idx = UnityEngine.Random.Range(0, src.Count);
                result.Add(src[idx]);
            }
        }

        return result;
    }

    private GameObject SpawnBoundSelect(Transform spawnPoint, string boundCode)
    {
        var go = Instantiate(boundSelectPrefab, spawnPoint);

        var bound = CSVLoader.Instance.GetBoundByCode(boundCode);
        if (bound == null)
            Debug.LogWarning($"[ChooseBound] Bound 메타 없음: {boundCode}");

        var info = go.GetComponent<EachBoundInfo>();
        if (info != null)
        {
            info.boundData = bound;
            info.ApplyBoundData();
        }
        else
        {
            Debug.LogWarning("[ChooseBound] EachBoundInfo가 프리팹에 없음");
        }

        var selector = go.GetComponent<WhichCardSelected>();
        if (selector == null) selector = go.AddComponent<WhichCardSelected>();
        selector.SetKind(WhichCardSelected.SelectKind.Bound);

        return go;
    }

   
}