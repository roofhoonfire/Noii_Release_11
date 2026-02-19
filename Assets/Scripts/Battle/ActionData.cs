using System.Collections.Generic;
using System;
using UnityEngine;

public class ActionData //여기 뭐 추가할 거면 carddragHandler로 수정해야함
{

    public int actionId; //0 이면 이동 1이면 카드, 99면 도트 뎀 
    public int destindex = -10; // 이동 일 경우 목적지 인덱스를 저장// 액션이 destIndex가 있을 경우 해당 위치로 이동 (플레이어 딕셔너리 저장은 따로)//즉 렌더링 용 변수이다 
    public int defense;
    public int damage;
    public int rumblePoint;
    public int actionClock;
    public string cardcode;
    public int plusAlpha; //플러스 알파는 액션 단위로 관리해야함// 이거 이제 없애도 됨 //물론 바꾸긴 해야겟디만 ㅎ
    public List<int> flags = new List<int>();//
    public bool isfirstStrikeSuccess = false; //이것도 사실상 의미가 없어졌지만, 일단 냅두자
    public string cardname;
    public int seqType;
    public int zoneIndex;
    public List<AnimationClip> animations;//사실상 필요가 없어졌다
    public bool asCombo = false;
    public List<CardEffect> effects = new();
    public int Dot_to; //도트 딜이 노리는 액터넘버
    public List<int> effectTiles = new();
    //public HitResolution? resolvedOutcome;
    public List<string> blessList = new();

    [NonSerialized]
    public bool hasOtherExecutedSinceInsertion = false; //선공, 대처 결정용 변수

    public int nthaction = 0;
    public bool isDot = false;

    //텍스트
    public string actionNotice;

    // ===== 패링 관련 =====
    public bool isEnemyAction = false;     // 몹 액션인지 표기 (패링 전용)
    public bool parryable = false;         // 패링 가능한 공격인지

    // 한 공격 안의 패링 구간들 (시간은 "애니 시작 기준 상대시간")
    public List<ParryWindow> parryWindows = new();

    // 결과값
    public int parrySuccessCount = 0;      // 몇 번이나 패링 성공했는지
    public List<int> parriedHitIndices = new(); // 어느 hitIndex가 패링됐는지
    public bool wasParried = false;
    /*   public void InitializeEffects()
       {

           if (effects != null && effects.Count > 0)
               effects.AddRange(CardDEffectDatabase.GetEffects(cardcode));
           else
               effects = CardDEffectDatabase.GetEffects(cardcode);
           // 여기서 이펙트 리스트 채워넣고
           // 그다음에 럼블 포인트, 수비력 등 도 채워넣으면댐
       }



       public void ProcessHook(
           HookType hook,
           int actorNum,
           ActionData rightNextOpponent,
           int oppActorNum,
           ActionData rightNowOp,
           ActionData myRightNextMove)
       {
           if (effects == null)
               return;

           foreach (var e in effects)
           {
               // 훅 타입이 다르면 스킵
               if (e.hookType != hook)
                   continue;

               // 선공 훅인데 이미 다른 행동이 실행된 적이 있으면 스킵 (flag 상관없이 적용)
               if (e.hookType == HookType.Priority && hasOtherExecutedSinceInsertion)
                   continue;

               // flag가 0이 아닌 경우, flags 리스트에 포함되어야 적용
               if (e.flag != 0)
               {
                   if (flags == null || !flags.Contains(e.flag))
                       continue;
               }
               //rightNowOp는 대처, 카운터, IQA시 사용되는 액션임
               //
               // 모든 조건 통과 시 효과 적용
               e.Apply(actorNum, this, oppActorNum, rightNextOpponent, rightNowOp, myRightNextMove);
               flags.Remove(e.flag);
           }

       }
       public IEnumerator Norm_ProcessHook(
        HookType hook,
        int hActorNum,
        int hOpActorNum,
        ActionData ttAction,
        ActionData hOpMainAction,
        ActionData hMainAction)s
       {
           if (effects == null)
               yield break;

           foreach (var e in effects)
           {
               // 훅 타입이 다르면 스킵
               if (e.hookType != hook)
                   continue;

               // 선공 훅인데 이미 다른 행동이 실행된 적이 있으면 스킵
               if (e.hookType == HookType.Priority && hasOtherExecutedSinceInsertion)
                   continue;

               // flag 조건 확인
               if (e.flag != 0)
               {
                   if (flags == null || !flags.Contains(e.flag))
                       continue;
               }

               // 효과 적용 (여기서 애니메이션 대기 등 비동기 처리가 필요하다면 yield 가능)
               yield return e.Norm_Apply(hActorNum, hOpActorNum, this, hOpMainAction, hMainAction, ttAction);
               //  flags.Remove(e.flag);

               // flag 제거
               // 영구 플래그 처리 필요하면 여기에 조건 추가
           }

       }








       */

}
