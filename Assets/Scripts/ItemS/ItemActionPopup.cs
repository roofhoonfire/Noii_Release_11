using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EachItemInfo))]
public class ItemActionPopup : MonoBehaviour, IPointerClickHandler
{
    [Header("Buttons (Drag & Drop)")]
    [SerializeField] private Button useButton;
    [SerializeField] private Button dontButton;

    private EachItemInfo _info;
    private bool _popupOpen;

    private void Awake()
    {
        _info = GetComponent<EachItemInfo>();

        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(OnUseClicked);
        }

        if (dontButton != null)
        {
            dontButton.onClick.RemoveAllListeners();
            dontButton.onClick.AddListener(OnDontClicked);
        }

        HidePopup();
    }

    private void Update()
    {
        if (!_popupOpen) return;

        // 팝업이 켜진 상태에서, UI 아무 곳 클릭이 발생했을 때
        if (Input.GetMouseButtonDown(0))
        {
            // 클릭된 UI가 "useButton/dontButton(또는 그 자식)"이면 무시 (버튼 onClick이 처리)
            if (IsPointerOver(useButton) || IsPointerOver(dontButton))
                return;

            // 그 외 공간 클릭이면 닫기
            HidePopup();
        }
    }

    // 아이템(루트 이미지)을 클릭하면 호출됨
    public void OnPointerClick(PointerEventData eventData)
    {
        // 버튼 위를 눌렀을 때 루트 클릭으로 같이 들어오는 경우 방지
        if (IsPointerOver(useButton) || IsPointerOver(dontButton))
            return;

        ShowPopup();
    }

    private void OnDontClicked()
    {
        HidePopup();
    }

    private void OnUseClicked()
    {
        HidePopup();

        if (_info == null || _info.itemData == null)
        {
            Debug.LogWarning("[ItemActionPopup] itemData가 비어있습니다.");
            return;
        }

        int type = _info.itemData.itemType;

        if (type == 1)
        {

            GairinLetterEvent.Instance.StartGairinEvent();


        }
        else if (type == 0) Debug.Log("사용불가능한 아이템입니다");

        else if (type == 2) GairinLetterEvent.Instance.StartPatchEvent();

        else Debug.Log($"알 수 없는 아이템 타입입니다: {type}");
    }

    private void ShowPopup()
    {
        _popupOpen = true;

        if (useButton != null) useButton.gameObject.SetActive(true);
        if (dontButton != null) dontButton.gameObject.SetActive(true);
    }

    private void HidePopup()
    {
        _popupOpen = false;

        if (useButton != null) useButton.gameObject.SetActive(false);
        if (dontButton != null) dontButton.gameObject.SetActive(false);
    }

    private static bool IsPointerOver(Button btn)
    {
        if (btn == null) return false;
        if (EventSystem.current == null) return false;

        var go = EventSystem.current.currentSelectedGameObject;
        if (go == null) return false;

        // 선택된 오브젝트가 버튼 자신/자식이면 true
        return go == btn.gameObject || go.transform.IsChildOf(btn.transform);
    }



}
