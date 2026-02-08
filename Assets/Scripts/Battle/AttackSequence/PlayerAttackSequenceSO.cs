// PlayerAttackSequenceSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Noii/Battle/PlayerAttackSequenceSO")]
public class PlayerAttackSequenceSO : ScriptableObject
{
    [Header("Key")]
    public string cardCode;

    [Header("Text")]
    public string actionNotice;

    // ─────────────────────────────────────────────
    // Camera
    // ─────────────────────────────────────────────
    [Header("Camera (Shot Id)")]
    public BattleCameraDirector.ShotId cam2Shot = BattleCameraDirector.ShotId.Cam2;
    public BattleCameraDirector.ShotId cam3Shot = BattleCameraDirector.ShotId.Cam3;

    // ─────────────────────────────────────────────
    // Player Billboard Sequences
    // ─────────────────────────────────────────────
    [Header("Player Sequence (Billboard Actor)")]
    public AnimationClip playerSequenceClip;
    public AnimationClip playerSequenceClipAfterCutIn;

    [Range(0f, 1f)] public float playerFreezeNormalizedTime = 1.0f;
    public bool freezePlayerAtTime = true;

    // ─────────────────────────────────────────────
    // CutIn Overlay
    // ─────────────────────────────────────────────
    [Header("CutIn (Canvas Sprite)")]
    public Sprite cutInSprite;
    public float cutInHoldSeconds = 0.25f;
    public float afterCutInDelayBeforeCam3 = 0.05f;

    // ─────────────────────────────────────────────
    // Cam2 Dolly UI Overlay
    // ─────────────────────────────────────────────
    [Header("Cam2 Dolly UI Overlay")]
    public float characterCutInFadeSeconds = 0.3f;

    // ─────────────────────────────────────────────
    // Cam2 Dolly Movement
    // ─────────────────────────────────────────────
    [Header("Cam2 Dolly (Attack Camera Movement)")]
    public bool useCam2Dolly = true;
    [Range(0f, 1f)] public float cam2DollyStart = 0f;
    [Range(0f, 1f)] public float cam2DollyEnd = 1f;
    public float cam2DollyDuration = 0.35f;
    [Range(0f, 1f)] public float cam2CutInTriggerPos = 1f;
    public bool stopDollyAtCutInPoint = true;

    // ─────────────────────────────────────────────
    // Cam3 Enemy Hit Effect
    // ─────────────────────────────────────────────
    [Header("Cam3 Enemy Hit Effect (Sprite+Animator Object)")]
    public AnimationClip enemyHitEffectClip;
    public bool useEnemyHitEffect = true;

    // ─────────────────────────────────────────────
    // HitStop Info (NEW)
    // ─────────────────────────────────────────────
    [Header("HitStop Info (Animation Event Index -> This List Index)")]
    public List<HitStopInfo> hitStopInfos = new();

    [Serializable]
    public class HitStopInfo
    {
        [Tooltip("몇 번째 히트스톱인지(0부터). 애니 이벤트 int 파라미터와 대응.")]
        public int index;

        [Tooltip("히트스톱 지속 시간(초)")]
        public float hitStopSeconds = 0.08f;

        [Tooltip("카메라 쉐이크 강도(Impulse 기준)")]
        public float cameraShakeStrength = 1f;

        [Tooltip("추후 확장용: enemy hit 트리거/키")]
        public int enemyHitTrigger = 0;
    }

    /// <summary>
    /// index로 HitStopInfo 찾기 (없으면 null)
    /// </summary>
    public HitStopInfo GetHitStopInfo(int index)
    {
        if (hitStopInfos == null) return null;
        for (int i = 0; i < hitStopInfos.Count; i++)
        {
            if (hitStopInfos[i] != null && hitStopInfos[i].index == index)
                return hitStopInfos[i];
        }
        return null;
    }
}
