---
name: repo-cleanup
description: >-
  The playbook for tidying FalcoDemo.Api — the exact formatting and documentation
  conventions, the commands that verify a change, the paths that are off limits,
  and the pull request shape to produce.
---

# Repo cleanup playbook

## The commands that decide whether a change is correct

This agent cannot run these itself (it has no `shell` tool — see
`.github/agents/repo-cleanup.md` for why). They are recorded here because they are
what CI runs, and therefore what your proposed diff will be judged against.

```bash
dotnet tool restore
dotnet fantomas src tests --check        # formatting gate
dotnet build FalcoDemo.slnx --configuration Release
dotnet test  FalcoDemo.slnx --configuration Release --no-build
```

A cleanup PR that breaks any of the three is a failed cleanup, no matter how tidy
the diff looks.

## Formatting conventions

Driven by `.editorconfig`. The rules that get violated most often here:

| Rule | Value |
| --- | --- |
| Max line length | 120 |
| Indent | 4 spaces, never tabs |
| Multiline record brackets | On the same line as the first field |
| Trailing whitespace | Never |
| File ends with | A single newline |

## Documentation conventions

- Every public function in `Domain.fs` and `Store.fs` carries a `///` XML doc
  comment describing **what it guarantees**, not how it is implemented.
- Inline `//` comments only where the intent cannot be read off the code. Delete a
  comment that merely restates the next line.
- A commented-out code block with no explanation is dead weight. Remove it.

## F# specifics that trip up automated edits

- **Compile order is significant.** `Domain.fs` must precede `Store.fs`, which must
  precede `Handlers.fs`, which must precede `Program.fs`. Never reorder the
  `<Compile Include=...>` entries in the `.fsproj`.
- An `open` that looks unused may still be bringing an operator or a type extension
  into scope. Remove one only when nothing in the file references anything from it.
- `TreatWarningsAsErrors` is on. An unused binding is a build failure, not a hint.

## Paths you must never touch

```
.github/workflows/**        CI definition — changes belong in their own reviewed PR
.github/agents/**           This agent's own identity
.github/skills/**           This playbook
.github/CODEOWNERS          Review routing
.github/dependabot.yml      Dependency policy
**/*.fsproj                 Package and compile-order changes need a human
FalcoDemo.slnx               Project membership
.config/dotnet-tools.json   Tool pinning
```

If a cleanup appears to require editing one of these, stop and write it up in the
pull request body instead.

## Pull request shape

- **Title:** `[repo-cleanup] <short summary>`
- **Labels:** `automated`, `cleanup`
- **Body:**
  1. A one-line summary of what was tidied.
  2. A table of every file changed and the one-line reason.
  3. A "Noticed but not fixed" section — anything out of scope, with the reason.
  4. The verification commands above, so the reviewer can copy and paste them.

## What happens to your pull request

Nothing special. It goes through the same gate as a human's:

- The branch ruleset requires a passing `Build and test`, `Format check` and
  `Smoke test the API`.
- `CODEOWNERS` auto-requests a named human reviewer.
- Copilot code review runs on it automatically.
- The branch ruleset forbids self-merge, and being a bot grants no bypass.

If your PR cannot merge, that is the system working, not a bug to route around.
