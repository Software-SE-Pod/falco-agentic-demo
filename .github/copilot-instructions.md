# Copilot instructions — FalcoDemo.Api

## What this repository is

A small F# HTTP API built on [Falco](https://www.falcoframework.com/) over ASP.NET Core.
It serves an in-memory product catalogue. The persistence layer is deliberately trivial —
the substance of this repository is the GitHub platform surface around the code
(rulesets, CODEOWNERS, Actions, Copilot code review, custom agents and automations).

## Build, test and run

```bash
dotnet restore FalcoDemo.slnx
dotnet build FalcoDemo.slnx --configuration Release --no-restore
dotnet test  FalcoDemo.slnx --configuration Release --no-build
dotnet run --project src/FalcoDemo.Api      # listens on http://localhost:5000
```

Formatting is Fantomas, driven by `.editorconfig`:

```bash
dotnet tool restore
dotnet fantomas src tests --check   # verify
dotnet fantomas src tests           # fix
```

## Layout

| Path | Purpose |
| --- | --- |
| `src/FalcoDemo.Api/Domain.fs` | Records and pure validation. No I/O, no ASP.NET types. |
| `src/FalcoDemo.Api/Store.fs` | `ProductStore`, an in-memory `ConcurrentDictionary`. |
| `src/FalcoDemo.Api/Handlers.fs` | Falco `HttpHandler` values. One handler per endpoint. |
| `src/FalcoDemo.Api/Program.fs` | Endpoint table and host wiring. Nothing else. |
| `tests/FalcoDemo.Tests/Tests.fs` | xUnit tests against `Domain` and `Store`. |

F# compilation order is significant. A new file must be added to the `<Compile Include=...>`
list in the `.fsproj` **in dependency order**, not appended blindly.

## Conventions

- Return `Result<'T, ValidationError list>` from validation. Do not throw for expected
  failures, and do not return a bare `bool`.
- Validation accumulates **every** problem and returns them together. Do not short-circuit
  on the first error — the API contract is that a 422 lists all offending fields.
- Handlers stay thin. Business rules belong in `Domain.fs` where they are unit-testable
  without a web host.
- Prefer `Option` over `null`. Prefer pattern matching over `if/else` chains.
- Errors are returned as an object carrying `status`, `title` and `detail`.
- Use the pipeline operator for composition; avoid deeply nested parentheses.
- Public functions carry an XML doc comment (`///`). Inline comments only where the code
  cannot explain itself.

## Testing expectations

- Every new branch in `Domain.fs` needs a test.
- Test names are backtick-quoted sentences describing the behaviour, not the method name.
- Tests must not depend on execution order or on a shared mutable store — construct a
  fresh `ProductStore()` per test.

## Things not to do

- Do not add a database, an ORM, or a DI container. The store is intentionally in-memory.
- Do not introduce `Newtonsoft.Json`; `System.Text.Json` via Falco is what we use.
- Do not edit files under `.github/workflows/` as part of an unrelated change.
- Do not commit secrets, tokens, connection strings or `.env` files.
- Do not disable `TreatWarningsAsErrors`.
