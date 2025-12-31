using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// BattleCameraDirector
/// - Cinemachine 3.x(CinemachineCamera + CinemachineBrain) ±â¹Ý.
/// - ±âº» Back/Front ¼¦ ÀüÈ¯ + ºí·»µå ÁøÇà·ü ÀÌº¥Æ®.
/// - Ãß°¡ ¿¬Ãâ ¼¦(Cam2/Cam3 µî)À» Push/Pop(½ºÅÃ) ¹æ½ÄÀ¸·Î ¿Ã·È´Ù ³»¸± ¼ö ÀÖÀ½.
/// 
/// ¿ÜºÎ¿¡¼­ ¾²´Â API (ÃßÃµ):
/// 1) SetShot(ShotId.Back / ShotId.Front / ShotId.Cam2 / ShotId.Cam3 ...)
/// 2) PushShot(ShotId.Cam2) -> PushShot(ShotId.Cam3) -> PopShot() -> PopShot()
/// 
/// ±âÁ¸ È£È¯:
/// - EnterSelectShot() == SetShot(Front)
/// - ExitSelectShot()  == SetShot(Back)
/// </summary>
public class BattleCameraDirector : MonoBehaviour
{
    public static BattleCameraDirector Instance { get; private set; }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Shots
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public enum ShotId
    {
        Back,
        Front,
        Cam2,
        Cam3
        // ÇÊ¿äÇÏ¸é °è¼Ó Ãß°¡ °¡´É
    }

    [System.Serializable]
    public class Shot
    {
        public ShotId id;
        public CinemachineCamera camera;
    }

    [Header("Cinemachine Cameras (CM3)")]
    [Tooltip("¾À¿¡ ¹ÚÇôÀÖ´Â CinemachineCameraµéÀ» µî·ÏÇÑ´Ù.")]
    [SerializeField] private List<Shot> shots = new();

    [Header("Main Camera Brain")]
    [Tooltip("Main Camera¿¡ ºÙÀº CinemachineBrainÀ» ³Ö¾î¶ó.")]
    [SerializeField] private CinemachineBrain brain;

    [Header("Priority")]
    [Tooltip("ºñÈ°¼º ¼¦ priority")]
    [SerializeField] private int basePriority = 0;

    [Tooltip("È°¼º ¼¦ priority")]
    [SerializeField] private int activePriority = 100;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Blend Progress Events
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    [Header("Blend Progress Triggers (0~1)")]
    [Range(0f, 1f)]
    [Tooltip("Back -> Front ºí·»µå ÁøÇà·üÀÌ ÀÌ °ª ÀÌ»óÀÌ µÇ´Â ¼ø°£ ÀÌº¥Æ® ¹ß»ç")]
    [SerializeField] private float backToFrontTriggerProgress = 0.6f;

    [Range(0f, 1f)]
    [Tooltip("Front -> Back ºí·»µå ÁøÇà·üÀÌ ÀÌ °ª ÀÌ»óÀÌ µÇ´Â ¼ø°£ ÀÌº¥Æ® ¹ß»ç")]
    [SerializeField] private float frontToBackTriggerProgress = 0.4f;

    [Header("Events (Animator Trigger µîÀ» ¿©±â¼­ ¿¬°á)")]
    public UnityEvent onBackToFrontProgressReached;
    public UnityEvent onFrontToBackProgressReached;

    [Header("Camera Shake (Optional)")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Internal state
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    private readonly Dictionary<ShotId, CinemachineCamera> _map = new();
    private readonly Stack<ShotId> _shotStack = new();          // Push/Pop¿ë
    private ShotId _currentShot = ShotId.Back;

    private Coroutine _watchBlendRoutine;

    private enum BlendDir { BackToFront, FrontToBack, Other }

    private BlendDir GetBlendDir(ShotId prev, ShotId next)
    {
        if (prev == ShotId.Back && next == ShotId.Front) return BlendDir.BackToFront;
        if (prev == ShotId.Front && next == ShotId.Back) return BlendDir.FrontToBack;
        return BlendDir.Other;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Brain ÀÚµ¿ Å½»ö
        if (brain == null)
        {
            var cam = Camera.main;
            if (cam != null) brain = cam.GetComponent<CinemachineBrain>();
        }

        BuildMap();

        // ½ÃÀÛÀº BackÀ¸·Î Á¤·Ä
        SetShotInstant(ShotId.Back);
    }
    public void Shake(float strength = 1f)
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulse(strength);
    }
    private void BuildMap()
    {
        _map.Clear();
        foreach (var s in shots)
        {
            if (s == null || s.camera == null) continue;
            _map[s.id] = s.camera;
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Public API (¿ÜºÎ¿¡¼­ ºÎ¸£´Â ±ò²ûÇÑ ÁøÀÔÁ¡)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    /// <summary>
    /// ÇöÀç ¼¦À» ÁöÁ¤µÈ ¼¦À¸·Î "±³Ã¼"ÇÑ´Ù.
    /// (½ºÅÃÀ» °Çµå¸®Áö ¾Ê´Â ¼ø¼ö ÀüÈ¯)
    /// </summary>
    public void SetShot(ShotId id)
    {
        if (!_map.TryGetValue(id, out var target) || target == null)
            return;

        //  "ÀÌÀü ¼¦"À» ÀúÀåÇØµÎ°í
        var prev = _currentShot;

        // ¸ðµÎ base·Î ³»¸®°í, target¸¸ active·Î
        ApplyExclusive(target);

        //  current °»½Å
        _currentShot = id;

        //  prev->id ÀüÈ¯¿¡ ´ëÇØ¼­¸¸ Back<->Front ÁøÇà·ü ÀÌº¥Æ® °¨½Ã
        StartWatchingBlend(GetBlendDir(prev, id));
    }

    /// <summary>
    /// ¿¬Ãâ¿ë: ÇöÀç ¼¦ À§¿¡ »õ·Î¿î ¼¦À» "½×´Â´Ù".
    /// ¿¹) PushShot(Cam2) -> PushShot(Cam3) -> PopShot() -> PopShot()
    /// </summary>
    public void PushShot(ShotId id)
    {
        // ÇöÀç ¼¦À» ½ºÅÃ¿¡ ÀúÀåÇÏ°í »õ ¼¦À¸·Î ÀüÈ¯
        _shotStack.Push(_currentShot);
        SetShot(id);
    }

    /// <summary>
    /// ¿¬Ãâ¿ë: Push·Î ¿Ã¸° ¼¦À» µÇµ¹¸°´Ù.
    /// ½ºÅÃÀÌ ºñ¾îÀÖÀ¸¸é BackÀ¸·Î º¹±Í.
    /// </summary>
    public void PopShot()
    {
        if (_shotStack.Count <= 0)
        {
            SetShot(ShotId.Back);
            return;
        }

        var prev = _shotStack.Pop();
        SetShot(prev);
    }

    /// <summary>
    /// ½ºÅÃÀ» ¸ðµÎ ºñ¿ì°í ±âº» BackÀ¸·Î º¹±Í.
    /// </summary>
    public void ResetToBack()
    {
        _shotStack.Clear();
        SetShot(ShotId.Back);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ±âÁ¸ È£È¯ API (³×°¡ ÀÌ¹Ì ¿ÜºÎ¿¡¼­ È£Ãâ ÁßÀÎ °Í)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    /// <summary>
    /// Çàµ¿ ¼±ÅÃ ÁøÀÔ: Front ¼¦À¸·Î ÀüÈ¯ + (Back->Front ÁøÇà·ü ÀÌº¥Æ®)
    /// </summary>
    public void EnterSelectShot() => SetShot(ShotId.Front);

    /// <summary>
    /// Çàµ¿ ¼±ÅÃ Á¾·á: Back ¼¦À¸·Î º¹±Í + (Front->Back ÁøÇà·ü ÀÌº¥Æ®)
    /// </summary>
    public void ExitSelectShot() => SetShot(ShotId.Back);

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Core: Priority Àû¿ë
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    /// <summary>
    /// ÁöÁ¤µÈ Ä«¸Þ¶ó¸¸ activePriority·Î ¿Ã¸®°í ³ª¸ÓÁö´Â basePriority·Î ³»¸°´Ù.
    /// </summary>
    private void ApplyExclusive(CinemachineCamera activeCam)
    {
        foreach (var kv in _map)
        {
            if (kv.Value == null) continue;
            kv.Value.Priority = basePriority;
        }

        activeCam.Priority = activePriority;
    }

    /// <summary>
    /// ½ÃÀÛ ½Ã Áï½Ã ÀüÈ¯¿ë (ºí·»µå °¨½Ã ¾È ÇÔ)
    /// </summary>
    private void SetShotInstant(ShotId id)
    {
        if (!_map.TryGetValue(id, out var cam) || cam == null)
            return;

        ApplyExclusive(cam);
        _currentShot = id;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Blend Progress Watcher
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    private BlendDir GetBlendDirFor(ShotId newShot)
    {
        // ¡°ÀÌÀü -> »õ¼¦¡± ±â¹ÝÀ¸·Î ¹æÇâÀ» ÆÇ´ÜÇÏ°í ½Í´Ù¸é,
        // ÀÌ ÇÔ¼ö´Â SetShot Á÷Àü¿¡ prev¸¦ ±â·ÏÇØ¼­ ºñ±³ÇÏ´Â ±¸Á¶·Î ¹Ù²Ù¸é µÊ.
        // Áö±ÝÀº "Back/Front ÀüÈ¯¸¸ ÀÌº¥Æ®°¡ ÇÊ¿ä"¶ó´Â ³× ±âÁ¸ ÄÁ¼ÁÀ» À¯ÁöÇÏ±â À§ÇØ,
        // currentShot ÀÚÃ¼·Î ÀÌº¥Æ® Å¸ÀÔÀ» °áÁ¤ÇÏ´Â ´Ü¼ø ±ÔÄ¢À» µÐ´Ù.

        // ±ÔÄ¢:
        // - Front·Î °¡´Â ÀüÈ¯(=Back->Front) °¨½Ã
        // - BackÀ¸·Î °¡´Â ÀüÈ¯(=Front->Back) °¨½Ã
        // - ±× ¿Ü(Cam2/Cam3)´Â Progress Event °¨½Ã ¾È ÇÔ(ÇÊ¿äÇÏ¸é È®Àå °¡´É)
        if (newShot == ShotId.Front) return BlendDir.BackToFront;
        if (newShot == ShotId.Back) return BlendDir.FrontToBack;
        return BlendDir.Other;
    }

    private void StartWatchingBlend(BlendDir dir)
    {
        // Other´Â °¨½Ã ÇÊ¿ä ¾øÀ½
        if (dir == BlendDir.Other)
        {
            StopWatchingBlend();
            return;
        }

        if (_watchBlendRoutine != null)
            StopCoroutine(_watchBlendRoutine);

        _watchBlendRoutine = StartCoroutine(WatchBlendProgressRoutine(dir));
    }

    private void StopWatchingBlend()
    {
        if (_watchBlendRoutine != null)
            StopCoroutine(_watchBlendRoutine);
        _watchBlendRoutine = null;
    }

    private IEnumerator WatchBlendProgressRoutine(BlendDir dir)
    {
        // Brain ¾øÀ¸¸é Áï½Ã ÀÌº¥Æ® ¹ß»çÇÏ°í Á¾·á
        if (brain == null)
        {
            FireProgressEvent(dir);
            _watchBlendRoutine = null;
            yield break;
        }

        float trigger = (dir == BlendDir.BackToFront)
            ? backToFrontTriggerProgress
            : frontToBackTriggerProgress;

        bool fired = false;

        // ºí·»µå ½ÃÀÛ ´ë±â (ÇÁ·¹ÀÓ Â÷ÀÌ º¸Á¤)
        for (int i = 0; i < 3; i++)
        {
            if (brain.IsBlending) break;
            yield return null;
        }

        // ºí·»µå ÁøÇà·ü °¨½Ã
        while (brain.IsBlending)
        {
            var blend = brain.ActiveBlend;
            float duration = blend.Duration;
            float time = blend.TimeInBlend;

            float u = (duration <= 0.0001f) ? 1f : Mathf.Clamp01(time / duration);

            if (!fired && u >= trigger)
            {
                fired = true;
                FireProgressEvent(dir);
            }

            yield return null;
        }

        // ºí·»µå Á¾·á º¸Á¤ ¹ß»ç
        if (!fired)
            FireProgressEvent(dir);

        _watchBlendRoutine = null;
    }

    private void FireProgressEvent(BlendDir dir)
    {
        if (dir == BlendDir.BackToFront)
            onBackToFrontProgressReached?.Invoke();
        else if (dir == BlendDir.FrontToBack)
            onFrontToBackProgressReached?.Invoke();
    }
    public bool IsBlending()
    {
        return brain != null && brain.IsBlending;
    }

    /// <summary>
    /// ÇöÀç ºí·»µå°¡ ³¡³¯ ¶§±îÁö ±â´Ù¸°´Ù.
    /// (ºí·»µå°¡ ¾øÀ¸¸é Áï½Ã Åë°ú)
    /// </summary>
    public IEnumerator WaitForBlendComplete()
    {
        if (brain == null) yield break;

        // È¤½Ã ÀüÈ¯ ÇÁ·¹ÀÓ Å¸ÀÌ¹Ö ¶§¹®¿¡, ºí·»µå°¡ "¸· ½ÃÀÛÇÏ±â Àü" ÇÁ·¹ÀÓÀÏ ¼ö ÀÖ¾î 1~2ÇÁ·¹ÀÓ ¿©À¯
        for (int i = 0; i < 2; i++)
        {
            if (brain.IsBlending) break;
            yield return null;
        }

        while (brain.IsBlending)
            yield return null;
    }

#if UNITY_EDITOR
    private void Update()
    {
        // ¿¡µðÅÍ Å×½ºÆ®¿ë (¿øÇÏ¸é ¼öÁ¤/»èÁ¦)
        if (Input.GetKeyDown(KeyCode.V)) EnterSelectShot();
        if (Input.GetKeyDown(KeyCode.B)) ExitSelectShot();

        if (Input.GetKeyDown(KeyCode.Alpha2)) PushShot(ShotId.Cam2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) PushShot(ShotId.Cam3);
        if (Input.GetKeyDown(KeyCode.Alpha1)) PopShot();
    }
#endif
}
