using UnityEngine;

public class SelectionBuffer : MonoBehaviour
{
    public static SelectionBuffer Instance { get; private set; }

    [Header("Selection Result")]
    public string selectedResultCode;
    public bool isSelectingDone;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void BeginSelection()
    {
        selectedResultCode = null;
        isSelectingDone = false;
    }

    public void SetResult(string code)
    {
        selectedResultCode = code;
        isSelectingDone = true;
    }
}
