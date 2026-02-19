using UnityEngine;

/// <summary>
/// Singleton: 특정 오브젝트의 SpriteRenderer.material 을 지정한 머티리얼로 교체.
/// </summary>
public class SpriteMaterialSwapper : MonoBehaviour
{
    public static SpriteMaterialSwapper Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private GameObject targetObject;   // SpriteRenderer 달린 오브젝트 드래그&드랍

    [Header("Material")]
    [SerializeField] private Material materialToApply;  // 적용할 머티리얼 드래그&드랍

    private SpriteRenderer _sr;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CacheSpriteRenderer();
    }

    private void CacheSpriteRenderer()
    {
        _sr = null;

        if (targetObject == null) return;

        _sr = targetObject.GetComponent<SpriteRenderer>();
        if (_sr == null)
            Debug.LogError("[SpriteMaterialSwapper] targetObject에 SpriteRenderer가 없음.");
    }

    public void SetTarget(GameObject newTarget)
    {
        targetObject = newTarget;
        CacheSpriteRenderer();
    }

    public void SetMaterial(Material newMaterial)
    {
        materialToApply = newMaterial;
    }

    public void Apply()
    {
        if (_sr == null) CacheSpriteRenderer();
        if (_sr == null) return;

        if (materialToApply == null)
        {
            Debug.LogError("[SpriteMaterialSwapper] materialToApply가 비어있음.");
            return;
        }

        // SpriteRenderer는 sharedMaterial/material 둘 다 가능.
        // 런타임 개별 인스턴스가 필요 없으면 sharedMaterial 권장.
        _sr.sharedMaterial = materialToApply;
    }
}
