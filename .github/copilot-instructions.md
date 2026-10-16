All classes shall be fully documented. Use <inheritdoc/> as appropriate, but include xaml documentation on all public and private properties and methods. Private fields do not need to be documented.

After making changes, use `dotnet test` to run and verify all tests succeed. We do not need to analyze code coverage for every change.

Following any changes to the plan, update README.md at the root of the workspace to reflect any architectural decisions or major changes. Keep the document succinct, do not elaborate unnecessarily.

Changes that materially affect a combat scenario, including objectives, enemy stats or capabilities, or the player's initial state, require simulating that scenario to ensure it is still viable. Run each player strategy once per progression graph, with all abilities from that graph learned. Omit `--runs` to execute the complete configured matrix (currently 12 graphs x 10 player-supported strategies = 120 runs):
dotnet run --project .\Simulator -- --state .\Simulator\examples\intro.json

Use the affected scenario's state file when validating another encounter. Do not repeat the matrix across additional seeds for routine validation; repeated runs are only needed when explicitly requested to investigate randomness.
