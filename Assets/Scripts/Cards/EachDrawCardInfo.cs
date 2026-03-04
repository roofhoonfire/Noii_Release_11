using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EachDrawCardInfo : MonoBehaviour
{
    [Header("Data")]
    public DrawCard drawCardData;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI playText;

    [Header("Artwork")]
    [SerializeField] private Image artworkImage;
    [SerializeField] private GameObject artworkImageMom;

    [Header("Patching FX")]
    [SerializeField] private GameObject drawcardPatching; //  추가

    public void ApplyDrawCardData()
    {
        if (drawCardData == null)
        {
            Debug.LogWarning("[EachDrawCardInfo] drawCardData가 비어있습니다.");
            return;
        }

        if (nameText != null)
            nameText.text = drawCardData.name ?? string.Empty;

        if (playText != null)
            playText.text = (drawCardData.cardText ?? string.Empty).Replace("\\n", "\n");

        // Artwork
        if (artworkImageMom != null)
        {
            bool hasSprite = (drawCardData.sprite != null && artworkImage != null);
            artworkImageMom.SetActive(hasSprite);

            if (hasSprite)
                artworkImage.sprite = drawCardData.sprite;
        }
        else
        {
            if (artworkImage != null)
                artworkImage.sprite = drawCardData.sprite;
        }

        //  기본은 꺼둠 (연출 타이밍에 켤 거니까)
        if (drawcardPatching != null)
            drawcardPatching.SetActive(false);
    }

    public void SetPatchingActive(bool active)
    {
        if (drawcardPatching != null)
            drawcardPatching.SetActive(active);
    }

    public Animator GetPatchingAnimator()
    {
        if (drawcardPatching == null) return null;
        return drawcardPatching.GetComponent<Animator>();
    }
}