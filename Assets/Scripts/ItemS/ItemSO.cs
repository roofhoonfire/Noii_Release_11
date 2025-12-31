using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


[System.Serializable]

public class Item
{
    public string code;
    public int itemType; // 0:field Use. 1: battle use
    public string name;

    public Sprite sprite;
    
    
    public string itemText;

   

}



[CreateAssetMenu(fileName = "ItemSo_", menuName = "Scriptable Object/ItemSO")]
public class ItemSO : ScriptableObject
{
    public Item[] items;
}