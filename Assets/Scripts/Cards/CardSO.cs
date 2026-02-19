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

public class Card
{
    public string code;
    public int cardType; // 0: Action, 1: Support, 2: ImmediateSupport
    public string name;

    public Sprite sprite;
    public List<AnimationClip> animations;  // 변경: 단일 → 리스트

    public int actionClock;
    public int rumblePoint;
    public int defense;
    public int disappear;
    public int mana;
    public int seqType;
    public int zoneIndex;
    public int energy;
    public int damage;

    public string cardText;

    public string GetDisplayText()
    {
        return CardTextFormatter.Resolve(cardText, this);
    }
    // 확장: 특정 필드(placeHolderName)만 delta를 더해서 치환

    public string GetDisplayText(string placeHolderName, int delta)
    {
        Debug.Log($"{placeHolderName}, {delta}가보자구 ");
        return CardTextFormatter.Resolve(cardText, this, placeHolderName, delta);
    }

}
/*public enum HookType { Activate, Priority, IQA, Counter, Guard, BeforeRumble, RumbleWin, RumbleLose, Combo, Support, Dot }
public enum EffectType
{
    Add_ActionClock_Op, Add_Defense_Op, Add_Defense_Op_intheQ,
    Burn_Op, StartDot_Heal, StartDot_Stealth_Off, Stealth_Off,
    Blind_Op,
    Dot_Heal, Dot_Burn, Dot_Stealth_Off, Dot_Blind_Off,
    GA_On,
    whenDamaged_FlagOn, Element_Check_FlagOn, intheRange_FlagOn, withOutFlag_FlagOn,
    Flag_Off, Remove_Element, Invincible_forOneAction, Stealth, Damage, whenAttackedFlagOn, Move, Get_Element, Heal, StackDamage, GetDefense, DamageMeBangMoo, AddDamage, MoveToSelectedTile, OpNextActionisMoveFlagOn,
    NotRumbleFlagOn, ExtraSelect_Kawari, NextTurn_AddDamage, ReplaceNextOpsMovetoStun, StunRecovery, SelectActionClockChange, UseEnergy, TrueDamage, MakeItTrue, PrevCycleClockFlagOn, ReduceMyNextTurnActionClock,
}
*/


[CreateAssetMenu(fileName = "ItemSo", menuName = "Scriptable Object/CardSO")]
public class CardSO : ScriptableObject
{
    public Card[] cards;
}