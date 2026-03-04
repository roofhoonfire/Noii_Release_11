using UnityEngine;

/// <summary>
/// Singleton: 드래그드랍한 SpriteRenderer의 색을 흰색으로 변경.
/// </summary>
public class SpriteMaterialSwapper : MonoBehaviour
{
    public static SpriteMaterialSwapper Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private SpriteRenderer targetRenderer; // SpriteRenderer 달린 오브젝트 드래그&드랍

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetTarget(SpriteRenderer sr)
    {
        targetRenderer = sr;
    }

    public void Apply()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning("[SpriteMaterialSwapper] targetRenderer가 비어있음.");
            return;
        }

        targetRenderer.color = Color.white;
    }
}