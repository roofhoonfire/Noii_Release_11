using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EachItemInfo : MonoBehaviour
{


    public Item itemData;

    [SerializeField] private TooltipTriggerUI tooltip;
   // public TextMeshProUGUI nameText;
  //  public TextMeshProUGUI descriptionText;
    
        [Header("Artwork")]

    [SerializeField] private Image artworkImage;   // ★ 인스펙터 드래그&드롭
    //[SerializeField] private GameObject artworkImageMom;   // ★ 인스펙터 드래그&드롭
                                                           // Start is called before the first frame update
    public void ApplyItemData()
    {


        if (itemData != null ) {

         //   nameText.text = itemData.name;
          //  descriptionText.text = itemData.itemText.Replace("\\n", "\n"); ;
            if (itemData.sprite != null && artworkImage != null )
            {
                //artworkImageMom.SetActive(true);
                artworkImage.sprite = itemData.sprite;

            }

            if (tooltip != null)
            {
                //artworkImageMom.SetActive(true);
                tooltip.description = itemData.itemText;
                Debug.LogWarning("설명일발장전");

            }
        }
        else
        {
            Debug.LogWarning("[EachItemInfo] itemData 또는 nameText가 비어있습니다.");
        }

    }
}
