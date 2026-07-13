# Balance Sim — headless eon runner

Runs complete 6,500-year worlds of Project Eternity in seconds, with no Unity
required, by compiling the game's sim layer against a minimal `UnityEngine` stub.

```bash
dotnet run   # needs .NET SDK 8+
```

Three seeded worlds run start-to-finish with a fully passive player. Output per world:
era timeline, periodic snapshots (population, science rate, wars, faiths, armies),
the closing chronicle, and sanity checks (NaN guards, "did history happen").

Use it for the fine-tune pass: edit `Assets/Scripts/Core/Tuning.cs`, re-run, watch
how the eons change shape. Add seeds or player scripting in `Program.cs`.

`UnityStub.cs` is compile-check scaffolding only — it never ships with the game.
