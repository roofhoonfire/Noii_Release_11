using System;
using System.Collections;
using UnityEngine;

public class HitStopController : MonoBehaviour
{
    public static HitStopController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    //  timeScale=0 "진입 직후"에 뭔가(쉐이크 등) 실행할 수 있게 콜백 추가
    public IEnumerator DoHitStop(float seconds, Action onEnterHitStop = null)
    {
        if (seconds <= 0f)
        {
            onEnterHitStop?.Invoke();
            yield break;
        }

        float prev = Time.timeScale;
        Time.timeScale = 0f;

        //  히트스톱 상태(=0) 들어간 직후 실행
        onEnterHitStop?.Invoke();

        // timeScale=0 이므로 반드시 Realtime로 기다려야 함
        yield return new WaitForSecondsRealtime(seconds);

        Time.timeScale = prev;
    }
}
