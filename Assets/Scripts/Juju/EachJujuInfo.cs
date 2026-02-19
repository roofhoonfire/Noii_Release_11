using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

using UnityEngine;

public class EachJujuInfo : MonoBehaviour
{
    public Juju jujuData;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI playtext;

    [Tooltip("카드 일러스트가 표시될 UI Image")]
    [SerializeField] private Image artworkImage;   // ★ 인스펙터 드래그&드롭
    [SerializeField] private GameObject artworkImageMom;   // ★ 인스펙터 드래그&드롭


    public void ApplyJujuData()
    {


        if (jujuData != null && nameText != null)
        {
            nameText.text = jujuData.name;
            playtext.text = jujuData.text;

            if (jujuData.sprite != null && artworkImage != null && artworkImageMom != null)
            {
                artworkImageMom.SetActive(true);
                artworkImage.sprite = jujuData.sprite;

            }
        }
        else
        {
            Debug.LogWarning("[주주데이터] 주주데이터 또는 nameText가 비어있습니다.");
        }
    }


}
