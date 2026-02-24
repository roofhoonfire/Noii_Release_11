using System.Collections.Generic;
using UnityEngine;



public class TempManager : MonoBehaviour
{
    public List<string> tempHands;

    public List<KnowledgeType> tempKnowledges;

    [Header("Runtime spawned knowledge prefabs")]
    public List<EachKnowledgeInfo> spawnedKnowledgeUIs = new List<EachKnowledgeInfo>();

    public static TempManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}