using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GairinLetterRub : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Rub")]
    [SerializeField] private int alphaStep = 10;
    [SerializeField] private int destroyAlphaThreshold = 100;
    [SerializeField] private float scrubStepPixels = 18f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip rubClip;

    private float _accumMove;
    private Vector3 _prevMousePos;

    private bool _destroyScheduled;

    private void Awake()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (targetImage == null) return;
        if (_destroyScheduled) return;

        if (Input.GetMouseButtonDown(0))
        {
            _prevMousePos = Input.mousePosition;
            _accumMove = 0f;
        }

        if (!Input.GetMouseButton(0)) return;
        if (!IsMouseOverThisImage()) return;

        Vector3 cur = Input.mousePosition;
        float delta = (cur - _prevMousePos).magnitude;
        _prevMousePos = cur;

        _accumMove += delta;

        if (_accumMove >= scrubStepPixels)
        {
            _accumMove -= scrubStepPixels;

            if (audioSource != null && rubClip != null && !audioSource.isPlaying)
            {
                audioSource.clip = rubClip;
                audioSource.Play();
            }

            Color c = targetImage.color;
            int a255 = Mathf.RoundToInt(c.a * 255f);
            a255 = Mathf.Max(0, a255 - alphaStep);
            c.a = a255 / 255f;
            targetImage.color = c;

            if (a255 <= destroyAlphaThreshold)
            {
                _destroyScheduled = true;

                // 여기서 한 번만 보상/메시지 처리
                NarrationOverlay.Instance.Show("지혜가 2 상승했습니다");
                var charState = GameStateManager.Instance.GetCharacterState("player");
                charState.intel += 2;
                // TODO: charState.intel += 5; 같은 실제 반영 코드 넣고 싶으면 여기서

                StartCoroutine(DestroyAfterAudio());
            }
        }
    }

    private IEnumerator DestroyAfterAudio()
    {
        // 오디오가 재생 중이면 끝날 때까지 대기
        if (audioSource != null && audioSource.isPlaying)
        {
            // clip.length로 기다리는 것보다 실제 재생 상태를 기다리는 게 안전
            yield return new WaitWhile(() => audioSource != null && audioSource.isPlaying);
        }

        Destroy(gameObject);
    }

    private bool IsMouseOverThisImage()
    {
        RectTransform rt = targetImage.rectTransform;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);
    }
}
