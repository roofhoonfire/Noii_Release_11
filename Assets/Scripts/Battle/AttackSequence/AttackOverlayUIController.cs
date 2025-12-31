using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AttackOverlayUIController : MonoBehaviour
{
    public static AttackOverlayUIController Instance { get; private set; }

    [Header("Background Wallpaper")]
    [SerializeField] private GameObject bgRoot;
    [SerializeField] private Image bgImage;

    [Header("Character CutIn (uses seq.cutInSprite)")]
    [SerializeField] private GameObject charRoot;
    [SerializeField] private Image charImage;

    [Header("Cut Line (no alpha needed)")]
    [SerializeField] private GameObject cutLineRoot;

    private Coroutine _bgFadeRoutine;
    private Coroutine _charFadeRoutine;

    private void Awake()
    {
        Instance = this;
        HardReset();
    }

    public void HardReset()
    {
        StopRunning();

        if (bgRoot) bgRoot.SetActive(false);
        if (charRoot) charRoot.SetActive(false);
        if (cutLineRoot) cutLineRoot.SetActive(false);

        SetAlpha(bgImage, 0f);
        SetAlpha(charImage, 0f);

        if (charImage) charImage.sprite = null;
    }

    public void BeginBackgroundFadeIn(float duration)
    {
        if (bgRoot == null || bgImage == null) return;

        bgRoot.SetActive(true);
        SetAlpha(bgImage, 0f);

        if (_bgFadeRoutine != null) StopCoroutine(_bgFadeRoutine);
        _bgFadeRoutine = StartCoroutine(FadeTo_Unscaled(bgImage, 1f, duration));
    }
    // AttackOverlayUIController.cs 에 추가/수정

    public void PrepareAndScheduleBackground(Sprite sprite, float delay, float fadeSeconds)
    {
        if (bgRoot == null || bgImage == null) return;

        // (원하면 배경 스프라이트도 교체 가능. 필요 없으면 이 줄 제거)
        if (sprite != null) bgImage.sprite = sprite;

        bgRoot.SetActive(false);
        SetAlpha(bgImage, 0f);

        if (_bgFadeRoutine != null) StopCoroutine(_bgFadeRoutine);
        _bgFadeRoutine = StartCoroutine(DelayThenFade_Unscaled(bgRoot, bgImage, delay, 1f, fadeSeconds));
    }

    public void PrepareAndScheduleCharacterCutIn(Sprite sprite, float delay, float fadeSeconds)
    {
        if (charRoot == null || charImage == null) return;

        charImage.sprite = sprite;

        // (선택 개선) delay 동안은 꺼두고, 시작할 때 켜는 편이 더 깔끔함
        charRoot.SetActive(false);
        SetAlpha(charImage, 0f);

        if (_charFadeRoutine != null) StopCoroutine(_charFadeRoutine);
        _charFadeRoutine = StartCoroutine(DelayThenFade_Unscaled(charRoot, charImage, delay, 1f, fadeSeconds));
    }

    public void ShowCutLine()
    {
        if (cutLineRoot) cutLineRoot.SetActive(true);
    }

    public void HideAllAndResetAlpha() => HardReset();

    // ─────────────────────────────

    private void StopRunning()
    {
        if (_bgFadeRoutine != null) StopCoroutine(_bgFadeRoutine);
        if (_charFadeRoutine != null) StopCoroutine(_charFadeRoutine);
        _bgFadeRoutine = null;
        _charFadeRoutine = null;
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    //  timeScale 영향 안 받는 페이드
    private static IEnumerator FadeTo_Unscaled(Image img, float targetA, float duration)
    {
        if (img == null) yield break;

        float startA = img.color.a;
        duration = Mathf.Max(0.0001f, duration);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;          // ★ 여기만 변경
            float u = Mathf.Clamp01(t / duration);

            var c = img.color;
            c.a = Mathf.Lerp(startA, targetA, u);
            img.color = c;

            yield return null;
        }

        var end = img.color;
        end.a = targetA;
        img.color = end;
    }

    // ✅ delay도 timeScale 무시
    private static IEnumerator DelayThenFade_Unscaled(GameObject root, Image img, float delay, float targetA, float duration)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);   // ★ 변경

        if (root != null) root.SetActive(true);

        // duration이 0이면 즉시
        if (duration <= 0.0001f)
        {
            var c = img.color;
            c.a = targetA;
            img.color = c;
            yield break;
        }

        yield return FadeTo_Unscaled(img, targetA, duration);
    }
}
