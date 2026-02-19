using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3.x 네임스페이스

public class F1FieldCameraController : MonoBehaviour
{
    public static F1FieldCameraController Instance { get; private set; }

    [Serializable]
    public class TriggerCamPair
    {
        public BoxCollider trigger;                 // IsTrigger 체크
        public CinemachineCamera cinemachineCam;    // ★ 예전 vcam 자리에 해당
    }

    [Header("Pairs (Trigger -> Cinemachine Camera)")]
    [SerializeField] private List<TriggerCamPair> pairs = new();

    [Header("Priority")]
    [Tooltip("기본 우선순위(평소엔 이 값으로 유지)")]
    [SerializeField] private int basePriority = 0;

    [Tooltip("트리거 진입 시 줄 우선순위(이 값이 가장 높아져 LIVE가 됨)")]
    [SerializeField] private int activePriority = 10;

    private readonly Dictionary<Collider, CinemachineCamera> _map = new();
    private CinemachineCamera _currentLive;
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
            if (p == null || p.trigger == null || p.cinemachineCam == null) continue;

            p.trigger.isTrigger = true;
            _map[p.trigger] = p.cinemachineCam;

            // 실수 방지: 트리거 리스너 자동 부착
            var listener = p.trigger.GetComponent<F1FieldCameraTrigger>();
            if (listener == null)
                listener = p.trigger.gameObject.AddComponent<F1FieldCameraTrigger>();

            // 초기에는 전부 basePriority로 깔아두기
            p.cinemachineCam.Priority = basePriority;
        }

        _initialized = true;
    }

    /// <summary>
    /// 트리거에 들어갔을 때 호출: 해당 트리거에 매핑된 CinemachineCam을 LIVE로 만든다.
    /// </summary>
    public void NotifyEnter(Collider triggerCollider)
    {
        if (triggerCollider == null) return;
        if (!_initialized) BuildMap();

        if (!_map.TryGetValue(triggerCollider, out var cam) || cam == null)
            return;

        SetLive(cam);
    }

    private void SetLive(CinemachineCamera cam)
    {
        if (_currentLive == cam) return;

        // 이전 LIVE를 basePriority로 내림
        if (_currentLive != null)
            _currentLive.Priority = basePriority;

        // 새 LIVE를 activePriority로 올림
        cam.Priority = activePriority;
        _currentLive = cam;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        BuildMap();
    }
#endif
}