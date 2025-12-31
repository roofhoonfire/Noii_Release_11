using UnityEngine;

[CreateAssetMenu(menuName = "Noii/Battle/PlayerAttackSequenceSO")]
public class PlayerAttackSequenceSO : ScriptableObject
{
    [Header("Key")]
    public string cardCode;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Camera
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("Camera (Shot Id)")]
    [Tooltip("Cam2·Î ¾µ ¼¦ ID")]
    public BattleCameraDirector.ShotId cam2Shot = BattleCameraDirector.ShotId.Cam2;

    [Tooltip("Cam3·Î ¾µ ¼¦ ID")]
    public BattleCameraDirector.ShotId cam3Shot = BattleCameraDirector.ShotId.Cam3;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Player Billboard Sequences
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("Player Sequence (Billboard Actor)")]
    public AnimationClip playerSequenceClip;

    [Tooltip("ÄÆÀÎ È¦µå°¡ ³¡³­ Á÷ÈÄ(= Cam3·Î ³Ñ¾î°¡±â Á÷Àü) Cam2¸¦ À¯ÁöÇÑ Ã¤ Àç»ýÇÒ ¾Ö´Ï¸ÞÀÌ¼Ç")]
    public AnimationClip playerSequenceClipAfterCutIn;

    [Range(0f, 1f)]
    public float playerFreezeNormalizedTime = 1.0f;

    public bool freezePlayerAtTime = true;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // CutIn Overlay
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("CutIn (Canvas Sprite)")]
    [Tooltip("Ä³¸¯ÅÍ ÄÆÀÎ ÀÌ¹ÌÁö(Äµ¹ö½º)")]
    public Sprite cutInSprite;

    [Tooltip("ÄÆ¶óÀÎ µîÀå ÈÄ È¦µå ½Ã°£(ÃÊ)")]
    public float cutInHoldSeconds = 0.25f;

    [Tooltip("ÄÆÀÎ È¦µå Á¾·á ÈÄ Cam3·Î ³Ñ¾î°¡±â Àü Ãß°¡ µô·¹ÀÌ(ÃÊ)")]
    public float afterCutInDelayBeforeCam3 = 0.05f;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Cam2 Dolly UI Overlay
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("Cam2 Dolly UI Overlay")]
    [Tooltip("¹è°æ/Ä³¸¯ÅÍ ÄÆÀÎÀÌ ¾ËÆÄ 0¡æ1·Î ¿Ã¶ó°¡´Â ½Ã°£(ÃÊ). DollyDuration¿¡¼­ ÀÌ °ªÀ» »« ½ÃÁ¡(delay)¿¡ µîÀå.")]
    public float characterCutInFadeSeconds = 0.3f;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Cam2 Dolly Movement
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("Cam2 Dolly (Attack Camera Movement)")]
    [Tooltip("CM_Attack¿¡ ºÙÀº CinemachineSplineDolly(¶Ç´Â À¯»ç ÄÄÆ÷³ÍÆ®)ÀÇ Normalized PositionÀ» 0~1·Î ÀÌµ¿½ÃÅ²´Ù.")]
    public bool useCam2Dolly = true;

    [Range(0f, 1f)]
    [Tooltip("Dolly ½ÃÀÛ À§Ä¡(º¸Åë 0)")]
    public float cam2DollyStart = 0f;

    [Range(0f, 1f)]
    [Tooltip("Dolly Á¾·á À§Ä¡(º¸Åë 1)")]
    public float cam2DollyEnd = 1f;

    [Tooltip("cam2DollyStart -> cam2DollyEnd ÀÌµ¿ ½Ã°£(ÃÊ)")]
    public float cam2DollyDuration = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("ÀÌ À§Ä¡¿¡ µµ´ÞÇÏ´Â ¼ø°£ ÄÆÀÎÀ» ¶ç¿î´Ù(¸ØÃã Æ÷ÀÎÆ®). end¿Í °°¾Æµµ µÊ.")]
    public float cam2CutInTriggerPos = 1f;

    [Tooltip("ÄÆÀÎ Æ®¸®°Å ÁöÁ¡¿¡ µµ´ÞÇßÀ» ¶§ Dolly¸¦ Á¤Áö½ÃÅ³Áö")]
    public bool stopDollyAtCutInPoint = true;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Cam3 Enemy Hit Effect (NEW)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("Cam3 Enemy Hit Effect (Sprite+Animator Object)")]
    [Tooltip("Cam3¿¡¼­ Àç»ýÇÒ 'ÇÇ°Ý ÀÌÆåÆ®' ¾Ö´Ï¸ÞÀÌ¼Ç Å¬¸³ (Animator¿¡ clip.name°ú µ¿ÀÏÇÑ StateNameÀÌ ÀÖ¾î¾ß ÇÔ)")]
    public AnimationClip enemyHitEffectClip;

    [Tooltip("Cam3 ÇÇ°Ý ÀÌÆåÆ®¸¦ »ç¿ëÇÒÁö")]
    public bool useEnemyHitEffect = true;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // HitStop (based on Enemy Hit Effect)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("HitStop (based on Enemy Hit Effect)")]
    [Tooltip("HitStopÀ» »ç¿ëÇÒÁö")]
    public bool useHitStop = true;

    [Tooltip("È÷Æ®½ºÅ¾ Áö¼Ó ½Ã°£(ÃÊ)")]
    public float hitStopSeconds = 0.08f;

    [Tooltip("ÇÁ·¹ÀÓ ÀÎµ¦½º ±â¹ÝÀ¸·Î È÷Æ®½ºÅ¾ Å¸ÀÌ¹ÖÀ» °è»êÇÒÁö")]
    public bool hitStopUseFrameIndex = true;

    [Tooltip("ÇÇ°Ý ÀÌÆåÆ® ¾Ö´Ï¸ÞÀÌ¼ÇÀÇ N¹øÂ° ÇÁ·¹ÀÓ¿¡¼­ È÷Æ®½ºÅ¾ ¹ßµ¿ (0ºÎÅÍ)")]
    public int hitStopEffectFrameIndex = 2;

    [Tooltip("ÇÇ°Ý ÀÌÆåÆ® ¾Ö´Ï¸ÞÀÌ¼Ç FPS(ÇÁ·¹ÀÓ ±â¹Ý °è»ê¿ë)")]
    public float hitEffectFps = 12f;

    [Tooltip("ÇÁ·¹ÀÓ ´ë½Å 'ÃÊ'·Î È÷Æ®½ºÅ¾ ¹ßµ¿ Å¸ÀÌ¹ÖÀ» ÁÖ°í ½ÍÀ¸¸é ¿©±â(0ÀÌ¸é ¹«½Ã)")]
    public float hitStopEffectTimeSeconds = 0f;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Camera Shake on HitStop
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("Camera Shake (on HitStop)")]
    [Tooltip("È÷Æ®½ºÅ¾ ¼ø°£ Ä«¸Þ¶ó ½¦ÀÌÅ©¸¦ »ç¿ëÇÒÁö")]
    public bool useCameraShake = true;

    [Tooltip("½¦ÀÌÅ© °­µµ(Impulse ±âÁØ). 1À» ±âº»À¸·Î Æ©´×.")]
    public float cameraShakeStrength = 1f;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Legacy (Optional) - ±âÁ¸ EnemyAnimator Æ®¸®°Å ¹æ½Ä ³²°ÜµÎ°í ½ÍÀ¸¸é
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("Legacy Enemy Animator Trigger (Optional)")]
    [Tooltip("±âÁ¸ EnemyAnimator¿¡ ³¯¸± Æ®¸®°Å(¾È ¾²¸é ºñ¿öµÖµµ µÊ)")]
    public string enemyHitTrigger = "Hit";
}
