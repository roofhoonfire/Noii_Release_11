using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CSVLoader : MonoBehaviour //이자식은 카드의 메타데이터 //나중에 이것도 걍 스태틱으로 만들까
{
    public static CSVLoader Instance;



    // 런타임에 생성된 SO 인스턴스
    public CardSO runtimeCardSO; //카드 메타 데이터
    public ItemSO runtimeItemSO;// 아이템 메타 
    public CardEffectSO runtimeCardEffectSO;
    public BoundSO runtimeBoundSO;
    public BoundEffectSO runtimeBoundEffectSO;
    public JujuSO runtimeJujuSO;
    public JujuEffectSO runtimeJujuEffectSO;
    // 아이템 SO도 여기 하나 만듬




    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }
    void Start()
    {
        //카드 이펙트 로드
      //  CardDEffectDatabase.LoadEffectFromCSV(Resources.Load<TextAsset>("cardsEffect"));  //효과 데이터, 정적 클래스로 관리

        runtimeCardSO = CardMetaDatabase.LoadMetaFromCSV(); //메타 데이터 로드

        Debug.Log($"[CardCSVLoader] 런타임 카드 SO 생성 완료 - 총 {runtimeCardSO.cards.Length}장");

        runtimeItemSO = ItemMetaDatabase.LoadMetaFromCSV(); //메타 데이터 로드

        Debug.Log($"[ItemCSVLoader] 런타임 카드 SO 생성 완료 - 총 {runtimeItemSO.items.Length}장");


        runtimeCardEffectSO = CardEffectDatabase.LoadEffectsFromCSV();
        //    BoundLoader.LoadBoundsFromCSV(Resources.Load<TextAsset>("Bound"));

        runtimeBoundSO = BoundMetaDatabase.LoadMetaFromCSV();
        runtimeBoundEffectSO = BoundEffectDatabase.LoadEffectFromCSV();
        runtimeJujuSO = JujuMetaDatabase.LoadMetaFromCSV();
        runtimeJujuEffectSO = JujuEffectDatabase.LoadEffectFromCSV();

        //  JujuLoader.LoadJujuFromCSV(Resources.Load<TextAsset>("Juju"));
    }

    public Card GetCardByCode(string code)
    {
        foreach (var card in runtimeCardSO.cards)
        {
            if (card.code == code)
                return card;
        }
        return null; // 없으면 null 반환
    }


    public Item GetItemByCode(string code)
    {
        foreach (var item in runtimeItemSO.items)
        {
            if (item.code == code)
                return item;
        }
        return null; // 없으면 null 반환
    }

    public Bound GetBoundByCode(string code)
    {
        foreach (var bound in runtimeBoundSO.bounds)
        {
            if (bound.code == code)
                return bound;
        }
        return null; // 없으면 null 반환

    }

    public Juju GetJujuByCode(string code)
    {
        foreach (var juju in runtimeJujuSO.jujus)
        {
            if (juju.code == code)
                return juju;
        }
        return null; // 없으면 null 반환

    }
  

    /// <summary>
    /// 카드 코드에 해당하는 모든 CardEffect를 리스트로 반환
    /// </summary>
    public List<CardEffect> GetCardEffectsByCode(string code)
    {
        var result = new List<CardEffect>();

        if (runtimeCardEffectSO == null || runtimeCardEffectSO.cardeffects == null)
            return result;

        foreach (var effect in runtimeCardEffectSO.cardeffects)
        {
            if (effect == null)
                continue;

            if (effect.code == code)
                result.Add(effect);
        }

        return result;
    }

    public List<BoundEffect> GetBoundEffectsByCode(string code)
    {
        var result = new List<BoundEffect>();

        if (runtimeBoundEffectSO == null || runtimeBoundEffectSO.effects == null)
            return result;

        foreach (var effect in runtimeBoundEffectSO.effects)
        {
            if (effect == null)
                continue;

            if (effect.code == code)
                result.Add(effect);
        }

        return result;
    }

    public List<JujuEffect> GetJujuEffectByCode(string code)
    {
        var result = new List<JujuEffect>();
        if (runtimeJujuEffectSO == null || runtimeJujuEffectSO.effects == null)
            return result;
        foreach (var effect in runtimeJujuEffectSO.effects)
        {
            if (effect == null)
                continue;

            if (effect.code == code)
                result.Add(effect);
        }
        return result; // 없으면 null 반환

    }
}
