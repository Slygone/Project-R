### Relic list
 
!For AI DO NOT READ FROM HERE NOW!!

Relic rules:
- Relics can drop from bosses, elite enemies and be bought from shop.
- Relics have three rarites. Common Relics, Legendary Relic and Cursed Relics.
- Common Relics are droped from Elits and Shops.
- Legendary Relics are droped from Bosses and special events.
- Cursed Relics are droped from special events and have negative effects.
- Relic should have a name (first word) and should also have a type displayed (common or legendary) and should then have the effect description.


Common Relics:

- Vital Totem — Icrease Health by 10%
- Sharpened Edge — Increase max damage by +2.
- Deadeye Counter — Every 10 skills used the next one is guaranteed to be a critical hit. (Carries across combats)
- First Pulse — On start of combat give 2 extra AP (First turn only)  
- Mark Echo — Once every 5 turns your next skill applies 1 extra mark.
- Last Stand Blade — Deal Physical Damage equal to % of health missing. Max 30% bonus.
- Sunder Ward — You are now immune to Sunder Status.
- Weaken Ward — You are now immune to Weaken Status.
- Ambush Seal — On start of combat apply weaken and vaunrable status to all enemies. Lasts 1 turn. You start combat with 2 AP.
- Field Rations — After each combat, heal 3% of max HP
- Marking Needle — The first time each turn you apply a mark, apply +1 extra mark (same element) to that target.
- Guard Charm — Start combat with a Shield equal to 8% of your max HP
- Focus Lens — Perfect QTE grants +1 AP next turn (once per turn)
- Reinforced Plates — Gain +10% Physical Resistance.
- Elemental Lining — Gain +10% Elemental Resistance.
- Catalyst Splinter — The first Dual Reaction you trigger each combat requires 2 marks + 2 marks instead of 3 + 3.
- Reaction Rebate — The first reaction you trigger each turn refunds 1 AP.
- Elemental Drip — At the end of your turn, apply 1 mark of your equipped element to a random enemy.
- Mark Transfer — When an enemy dies, transfer up to 2 marks (random existing marks) to another enemy.
- Clean Trigger — If your reaction QTE result is Perfect, 1 mark is not consumed.

Legendary Relics:

- Rhythm Discount — Every 3 turns reduce AP cost of your next 2 skills by 1.
- Cooldown Lottery — Every 20 AP spent (Carries over combat) one random skill gets its cooldown refreshed and becomes costs 0 AP to cast. If no skill is on cooldown then its just 0 AP to cast. Cannot target Ultimate.
- Early Guard Override — Disable Defensive QTE for the first 2 turns of combat. Gain 10% more resistances.
- Sigil Renounce — Disable Reactions QTE and removes all sigils from skills. Gain 20% more physical damage, 10% crit rating and 20% crit damage for the rest of the run. You cannot earn Sigils anymore.
- Fracture Revival — Revives you once per run. You come back with 30% health. This relic is then broken and cannot be used again it also reduces your max HP by 15% for the rest of the run.
- Elemental Broadcast — On start of combat apply 1 of your equipped elemental mark type on all enemies.
- Ice Cream Core — At the end of your turn, you keep any unspent AP instead of losing it. At the start of your next turn, you refresh to your Max AP as normal, then gain the saved AP on top. Saved AP can exceed Max AP and is spent first.
- Reactor Core — The first reaction you trigger each combat triggers twice (second trigger at 50% effect).
- Dual Specialist — When you trigger a Dual Reaction, gain a Shield = 5% max HP. (once per combat)

Cursed Relics:

- Crippling Weakness — Start combat with Weaken status on yourself lasts 2 turns.
- Rustbound Sunder — Start combat with Sundered status on yourself lasts 2 turns.
- Blood Toll — Every end of combat lose 1 hp.
- Overcharged Ultimate — Increases the energy requirment of your ultimate by 100%
- Cracked Battery — Start each combat with -2 AP for 2 turns.
- Rusty Blade — Your Crit Damage is reduced by 20%
- Siphoning Aura — Each turn you lose 1 AP at the start of your turn. (Max AP is not affected you basically start 9/10 each turn)
- Reaction Exhaustion — After you trigger a reaction, gain Weaken (1 turn). (once per turn)
- Elemental Fog — You can’t see enemy marks (UI hidden), but you gain +1 AP on turn 1.
- Overconsumption — Reactions consume +1 extra mark total (Dual consumes 3 + 4, Mono consumes 7).



Vital Totem + 10% hp Works
Sharpen Edge + 2 damage Works
Deadeye Counter + 10% crit chance should start counting from the moment it's aquired by the player. When aquired it should be 0/10 and every skill used it should count up to 10. When it reaches 10 it should trigger and reset to 0/10. when it is 10/10 it should be 100% crit chance and the next skill used should reset the counter to 0/10.
First Pulse + 2 AP does not work. It should apply + 2 AP to the player at the start of combat. Combat should start 12/10 since + 2 AP is applied at the start of combat. 
Mark Echo + 1 extra mark does not aplly + 1 mark when triggered. Once every 5 turns your next skill should apply + 1 mark to the target. 
Ambush Seal does not work. It should apply Weaken and Vunrable to all enemies for 1 turn at the start of combat. That should be displayed as a debuff on the enemy status container. Remove the + 2 AP on the start of combat from the relic.
Remove Marking Needle as a relic from the json and completle from the game. Its a bad relic design. No code should have it. 
Catalyst Splinter does not work. When Aquired reactions should require 2 marks + 2 marks instead of 3 + 3. It should only be the case for dual reactions not mono reactions from a single element. 
Remove the Reaction Rebate relic from the json and completle from the game. It should not exist. 
Rhythm Discount does not work. It should reduce AP cost of your next 2 skills by 1 every 3 turns. The reduced skill should be reflected in the skills UI with a yellow outlier and the AP cost should be reduced by 1 in the UI. 
Cooldown Lottery does not work. It should every 20 AP spent (Carries over combat) one random skill gets its cooldown refreshed and becomes costs 0 AP to cast. If no skill is on cooldown then its just 0 AP to cast. Cannot target Ultimate. The reduced skill should be reflected in the skills UI with a yellow outlier and the AP cost should be reduced by 0 in the UI.
Early Guard Override does not work. It should disable Defensive QTE for the first 2 turns of combat. Gain 10% more resistances. The resistance increase should be applied to the player for 2 turns only. It should also be displayede when hovering over the players tooltip. 
Ic Cream Core doe snot work. It should store ANY unspent AP at the end of turn and then add those to the start of the next turn to the player. example player ended turn with 5/10 AP. Next turn the player starts with 15/10 AP. Player ends turn with 15/10 AP. Next turn the player starts with 25/10 AP. This should have a cap of 50 and resets when combat ends. 
Fracture Revival does not work. It should revive the player once per run at 30% health. This relic should be broken and cannot be used again it also reduces the players max HP by 15% for the rest of the run. 
Elemental Broadcast does not work. It should apply 1 of your equipped elemental sigls to all enemies. For example if our skills have only watter equiped (or one type) then it should apply water to all enemies. If we have multiple types then it should apply 1 random type to all enemies. If no sigils are equipped then nothing should be applied. 
Reactor Core does not work. It should trigger the first reaction you trigger each combat twice (second trigger at 50% effect). The second reaction triggered should also have a QTE. 
Overcharged Ultimate should be reworked. It should increase the cooldown of the ultimate to + 2 turns of the original cooldown. For example if ultimate has 5 turn cooldown it should be 7 turn cooldown when this cursed relic is aquired. 
Cracked Battery does not work. It should reduce AP by 2 at the start of combat and for 2 turns after that. When aquired player should start with -2 AP at the start of combat and for 2 turns after that. For example if player has 10/10 AP at the start of combat it should be 8/10 AP at the start of combat and for 2 turns after that and turn 3 it should be 10/10 AP. If player has a relic like Fast Pulse or lighting reaction Overcharge then the cursed relic should still be applied -2 AP but the bonus is still gained. 
Siphoning Aura does not work. Every turn the player loses 1 AP at the start of their turn. (Max AP is not affected you basically start 9/10 each turn (-1 AP))
Reaction Exhaustion does not work. After you trigger a reaction, gain Weaken (1 turn). (once per turn)
Elemental Fog does not work. Let's rework it. When aquired it should hide the enemy intentions (attacks tooltip). Every 3rd turn it should randomize the AP cost of each skill (except ultimate) and the new skills cost should be in the skills UI.
Reaction Exhaust does not work. After you trigger a reaction, gain Weaken (1 turn). (once per turn)
Overconsumption does not work. Reactions consume +1 extra mark total. How it should work:
a) Target has 3 fire marks and 3 ice marks since this relic is aquired to trigger a fire-ice reaction the target should have +1 fire or ice mark. 
b) Target has 5 fire marks. To trigger a fire-fire reaction the target should have 7 fire marks and not the usual 6.
This was an example of marks and fire or ice should not be the only one this rule applies when the relic is Aquired. The idea is to increase the total marks needed to trigger a reaction with ANY mark. 

