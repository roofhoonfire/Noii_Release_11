using UnityEngine;

public class NpcTalker : FieldInteractableBase
{
    [Header("NPC 전용 설정")]
    public string npcName = "???";

    protected override void OnInteract(GameObject interactor)
    {
        Debug.Log($"[NPC] {npcName} 와(과) 대화 시작! interactor = {interactor.name}");
        // TODO: 대화 UI 열기 등
    }
}
