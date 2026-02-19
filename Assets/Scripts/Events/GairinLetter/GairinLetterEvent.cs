using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class GairinLetterEvent : MonoBehaviour
{
    public static GairinLetterEvent Instance { get; private set; }

    [Header("Targets (CanvasGroup required)")]
    [SerializeField] private GameObject fadeOutAndDisableObj; // 오브젝트1
    [SerializeField] private GameObject fadeInObj;            // 오브젝트2

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Optional: block gameplay input by disabling a controller)")]
    [SerializeField] private MonoBehaviour[] disableWhileEvent; // 필요하면 여기다 플레이어 입력 스크립트들 넣어라
    [SerializeField] FieldPlayerController3D fpc;



    [Header("gairin dialogue")]
    [SerializeField] private GameObject Stain; // 필요하면 여기다 플레이어 입력 스크립트들 넣어라
    [SerializeField] private JustDialogue JD;




    [Header("Patch dialogue")]
    [SerializeField] private JustDialogue JD2;



    [Header("garinLetterImage")]

    [SerializeField] private Image targetImage;          // 이미지 적용 대상
[SerializeField] private Sprite readableSprite;      // 10 이상
[SerializeField] private Sprite unreadableSprite;    // 10 미만







public bool IsEventActive { get; private set; }

    private CanvasGroup _cg1;
    private CanvasGroup _cg2;

    private Tween _tween1;
    private Tween _tween2;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (fadeOutAndDisableObj != null)
            _cg1 = GetOrAddCanvasGroup(fadeOutAndDisableObj);

        if (fadeInObj != null)
            _cg2 = GetOrAddCanvasGroup(fadeInObj);

        // 시작 상태: 오브젝트2는 꺼져있다고 가정(원하면 주석 해제)
        // if (fadeInObj != null) fadeInObj.SetActive(false);
    }

    private void Update()
    {
        if (!IsEventActive) return;

        // ESC로 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseEventUI();
            return;
        }

        // "오브젝트2 밖" 클릭하면 닫기
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerInsideObject2())
                CloseEventUI();
        }
    }

    /// <summary>
    /// 이벤트 시작: Obj1은 페이드아웃 후 비활성, Obj2는 활성 후 페이드인
    /// </summary>
    public void StartGairinEvent()
    {

        var state = GameStateManager.Instance.GetCharacterState("player");

        //  LanguageLev 체크
        if (targetImage != null)
        {
            int languageLevel = state.GetStat(DialogueFlag.LanguageLev);

            if (languageLevel >= 10)
                targetImage.sprite = readableSprite;
            else
                targetImage.sprite = unreadableSprite;
        }


        if (fadeOutAndDisableObj == null || fadeInObj == null)
        {
            Debug.LogWarning("[GairinLetterEvent] 오브젝트 참조가 비어있습니다.");
            return;
        }

        IsEventActive = true;
        SetInputBlocked(true);

        _cg1 = GetOrAddCanvasGroup(fadeOutAndDisableObj);
        _cg2 = GetOrAddCanvasGroup(fadeInObj);

        // Obj2 먼저 켜고 0에서 시작
        fadeInObj.SetActive(true);
        _cg2.alpha = 0f;
        _cg2.interactable = true;
        _cg2.blocksRaycasts = true;

        // Obj1 페이드아웃 → 끄기
        _tween1?.Kill();
        _tween1 = _cg1.DOFade(0f, fadeDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                fadeOutAndDisableObj.SetActive(false);
            });

        // Obj2 페이드인
        _tween2?.Kill();
        _tween2 = _cg2.DOFade(1f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        if (Stain != null)
        {

            JD.StartJustDialogue();
        }
    }

    public void StartPatchEvent()
    {

        JD2.StartJustDialogue();
    }


    private void CloseEventUI()
    {
        if (!IsEventActive) return;
        if (fadeInObj == null) { EndEvent(); return; }

        _cg2 = GetOrAddCanvasGroup(fadeInObj);

        _tween2?.Kill();
        _tween2 = _cg2.DOFade(0f, fadeDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                fadeInObj.SetActive(false);
                EndEvent();
            });
    }

    private void EndEvent()
    {
        IsEventActive = false;
        SetInputBlocked(false);
    }

    private void SetInputBlocked(bool blocked)
    {
        fpc.SetInputEnabled(!blocked);
        // 가장 확실: 이벤트 동안 특정 스크립트들을 꺼버린다
        if (disableWhileEvent != null)
        {
            for (int i = 0; i < disableWhileEvent.Length; i++)
            {
                if (disableWhileEvent[i] != null)
                    disableWhileEvent[i].enabled = !blocked;
            }
        }
    }

    private bool IsPointerInsideObject2()
    {
        if (_cg2 == null) return false;
        if (!fadeInObj.activeInHierarchy) return false;

        var rt = fadeInObj.transform as RectTransform;
        if (rt == null) return false;

        Camera cam = null;
        var canvas = rt.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, cam);
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }


}
