using UnityEngine;

public class NextActionMonitor : MonoBehaviour
{
    public static NextActionMonitor Instance { get; private set; }

    [Header("Watched Actions")]
    public ActionData myNextAction;
    public ActionData opNextAction;

    public ActionData preprevExecuted=null;
    public ActionData prevExecuted = null;
    public int prevActor = 1;
    public int preprevActor = 1;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 내 다음 액션 참조 설정 (참조 공유!)
    /// </summary>
    public void SetMyNextAction(ActionData action)
    {
        myNextAction = action;
    }

    /// <summary>
    /// 상대 다음 액션 참조 설정
    /// </summary>
    public void SetOpNextAction(ActionData action)
    {
        opNextAction = action;
    }

    /// <summary>
    /// 액션 소비/무효화 시 클리어
    /// </summary>
    public void ClearMyNextAction()
    {
        myNextAction = null;
    }
}
