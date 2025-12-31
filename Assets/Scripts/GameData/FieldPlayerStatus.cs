using UnityEngine;

public class FieldPlayerStatus : MonoBehaviour
{
    [Header("이 오브젝트가 참조할 캐릭터 ID")]
    public string characterId = "player"; // 플레이어면 고정 "player" 등

    private CharacterState state;

    private void Start()
    {
        state = GameStateManager.Instance.GetCharacterState(characterId);

        // 필요하면 여기서 HP를 초기화해서 UI에 반영
        Debug.Log($"[FieldPlayerStatus] Loaded HP: {state.currentHP}/{state.maxHP}");
    }

    public int CurrentHP => state.currentHP;

    public void TakeDamage(int amount)
    {
        state.currentHP -= amount;
        if (state.currentHP < 0) state.currentHP = 0;

        // HP UI 갱신 등
        Debug.Log($"[FieldPlayerStatus] Damaged! HP: {state.currentHP}/{state.maxHP}");
    }

    public void Heal(int amount)
    {
        state.currentHP += amount;
        if (state.currentHP > state.maxHP)
            state.currentHP = state.maxHP;

        // HP UI 갱신 등
    }

    public void AddItem(string itemId)
    {
       // if (!state.itemIds.Contains(itemId))
            state.itemIds.Add(itemId);
    }

    public void AddCard(string cardId)
    {
        state.cardIds.Add(cardId);
    }
}
