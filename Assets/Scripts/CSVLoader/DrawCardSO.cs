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

public class DrawCard
{
    public string code;
    public string name;

    public Sprite sprite;
    public KnowledgeType kt;
    public int amount;
    public string cardText;

    /*public string GetDisplayText()
    {
        return CardTextFormatter.Resolve(cardText, this);
    }
    // 확장: 특정 필드(placeHolderName)만 delta를 더해서 치환
    */
    /*public string GetDisplayText(string placeHolderName, int delta)
    {
        Debug.Log($"{placeHolderName}, {delta}가보자구 ");
        return CardTextFormatter.Resolve(cardText, this, placeHolderName, delta);
    }*/

}

[CreateAssetMenu(fileName = "drawCardSO", menuName = "Scriptable Object/DrawCardSO")]

public class DrawCardSO : ScriptableObject
{
    public DrawCard[] drawcards;  
}
