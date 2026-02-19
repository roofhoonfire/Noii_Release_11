using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum KnowledgeType
{
    sword,
    defend,
    heist,
}
public class StellarNode : MonoBehaviour
{

    [Header("StellarNodeInfo")]
    [SerializeField] public List <StellarNode> childNodes;
    [SerializeField] int nodeNum;
    [SerializeField] public StellarNode paretNode;
    [SerializeField] public string rewarCardCode;
    [SerializeField] public List <KnowledgeType> costKnowledge;


}
