using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EightDirectionAnimator_Numpad : MonoBehaviour
{
    private Animator anim;

    private static readonly string[] DirNames =
    {
        "N",  // 0
        "NE", // 1
        "E",  // 2
        "SE", // 3
        "S",  // 4
        "SW", // 5
        "W",  // 6
        "NW"  // 7
    };

    private int lastDir = 0;
    private bool isMoving = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        int inputDir = GetNumpadDirection();

        if (inputDir != -1)
        {
            // Move
            if (inputDir != lastDir || !isMoving)
            {
                PlayMove(inputDir);
            }
        }
        else
        {
            // Idle
            if (isMoving)
            {
                PlayIdle(lastDir);
            }
        }
    }

    /// <summary>
    /// 숫자키 입력을 0~7 방향 인덱스로 변환 (-1 = 입력 없음)
    /// </summary>
    int GetNumpadDirection()
    {
        if (Input.GetKey(KeyCode.Keypad8)) return 0; // N
        if (Input.GetKey(KeyCode.Keypad9)) return 1; // NE
        if (Input.GetKey(KeyCode.Keypad6)) return 2; // E
        if (Input.GetKey(KeyCode.Keypad3)) return 3; // SE
        if (Input.GetKey(KeyCode.Keypad2)) return 4; // S
        if (Input.GetKey(KeyCode.Keypad1)) return 5; // SW
        if (Input.GetKey(KeyCode.Keypad4)) return 6; // W
        if (Input.GetKey(KeyCode.Keypad7)) return 7; // NW

        return -1;
    }

    void PlayMove(int dir)
    {
        lastDir = dir;
        isMoving = true;

        string stateName = $"Noii_Move_{DirNames[dir]}";
        anim.Play(stateName, 0, 0f);
    }

    void PlayIdle(int dir)
    {
        isMoving = false;

        string stateName = $"Noii_Idle_{DirNames[dir]}";
        anim.Play(stateName, 0, 0f);
    }
}
