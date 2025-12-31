using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EachItemInfo : MonoBehaviour
{


    public Item itemData;

    public TextMeshProUGUI nameText;
  //  public TextMeshProUGUI descriptionText;
    
        [Header("Artwork")]

    [SerializeField] private Image artworkImage;   // ★ 인스펙터 드래그&드롭
    [SerializeField] private GameObject artworkImageMom;   // ★ 인스펙터 드래그&드롭
                                                           // Start is called before the first frame update
    public void ApplyItemData()
    {


        if (itemData != null && nameText != null) {

            nameText.text = itemData.name;
          //  descriptionText.text = itemData.itemText.Replace("\\n", "\n"); ;
            if (itemData.sprite != null && artworkImage != null && artworkImageMom != null)
            {
                artworkImageMom.SetActive(true);
                artworkImage.sprite = itemData.sprite;

            }
        }
        else
        {
            Debug.LogWarning("[EachItemInfo] itemData 또는 nameText가 비어있습니다.");
        }

    }
}
