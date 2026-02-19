using Microlight.MicroBar;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPBarManager : MonoBehaviour
{

    [Header("Health Bar")]
    //  [SerializeField] Transform leftBarHolder;

    [SerializeField] public MicroBar myBarHolder;
    [SerializeField] public MicroBar enemBarHolder;
    [SerializeField] public MicroBar test;

    int myHP;
    int opHP;
    public int OpHP => opHP;
    public int MyHP => myHP;
    //추후 데이터값으로 초기화 

    public static HPBarManager Instance { get; private set; }

    // Start is called before the first frame update
    private void Awake()
    {
        // 싱글톤 보장
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;


    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void HPBar_Init(int StartingHp)
    {



        if (myBarHolder != null) myBarHolder.Initialize(StartingHp);
        if (enemBarHolder != null) enemBarHolder.Initialize(StartingHp);
 //       test.Initialize(StartingHp);
        myHP = StartingHp;
        opHP = StartingHp;
    }

    #region Damage/Heal



    public void DamageMe(int deal)
    {
        int newHP = myHP - deal;
        // if (damageAmount < 0)
        //   return;


        // Update HealthBar
        if (myBarHolder != null)
        {
            myBarHolder.UpdateBar(newHP);

        }
        myHP = newHP;

        //leftAnimator.SetTrigger("Damage");
    }
    public int DamageOp(int deal)
    {
        int newHP = opHP - deal;
        if (enemBarHolder != null) enemBarHolder.UpdateBar(newHP);
        opHP = newHP;
        return newHP;
    }

    #endregion

}
// Update is called once per frame


