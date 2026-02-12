using System;
using System.Collections.Generic;
using UnityEngine;

public class F1FieldCameraController : MonoBehaviour
{
    public static F1FieldCameraController Instance { get; private set; }

    [Serializable]
    public class TriggerCamPair
    {
        public BoxCollider trigger;      // IsTrigger 체크
        public Transform cameraTarget;   // 카메라가 이동할 위치(포지션)
    }

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform; // 메인 카메라 or 시네머신 리그(움직일 트랜스폼)

    [Header("Pairs (Trigger -> Camera Target)")]
    [SerializeField] private List<TriggerCamPair> pairs = new();

    [Header("Move Tuning")]
    [Tooltip("이동시간 = 거리 * 이 값 (초/유닛)")]
    [SerializeField] private float secondsPerUnit = 0.12f;

    [Tooltip("최소 이동 시간")]
    [SerializeField] private float minDuration = 0.15f;

    [Tooltip("최대 이동 시간")]
    [SerializeField] private float maxDuration = 1.25f;

    [Tooltip("스무스 이동 속도(값이 클수록 빨리 수렴)")]
    [SerializeField] private float smooth = 12f;

    private readonly Dictionary<Collider, Transform> _map = new();
    private Transform _currentTarget;
    private bool _initialized;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildMap();
    }

    private void OnEnable()
    {
        if (!_initialized) BuildMap();
    }

    private void BuildMap()
    {
        _map.Clear();

        for (int i = 0; i < pairs.Count; i++)
        {
            var p = pairs[i];
            if (p == null || p.trigger == null || p.cameraTarget == null) continue;

            // 트리거 강제
            p.trigger.isTrigger = true;

            _map[p.trigger] = p.cameraTarget;

            // 트리거에 리스너가 없으면 자동으로 붙여줌 (실수 방지)
            var listener = p.trigger.GetComponent<F1FieldCameraTrigger>();
            if (listener == null)
                listener = p.trigger.gameObject.AddComponent<F1FieldCameraTrigger>();
        }

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        _initialized = true;
    }

    private void Update()
    {
        if (cameraTransform == null || _currentTarget == null) return;

        // SmoothDamp도 가능하지만, 시간기반 요구(거리*조절값) 때문에 exp-smooth로 처리
        // 프레임레이트 독립 수렴
        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, _currentTarget.position, t);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, _currentTarget.rotation, t);
    }

    /// <summary>
    /// 트리거에 들어갔을 때 호출: 해당 트리거에 매핑된 타겟으로 카메라 이동 시작
    /// "겹쳐서 연속 진입"되면 새로 들어간 놈이 우선(기존은 즉시 교체)
    /// </summary>
    public void NotifyEnter(Collider triggerCollider)
    {
        if (triggerCollider == null) return;
        if (!_initialized) BuildMap();

        if (!_map.TryGetValue(triggerCollider, out var target) || target == null)
            return;

        // 새로 진입한 타겟으로 즉시 교체 (요구사항: 박고있는 중에도 새로 박은 놈으로)
        SetTarget(target);
    }

    private void SetTarget(Transform target)
    {
        _currentTarget = target;

        // 거리 기반 “느낌”을 주고 싶으면 smooth를 거리로 역산해서 조절할 수도 있는데,
        // 오늘은 안정성/속도 우선이라 secondsPerUnit을 기반으로 smooth를 순간 조정해준다.
        if (cameraTransform == null || _currentTarget == null) return;

        float dist = Vector3.Distance(cameraTransform.position, _currentTarget.position);
        float duration = Mathf.Clamp(dist * secondsPerUnit, minDuration, maxDuration);

        // duration이 작을수록 빨리 수렴해야 하니까 smooth를 크게
        // (대충: 95% 수렴을 duration 안에)
        // exp(-smooth*duration) ≈ 0.05 → smooth ≈ -ln(0.05)/duration
        smooth = Mathf.Clamp(-Mathf.Log(0.05f) / Mathf.Max(0.01f, duration), 3f, 40f);
    }

#if UNITY_EDITOR
    // 인스펙터에서 값 바꾸면 자동 리빌드
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        BuildMap();
    }
#endif
}
