using UnityEngine;

public class BombZone : MonoBehaviour
{
    public enum Shape { Box, Sphere }

    [Header("ID")]
    [Tooltip("패턴에서 참조할 폭탄 존 ID (0~29 등)")]
    public int zoneId = 0;

    [Header("Shape")]
    public Shape shape = Shape.Box;
    public Vector3 boxHalfExtents = new Vector3(1.2f, 1.0f, 1.2f);
    public float sphereRadius = 1.5f;

    [Header("Collision")]
    public LayerMask playerLayer;

    
    [Header("Visual Child")]
    [Tooltip("하위 비주얼 오브젝트의 Animator (폭발 애니메이션이 있는 곳)")]
    public Animator visualAnimator;

    [Tooltip("delayBeforeStep 시작 순간에 쏠 트리거")]
    public string bombTrigger = "Bomb";
   
    public void PlayBombTimelineLike()
    {
        if (visualAnimator == null) return;
        if (!string.IsNullOrEmpty(bombTrigger))
            visualAnimator.SetTrigger(bombTrigger);
    }

    public bool CheckPlayerHitNow()
    {
        if (shape == Shape.Box)
        {
            var cols = Physics.OverlapBox(
                transform.position,
                boxHalfExtents,
                transform.rotation,
                playerLayer,
                QueryTriggerInteraction.Collide);

            return cols != null && cols.Length > 0;
        }
        else
        {
            var cols = Physics.OverlapSphere(
                transform.position,
                sphereRadius,
                playerLayer,
                QueryTriggerInteraction.Collide);

            return cols != null && cols.Length > 0;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);

        if (shape == Shape.Box)
        {
            var prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, boxHalfExtents * 2f);
            Gizmos.matrix = prev;
        }
        else
        {
            Gizmos.DrawSphere(transform.position, sphereRadius);
        }
    }
#endif
}