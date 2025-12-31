using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Game/Dialogue Asset")]
public class DialogueAsset : ScriptableObject
{
    [Serializable]
    public class Line
    {
        [Tooltip("말하는 캐릭터 이름 (예: 노이, 행인A)")]
        public string speakerName;

        [TextArea(2, 5)]
        [Tooltip("대사 텍스트")]
        public string text;

        [Tooltip("초상화 스프라이트 (없으면 null)")]
        public Sprite portrait;
    }

    [Serializable]
    public class Reward
    {
        public enum RewardType { Item, Card }

        [Tooltip("언제 줄지: 0,1,2...는 해당 라인 직후, -1은 대화 끝난 뒤")]
        public int triggerLineIndex = -1;

        [Tooltip("아이템/카드 ID (CharacterState.itemIds / cardIds에 들어갈 코드)")]
        public string id;

        [Tooltip("몇 개 줄지 (카드는 장수, 아이템은 개수)")]
        public int amount = 1;

        [Tooltip("아이템인지 카드인지")]
        public RewardType type;
    }

    [Header("대사 리스트 (위에서 아래 순서로 진행)")]
    public List<Line> lines = new List<Line>();

    [Header("이 대화 도중/후에 줄 보상들")]
    public List<Reward> rewards = new List<Reward>();
}
