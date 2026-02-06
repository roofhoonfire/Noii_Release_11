// CardEffectSO.cs
using System;
using UnityEngine;

[Serializable]
public class CardEffect
{
    public string code;

    public Phase phase;
    public EffectType effectType;

    public int amount;
    public int flag;
}

public enum Phase
{
    Activate,
    AttackSequence,
    ParryingSucceed,
    Drop,
    SuddenAttack,
}

public enum EffectType
{
    damage,
    add_Damage,
    SuddenAttack_On,
}

[CreateAssetMenu(menuName = "Noii/Card Effect SO", fileName = "CardEffectSO")]
public class CardEffectSO : ScriptableObject
{
    public CardEffect[] cardeffects;
}
