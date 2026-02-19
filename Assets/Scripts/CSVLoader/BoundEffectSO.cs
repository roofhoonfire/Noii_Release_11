// BoundEffectSO.cs
using System;
using UnityEngine;

public enum BoundEffectType
{
    UseLeft,
    Add_Damage, 
    Add_Cast
}

[Serializable]
public class BoundEffect
{
    public string code;
    public BoundEffectType boundEffectType;
    public int amount;
    public string extraString;
    public string extraString2;
}

[CreateAssetMenu(menuName = "Noii/DB/BoundEffectSO")]
public class BoundEffectSO : ScriptableObject
{
    public BoundEffect[] effects;
}