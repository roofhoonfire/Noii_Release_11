using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SRandom = System.Random;

/// <summary>
/// 배틀 씬에서 사용할 액터(플레이어/몹) 공통 데이터
/// </summary>
[Serializable]
public class BattleActor
{
    public int actorId;      // 0 = 플레이어, 1 = 몹 (기본 가정)
    public string name;
    public bool isPlayer;

    public int hp = 100;
    public int maxHp = 100;
    public int curPos;       // 타일 인덱스 (필요 없다면 빼도 됨)
    public int energy = 3;

    // 필요하면 여기다가 디펜스, 상태이상, 덱/핸드 등 추가
}

/// <summary>
/// 몹 SO에 들어갈 "행동 템플릿"
/// → 여기서 카드코드, 기본 데미지, 코스트 등을 정의하고
/// → 런타임에 ActionData로 변환해서 사용
/// </summary>
[Serializable]
public class EnemyActionTemplate
{
    public string cardCode;
    public int baseDamage;
    public int baseDefense;
    public int baseRumble;
    public int baseActionClock;
    public string ActionNotice;
    // 필요하면 tileType, zoneIndex 등도 미리 넣어둘 수 있음
    // --- 패링 관련 ---
    [Tooltip("이 공격이 패링 가능한가? (parryWindows 비어있어도 강제 ON/OFF 용)")]
    public bool parryable = false;

    [Tooltip("이 공격 애니메이션 (선택)")]
    public AnimationClip attackAnim;

    [Tooltip("한 공격 안의 여러 패링 타이밍들")]
    public List<ParryWindow> parryWindows = new();
}
[Serializable]
public class ParryWindow
{
    public float startTime;  // 애니 시작 기준, 이 시점부터
    public float endTime;    // 이 시점까지 패링 판정 가능

    // 선택: 몇 번째 타인지 표시하고 싶으면
    public int hitIndex;     // 0,1,2... (UI 연출 등에 쓸 수 있음)
}
[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Battle/EnemyDefinition")]
public class EnemyDefinitionSO : ScriptableObject
{
    public string enemyId;
    public string enemyName;
    public int maxHp = 100;



    [Header("Animations")]
    public AnimationClip idleAnim;

    [Tooltip("이 몹이 사용할 수 있는 액션 템플릿들")]
    public List<EnemyActionTemplate> actions = new();
}
