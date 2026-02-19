// BoundSO.cs
using System;
using UnityEngine;

public enum BoundType
{
    Force,
    Condition
}

[Serializable]
public class Bound
{
    public string code;
    public string name;
    public Sprite sprite;
    public int boundPoint;
    public BoundType boundType;
    public string text;
}

[CreateAssetMenu(menuName = "Noii/DB/BoundSO")]
public class BoundSO : ScriptableObject
{
    public Bound[] bounds;
}