using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JujuManager : MonoBehaviour
{
    public static JujuManager Instance { get; private set; }

    [Header("for JujuSelect")]
    [SerializeField] private Transform SpawnA;
    [SerializeField] private Transform SpawnB;
    [SerializeField] private Transform SpawnC;

    [SerializeField] private GameObject jujuSelectPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public IEnumerator ChooseJuju()
    {
        if (jujuSelectPrefab == null || SpawnA == null || SpawnB == null || SpawnC == null)
        {
            Debug.LogWarning("[ChooseJuju] 주주 선택 세팅 누락(프리팹/스폰포인트)!");
            yield break;
        }

        if (CSVLoader.Instance == null)
        {
            Debug.LogWarning("[ChooseJuju] CSVLoader.Instance가 없습니다.");
            yield break;
        }

        if (SelectionBuffer.Instance == null)
        {
            Debug.LogWarning("[ChooseJuju] SelectionBuffer.Instance가 없습니다.");
            yield break;
        }

        if (BattleManager.Instance == null)
        {
            Debug.LogWarning("[ChooseJuju] BattleManager.Instance가 없습니다.");
            yield break;
        }

        // jujuList가 private면 접근 불가 → BattleManager에 public getter 필요
        var list = BattleManager.Instance.jujuList;
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("[ChooseJuju] jujuList가 비어있음");
            yield break;
        }

        SelectionBuffer.Instance.BeginSelection();

        var picked = Pick3(list); // string code 3개

        GameObject a = SpawnJujuSelect(SpawnA, picked[0]);
        GameObject b = SpawnJujuSelect(SpawnB, picked[1]);
        GameObject c = SpawnJujuSelect(SpawnC, picked[2]);

        while (!SelectionBuffer.Instance.isSelectingDone)
            yield return null;

        string selected = SelectionBuffer.Instance.selectedResultCode;
        Debug.Log($"[ChooseJuju] Juju 선택 결과: {selected}");

        var juju = CSVLoader.Instance.GetJujuByCode(selected);
        if (juju != null)
        {
            // TODO: 여기서 주주 효과 발동 코루틴 호출하면 됨
            // yield return JujuActivator.Instance.ActivateJujuEffects(juju);
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
                int idx = Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                int idx = Random.Range(0, src.Count);
                result.Add(src[idx]);
            }
        }

        return result;
    }

    private GameObject SpawnJujuSelect(Transform spawnPoint, string jujuCode)
    {
        var go = Instantiate(jujuSelectPrefab, spawnPoint);

        var juju = CSVLoader.Instance.GetJujuByCode(jujuCode);
        if (juju == null)
            Debug.LogWarning($"[ChooseJuju] Juju 메타 없음: {jujuCode}");

        //  네 프로젝트에 EachJujuInfo가 있다고 가정 (없으면 이 부분만 네 스크립트명으로 바꿔)
        var info = go.GetComponent<EachJujuInfo>();
        if (info != null)
        {
            info.jujuData = juju;
            info.ApplyJujuData();
        }
        else
        {
            Debug.LogWarning("[ChooseJuju] EachJujuInfo가 프리팹에 없음");
        }

        var selector = go.GetComponent<WhichCardSelected>();
        if (selector == null) selector = go.AddComponent<WhichCardSelected>();
        selector.SetKind(WhichCardSelected.SelectKind.Juju); //  Juju 분기용

        return go;
    }
}