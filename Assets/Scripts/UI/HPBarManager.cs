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



    public void DamageMe()
    {
        int newHP = myHP - 10;
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
    public void DamageOp()
    {
        int newHP = opHP - 10;
        //if (damageAmount < 0)
        //  return;

        //  soundSource.clip = hurtSound;
        // if(soundOn) soundSource.Play();

        // Update HealthBar
        if (enemBarHolder != null)
       {
           enemBarHolder.UpdateBar(newHP);
          Debug.Log("된겨 안된겨");
       }

      //  test.UpdateBar(newHP);
        opHP = newHP;
        //leftAnimator.SetTrigger("Damage");
    }

    #endregion

}
// Update is called once per frame


