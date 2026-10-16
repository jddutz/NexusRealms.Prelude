# Zodiac progression

Each Zodiac has one immutable ProgressionGraph in Prelude/Database/Progression.
The centre is the player's Zodiac image. The four roots point NORTH (up), EAST
(right), SOUTH (down), WEST (left). Each direction groups related ability nodes, with
sign-specific positions and parent connections. A null parent connects to the root.

Directions are layout/grouping metadata and do not gate learning by chapter.
Each chapter contains four sequential ProgressNodes. Each visit
learns one available ability whose parent is already learned, then enables Continue.
Twenty visits lead to the existing alleyway CombatScenario and its outcome routes.
Its existing ch1_p1_Intro ID is retained for saved combat simulator inputs.

Rules.ProgressionRules provides reusable learning rules, GameState tracks completed visits, and
CombatLoadout owns learned abilities. Graph data contains no scene or player state.
GameState.Zodiac currently defaults to Aries and is ready for character creation.
The twenty direction/number abilities are placeholders without combat effects.
Their final names, effects, icons, and Zodiac-specific assignments remain to be authored.

Run: dotnet run --project Tests/ProgressionChecks