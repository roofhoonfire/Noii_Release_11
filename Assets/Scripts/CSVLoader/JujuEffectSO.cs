// JujuEffectSO.cs
using System;
using UnityEngine;

public enum JujuEffectType
{
    AddDamage
}

[Serializable]
public class JujuEffect
{
    public string code;
    public JujuEffectType jujuEffectType;
    public int amount;
    public string extraString;
    public string extraString2;
}

[CreateAssetMenu(menuName = "Noii/DB/JujuEffectSO")]
public class JujuEffectSO : ScriptableObject
{
    public JujuEffect[] effects;
}