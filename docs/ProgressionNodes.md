# Zodiac progression

Each Zodiac has one immutable ProgressionGraph in Prelude/Database/Progression.
The centre is the player's Zodiac image, with sign-specific ability positions and
parent connections. A null parent means the ability has no prerequisite.

The story starts at the authored Chapter1.Part1.Begin ProgressNode. Players may
learn multiple abilities per visit while points and prerequisites permit.
Availability refreshes after every purchase; unavailable, unlearned glyphs fade to
25% opacity without fading frames or backgrounds. Continue is enabled after a
purchase, or when points or unlearned abilities are exhausted, and follows
Begin.NextNode directly to the intro CombatScenario and its outcome routes.
The existing ch1_p1_Intro ID is retained for saved combat simulator inputs.

Rules.ProgressionRules provides reusable learning rules, GameState tracks completed visits, and
CombatLoadout owns learned abilities. Graph data contains no scene or player state.
GameState.Zodiac currently defaults to Aries and is ready for character creation.

Run: dotnet run --project Tests/ProgressionChecks