All classes shall be fully documented. Use <inheritdoc/> as appropriate, but include xaml documentation on all public and private properties and methods. Private fields do not need to be documented.

After making changes, use `dotnet test` to run and verify all tests succeed. We do not need to analyze code coverage for every change.

Following any changes to the plan, update README.md at the root of the workspace to reflect any architectural decisions or major changes. Keep the document succinct, do not elaborate unnecessarily.

Combat simulation is an on-demand workflow handled by `$nexusrealms-simulate`. Ordinary ability or combat changes do not automatically require a scenario matrix or the simulator acceptance executable; use the usual tests appropriate to the change. Run simulation when it is requested as a separate task.
