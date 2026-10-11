All classes shall be fully documented. Use <inheritdoc/> as appropriate, but include xaml documentation on all public and private properties and methods. Private fields do not need to be documented.

After making changes, use `dotnet test` to run and verify all tests succeed. We do not need to analyze code coverage for every change.

Following any changes to the plan, update README.md at the root of the workspace to reflect any architectural decisions or major changes. Keep the document succinct, do not elaborate unnecessarily.

Changes that materially affect a particular combat scenario including objectives, enemy character stats or capabilities, or the initial state of the player require simulating the scenario to ensure it is still viable:
dotnet run --project Prelude/Simulator -- --state Prelude/Simulator/examples/intro.json --runs 1000
