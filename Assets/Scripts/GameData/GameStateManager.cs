using System.IO;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }


    //일단 이렇게 하고 본격 개발되면 불러오기로 바꾸기
    public GameSaveData CurrentSave { get; private set; } = new GameSaveData();

    private string SavePath => Path.Combine(Application.persistentDataPath, "save_slot0.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    
        //나중에 수정
        //   DontDestroyOnLoad(gameObject);

        // 필요하면 시작 시 자동 로드
        // TryLoadGame();
    }

    public CharacterState GetCharacterState(string id)
    {
        return CurrentSave.GetOrCreateCharacter(id);
    }

    #region Save / Load

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(CurrentSave, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[GameStateManager] Saved to {SavePath}");
    }

    public bool LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("[GameStateManager] No save file found.");
            return false;
        }

        string json = File.ReadAllText(SavePath);
        CurrentSave = JsonUtility.FromJson<GameSaveData>(json) ?? new GameSaveData();
        Debug.Log($"[GameStateManager] Loaded from {SavePath}");
        return true;
    }

    #endregion
}
