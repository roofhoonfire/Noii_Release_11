using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public List<CharacterState> characters = new List<CharacterState>();

    public CharacterState GetOrCreateCharacter(string id)
    {
        var found = characters.Find(c => c.characterId == id);
        if (found != null) return found;

        var created = new CharacterState
        {
            characterId = id
        };
        characters.Add(created);
        return created;
    }
}
