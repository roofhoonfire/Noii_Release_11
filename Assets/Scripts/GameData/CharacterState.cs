using System;
using System.Collections.Generic;

[Serializable] // 이거 덕에 JsonUtility로 바로 세이브가 가능해지는 것이여
public class CharacterState
{
    public string characterId;   // "player", "npc_001" 같은 고유 ID

    public int maxHP = 100;
    public int currentHP = 100;

    // 나중에 상태이상도 string ID로 관리 가능 ("poison", "stun" 등)
    public List<string> statusIds = new List<string>();//나중에 싹다 enum으로 ㄱㄱ


    // 소지 아이템/카드 - 지금은 문자열 ID만
    public List<string> itemIds = new List<string>();
    public List<string> cardIds = new List<string>();
}
