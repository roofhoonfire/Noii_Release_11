using System.Collections;
using UnityEngine;

public class TempScript : MonoBehaviour
{
    [Header("Canvas Root (CanvasGroup ÇÊ¼ö)")]
    [SerializeField] private GameObject canvasRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Children")]
    [SerializeField] private GameObject a;
    [SerializeField] private GameObject b;
    [SerializeField] private GameObject c;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.3f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (!canvasGroup && canvasRoot)
            canvasGroup = canvasRoot.GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            canvasRoot.SetActive(true);
            ActivateOnly(a);
            Fade(0f, 1f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ActivateOnly(b);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ActivateOnly(c);
        }
        else if (Input.GetKeyDown(KeyCode.Minus))
        {
            Fade(1f, 0f, () => canvasRoot.SetActive(false));
        }
    }

    private void ActivateOnly(GameObject target)
    {
        a.SetActive(false);
        b.SetActive(false);
        c.SetActive(false);

        target.SetActive(true);
    }

    private void Fade(float from, float to, System.Action onComplete = null)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(from, to, onComplete));
    }

    private IEnumerator FadeRoutine(float from, float to, System.Action onComplete)
    {
        float t = 0f;
        canvasGroup.alpha = from;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = to;
        onComplete?.Invoke();
    }
}
