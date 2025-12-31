using UnityEngine;
using UnityEngine.Events;

public abstract class FieldInteractableBase : MonoBehaviour, IInteractable
{
    [Header("Common Info")]
    [Tooltip("디버그/로그용 이름")]
    public string displayName = "Interactable";

    [TextArea]
    [Tooltip("간단 설명 (디버그용)")]
    public string description;

    [Header("Events")]
    [Tooltip("상호작용 시 추가로 실행할 유니티 이벤트 (애니메이션, 사운드 등 연결용)")]
    public UnityEvent onInteract;

    /// <summary>
    /// IInteractable 인터페이스 구현
    /// </summary>
    public void Interact(GameObject interactor)
    {
        Debug.Log($"[Interact] {displayName} interacted by {interactor.name}");

        // 공통 이벤트 먼저
        onInteract?.Invoke();

        // 파생 클래스 전용 로직
        OnInteract(interactor);
    }

    /// <summary>
    /// 파생 클래스에서 구현하는 실제 상호작용 내용
    /// </summary>
    protected abstract void OnInteract(GameObject interactor);
}
