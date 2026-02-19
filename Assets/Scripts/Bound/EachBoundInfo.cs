using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

using UnityEngine;

public class EachBoundInfo : MonoBehaviour
{
    public Bound boundData;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI playtext;

    [Tooltip("카드 일러스트가 표시될 UI Image")]
    [SerializeField] private Image artworkImage;   // ★ 인스펙터 드래그&드롭
    [SerializeField] private GameObject artworkImageMom;   // ★ 인스펙터 드래그&드롭


    public void ApplyBoundData()
    {

      
            if (boundData != null && nameText != null)
            {
                nameText.text = boundData.name;
                playtext.text = boundData.text;

            if (boundData.sprite != null && artworkImage != null && artworkImageMom != null)
            {
                artworkImageMom.SetActive(true);
                artworkImage.sprite = boundData.sprite;

            }
        }
            else
            {
                Debug.LogWarning("[boundData] boundData 또는 nameText가 비어있습니다.");
            }
        }

      
}
