# Combat interaction

The portrait opens Character, the zodiac opens the ability map, and three quick slots select commands. Only the right panel confirms a command. Selection and free assignments do not advance the timeline.

`GameState.Loadout` owns the current player's command definitions, carried items, committed equipment, focus, and shortcuts. The encounter currently authors only Wait, using the existing 0.6-turn pass operation. No attack damage or progression rules have been added. Item actions and additional learned/unlearned nodes render from the loadout's definitions when authored.

A command identifies its operation and optional required item and slot. Definitions supply execution effects, target rules, turn/focus cost, and icon. The default targeted rule preserves the former front-enemy restriction; an authored target rule can override it. Missing equipment and resources preserve assignments while disabling execution. Confirmation rechecks current target state and requirements in gameplay.

Character edits use an independent equipment dictionary. The final difference determines gameplay's equipment cost; reverting all changes costs zero. Confirmation validates ownership/slots and current actor, applies equipment and initiative changes in one timeline action, and closes only on success. Cancel, Escape, and Back discard the working dictionary. Inventory ordering and shortcuts persist independently. The dialog keeps its input scope until the next update after dismissal to prevent the closing event reaching the scene.

Validation: `dotnet run --project tests/CombatTimelineChecks --no-restore` and `dotnet run --project tests/TargetSelectionChecks --no-restore`. These cover resource failures, invalid targets, retained shortcuts, reentrant confirmation, transaction failure, equipment/stat commit, zero-cost reversion, and target-independent commands. Visual layout still requires an in-game check.
