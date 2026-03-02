using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombAvoidEvent : MonoBehaviour
{
    [Header("Pattern")]
    [SerializeField] private BombPatternSO pattern;

    [Header("Zones Source")]
    [Tooltip("비워두면 씬에서 BombZone을 자동 탐색한다.")]
    [SerializeField] private List<BombZone> zones = new();

    [Header("Hit Output")]
    [SerializeField] private bool debugLogOnHit = true;
    [SerializeField] private string hitLog = "우웩";

    public bool IsRunning => _running;

    private bool _running;
    private Coroutine _routine;

    // zoneId -> BombZone (런타임 매핑)
    private readonly Dictionary<int, BombZone> _zoneMap = new();

    public void StartEvent()
    {
        if (_running) return;

        if (pattern == null || pattern.steps == null || pattern.steps.Count == 0)
        {
            Debug.LogWarning("[BombAvoidEvent] Pattern이 비어있음.");
            return;
        }

        if (zones == null || zones.Count == 0)
        {
            zones = new List<BombZone>(FindObjectsOfType<BombZone>(true));
        }

        BuildZoneMap();


        _running = true;
        _routine = StartCoroutine(RunRoutine());
    }

    public void StopEvent()
    {
        if (!_running) return;

        _running = false;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

    }

    private void BuildZoneMap()
    {
        _zoneMap.Clear();

        for (int i = 0; i < zones.Count; i++)
        {
            var z = zones[i];
            if (z == null) continue;

            if (_zoneMap.ContainsKey(z.zoneId))
            {
                Debug.LogWarning($"[BombAvoidEvent] 중복 zoneId 발견: {z.zoneId} (오브젝트: {z.name})");
                continue;
            }

            _zoneMap.Add(z.zoneId, z);
        }
    }

    private IEnumerator RunRoutine()
    {
        while (_running)
        {
            for (int i = 0; i < pattern.steps.Count; i++)
            {
                if (!_running) yield break;

                var step = pattern.steps[i];
                if (step == null) continue;

                // 0) 스텝 시작 순간: 애니 트리거
                for (int k = 0; k < step.zoneIds.Count; k++)
                {
                    if (_zoneMap.TryGetValue(step.zoneIds[k], out var zone) && zone != null)
                        zone.PlayBombTimelineLike();
                }

                // 1) 애니가 터지는 프레임까지 기다림
                if (step.delayBeforeExplode > 0f)
                    yield return new WaitForSeconds(step.delayBeforeExplode);

                // 2) 폭발 판정(순간)
                bool playerHit = false;

                for (int k = 0; k < step.zoneIds.Count; k++)
                {
                    if (!_zoneMap.TryGetValue(step.zoneIds[k], out var zone) || zone == null)
                        continue;

                    if (!playerHit && zone.CheckPlayerHitNow())
                        playerHit = true;
                }

                if (playerHit)
                    OnPlayerHit();

                // 3) (옵션) 비주얼 정리 - 너는 애니가 Idle로 돌아가니 굳이 안 해도 됨
                // 필요하면 여기서 색/이펙트만 정리
            }

            if (!pattern.loop) break;

            if (pattern.loopInterval > 0f)
                yield return new WaitForSeconds(pattern.loopInterval);
        }

        _running = false;
        _routine = null;
    }

    private void OnPlayerHit()
    {
        if (debugLogOnHit)
            Debug.Log(hitLog);
    }
}