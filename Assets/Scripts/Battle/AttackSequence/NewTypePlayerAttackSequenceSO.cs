using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Noii/Battle/NewTypePlayerAttackSequenceSO", fileName = "NewTypePlayerAttackSequenceSO")]
public class NewTypePlayerAttackSequenceSO : ScriptableObject
{
    [Header("Key")]
    public string cardCode;

    [Header("Text")]
    public string actionNotice;

    [Header("Camera (Shot Id)")]
    public BattleCameraDirector.ShotId wideShot = BattleCameraDirector.ShotId.Cam2;
    public BattleCameraDirector.ShotId enemyShot = BattleCameraDirector.ShotId.Cam3;

    [Header("Root Player Anim Clips (Animator state name == clip.name 가정)")]
    public AnimationClip playerPrepareClip;
    public AnimationClip playerAttackClip;

    [Header("Small Cutscene (UI Object Animator state name == clip.name 가정)")]
    public AnimationClip smallCutsceneClip;

    [Header("Enemy Hit Effect (Sprite+Animator Object)")]
    public AnimationClip enemyHitEffectClip;
    public bool useEnemyHitEffect = true;

    [Header("HitStop Info (Animation Event Index -> This List Index)")]
    public List<HitStopInfo> hitStopInfos = new();

    [Serializable]
    public class HitStopInfo
    {
        public int index;
        public float hitStopSeconds = 0.08f;
        public float cameraShakeStrength = 1f;
        public int enemyHitTrigger = 0; // 확장용
    }

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
