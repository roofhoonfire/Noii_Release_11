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
    public string extraString;   // 
    public int flag;
}

public enum Phase
{
    Activate,
    AttackSequence,
    ParryingSucceed,
    Drop,
    SuddenAttack,
    Dot,
    End,
}

public enum EffectType
{
    damage,
    add_Damage,
    SuddenAttack_On,
    Chant,
    add_Chakra,
    add_n1inQ,
    ifPrevWasTemp,
}

