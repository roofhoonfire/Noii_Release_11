// JujuSO.cs
using System;
using UnityEngine;

[Serializable]
public class Juju
{
    public string code;
    public string name;
    public Sprite sprite;
    public string jujuPoint;
    public string text;
}

[CreateAssetMenu(menuName = "Noii/DB/JujuSO")]
public class JujuSO : ScriptableObject
{
    public Juju[] jujus;
}