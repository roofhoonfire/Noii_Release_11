using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterVisualController : MonoBehaviour
{
    private Animator _anim;

    private static readonly string[] DirNames =
    {
        "N","NE","E","SE","S","SW","W","NW"
    };

    [Header("Settings")]
    [SerializeField] private float moveThreshold = 0.01f;

    // 대각 방향 기억 유지 시간
    [SerializeField] private float diagonalMemoryTime = 0.15f;

    private int _currentDirIndex = 0;
    private bool _isMoving = false;

    // ★ 최근 대각선 정보
    private int _lastDiagonalDirIndex = -1;
    private float _lastDiagonalTime = -999f;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    public void SetMoveDirection(Vector3 worldMoveDir)
    {
        Vector3 flat = new Vector3(worldMoveDir.x, 0f, worldMoveDir.z);
        float sqrMag = flat.sqrMagnitude;

        bool nowMoving = sqrMag > (moveThreshold * moveThreshold);

        if (nowMoving)
        {
            int dirIndex = GetDirectionIndex(flat);

            // ★ 둘 다 눌려서 대각선으로 움직이는 중이면 기억해 둔다
            if (Mathf.Abs(flat.x) > 0f && Mathf.Abs(flat.z) > 0f)
            {
                _lastDiagonalDirIndex = dirIndex;
                _lastDiagonalTime = Time.time;
            }

            if (!_isMoving || dirIndex != _currentDirIndex)
            {
                PlayMove(dirIndex);
            }

            _currentDirIndex = dirIndex;
        }
        else
        {
            if (_isMoving)
            {
                // ★ 멈추는 순간, 최근 대각선이 “짧은 시간 안에” 있었으면 그걸로 Idle
                int idleIndex = _currentDirIndex;
                if (_lastDiagonalDirIndex != -1 &&
                    Time.time - _lastDiagonalTime <= diagonalMemoryTime)
                {
                    idleIndex = _lastDiagonalDirIndex;
                }

                PlayIdle(idleIndex);
            }
        }

        _isMoving = nowMoving;
    }

    private int GetDirectionIndex(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f)
            return _currentDirIndex;

        dir.Normalize();

        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        int index = Mathf.RoundToInt(angle / 45f) % 8;
        return index;
    }

    private void PlayMove(int dirIndex)
    {
        string stateName = $"Noii_Move_{DirNames[dirIndex]}";
        _anim.Play(stateName, 0, 0f);
    }

    private void PlayIdle(int dirIndex)
    {
        string stateName = $"Noii_Idle_{DirNames[dirIndex]}";
        _anim.Play(stateName, 0, 0f);
    }
}
