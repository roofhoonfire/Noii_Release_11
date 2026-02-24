// StellarNode.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StellarNode : MonoBehaviour
{
    [Header("StellarNodeInfo")]
    [SerializeField] public List<StellarNode> childNodes;
    [SerializeField] int nodeNum;
    [SerializeField] public StellarNode paretNode;
    [SerializeField] public string rewarCardCode;
    [SerializeField] public List<KnowledgeType> costKnowledge;
    [SerializeField] public int nodeTier;

    [Header("UI (child Image drag&drop)")]
    [SerializeField] private Image clickImage;

    public string debugString;
    private void Reset()
    {
        // 루트에 붙었을 때, 자식에서 Image 자동으로 하나 잡아보려는 시도(선택)
        if (clickImage == null) clickImage = GetComponentInChildren<Image>(true);
    }

    private void Awake()
    {
        // 자식 Image에 클릭 프록시(더블클릭 감지) 자동 부착
        if (clickImage != null)
        {
            var proxy = clickImage.GetComponent<StellarNodeClickProxy>();
            if (proxy == null) proxy = clickImage.gameObject.AddComponent<StellarNodeClickProxy>();
            proxy.Bind(this);
        }
        else
        {
            Debug.LogWarning($"[StellarNode] clickImage가 비어있음: {name}");
        }
    }

    public void OnDoubleClicked()
    {
        // 요구 재료 = childNodes(카드코드) + costKnowledge
        // 보상 = 내 rewarCardCode
        StellarStateMode.Instance.TrySynthesize(childNodes, costKnowledge, rewarCardCode);
        KnowledgeManger.Instance.Rebuild();
    
    }
}