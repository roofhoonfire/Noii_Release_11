using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;
using UnityEngine.UI;

public class F1Director : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector timeline;

    [Header("Player")]
    [SerializeField] private FieldPlayerController3D playerController;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private GameObject characterVisualRoot;
    [SerializeField] private SpriteRenderer Noii_RealSprite;
    [SerializeField] private GameObject Noii_TimeLine;

    [Header("Arrows UI")]
    [SerializeField] private GameObject leftArrow;
    [SerializeField] private GameObject rightArrow;

    [Header("Images")]
    [SerializeField] private SpriteRenderer characterSpriteRenderer;
    [SerializeField] private Sprite getUpIdle1;
    [SerializeField] private Sprite getUpIdle2;

    [Header("Animations")]
    [SerializeField] private AnimationClip getUpAnim1;
    [SerializeField] private AnimationClip getUpAnim2;
    [SerializeField] private AnimationClip getUpAnim3;
    [SerializeField] private AnimationClip getUpAnim4;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera wideShotCam;
    [SerializeField] private CinemachineCamera timeLineLastshot;


    [Header("SetActiveKeyBoard")]
    [SerializeField] private GameObject Ekey1;
    [SerializeField] private GameObject Ekey2;


    private int _inputCount = 0;
    private KeyCode _lastKey = KeyCode.None;

    private void Start()
    {
        StartCoroutine(MainSequence());
    }

    private IEnumerator MainSequence()
    {
        Color color = Noii_RealSprite.color;
        color.a = 0;
        Noii_RealSprite.color = color;
        // 1️⃣ 타임라인 재생
        if (timeline != null)
        {
            timeline.Play();
            yield return new WaitForSeconds((float)timeline.duration);

         

        }

        // 2️⃣ 플레이어 입력 차단
        playerController.SetInputEnabled(false);
 

        // 3️⃣ 1페이즈
        yield return StartCoroutine(GetUpPhase(
      getUpIdle1,
      getUpAnim1,
      getUpAnim2
  ));

        yield return StartCoroutine(GetUpPhase(
            getUpIdle2,
            getUpAnim3,
            getUpAnim4
        ));

        // 5️⃣ 정상 복구
        playerController.SetInputEnabled(true);

        if (wideShotCam != null)
        {
            timeLineLastshot.Priority = 0;
            wideShotCam.Priority = 10; // 다른 필드카메라보다 높게
        }

        Noii_TimeLine.SetActive(false);

    
        color.a = 1;
        Noii_RealSprite.color = color;
    
        Ekey1.SetActive(true);
        Ekey2.SetActive(true);
    }

    private IEnumerator GetUpPhase(Sprite idleSprite, AnimationClip pressClip, AnimationClip transitionClip)
    {
        leftArrow.SetActive(true);
        rightArrow.SetActive(true);

        _inputCount = 0;
        _lastKey = KeyCode.None;

        if (characterSpriteRenderer != null && idleSprite != null)
            characterSpriteRenderer.sprite = idleSprite;

        while (_inputCount < 20)
        {
            if (CheckAlternatingInput())
            {
                if (pressClip != null)
                    characterAnimator.Play(pressClip.name, 0, 0f);

                _inputCount++;
            }

            yield return null;
        }

        if (transitionClip != null)
            characterAnimator.Play(transitionClip.name, 0, 0f);

        yield return new WaitForSeconds(transitionClip != null ? transitionClip.length : 1f);

        leftArrow.SetActive(false);
        rightArrow.SetActive(false);
    }
    private bool CheckAlternatingInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (_lastKey != KeyCode.LeftArrow)
            {
                _lastKey = KeyCode.LeftArrow;
                return true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (_lastKey != KeyCode.RightArrow)
            {
                _lastKey = KeyCode.RightArrow;
                return true;
            }
        }

        return false;
    }
}