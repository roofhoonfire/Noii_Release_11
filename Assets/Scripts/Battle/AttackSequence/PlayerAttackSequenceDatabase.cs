using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttackSequenceDatabase : MonoBehaviour
{



    public List<PlayerAttackSequenceSO> playerAttackSequenceSOs = new List<PlayerAttackSequenceSO>();

    public static PlayerAttackSequenceDatabase Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }






    public PlayerAttackSequenceSO Get (string code)
    {


        foreach (PlayerAttackSequenceSO so  in playerAttackSequenceSOs){

           if ( so.cardCode == code){

                return so;

            }
        }
        Debug.Log("플레이어 공격 SO 파일 못찾음");
        return null;

    }
}
