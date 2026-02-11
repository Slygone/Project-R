## Rules for Reactions.

- Marks left from attacks are consumed to gain reaction. 
- Marks left from reactions are separate status.
- All reactions that deal damage are their own separate source of damage. They are not affected by crit UNLESS the reaction specifically states that they can crit. Only reaction that can crit the damage is the Fire - Ice Reaction. But lets add a can crit option to the reactions json file in case we want to change our mind and balance what we want to crit. 
- If a reaction grants a buff like the Ice-Ice reaction it should be displayed next to the debuff on the players status bar.
- If a reaction grants a debuff like the Fire-Fire reaction it should be displayed as a mark chip on the enemy status container. 
-  



## Mono reaction. Marks needed (6) of a single mark
Fire-Fire -> Deals damage range of character damage and applies **Ignite** for 2 turns (refreshed when re-applied). Ignite deals x damage per turn. If the enemy already has Ignite DoT, the reaction damage deals 10% more damage. DOT damage is also increased by 10%.

Ice-Ice -> Player gains bonus Crit Damage 20% for the next turn. 

Lightning-Lightning -> Deals damage range of character and gains 2 extra max AP this and next turn. (when gaining AP is also restored 2 AP if user has 0 AP when procking reaction they gain 2 max ap and they are usable) 

Rock-Rock -> Applies shield to the player equal to 10 value. As long as player is affected by shield player deals 10% more rock damage.

Water-Water -> Deals damage range of character and leaves mark on enemy for 2 turns (Refreshed when re-applied). For the next 2 turns when the enemy attcks the player the player heals for 1% of max hp. Once per turn. 

Wind-Wind -> Deals damage range of character and reduces all resistances by 40% on the enemy(as a debuff) for 2 turns.

## Dual Reaction. Marks needed of element A (3) and element B (3). 
Fire-Ice ->  Deals twice the amount of damage range of character to the enemy. This reaction CAN crit.

Fire-Lightning -> Deals single target damage to the main target and area of effect damage to the surrounding enemies. 

Fire-Rock -> Leaves a fire DOT on the enemy dealing 25% of damage range of character per turn for 2 turns. This debuff can stack up to 3 times. When reapplying the dot the debuff turn is refreshed. 

Fire-Water -> Deals damage range of character to the enemy and applies weak 50% to the enemy for 2 turns. Weak debuff is 50% less damage dealt. 

Fire-Wind -> Deals damage range of character to the enemy and lowers resistances to fire and wind by 25% for 2 turns

Ice-Lightning -> Ignores all resistances when dealing reaction damage. Deals damage range x 1.5 of character to the enemy.

Ice-Rock -> Deals damage range of character to the enemy and applies Shatter debuff on the enemy for 2 turns. While Shatter debuff is active, the enemy accumulates damage taken. When accumulated damage reaches damage range of character, Shatter debuff pops: deal bonus damage range of character to the enemy and remove Shatter debuff. Re applying the debuff does not refresh the Shatter.

Ice-Water -> Freezes the enemy for one turn making them skip their turn. Freze =/= Stun. Stun is a separate debuff.

Ice-Wind -> deals damage range of character to the enemy and reduces resistances to ice and wind by 25% for 2 turns

Lightning-Rock -> Gain Reflective Armor for next turn: when hit, reflect 30% of damage taken back to the attacker. Also the character reduces damage taken by 10%.

Lightning-Water -> Leaves a mark on the enemy for 2 turns(Refeshed when re-applied). Each time the enemy is attacked mark is triggered dealing damage. Stacks up to 3 times making the damage taken 3 x stronger. 
Mark x 1 deals 0.2 damage range of character to the enemy.
Mark x 2 deals 0.25 damage range of character to the enemy 
Mark x 3 deals 0.3 damage range of character to the enemy.

Lightning-Wind -> deals damage range of character to the enemy and reduces resistances to lightning and wind by 25% for 2 turns

Rock-Water -> Applies mark on the enemy for 2 turns (refreshed when re-applied). At 3 marks, consume marks: deal damage and apply Stun 1 turn. 
Mark x 1 deals 0.3 damage range of character to the enemy.
Mark x 2 deals 0.4 damage range of character to the enemy 
Mark x 3 deals 0.5 damage range of character to the enemy.

Rock-Wind -> deals flat damage and reduces resistances to rock and wind by 25% for 2 turns

Water-Wind -> deals flat damage and reduces resistances to water and wind by 25% for 2 turns


## Reaction names.
Fire-Fire -> Name: Pyroclasm
Ice-Ice -> Name: Glacial Focus
Lightning-Lightning -> Name: Overcharge
Rock-Rock -> Name: Stoneguard
Water-Water -> Name: Tidal Surge
Wind-Wind -> Name: Gale

Fire-Ice -> Name: Melt
Fire-Lightning -> Name: Overload
Fire-Rock -> Name: Magma Scorch
Fire-Water -> Name: Vaporize
Fire-Wind -> Name: Inferno

Ice-Lightning -> Name: Superconduct
Ice-Rock -> Name: Permafrost
Ice-Water -> Name: Flash Freeze
Ice-Wind -> Name: Boreal Howl

Lightning-Rock -> Name: Thunderclad
Lightning-Water -> Name: Electrocute
Lightning-Wind -> Name: Tempest

Rock-Water -> Name: Mudslide
Rock-Wind -> Name: Sandstorm
Water-Wind -> Name: Monsoon Surge

