using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Noii/FieldEvents/Bomb Pattern", fileName = "BombPatternSO")]
public class BombPatternSO : ScriptableObject
{
    [Header("Loop")]
    public bool loop = true;
    public float loopInterval = 0.25f;

    [Header("Steps (Fixed Sequence)")]
    public List<BombStep> steps = new();

    [Serializable]
    public class BombStep
    {
        public float delayBeforeExplode = 0.25f;

        [Tooltip("이번 스텝에서 동시에 터질 폭탄 존 ID들")]
        public List<int> zoneIds = new();
    }
}