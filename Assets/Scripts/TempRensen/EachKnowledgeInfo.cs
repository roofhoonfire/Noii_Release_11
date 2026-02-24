using UnityEngine;
using TMPro; // TMP 쓰면
using UnityEngine.UI;

public class EachKnowledgeInfo : MonoBehaviour
{
    [Header("Runtime")]
    public KnowledgeType kt;
    public string knowledgeText;

    [Header("UI (optional)")]
    [SerializeField] private TextMeshProUGUI text;   // TMP 쓰면 이거
 
    public void ApplyData(KnowledgeType type)
    {
        kt = type;
        knowledgeText = $"this knowledge is {type}";

        if (text != null) text.text = knowledgeText;
     
    }
}