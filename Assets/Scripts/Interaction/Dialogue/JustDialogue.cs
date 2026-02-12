using UnityEngine;

public class JustDialogue : MonoBehaviour
{
    [Header("대화 그래프 데이터")]
    [SerializeField] private DialogueGraphAsset graph;

    [Header("대화 중 플레이어 움직임 잠그기")]
    [SerializeField] private FieldPlayerController3D playerController;

    [Header("대상 캐릭터 ID (비워두면 playerController에서 FieldPlayerStatus로 찾음)")]
    [SerializeField] private string characterIdOverride = "";

    private FieldPlayerStatus _playerStatus;
    private DialogueGraphRunner _runner;

    /// <summary>
    /// 외부 스크립트가 호출하면, 이 오브젝트에 연결된 그래프 대화를 시작한다.
    /// </summary>
    public void StartJustDialogue()
    {
        if (DialogueGraphUIController.Instance == null)
        {
            Debug.LogWarning("[JustDialogue] DialogueGraphUIController가 씬에 없음!");
            return;
        }

        // 이미 대화 중이면 무시
        if (DialogueGraphUIController.Instance.IsPlaying)
            return;

        if (graph == null)
        {
            Debug.LogWarning("[JustDialogue] graph가 비어있음!");
            return;
        }

        // playerController 찾기 (없으면 씬에서 Player 찾아봄)
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FieldPlayerController3D>();
        }

        if (playerController == null)
        {
            Debug.LogWarning("[JustDialogue] FieldPlayerController3D를 찾지 못함!");
            return;
        }

        // FieldPlayerStatus 찾기
        _playerStatus = playerController.GetComponent<FieldPlayerStatus>();
        if (_playerStatus == null)
            _playerStatus = playerController.GetComponentInChildren<FieldPlayerStatus>();

        // characterId 결정
        string characterId = characterIdOverride;
        if (string.IsNullOrEmpty(characterId))
        {
            if (_playerStatus == null)
            {
                Debug.LogWarning("[JustDialogue] FieldPlayerStatus를 찾지 못했고 characterIdOverride도 비어있음!");
                return;
            }
            characterId = _playerStatus.characterId;
        }

        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning("[JustDialogue] GameStateManager.Instance가 없음!");
            return;
        }

        var charState = GameStateManager.Instance.GetCharacterState(characterId);
        if (charState == null)
        {
            Debug.LogWarning($"[JustDialogue] CharacterState를 찾지 못함! id={characterId}");
            return;
        }

        // 이동 잠금
        playerController.SetInputEnabled(false);

        _runner = new DialogueGraphRunner(graph, charState);
        _runner.Start(onFinished: () =>
        {
            // 이동 다시 켜기
            if (playerController != null)
                playerController.SetInputEnabled(true);
        });
    }
}
