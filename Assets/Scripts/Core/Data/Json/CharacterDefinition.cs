using System;
using System.Collections.Generic;

[Serializable]
public class CharacterDefinition
{
    public string id;
    public string displayName;
    public int characterId;
    public CharacterStats stats;
    public int gold;
    public int maxActionPoints;
    public List<string> skillIds;
    public PerkIds perkIds;
}

[Serializable]
public class CharacterStats
{
    public int maxHealth;
    public int damageMin;
    public int damageMax;
    public int maxEnergy;
    public int critChance;
    public float critDamage;
    public int physicalResist;
    public int elementalResist;
}

[Serializable]
public class PerkIds
{
    public List<string> tier1;
    public List<string> tier2;
    public List<string> tier3;
    public List<string> tier4;
}

[Serializable]
public class CharactersFile
{
    public List<CharacterDefinition> characters;
}
