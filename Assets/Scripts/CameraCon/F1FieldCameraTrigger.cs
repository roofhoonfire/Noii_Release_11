using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class F1FieldCameraTrigger : MonoBehaviour
{
    private void Reset()
    {
        var bc = GetComponent<BoxCollider>();
        bc.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // "플레이어만" 원하면 태그 체크로 제한해라
        // if (!other.CompareTag("Player")) return;

        if (F1FieldCameraController.Instance == null) return;
        F1FieldCameraController.Instance.NotifyEnter(GetComponent<Collider>());
    }
}
