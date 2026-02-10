## GDD

### Enemies
- Currently enemies have only 2 skills in their json file, but they only have attack as a skill inside the game. Let's change that. 

Enemies should have 2 skills.

Regular enemies Rules:
- One skill should be a basic skill. 
- The basic skill has no cooldown.
- One skill should be a special skill.
- The special skill should be a skill that is unique to the enemy.
- The special skill should have a cooldown.
- First turn is always a basic skill.
- The special skill should be always casted when off cooldown by the enemy after the first turn. 
- Each regular enemy should have a different special skill. 

      "type": "Enemy",
      "displayName": "Thug",
      "skill1": "Quick stab", // Basic Skill 1.0 damage modifier
      "skill2": "Weaken", // Special Skill reduces the players damage by 50% for 2 turns
      "skill2Cooldown": 3,

      "type": "Enemy",
      "displayName": "Archer",
      "skill1": "Arrow", // Basic Skill 1.0 damage modifier
      "skill2": "Piercing Arrow", // Special Skill 1.0 damage modifier ignores resistance & shields.
      "skill2Cooldown": 2,

      "type": "Enemy",
      "displayName": "Pyromaniac",
      "skill1": "Fireball", // Basic Skill 1.0 damage modifier
      "skill2": "Ignite", // Special Skill applies a damage over time effect to the player. 3 turns of 1.5 damage modifier.
      "skill2Cooldown": 2,


      "type": "Enemy",
      "displayName": "Wizard",
      "skill1": "Arcane Bolt", // Basic Skill 1.0 damage modifier
      "skill2": "Arcane Shield", // Special Skill applies a shield to the enemy for 50% of enemy max health.
      "skill2Cooldown": 2,

      "type": "Enemy",
      "displayName": "Frostcaller",
      "skill1": "Frost Nova", // Basic Skill 1.0 damage modifier
      "skill2": "Frost Shield", // Special Skill applies a shield to the enemy increasing resistances by 20% for 1 turn. If broken the player takes 1.5 damage modifier. Brake point = 20% of max health recived while shiled is active.
      "skill2Cooldown": 2,

      "type": "Enemy",
      "displayName": "TideShaman",
      "skill1": "Tidal Wave", // Basic Skill 1.0 damage modifier
      "skill2": "Monsoon", // Special Skill applies a healing over time effect to the enemys. Healing them for 5% of max health per turn.
      "skill2Cooldown": 2,

      "type": "Enemy",
      "displayName": "Deserter",
      "skill1": "Slash", // Basic Skill 1.0 damage modifier
      "skill2": "Battle Shout", // Special Skill increases the enemys damage by 20% for 2 turns.
      "skill2Cooldown": 3,

      "type": "Enemy",
      "displayName": "Shieldbearer",
      "skill1": "Shield Slam", // Basic Skill 1.0 damage modifier
      "skill2": "Retaliation", // Special Skill the enemy returns the players attacks this turn. Each attack the enemy deals 1.0 damage modifier.
      "skill2Cooldown": 4,
   

Elite enemies Rules:
- One skill should be a basic skill.
- One skill should be a special skill.
- The special skill should be a skill that is unique to the enemy.
- The special skill should have a cooldown.
- First turn is always a basic skill.
- The special skill should be always casted when off cooldown by the enemy after the first turn.
- Each regular enemy should have a different special skill. 
 


      "type": "Elite", 
      "displayName": "Juggernaut",      
      "skill1": "Sunder Smash", // Basic Skill 1.0 damage modifier. Each attack the enemy leaves a mark on the player. Increasing damage taken by 10% for 2 turns. This can stack up to 50%
      "skill2": "Brutal Strikes", // Special Skill 0.7 damage modifier. Attacks 3 times in a row. 
      "skill2Cooldown": 2,

      "type": "Elite",
      "displayName": "Spellbreaker",
      "skill1": "Spell blade", // Basic Skill 1.0 damage modifier.If player has a shield it deals 1.5 damage modifier to shileds. 
      "skill2": "Syphon Magic", // Special Skill steals all shield from the player. Deals 1.0 damage for each point of shield stolen.
      "skill2Cooldown": 3,

      "type": "Elite",
      "displayName": "StormCaptain",
      "skill1": "Lighting Cutlass", // Basic Skill 1.0 damage modifier. 
      "skill2": "Null Sigil", // Special Skill blocks all marks being applied to enemies for 2 turns. Deals 0.5 damage modifier to the player for each sigil blocked. (5 sigils = 2.5 damage modifier)
      "skill2Cooldown": 3,
      
    
      "type": "Elite",
      "displayName": "StoneColossus",      
      "skill1": "Crush", // Basic Skill 1.0 damage modifier.
      "skill2": "Granite Bastion", // Special Skill applies a shield to the enemy shielding them for 20% of max health for the combat. Each stack of Granite Bastion increases the damage of the crush skill by 10% damage modifier. 
      "skill2Cooldown": 2,
  
  Boss enemies Rules:
  - Bosses have varied skills.
  - Bosses have attack patterns. 
  
      "type": "Boss",
      "displayName": "SlimeBoss",
      "skill1": "Slam", // Basic Skill 1.0 damage modifier.
      "skill2": "Weaken", // Special Skill applies an effect weaken the player reducing damage done by 50% for 2 turns.
      "skill2Cooldown": 2,
      "skill3": "Sunder", // Special Skill applies an effect Sunder the player reducing shield gain by 50% for 2 turns.
      "skill3Cooldown": 2,
      "skill4": "Split", // Special Skill splits the SlimeBoss into 2 slimes. Each slime has 50% of the SlimeBoss current health. Split is Casted when Slimeboss is <= 66% of max health. 

      SlimeBoss Attack Pattern:
      - SlimeBoss attacks with Slam Turn 1
      - SlimeBoss attacks with Weaken Turn 2
      - SlimeBoss attacks with Slam Turn 3
      - SlimeBoss attacks with Sunder Turn 4
      - Repeat
      
      "type": "Boss",
      "displayName": "MadSlime", //MadSlime is a an enemy that can only be spawned when SlimeBoss splits.
      "skill1": "Slam", // Basic Skill 1.0 damage modifier.
      "skill2": "Weaken", // Special Skill applies an effect weaken the player reducing damage done by 50% for 2 turns.
      "skill2Cooldown": 2,

      MadSlime Attack Pattern:
      - MadSlime attacks with Slam Turn 1
      - MadSlime attacks with Weaken Turn 2
      - MadSlime attacks with Slam Turn 3
      - Repeat

      "type": "Boss", 
      "displayName": "SadSlime", //SadSlime is a an enemy that can only be spawned when SlimeBoss splits.
      "skill1": "Slam", // Basic Skill 1.0 damage modifier.
      "skill2": "Sunder", // Special Skill applies an effect Sunder the player reducing shield gain by 50% for 2 turns.
      "skill2Cooldown": 2,

      SadSlime Attack Pattern:
      - SadSlime attacks with Slam Turn 1
      - SadSlime attacks with Sunder Turn 2
      - SadSlime attacks with Slam Turn 3
      - Repeat

      "type": "Boss",
      "displayName": "MirrorBoss",
      "skill1": "Strike", // Basic Skill 1.0 damage modifier.
      "skill2": "Mirror Shield", // Special Skill applies a shield to the MirrorBoss. 20% of max health.
      "skill3": "Mirror Reaction", // Special Skill mirrors the players last elemental reaction. 
      "skill4": "Mirror Strike", // Special Skill mirrors the players last attack. Deals 1.5 damage modifier.

      MirrorBoss Attack Pattern:
      - MirrorBoss attacks with Strike Turn 1
      - MirrorBoss attacks with Mirror Shield if player gains a shield.
      - MirrorBoss attacks with Mirror Reaction if player casts a reaction.
      - MirrorBoss attacks with Mirror Strike if player casts an attack. 
      // Mirror Attacks are casted in addition to the Mirror Boss Strike. If a player attacks, gains a shield and casts a reaction the MirrorBoss attacks with all 4 skills.
      - Repeat
    
      "type": "Boss",
      "displayName": "ElementalHydra",
      "skill1": "Hydera Bite", // Basic Skill 1.0 damage modifier. Heals the Hydra for 15% of damge dealt. 
      "skill2": "Hydra Breath", // Special Skill applies a damage over time effect to the player. 3 turns of 1.5 damage modifier.
      "skill2Cooldown": 2,
      "skill3": "Hydra Tail", // Special Skill stuns the enemy for 1 turn. Weakens and Sunders for 2 turns. 
      "skill3Cooldown": 4,
      

      Hydra Attack Pattern:
      - Hydra attacks with Hydera Bite Turn 1
      - Hydra attacks with Hydra Breath Turn 2
      - Hydra attacks with Hydra Tail Turn 3
      - Repeat


      "type": "Boss",
      "displayName": "FallenChampion",
      "skill1": "Champion Strike", // Basic Skill 2.0 damage modifier. 
      "skill2": "Short Combo", // Special Skill Attacks 3 times in a row. Each attack deals 1.2 damage modifier. If each attack is perfectly timed by the player the Short Combo skill deals 0 damage. If not the player takes full damage.
      "skill2Cooldown": 2,
      "skill3": "Long Combo", // Special Skill Attacks 6 times in a row. Each attack deals 1.5 damage modifier. If each 
      attack is perfectly timed by the player the Long Combo skill deals 0 damage. If not the player takes full damage. 
      "skill3Cooldown": 2,
      "skill4": "Reborn", // Special Skill revives the FallenChampion with 100% of max health and gains 100% more damage modifier. Removes cooldown on Long Combo. Can only happen once per combat.

      FallenChampion Attack Pattern:
      - FallenChampion attacks with Champion Strike Turn 1
      - FallenChampion attacks with Short Combo Turn 2
      - FallenChampion attacks with Long Combo Turn 3
      - Repeat
      
      Reborn FallenChampion Attack Pattern:
      - FallenChampion attacks with Long Combo Turn 1
      - Repeat
