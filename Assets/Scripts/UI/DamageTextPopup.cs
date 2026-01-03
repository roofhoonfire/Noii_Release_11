using System.Collections;
using TMPro;
using UnityEngine;

public class DamageTextPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text text;

    [Header("Motion")]
    [SerializeField] private float lifeTime = 0.7f;
    [SerializeField] private float riseDistance = 0.8f;
    [SerializeField] private float horizontalJitter = 0.25f;

    [Header("Scale")]
    [SerializeField] private float startScale = 0.85f;
    [SerializeField] private float peakScale = 1.2f;

    [Header("Fade")]
    [SerializeField] private float fadeOutStart = 0.35f; // 0~lifeTime 비율이 아니라 "초"로 두자
    [SerializeField] private float fadeOutDuration = 0.35f;

    private void Reset()
    {
        text = GetComponentInChildren<TMP_Text>();
    }

    public void Setup(string message)
    {
        if (text != null) text.text = message;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (text == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * riseDistance;

        // 살짝 옆으로 흔들리는 느낌(랜덤)
        endPos += new Vector3(Random.Range(-horizontalJitter, horizontalJitter), 0f, 0f);

        // 초기값
        transform.localScale = Vector3.one * startScale;

        Color c0 = text.color;
        c0.a = 1f;
        text.color = c0;

        float t = 0f;

        // 1) 처음엔 “팡” 커졌다가
        // 2) 위로 부드럽게 올라가며
        // 3) 후반엔 알파로 사라짐
        while (t < lifeTime)
        {
            float u = t / lifeTime;

            // 위치: easeOut 느낌
            float posU = EaseOutCubic(u);
            transform.position = Vector3.LerpUnclamped(startPos, endPos, posU);

            // 스케일: 초반에 팡 커지는 느낌
            float scaleU = EaseOutBack(Mathf.Clamp01(u * 1.2f));
            float sc = Mathf.Lerp(startScale, peakScale, scaleU);
            transform.localScale = Vector3.one * sc;

            // 페이드
            if (t >= fadeOutStart)
            {
                float fu = Mathf.Clamp01((t - fadeOutStart) / Mathf.Max(0.0001f, fadeOutDuration));
                Color c = text.color;
                c.a = Mathf.Lerp(1f, 0f, fu);
                text.color = c;
            }

            t += Time.unscaledDeltaTime; //  히트스탑(timeScale=0) 중에도 움직이게 하고 싶으면 이게 맞다
            yield return null;
        }

        Destroy(gameObject);
    }

    private static float EaseOutCubic(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    private static float EaseOutBack(float x)
    {
        // 살짝 튕기는 느낌
        x = Mathf.Clamp01(x);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
