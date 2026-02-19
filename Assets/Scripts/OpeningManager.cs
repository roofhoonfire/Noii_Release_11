using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class OpeningManager : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector timeline;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "Field_1Final";

    private void Start()
    {
        if (timeline == null)
        {
            Debug.LogWarning("[OpeningManager] PlayableDirector(타임라인)가 비어있음!");
            return;
        }

        timeline.stopped += OnTimelineStopped;
        timeline.Play();
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        // 안전하게 한 번만 실행되게 해제
        timeline.stopped -= OnTimelineStopped;

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[OpeningManager] nextSceneName이 비어있음!");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴될 때도 이벤트 해제 (중복 구독 방지)
        if (timeline != null)
            timeline.stopped -= OnTimelineStopped;
    }
}