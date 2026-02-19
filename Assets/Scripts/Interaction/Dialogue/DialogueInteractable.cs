using UnityEngine;

public class DialogueInteractable : FieldInteractableBase
{
    [Header("대화 그래프 데이터")]
    [SerializeField] private DialogueGraphAsset graph;

    [Header("대화 중 플레이어 움직임 잠그기")]
    [SerializeField] private FieldPlayerController3D playerController;

    private FieldPlayerStatus _playerStatus;
    private DialogueGraphRunner _runner;

    protected override void OnInteract(GameObject interactor)
    {
        if (DialogueGraphUIController.Instance == null)
        {
            Debug.LogWarning("[DialogueInteractable] DialogueGraphUIController가 씬에 없음!");
            return;
        }

        // 이미 대화 중이면 무시
        if (DialogueGraphUIController.Instance.IsPlaying)
            return;

        if (graph == null)
        {
            Debug.LogWarning("[DialogueInteractable] graph가 비어있음!");
            return;
        }

        // interactor에서 플레이어 컨트롤러 찾기
        if (playerController == null && interactor != null)
        {
            playerController = interactor.GetComponentInParent<FieldPlayerController3D>();
        }

        // 플레이어 상태 찾기
        if (playerController != null)
        {
            _playerStatus = playerController.GetComponent<FieldPlayerStatus>();
            if (_playerStatus == null)
                _playerStatus = playerController.GetComponentInChildren<FieldPlayerStatus>();
        }

        if (_playerStatus == null)
        {
            Debug.LogWarning("[DialogueInteractable] FieldPlayerStatus를 찾지 못함!");
            return;
        }

        // 이동 잠금
        if (playerController != null)
            playerController.SetInputEnabled(false);

        // CharacterState 가져오기 (FieldPlayerStatus가 내부 state를 들고 있으니, 거기서 직접 꺼내는 함수가 있으면 더 좋다)
        // 지금 구조상 FieldPlayerStatus가 state를 private로 들고 있어서,
        // 아래처럼 "GameStateManager에서 다시 가져오는" 방식이 가장 안전하다.
        var charState = GameStateManager.Instance.GetCharacterState(_playerStatus.characterId);

        _runner = new DialogueGraphRunner(graph, charState, gameObject);
        _runner.Start(onFinished: () =>
        {
            // 이동 다시 켜기
            if (playerController != null)
                playerController.SetInputEnabled(true);
        });
    }
}
