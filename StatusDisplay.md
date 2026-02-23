## Status Display

- The player and the enemy should have a small status display container that shows the current buffs or debuffs on the player and enemy.
- The status display should be a small container that is attached to the player and enemy.
- The status display should be a chip that is displayed in Green for buffs and Red for debuffs. 
- The Chips for buffs and debuffs should be displayed in a row and should be activated only when player or enemy is affected by buffs or debuffs. 
- The status chips (buff/debuff) should expand on pressing the X button to show the name of the buff/debuff and the duration of the buff/debuff.
- The chips inside the container should also have a tolltip that shows the explenation of the buff/debuff in details.
- What are considered buff/debuff:
    - Reaction Effects
        - Overcharge reaction +2 AP -> Buff
        - Reflective Armor -> Buff
        - Stoneguard -> Buff
        - Glacial Focus -> Buff
        - Similar buffs that provide us with some sort of a buff
    - Relic Effects
        - DeadEye Counter -> Provides us with 100% crit on next hit.-> Buff
        - Crippling Weakness -> Debuff
        - Rustbound Sunder -> Debuff
    - Other Status Effects
        - Sundered -> Debuff
        - Weakened -> Debuff
        - Vulnerable -> Debuff
        - Stunned -> Debuff
        - Frozen -> Debuff
        - Shatter -> Debuff


If you are not sure about what is considered a buff or debuff, ask me to create a list of all the possible buffs and debuffs. However our system should be able to handle any buff or debuff that is added to the game from the effects.json file.

Additionally the UI should be able to handle any status effect that is added to the game from the statuses.json file.

The UI looks like this:
We use the chips system we created for creating the Green Buff/ Red Debuff UI displayed on the enemy/player. 
On pressing X the container should appear and show the name of the buff/debuff and the duration of the buff/debuff using the same chip system we created. The difference here is that chip inside the container should now be mouse overed to show the tooltip with the description of the buff/debuff.

The container should be attached to the player and enemy and should be hidden when there are no buffs or debuffs. 
The container position should be in the middle of the body of the palyer/enemy. 

Rules for this prompt:
It should not create fallbacks
It should not create any new files unless explicitly asked to do so.
It should check the methods already present in this system and should not create any duplicate methods.
It should not hardocde data in the code but use the data driven approach to create the UI.
If there is something unclear ask me to explain it. 
Test the code and make sure it works as expected.
Any unicode value not found like "The character with Unicode value \u25C8 was not found in the [LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1 in text object [Text].
UnityEngine.Debug:LogWarning (object,UnityEngine.Object)
TMPro.TextMeshProUGUI:SetArraySizes (TMPro.TMP_Text/TextProcessingElement[]) (at ./Library/PackageCache/com.unity.ugui@7056cb05de4c/Runtime/TMP/TextMeshProUGUI.cs:2010)
TMPro.TMP_Text:ParseInputText () (at ./Library/PackageCache/com.unity.ugui@7056cb05de4c/Runtime/TMP/TMP_Text.cs:2021)
TMPro.TextMeshProUGUI:OnPreRenderCanvas () (at ./Library/PackageCache/com.unity.ugui@7056cb05de4c/Runtime/TMP/TextMeshProUGUI.cs:2491)
TMPro.TextMeshProUGUI:Rebuild (UnityEngine.UI.CanvasUpdate) (at ./Library/PackageCache/com.unity.ugui@7056cb05de4c/Runtime/TMP/TextMeshProUGUI.cs:227)
UnityEngine.Canvas:SendWillRenderCanvases ()" Should be fixed by using the correct font.

