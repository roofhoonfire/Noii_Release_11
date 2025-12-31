using UnityEngine;

public class DialogueInteractable : FieldInteractableBase
{
    [Header("대화 데이터")]
    [SerializeField] private DialogueAsset dialogue;

    [Header("대화 중 플레이어 움직임 잠그기")]
    [SerializeField] private FieldPlayerController3D playerController;

    private FieldPlayerStatus _playerStatus;

    protected override void OnInteract(GameObject interactor)
    {
        if (DialogueUIController.Instance == null)
        {
            Debug.LogWarning("[DialogueInteractable] DialogueUIController가 씬에 없음!");
            return;
        }

        // 이미 대화 중이면 무시
        if (DialogueUIController.Instance.IsPlaying)
            return;

        // interactor에서 플레이어 컨트롤러 찾기
        if (playerController == null && interactor != null)
        {
            playerController = interactor.GetComponentInParent<FieldPlayerController3D>();
        }

        // 플레이어 상태 컴포넌트도 찾기
        if (playerController != null)
        {
            _playerStatus = playerController.GetComponent<FieldPlayerStatus>();
            if (_playerStatus == null)
                _playerStatus = playerController.GetComponentInChildren<FieldPlayerStatus>();
        }

        // 이동 잠금
        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
        }

        // 대화 시작
        DialogueUIController.Instance.StartDialogue(
            dialogue,
            onFinished: () =>
            {
                // 대화 끝나면, triggerLineIndex == -1 보상 처리
                TryGiveRewardsForLine(-1);

                // 이동 다시 켜기
                if (playerController != null)
                {
                    playerController.SetInputEnabled(true);
                }
            },
            onLineChanged: (lineIndex) =>
            {
                // 각 라인에 연결된 보상 처리
                TryGiveRewardsForLine(lineIndex);
            });
    }

    private void TryGiveRewardsForLine(int lineIndex)
    {
        if (dialogue == null || dialogue.rewards == null || dialogue.rewards.Count == 0)
            return;

        if (_playerStatus == null)
            return;

        foreach (var reward in dialogue.rewards)
        {
            if (reward.triggerLineIndex != lineIndex)
                continue;

            GiveReward(reward);
        }
    }

    private void GiveReward(DialogueAsset.Reward reward)
    {
        if (_playerStatus == null) return;
        if (string.IsNullOrEmpty(reward.id)) return;

        int count = Mathf.Max(1, reward.amount);

        switch (reward.type)
        {
            case DialogueAsset.Reward.RewardType.Item:
                for (int i = 0; i < count; i++)
                {
                    _playerStatus.AddItem(reward.id);
                }
                Debug.Log($"[DialogueInteractable] 아이템 지급: {reward.id} x{count}");
                break;

            case DialogueAsset.Reward.RewardType.Card:
                for (int i = 0; i < count; i++)
                {
                    _playerStatus.AddCard(reward.id);
                }
                Debug.Log($"[DialogueInteractable] 카드 지급: {reward.id} x{count}");
                break;
        }
    }
}
